using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using AlibreX;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the owner occurrence retry of <see cref="AlibreGearExporter"/> (<c>WithGeometryOccurrence</c>) and of the candidate lists of
    /// the exporters. The Alibre calls are replaced by delegates and stand-in objects; Alibre itself is not run.
    /// </summary>
    [TestFixture]
    public class TestAlibreGeometryOccurrence
    {
        private const int NullReferenceCode = unchecked((int)0x80004003);
        private const int InvalidArgCode = unchecked((int)0x80070057);

        /// <summary>A stand-in for an Alibre interface: every call returns null, except the ones the handler answers.</summary>
        private sealed class StubProxy : RealProxy
        {
            private readonly Func<string, object> _handler;

            public StubProxy(Type type, Func<string, object> handler = null) : base(type)
            {
                _handler = handler;
            }

            public override IMessage Invoke(IMessage msg)
            {
                var call = (IMethodCallMessage)msg;
                var result = _handler?.Invoke(call.MethodName);
                return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
            }
        }

        private static IADRoot StubRoot()
        {
            return (IADRoot)new StubProxy(typeof(IADRoot)).GetTransparentProxy();
        }

        private static IADOccurrence StubOccurrence()
        {
            return (IADOccurrence)new StubProxy(typeof(IADOccurrence)).GetTransparentProxy();
        }

        private sealed class FeatureStub
        {
            public int Deleted { get; private set; }

            public IADPartFeature Feature { get; }

            public FeatureStub(bool hasError)
            {
                Feature = (IADPartFeature)new StubProxy(typeof(IADPartFeature), name =>
                {
                    if (name == "get_HasError") return hasError;
                    if (name == "Delete") Deleted++;
                    return null;
                }).GetTransparentProxy();
            }
        }

        /// <summary>The base exporter with the protected helper and the candidate list exposed.</summary>
        private sealed class BaseProbe : AlibreGearExporter
        {
            public BaseProbe(IADRoot root) : base(root)
            {
            }

            public List<string> Diagnostics { get; } = new List<string>();

            public IReadOnlyList<IADOccurrence> Candidates => GeometryOccurrenceCandidates;

            public T Run<T>(string what, Func<IADOccurrence, T> call) where T : class
            {
                return WithGeometryOccurrence(what, call);
            }

            public void NewPart()
            {
                ResetGeometryOccurrenceForm();
            }

            protected override void AddDiagnostic(string message)
            {
                Diagnostics.Add(message);
            }
        }

        /// <summary>The in-assembly exporter with the helper and candidates exposed and a part occurrence that can be set.</summary>
        private sealed class PdmProbe : AlibreAssemblyPartExporter
        {
            public PdmProbe(IADRoot root) : base(root)
            {
            }

            public List<string> Diagnostics { get; } = new List<string>();

            public IReadOnlyList<IADOccurrence> Candidates => GeometryOccurrenceCandidates;

            public void SetPartOccurrence(IADOccurrence occurrence)
            {
                typeof(AlibreAssemblyPartExporter).GetField("_currentOccurrence", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, occurrence);
            }

            public T Run<T>(string what, Func<IADOccurrence, T> call) where T : class
            {
                return WithGeometryOccurrence(what, call);
            }

            public void NewPart()
            {
                ResetGeometryOccurrenceForm();
            }

            protected override void AddDiagnostic(string message)
            {
                Diagnostics.Add(message);
            }
        }

        private static PdmProbe PartBuild(out IADOccurrence partOccurrence)
        {
            var probe = new PdmProbe(StubRoot());
            partOccurrence = StubOccurrence();
            probe.SetPartOccurrence(partOccurrence);
            return probe;
        }

        [Test]
        public void BaseExporter_HasOneCandidate_None()
        {
            var probe = new BaseProbe(StubRoot());

            Assert.AreEqual(1, probe.Candidates.Count);
            Assert.IsNull(probe.Candidates[0]);
        }

        [Test]
        public void AssemblyExporter_OutsideAPartBuild_HasOnlyTheNullCandidate()
        {
            var probe = new PdmProbe(StubRoot());

            Assert.AreEqual(1, probe.Candidates.Count);
            Assert.IsNull(probe.Candidates[0]);
        }

        [Test]
        public void AssemblyExporter_DuringAPartBuild_TriesNoneFirstThenThePartOccurrence()
        {
            var probe = PartBuild(out var partOccurrence);

            Assert.AreEqual(2, probe.Candidates.Count);
            Assert.IsNull(probe.Candidates[0], "The form without an owner occurrence comes first.");
            Assert.AreSame(partOccurrence, probe.Candidates[1]);
        }

        [Test]
        public void SingleCandidate_CallsOnceAndPassesTheOriginalExceptionOn()
        {
            var probe = new BaseProbe(StubRoot());
            var calls = 0;

            var ex = Assert.Throws<COMException>(() => probe.Run<IADPartFeature>("the pattern", occurrence =>
            {
                calls++;
                throw new COMException("boom", NullReferenceCode);
            }));

            Assert.AreEqual(1, calls);
            Assert.AreEqual(NullReferenceCode, ex.ErrorCode);
            Assert.IsEmpty(probe.Diagnostics, "The Windows route logs nothing extra.");
        }

        [Test]
        public void SingleCandidate_ReturnsAFeatureWithAnErrorUnchanged()
        {
            var probe = new BaseProbe(StubRoot());
            var bad = new FeatureStub(true);

            var result = probe.Run("the pattern", occurrence => bad.Feature);

            Assert.AreSame(bad.Feature, result);
            Assert.AreEqual(0, bad.Deleted, "The builder's own check handles it, as before.");
        }

        [Test]
        public void SingleCandidate_ReturnsNullUnchanged()
        {
            var probe = new BaseProbe(StubRoot());

            Assert.IsNull(probe.Run<IADPartFeature>("the pattern", occurrence => null));
        }

        [Test]
        public void FirstFormThrows_SecondSucceeds_ReturnsTheSecondResult()
        {
            var probe = PartBuild(out var partOccurrence);
            var good = new FeatureStub(false);
            var passed = new List<IADOccurrence>();

            var result = probe.Run("the circular pattern of the tooth", occurrence =>
            {
                passed.Add(occurrence);
                if (occurrence == null) throw new COMException("Object reference not set to an instance of an object", NullReferenceCode);
                return good.Feature;
            });

            Assert.AreSame(good.Feature, result);
            Assert.AreEqual(2, passed.Count);
            Assert.IsNull(passed[0]);
            Assert.AreSame(partOccurrence, passed[1]);
            Assert.IsTrue(probe.Diagnostics.Exists(line => line.StartsWith("geometry occurrence for the circular pattern of the tooth: part occurrence")),
                string.Join("\n", probe.Diagnostics));
            Assert.IsTrue(probe.Diagnostics.Exists(line => line.Contains("0x80004003")), "The failed first form is logged with its HRESULT.");
        }

        [Test]
        public void FirstFormWorks_ReturnsItAndLogsNone()
        {
            var probe = PartBuild(out _);
            var good = new FeatureStub(false);
            var passed = new List<IADOccurrence>();

            var result = probe.Run("the pattern", occurrence =>
            {
                passed.Add(occurrence);
                return good.Feature;
            });

            Assert.AreSame(good.Feature, result);
            Assert.AreEqual(1, passed.Count);
            Assert.IsNull(passed[0]);
            CollectionAssert.AreEqual(new[] { "geometry occurrence for the pattern: none" }, probe.Diagnostics);
        }

        [Test]
        public void TheWorkingFormIsTriedFirstForLaterCallsOfTheSamePart_AndForgottenForTheNextPart()
        {
            var probe = PartBuild(out var partOccurrence);
            var good = new FeatureStub(false);

            probe.Run("first", occurrence =>
            {
                if (occurrence == null) throw new COMException("rejected", NullReferenceCode);
                return good.Feature;
            });

            var passed = new List<IADOccurrence>();
            probe.Run("second", occurrence =>
            {
                passed.Add(occurrence);
                return good.Feature;
            });

            Assert.AreEqual(1, passed.Count, "The form that worked is not preceded by the one that failed.");
            Assert.AreSame(partOccurrence, passed[0]);

            probe.NewPart();
            passed.Clear();
            probe.Run("third", occurrence =>
            {
                passed.Add(occurrence);
                return good.Feature;
            });

            Assert.AreEqual(1, passed.Count);
            Assert.IsNull(passed[0], "A new part is probed again, none first.");
        }

        [Test]
        public void BothFormsFail_TheMessageNamesBothAttemptsWithHresultAndMessage()
        {
            var probe = PartBuild(out _);
            var firstError = new COMException("first message", NullReferenceCode);

            var ex = Assert.Catch<InvalidOperationException>(() => probe.Run<IADPartFeature>("the circular pattern of the tooth", occurrence =>
            {
                if (occurrence == null) throw firstError;
                throw new COMException("second message", InvalidArgCode);
            }));

            StringAssert.Contains("the circular pattern of the tooth", ex.Message);
            StringAssert.Contains("none: COMException 0x80004003: first message", ex.Message);
            StringAssert.Contains("part occurrence: COMException 0x80070057: second message", ex.Message);
            Assert.AreSame(firstError, ex.InnerException, "The first exception is kept as the inner exception.");
        }

        [Test]
        public void AFeatureWithAnError_IsDeleted_AndTheNextFormIsTried()
        {
            var probe = PartBuild(out var partOccurrence);
            var bad = new FeatureStub(true);
            var good = new FeatureStub(false);

            var result = probe.Run("the pattern", occurrence => occurrence == null ? bad.Feature : good.Feature);

            Assert.AreSame(good.Feature, result);
            Assert.AreEqual(1, bad.Deleted, "The feature with an error is deleted quietly.");
            Assert.AreEqual(0, good.Deleted);
            Assert.AreSame(partOccurrence, probe.Candidates[1]);
        }

        [Test]
        public void ANullResult_IsTreatedAsAFailure_AndBothFormsFailingIsReported()
        {
            var probe = PartBuild(out _);
            var bad = new FeatureStub(true);

            var ex = Assert.Catch<InvalidOperationException>(() => probe.Run("the pattern", occurrence => occurrence == null ? null : bad.Feature));

            StringAssert.Contains("none: returned nothing", ex.Message);
            StringAssert.Contains("part occurrence: the feature has an error in Alibre", ex.Message);
            Assert.AreEqual(1, bad.Deleted);
            Assert.IsNull(ex.InnerException, "No form threw, so there is no inner exception.");
        }

        /// <summary>
        /// A stand-in for any Alibre interface: every call returns a stand-in of the interface it returns (true for IsClosed, false for
        /// other booleans, null or the default otherwise), except where the handler answers or throws.
        /// </summary>
        private sealed class DeepStub : RealProxy
        {
            private readonly Func<IMethodCallMessage, object> _handler;

            public DeepStub(Type type, Func<IMethodCallMessage, object> handler = null) : base(type)
            {
                _handler = handler;
            }

            public override IMessage Invoke(IMessage msg)
            {
                var call = (IMethodCallMessage)msg;
                try
                {
                    var result = _handler?.Invoke(call);
                    if (result == null)
                    {
                        var returnType = ((MethodInfo)call.MethodBase).ReturnType;
                        if (returnType == typeof(bool)) result = call.MethodName == "get_IsClosed";
                        else if (returnType.IsInterface) result = new DeepStub(returnType, _handler).GetTransparentProxy();
                        else if (returnType.IsValueType && returnType != typeof(void)) result = Activator.CreateInstance(returnType);
                    }

                    var parameters = ((MethodInfo)call.MethodBase).GetParameters();
                    var outArgs = new List<object>();
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].ParameterType.IsByRef) outArgs.Add(call.GetArg(i));
                    }

                    return new ReturnMessage(result, outArgs.ToArray(), outArgs.Count, call.LogicalCallContext, call);
                }
                catch (Exception ex)
                {
                    return new ReturnMessage(ex, call);
                }
            }
        }

        /// <summary>An exporter whose parts are stand-ins and whose pattern call is rejected, as seen in the PDM run.</summary>
        private sealed class PatternFailureExporter : AlibreGearExporter
        {
            private readonly IReadOnlyList<IADOccurrence> _candidates;
            private readonly IADPartSession _session;

            public PatternFailureExporter(IReadOnlyList<IADOccurrence> candidates)
                : base((IADRoot)new DeepStub(typeof(IADRoot)).GetTransparentProxy())
            {
                _candidates = candidates;
                _session = (IADPartSession)new DeepStub(typeof(IADPartSession), call =>
                {
                    if (call.MethodName != "AddCircularFeaturePattern") return null;
                    if (call.GetArg(3) == null) throw new COMException("Object reference not set to an instance of an object", NullReferenceCode);
                    throw new COMException("bad argument", InvalidArgCode);
                }).GetTransparentProxy();
            }

            protected override IReadOnlyList<IADOccurrence> GeometryOccurrenceCandidates => _candidates;

            protected override IADPartSession CreatePart(string name)
            {
                return _session;
            }
        }

        private static InvoluteGearComponent SpurGear()
        {
            return new InvoluteGearComponent("Gear 1") { Teeth = 20, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 10.0 };
        }

        [Test]
        public void TheTeethPatternRejectedByEveryForm_IsReportedWithTheStageTheGearAndBothAttempts()
        {
            var exporter = new PatternFailureExporter(new[] { null, StubOccurrence() });

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(SpurGear()));

            StringAssert.StartsWith("Alibre export of gear 'Gear 1' failed while patterning the tooth: ", ex.Message);
            StringAssert.Contains("none: COMException 0x80004003: Object reference not set to an instance of an object", ex.Message);
            StringAssert.Contains("part occurrence: COMException 0x80070057: bad argument", ex.Message);
            Assert.IsInstanceOf<COMException>(ex.InnerException.InnerException, "The first Alibre exception is kept at the end of the chain.");
        }

        [Test]
        public void TheTeethPatternWithASingleCandidate_KeepsTheOriginalStageMessage()
        {
            var exporter = new PatternFailureExporter(new IADOccurrence[] { null });

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(SpurGear()));

            StringAssert.StartsWith("Alibre export of gear 'Gear 1' failed while patterning the tooth: Object reference not set to an instance of an object", ex.Message);
            StringAssert.DoesNotContain("owner occurrence form", ex.Message);
            Assert.IsInstanceOf<COMException>(ex.InnerException);
        }

        /// <summary>An exporter whose part is a <see cref="DeepStub"/> session answered by the given handler.</summary>
        private sealed class ScriptedExporter : AlibreGearExporter
        {
            private readonly IReadOnlyList<IADOccurrence> _candidates;
            private readonly IADPartSession _session;

            public ScriptedExporter(IReadOnlyList<IADOccurrence> candidates, Func<IMethodCallMessage, object> handler)
                : base((IADRoot)new DeepStub(typeof(IADRoot)).GetTransparentProxy())
            {
                _candidates = candidates;
                _session = (IADPartSession)new DeepStub(typeof(IADPartSession), handler).GetTransparentProxy();
            }

            protected override IReadOnlyList<IADOccurrence> GeometryOccurrenceCandidates => _candidates;

            protected override IADPartSession CreatePart(string name)
            {
                return _session;
            }
        }

        /// <summary>A stand-in for a point or vector: X, Y and Z answer the given values.</summary>
        private static object Coordinates(Type type, double x, double y, double z)
        {
            return new StubProxy(type, name => name == "get_X" ? x : name == "get_Y" ? y : name == "get_Z" ? z : (object)null).GetTransparentProxy();
        }

        /// <summary>A design plane that reports a point at the given X and counts how often it is deleted.</summary>
        private sealed class PlaneStub : RealProxy
        {
            private readonly double _pointX;

            public int Deleted { get; private set; }

            public IADDesignPlane Plane => (IADDesignPlane)GetTransparentProxy();

            public PlaneStub(double pointX) : base(typeof(IADDesignPlane))
            {
                _pointX = pointX;
            }

            public override IMessage Invoke(IMessage msg)
            {
                var call = (IMethodCallMessage)msg;
                var method = (MethodInfo)call.MethodBase;
                switch (call.MethodName)
                {
                    case "GetGeometry":
                    {
                        var parameters = method.GetParameters();
                        var outArgs = new object[parameters.Length];
                        outArgs[0] = Coordinates(parameters[0].ParameterType.GetElementType(), _pointX, 0.0, 0.0);
                        return new ReturnMessage(null, outArgs, outArgs.Length, call.LogicalCallContext, call);
                    }
                    case "get_Normal":
                        return new ReturnMessage(Coordinates(method.ReturnType, 1.0, 0.0, 0.0), null, 0, call.LogicalCallContext, call);
                    case "Delete":
                        Deleted++;
                        break;
                }

                var result = method.ReturnType.IsValueType && method.ReturnType != typeof(void) ? Activator.CreateInstance(method.ReturnType) : null;
                return new ReturnMessage(result, null, 0, call.LogicalCallContext, call);
            }
        }

        [Test]
        public void TheRackLinearPattern_TriesBothFormsInEachOfTheTwoPatternGeometryAttempts_AndReportsThem()
        {
            var partOccurrence = StubOccurrence();
            var occurrences = new List<IADOccurrence>();
            var geometry = new List<bool>();
            var exporter = new ScriptedExporter(new[] { null, partOccurrence }, call =>
            {
                if (call.MethodName != "AddLinearFeaturePattern") return null;
                occurrences.Add((IADOccurrence)call.GetArg(3));
                geometry.Add((bool)call.GetArg(11));
                throw new COMException("pattern rejected", InvalidArgCode);
            });
            var rack = new RackComponent("R1") { Teeth = 12, Module = 2.0, PressureAngleDeg = 20.0, FaceWidth = 15.0 };

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(rack));

            Assert.AreEqual(4, occurrences.Count, "Two pattern geometry attempts, each with both occurrence forms.");
            Assert.IsNull(occurrences[0]);
            Assert.AreSame(partOccurrence, occurrences[1]);
            Assert.IsNull(occurrences[2]);
            Assert.AreSame(partOccurrence, occurrences[3]);
            CollectionAssert.AreEqual(new[] { exporter.PatternGeometry, exporter.PatternGeometry, !exporter.PatternGeometry, !exporter.PatternGeometry }, geometry);
            StringAssert.Contains("the linear pattern of 12 teeth along", ex.Message);
            StringAssert.Contains("was not accepted", ex.Message);
            StringAssert.Contains("attempt 1:", ex.Message);
            StringAssert.Contains("attempt 2:", ex.Message);
            StringAssert.Contains("none: COMException 0x80070057: pattern rejected", ex.Message);
            StringAssert.Contains("part occurrence: COMException 0x80070057: pattern rejected", ex.Message);
        }

        [Test]
        public void TheBevelOffsetPlaneOnTheWrongSide_IsDeleted_AndTheNextOccurrenceFormIsUsed()
        {
            var gear = new BevelGearComponent("b1")
            {
                Teeth = 18, MateTeeth = 45, Module = 10.16, PressureAngleDeg = 20.0, FaceWidth = 63.5, ShaftAngleDeg = 90.0, HasHub = false
            };
            var apexX = BevelGearSketch.Build(gear).BackConeApexX;
            var partOccurrence = StubOccurrence();
            var wrongPlane = new PlaneStub(apexX + 5.0);
            var rightPlane = new PlaneStub(apexX);
            var offered = new List<IADOccurrence>();
            var exporter = new ScriptedExporter(new[] { null, partOccurrence }, call =>
            {
                if (call.MethodName == "CreateAtOffsetToPlane")
                {
                    var occurrence = (IADOccurrence)call.GetArg(0);
                    offered.Add(occurrence);
                    return occurrence == null ? wrongPlane.Plane : rightPlane.Plane;
                }

                if (call.MethodName == "CreateBy2Planes") throw new COMException("axis rejected", InvalidArgCode);
                return null;
            });

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(gear));

            Assert.AreEqual(2, wrongPlane.Deleted, "Both signs gave a plane at the wrong X, so both are deleted.");
            Assert.AreEqual(0, rightPlane.Deleted, "The accepted plane is kept.");
            Assert.AreEqual(3, offered.Count);
            Assert.IsNull(offered[0]);
            Assert.IsNull(offered[1]);
            Assert.AreSame(partOccurrence, offered[2], "The part occurrence form is tried after the first form failed its plane check.");
            StringAssert.StartsWith("Alibre export of bevel gear 'b1' failed while adding an axis where the offset plane meets the ", ex.Message);
            StringAssert.Contains("axis rejected", ex.Message, "The next call, which got the accepted plane, is the one that failed.");
        }

        [Test]
        public void TheBevelBlankRevolveRejectedByEveryForm_KeepsTheStageAndTheGearEvenWhenTheSketchIsNotClosed()
        {
            var gear = new BevelGearComponent("b1")
            {
                Teeth = 18, MateTeeth = 45, Module = 10.16, PressureAngleDeg = 20.0, FaceWidth = 63.5, ShaftAngleDeg = 90.0, HasHub = false
            };
            var exporter = new ScriptedExporter(new[] { null, StubOccurrence() }, call =>
            {
                if (call.MethodName == "get_IsClosed") return false;
                if (call.MethodName == "AddRevolvedBoss") throw new COMException("revolve rejected", InvalidArgCode);
                return null;
            });

            var ex = Assert.Throws<InvalidOperationException>(() => exporter.Export(gear));

            StringAssert.StartsWith("Alibre export of bevel gear 'b1' failed while revolving the gear blank about the ", ex.Message);
            StringAssert.Contains("none: COMException 0x80070057: revolve rejected", ex.Message);
            StringAssert.Contains("part occurrence: COMException 0x80070057: revolve rejected", ex.Message);
        }
    }
}

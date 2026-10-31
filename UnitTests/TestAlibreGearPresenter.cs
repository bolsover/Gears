using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class TestAlibreGearPresenter
    {
        private class FakeAlibreGearView : IAlibreGearView
        {
            public int Teeth { get; set; } = 20;
            public double Module { get; set; } = 2.0;
            public double PressureAngle { get; set; } = 20.0;
            public double HelixAngle { get; set; } = 0.0;
            public string HelicalDirection { get; set; } = "R";
            public double Clearance { get; set; } = 0.5;
            public double Backlash { get; set; } = 0.0;
            public double FaceWidth { get; set; } = 15.0;
            public double ProfileShift { get; set; } = 0.0;
            public bool OptimalShift { get; set; } = false;
            public bool IsInternal { get; set; } = false;
            public double RimThickness { get; set; } = 10.0;
            public bool ShowFullGear { get; set; } = true;
            public bool ShowReferenceCircles { get; set; } = true;

            public bool RimThicknessEnabled { get; set; }
            public string TableTitle { get; set; }
            public string AddMeshedGearMenuText { get; set; }
            public bool RemoveMeshedGearMenuEnabled { get; set; }

            public int LastProgressPercentage { get; private set; }
            public string LastProgressMessage { get; private set; }
            public string LastErrorMessage { get; private set; }
            public CalculatedGearData LastCalcData { get; private set; }
            public CalculatedGearPairData LastPairData { get; private set; }
            public List<GearParameterRow> LastParameterRows { get; private set; } = new();

            public List<List<GearPoint>> LastPolylines { get; private set; }
            public GearMesh3DBuilder.Mesh3D LastMesh { get; private set; }
            public bool LastHasSecondGear { get; private set; }
            public PreviewViewMode LastViewMode { get; private set; } = PreviewViewMode.Mode2D;
            public Render3DMode LastMode3D { get; private set; } = Render3DMode.Solid;
            public bool ResetViewCalled { get; private set; }
            public string LastShownMessage { get; private set; }

            public bool DialogResultToReturn { get; set; } = true;
            public MeshedGearParameters DialogParamsToReturn { get; set; }
            public string SaveFilePathToReturn { get; set; }

            public event EventHandler CalculateRequested;
            public event EventHandler ModuleChanged;
            public event EventHandler InternalChanged;
            public event EventHandler AddOrEditMeshedGearRequested;
            public event EventHandler RemoveMeshedGearRequested;
            public event EventHandler<ExportFormat> ExportRequested;
            public event EventHandler<VisualisationModeEventArgs> VisualisationModeChanged;
            public event EventHandler ReferenceCirclesChanged;
            public event EventHandler ResetViewRequested;

            public void FireCalculate() => CalculateRequested?.Invoke(this, EventArgs.Empty);
            public void FireModuleChanged() => ModuleChanged?.Invoke(this, EventArgs.Empty);
            public void FireInternalChanged() => InternalChanged?.Invoke(this, EventArgs.Empty);
            public void FireAddOrEditMeshed() => AddOrEditMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            public void FireRemoveMeshed() => RemoveMeshedGearRequested?.Invoke(this, EventArgs.Empty);
            public void FireExport(ExportFormat format) => ExportRequested?.Invoke(this, format);
            public void FireVisualisationMode(PreviewViewMode mode, Render3DMode style = Render3DMode.Solid) =>
                VisualisationModeChanged?.Invoke(this, new VisualisationModeEventArgs(mode, style));
            public void FireReferenceCirclesChanged() => ReferenceCirclesChanged?.Invoke(this, EventArgs.Empty);
            public void FireResetView() => ResetViewRequested?.Invoke(this, EventArgs.Empty);

            public void UpdateProgress(int percentage, string message)
            {
                LastProgressPercentage = percentage;
                LastProgressMessage = message;
            }

            public void SetErrorMessage(string message)
            {
                LastErrorMessage = message;
            }

            public void UpdateResultLabels(CalculatedGearData calcData)
            {
                LastCalcData = calcData;
            }

            public void UpdateResultLabelsForPair(CalculatedGearPairData pairData)
            {
                LastPairData = pairData;
            }

            public void ClearResultLabels()
            {
                LastCalcData = null;
                LastPairData = null;
            }

            public void PopulateParameterTable(IEnumerable<GearParameterRow> parameterRows)
            {
                LastParameterRows = parameterRows?.ToList() ?? new List<GearParameterRow>();
            }

            public void UpdatePreviewSingle(
                List<List<GearPoint>> polylines,
                GearMesh3DBuilder.Mesh3D mesh,
                double pitchRadius,
                double baseRadius,
                double tipRadius,
                double rootRadius)
            {
                LastPolylines = polylines;
                LastMesh = mesh;
                LastHasSecondGear = false;
            }

            public void UpdatePreviewPair(
                List<List<GearPoint>> polylines,
                GearMesh3DBuilder.Mesh3D mesh,
                double g1Pitch,
                double g1Base,
                double g1Tip,
                double g1Root,
                double g2CenterDist,
                double g2Pitch,
                double g2Base,
                double g2Tip,
                double g2Root)
            {
                LastPolylines = polylines;
                LastMesh = mesh;
                LastHasSecondGear = true;
            }

            public void ClearPreview()
            {
                LastPolylines = null;
                LastMesh = null;
                LastHasSecondGear = false;
            }

            public void SetVisualisationMode(PreviewViewMode viewMode, Render3DMode mode3D = Render3DMode.Solid)
            {
                LastViewMode = viewMode;
                LastMode3D = mode3D;
            }

            public void ResetView()
            {
                ResetViewCalled = true;
            }

            public bool ShowAddMeshedGearDialog(ref MeshedGearParameters meshedParams, double sharedModule, double sharedPressureAngle)
            {
                if (DialogResultToReturn && DialogParamsToReturn != null)
                {
                    meshedParams = DialogParamsToReturn;
                    return true;
                }
                return DialogResultToReturn;
            }

            public string ShowSaveFileDialog(string filter, string defaultFileName)
            {
                return SaveFilePathToReturn;
            }

            public void ShowMessage(string message, string title, MessageBoxIcon icon)
            {
                LastShownMessage = message;
            }
        }

        [Test]
        public void TestPresenter_Initialization_CalculatesSingleGearSuccessfully()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 30,
                Module = 2.5,
                PressureAngle = 20.0
            };

            var presenter = new AlibreGearPresenter(fakeView);

            Assert.IsFalse(presenter.HasMeshedGear);
            Assert.AreEqual(100, fakeView.LastProgressPercentage);
            Assert.IsNotNull(fakeView.LastCalcData);
            Assert.AreEqual(75.0, fakeView.LastCalcData.PitchDiameter, 1e-4);
            Assert.IsNotNull(fakeView.LastPolylines);
            Assert.Greater(fakeView.LastPolylines.Count, 0);
            Assert.IsNotNull(fakeView.LastMesh);
            Assert.Greater(fakeView.LastMesh.Vertices.Count, 0);
            Assert.Greater(fakeView.LastMesh.TriangleIndices.Count, 0);
            Assert.Greater(fakeView.LastParameterRows.Count, 0);
            Assert.IsFalse(fakeView.LastHasSecondGear);
        }

        [Test]
        public void TestPresenter_ModuleChanged_UpdatesClearanceAndRecalculates()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 20,
                Module = 4.0
            };

            var presenter = new AlibreGearPresenter(fakeView);
            fakeView.Module = 4.0;
            fakeView.FireModuleChanged();

            Assert.AreEqual(1.0, fakeView.Clearance, 1e-4); // 0.25 * 4.0 = 1.0
            Assert.AreEqual(80.0, fakeView.LastCalcData.PitchDiameter, 1e-4);
        }

        [Test]
        public void TestPresenter_InternalChanged_UpdatesRimThicknessAndRecalculates()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 40,
                Module = 2.0,
                IsInternal = true,
                RimThickness = 12.0
            };

            var presenter = new AlibreGearPresenter(fakeView);
            fakeView.FireInternalChanged();

            Assert.IsTrue(fakeView.RimThicknessEnabled);
            Assert.AreEqual(100, fakeView.LastProgressPercentage);
            Assert.IsNotNull(fakeView.LastCalcData);
            Assert.IsTrue(fakeView.LastCalcData.IsInternal);

            // Verify outer rim circle is present in 2D polylines
            Assert.IsNotNull(presenter.CurrentPolylines);
            Assert.AreEqual(40 * 6, presenter.CurrentPolylines.Count);

            var expectedRimRadius = fakeView.LastCalcData.RootDiameter / 2.0 + 12.0;
            var maxRadius = 0.0;
            foreach (var polyline in presenter.CurrentPolylines)
            {
                foreach (var pt in polyline)
                {
                    var r = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
                    if (r > maxRadius) maxRadius = r;
                }
            }

            Assert.AreEqual(expectedRimRadius, maxRadius, 1e-4, "Maximum polyline radius must match internal gear outer rim radius");
        }

        [Test]
        public void TestPresenter_InternalGearSingleTooth_IncludesOuterRimArc()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 30,
                Module = 2.0,
                IsInternal = true,
                RimThickness = 8.0,
                ShowFullGear = false
            };

            var presenter = new AlibreGearPresenter(fakeView);

            Assert.IsNotNull(presenter.CurrentPolylines);
            Assert.AreEqual(6, presenter.CurrentPolylines.Count, "Single internal tooth outline must have 6 segments (5 inner + 1 outer rim arc)");

            var expectedRimRadius = fakeView.LastCalcData.RootDiameter / 2.0 + 8.0;
            var outerRimSegment = presenter.CurrentPolylines[5];
            Assert.Greater(outerRimSegment.Count, 1);

            foreach (var pt in outerRimSegment)
            {
                var r = Math.Sqrt(pt.X * pt.X + pt.Y * pt.Y);
                Assert.AreEqual(expectedRimRadius, r, 1e-4, "Single tooth outer rim arc radius must match rootRadius + rimThickness");
            }
        }

        [Test]
        public void TestPresenter_AddAndRemoveMeshedGear_Workflow()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 20,
                Module = 2.0,
                PressureAngle = 20.0,
                DialogResultToReturn = true,
                DialogParamsToReturn = new MeshedGearParameters
                {
                    Teeth = 40,
                    ProfileShift = 0.2,
                    FaceWidth = 15.0,
                    IsInternal = false,
                    Clearance = 0.5,
                    Backlash = 0.1,
                    HelixAngle = 15.0,
                    HelicalDirection = "L"
                }
            };

            var presenter = new AlibreGearPresenter(fakeView);

            // Add Meshed Gear
            fakeView.FireAddOrEditMeshed();

            Assert.IsTrue(presenter.HasMeshedGear);
            Assert.AreEqual("Edit Meshed Gear…", fakeView.AddMeshedGearMenuText);
            Assert.IsTrue(fakeView.RemoveMeshedGearMenuEnabled);
            Assert.IsTrue(fakeView.LastHasSecondGear);
            Assert.IsNotNull(fakeView.LastPairData);
            Assert.AreEqual(20, fakeView.LastPairData.Gear1.Teeth);
            Assert.AreEqual(40, fakeView.LastPairData.Gear2.Teeth);

            // Remove Meshed Gear
            fakeView.FireRemoveMeshed();

            Assert.IsFalse(presenter.HasMeshedGear);
            Assert.AreEqual("Add Meshed Gear…", fakeView.AddMeshedGearMenuText);
            Assert.IsFalse(fakeView.RemoveMeshedGearMenuEnabled);
            Assert.IsFalse(fakeView.LastHasSecondGear);
            Assert.IsNotNull(fakeView.LastCalcData);
        }

        [Test]
        public void TestPresenter_AddMeshedInternalGear_CalculatesAndUpdatesPreview()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 21, // Odd tooth count
                Module = 2.0,
                PressureAngle = 20.0,
                DialogResultToReturn = true,
                DialogParamsToReturn = new MeshedGearParameters
                {
                    Teeth = 60, // Even tooth count -> (21 - 60) odd parity
                    ProfileShift = 0.0,
                    FaceWidth = 15.0,
                    IsInternal = true,
                    RimThickness = 10.0,
                    Clearance = 0.5,
                    Backlash = 0.0,
                    HelixAngle = 0.0,
                    HelicalDirection = "R"
                }
            };

            var presenter = new AlibreGearPresenter(fakeView);
            fakeView.FireAddOrEditMeshed();

            Assert.IsTrue(presenter.HasMeshedGear);
            Assert.IsTrue(fakeView.LastHasSecondGear);
            Assert.IsNotNull(fakeView.LastPairData);
            Assert.AreEqual(21, fakeView.LastPairData.Gear1.Teeth);
            Assert.AreEqual(60, fakeView.LastPairData.Gear2.Teeth);
            Assert.IsNotNull(presenter.CurrentPolylines);
            Assert.Greater(presenter.CurrentPolylines.Count, 0);
            Assert.IsNotNull(presenter.CurrentMesh);
            Assert.Greater(presenter.CurrentMesh.Vertices.Count, 0);
        }

        [Test]
        public void TestPresenter_SetVisualisationMode()
        {
            var fakeView = new FakeAlibreGearView();
            var presenter = new AlibreGearPresenter(fakeView);

            fakeView.FireVisualisationMode(PreviewViewMode.Mode3D, Render3DMode.Wireframe);
            Assert.AreEqual(PreviewViewMode.Mode3D, fakeView.LastViewMode);
            Assert.AreEqual(Render3DMode.Wireframe, fakeView.LastMode3D);

            fakeView.FireReferenceCirclesChanged();

            fakeView.FireResetView();
            Assert.IsTrue(fakeView.ResetViewCalled);
        }

        [Test]
        public void TestPresenter_ExportDxfAndCsv_GeneratesValidContent()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 18,
                Module = 2.0
            };

            var presenter = new AlibreGearPresenter(fakeView);

            var dxf = AlibreGearPresenter.BuildDxf(presenter.CurrentPolylines);
            Assert.IsNotEmpty(dxf);
            Assert.IsTrue(dxf.Contains("SECTION"));
            Assert.IsTrue(dxf.Contains("LWPOLYLINE"));
            Assert.IsTrue(dxf.Contains("EOF"));

            var csv = AlibreGearPresenter.BuildCsv(presenter.CurrentPolylines);
            Assert.IsNotEmpty(csv);
            Assert.IsTrue(csv.StartsWith("segment,point,x,y"));

            // Test export action with fake save path
            var tempFile = Path.Combine(Path.GetTempPath(), "test_gear_export.dxf");
            try
            {
                fakeView.SaveFilePathToReturn = tempFile;
                fakeView.FireExport(ExportFormat.Dxf);
                Assert.IsTrue(File.Exists(tempFile));
                Assert.Greater(new FileInfo(tempFile).Length, 0);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Test]
        public void TestPresenter_ErrorHandling_ClearsPreviewAndReportsError()
        {
            var fakeView = new FakeAlibreGearView
            {
                Teeth = 10,
                Module = 1.0,
                ProfileShift = -50.0 // Extreme negative profile shift triggers baseRadius > pitchRadius exception
            };

            var presenter = new AlibreGearPresenter(fakeView);

            Assert.AreEqual(0, fakeView.LastProgressPercentage);
            Assert.IsNotEmpty(fakeView.LastErrorMessage);
            Assert.IsNull(fakeView.LastPolylines);
        }

        [Test]
        public void TestAlibreGearPreviewControl_RuntimeProperties_AreHiddenFromDesigner()
        {
            var type = typeof(AlibreGearPreviewControl);
            var propertiesToCheck = new[]
            {
                nameof(AlibreGearPreviewControl.Polylines),
                nameof(AlibreGearPreviewControl.Mesh),
                nameof(AlibreGearPreviewControl.PitchRadius),
                nameof(AlibreGearPreviewControl.BaseRadius),
                nameof(AlibreGearPreviewControl.AddendumRadius),
                nameof(AlibreGearPreviewControl.DedendumRadius),
                nameof(AlibreGearPreviewControl.HasSecondGear),
                nameof(AlibreGearPreviewControl.Gear2CenterDistance),
                nameof(AlibreGearPreviewControl.Gear2PitchRadius),
                nameof(AlibreGearPreviewControl.Gear2BaseRadius),
                nameof(AlibreGearPreviewControl.Gear2AddendumRadius),
                nameof(AlibreGearPreviewControl.Gear2DedendumRadius)
            };

            foreach (var propName in propertiesToCheck)
            {
                var prop = type.GetProperty(propName);
                Assert.IsNotNull(prop, $"Property {propName} should exist.");
                var browsable = prop.GetCustomAttributes(typeof(System.ComponentModel.BrowsableAttribute), false)
                    .Cast<System.ComponentModel.BrowsableAttribute>().FirstOrDefault();
                var designerSerialization = prop.GetCustomAttributes(typeof(System.ComponentModel.DesignerSerializationVisibilityAttribute), false)
                    .Cast<System.ComponentModel.DesignerSerializationVisibilityAttribute>().FirstOrDefault();

                Assert.IsNotNull(designerSerialization, $"Property {propName} should have DesignerSerializationVisibilityAttribute.");
                Assert.AreEqual(System.ComponentModel.DesignerSerializationVisibility.Hidden, designerSerialization.Visibility);
                Assert.IsNotNull(browsable, $"Property {propName} should have BrowsableAttribute.");
                Assert.IsFalse(browsable.Browsable);
            }
        }

        [Test]
        public void TestAlibreGearForm_ResxReader_LoadsWithoutGenericTypeExceptions()
        {
            var resxCandidatePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "GearsUI", "Gui", "Views", "AlibreGearForm.resx"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "GearsUI", "Gui", "Views", "AlibreGearForm.resx"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "GearsUI", "Gui", "Views", "AlibreGearForm.resx")
            };

            var resx = resxCandidatePaths.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
            if (resx != null)
            {
                using var reader = new System.Resources.ResXResourceReader(resx);
                Assert.DoesNotThrow(() =>
                {
                    var dict = reader.GetEnumerator();
                    while (dict.MoveNext())
                    {
                        Assert.IsNotNull(dict.Key);
                    }
                });
            }
        }

        [Test]
        public void TestAlibreGearForm_DesignerFile_IsLinkedWithDependentUpon()
        {
            var csprojCandidatePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "GearsUI", "GearsUI.csproj"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "GearsUI", "GearsUI.csproj"),
                Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "GearsUI", "GearsUI.csproj")
            };

            var csprojPath = csprojCandidatePaths.Select(Path.GetFullPath).FirstOrDefault(File.Exists);
            Assert.IsNotNull(csprojPath, "GearsUI.csproj should exist.");

            var doc = new System.Xml.XmlDocument();
            doc.Load(csprojPath);

            var nsManager = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("ms", "http://schemas.microsoft.com/developer/msbuild/2003");

            var compileNode = doc.SelectSingleNode("//ms:Compile[@Include='Gui\\Views\\AlibreGearForm.Designer.cs']", nsManager)
                              ?? doc.SelectSingleNode("//Compile[@Include='Gui\\Views\\AlibreGearForm.Designer.cs']");

            Assert.IsNotNull(compileNode, "Compile item for AlibreGearForm.Designer.cs must exist in GearsUI.csproj.");

            var dependentUponNode = compileNode.SelectSingleNode("ms:DependentUpon", nsManager)
                                   ?? compileNode.SelectSingleNode("DependentUpon");

            Assert.IsNotNull(dependentUponNode, "AlibreGearForm.Designer.cs must have a DependentUpon element.");
            Assert.AreEqual("AlibreGearForm.cs", dependentUponNode.InnerText.Trim(), "AlibreGearForm.Designer.cs must have <DependentUpon>AlibreGearForm.cs</DependentUpon> for IDE design view support.");
        }

        [Test]
        public void TestAlibreGearForm_PanelInputs_IsTableLayoutPanel()
        {
            var field = typeof(AlibreGearForm).GetField("panelInputs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "panelInputs field should exist in AlibreGearForm.");
            Assert.AreEqual(typeof(TableLayoutPanel), field.FieldType, "panelInputs field type must be TableLayoutPanel.");
        }

        [TestCase(30, false, 0.0, "R", "L")]
        [TestCase(31, false, 0.0, "R", "L")]
        [TestCase(40, true, 0.0, "R", "L")]
        [TestCase(41, true, 0.0, "R", "L")]
        [TestCase(30, false, 15.0, "R", "L")]
        public void TestGearAssembly_Pair_MatchesPresenterPairOutput(int teeth2, bool internal2, double helixAngle, string direction1, string direction2)
        {
            // Two decimal places of tolerance headroom: the assembly layer rounds the shaft origin and every vertex to 7 decimal places.
            const double tolerance = 2e-7;

            var view = new FakeAlibreGearView { HelixAngle = helixAngle, HelicalDirection = direction1 };
            var presenter = new AlibreGearPresenter(view);
            presenter.MeshedGear = new MeshedGearParameters
            {
                Teeth = teeth2,
                HelixAngle = helixAngle,
                HelicalDirection = direction2,
                Clearance = view.Clearance,
                Backlash = view.Backlash,
                ProfileShift = view.ProfileShift,
                FaceWidth = view.FaceWidth,
                IsInternal = internal2,
                RimThickness = view.RimThickness,
                OptimalShift = false
            };
            presenter.HasMeshedGear = true;
            presenter.ExecuteCalculation();
            Assert.IsTrue(string.IsNullOrEmpty(view.LastErrorMessage), "The presenter must calculate the pair without error: " + view.LastErrorMessage);

            var meshed = presenter.MeshedGear;
            var gear1 = new InvoluteGearComponent("g1")
            {
                Teeth = view.Teeth,
                Module = view.Module,
                PressureAngleDeg = view.PressureAngle,
                HelixAngleDeg = view.HelixAngle,
                HelicalDirection = view.HelicalDirection,
                Clearance = view.Clearance,
                Backlash = view.Backlash,
                ProfileShift = view.ProfileShift,
                FaceWidth = view.FaceWidth,
                IsInternal = view.IsInternal,
                RimThickness = view.RimThickness,
                OptimalShift = view.OptimalShift,
                ShowFullGear = view.ShowFullGear
            };
            var gear2 = new InvoluteGearComponent("g2")
            {
                Teeth = meshed.Teeth,
                Module = view.Module,
                PressureAngleDeg = view.PressureAngle,
                HelixAngleDeg = meshed.HelixAngle,
                HelicalDirection = meshed.HelicalDirection,
                Clearance = meshed.Clearance,
                Backlash = meshed.Backlash,
                ProfileShift = meshed.ProfileShift,
                FaceWidth = meshed.FaceWidth,
                IsInternal = meshed.IsInternal,
                RimThickness = meshed.RimThickness,
                OptimalShift = meshed.OptimalShift,
                ShowFullGear = view.ShowFullGear
            };

            var assembly = new GearAssembly();
            var shaft1 = assembly.AddShaft(new Shaft("s1"));
            var shaft2 = assembly.AddShaft(new Shaft("s2"));
            shaft1.Mount(gear1);
            shaft2.Mount(gear2);
            assembly.AddRelation(gear1, gear2);

            var scene = GearAssemblySceneBuilder.Build(assembly);

            Assert.AreEqual(presenter.CurrentCalculatedGearPairData.WorkingCentreDistance, assembly.Relations[0].Result.CentreDistance, tolerance);

            var expectedPolylines = presenter.CurrentPolylines;
            var actualPolylines = scene.CombinedPolylines();
            Assert.AreEqual(expectedPolylines.Count, actualPolylines.Count, "Polyline count");
            for (var s = 0; s < expectedPolylines.Count; s++)
            {
                Assert.AreEqual(expectedPolylines[s].Count, actualPolylines[s].Count, "Point count of polyline " + s);
                for (var i = 0; i < expectedPolylines[s].Count; i++)
                {
                    Assert.AreEqual(expectedPolylines[s][i].X, actualPolylines[s][i].X, tolerance, $"X of polyline {s} point {i}");
                    Assert.AreEqual(expectedPolylines[s][i].Y, actualPolylines[s][i].Y, tolerance, $"Y of polyline {s} point {i}");
                }
            }

            var expectedMesh = presenter.CurrentMesh;
            var actualMesh = scene.CombinedMesh();
            Assert.AreEqual(expectedMesh.Vertices.Count, actualMesh.Vertices.Count, "Vertex count");
            CollectionAssert.AreEqual(expectedMesh.TriangleIndices, actualMesh.TriangleIndices, "Triangle indices");
            for (var i = 0; i < expectedMesh.Vertices.Count; i++)
            {
                Assert.AreEqual(expectedMesh.Vertices[i].X, actualMesh.Vertices[i].X, tolerance, "Vertex X " + i);
                Assert.AreEqual(expectedMesh.Vertices[i].Y, actualMesh.Vertices[i].Y, tolerance, "Vertex Y " + i);
                Assert.AreEqual(expectedMesh.Vertices[i].Z, actualMesh.Vertices[i].Z, tolerance, "Vertex Z " + i);
            }

            Assert.AreEqual(expectedMesh.Normals.Count, actualMesh.Normals.Count, "Normal count");
            for (var i = 0; i < expectedMesh.Normals.Count; i++)
            {
                Assert.AreEqual(expectedMesh.Normals[i].X, actualMesh.Normals[i].X, tolerance, "Normal X " + i);
                Assert.AreEqual(expectedMesh.Normals[i].Y, actualMesh.Normals[i].Y, tolerance, "Normal Y " + i);
                Assert.AreEqual(expectedMesh.Normals[i].Z, actualMesh.Normals[i].Z, tolerance, "Normal Z " + i);
            }
        }
    }
}

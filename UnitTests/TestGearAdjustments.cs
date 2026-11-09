using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Gui.Presenters;
using Bolsover.Gears.Gui.Views;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Adjustable clearance, backlash, profile shift and centre distance of meshed involute gears, the working values
    /// that follow from them, and the gear train editor commands that change them.
    /// </summary>
    [TestFixture]
    public class TestGearAdjustments
    {
        private const double Tol = 1e-9;

        private static InvoluteGearComponent Gear(string id, int teeth, bool isInternal = false)
        {
            return new InvoluteGearComponent(id)
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                Clearance = 0.5,
                IsInternal = isInternal
            };
        }

        private static GearAssembly Pair(InvoluteGearComponent driver, InvoluteGearComponent driven, double? centreDistance = null)
        {
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(driver, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(driven, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            var relation = assembly.AddRelation(driver, driven);
            relation.CentreDistanceOverride = centreDistance;
            assembly.Solve();
            return assembly;
        }

        private static double Inv(double angle) => Math.Tan(angle) - angle;

        [Test]
        public void TestCentreDistance_Automatic_IsTheStandardDistanceWithoutProfileShift()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40));
            var result = assembly.Relations.First().Result;

            Assert.AreEqual(60.0, result.CentreDistance, Tol);
            Assert.IsFalse(result.PairData.CentreDistanceOverridden);
            Assert.AreEqual(20.0, result.PairData.WorkingPressureAngleDeg, 1e-7);
            Assert.AreEqual(0.0, result.PairData.WorkingCircularBacklash, 1e-9, "Zero profile shift and backlash give a zero-backlash mesh.");
        }

        [Test]
        public void TestCentreDistance_Fixed_MovesTheDrivenShaft()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 61.5);

            Assert.AreEqual(61.5, assembly.Shafts[1].Placement.Origin.X, 1e-7);
            Assert.AreEqual(61.5, assembly.Relations.First().Result.CentreDistance, 1e-7);
            Assert.IsTrue(assembly.Relations.First().Result.PairData.CentreDistanceOverridden);
        }

        [Test]
        public void TestCentreDistance_Fixed_ChangesPressureAngleAndBacklashByTheStandardFormulae()
        {
            const double centre = 60.5;
            var pair = Pair(Gear("A", 20), Gear("B", 40), centre).Relations.First().Result.PairData;

            var alpha = 20.0 * Math.PI / 180.0;
            var expectedAlphaW = Math.Acos(60.0 * Math.Cos(alpha) / centre);
            Assert.AreEqual(expectedAlphaW, pair.WorkingPressureAngleRad, 1e-12, "cos(alpha_w) = a cos(alpha) / a_w");

            // For an unshifted pair without backlash: j = 2 a_w (inv(alpha_w) - inv(alpha)).
            var expectedBacklash = 2.0 * centre * (Inv(expectedAlphaW) - Inv(alpha));
            Assert.AreEqual(expectedBacklash, pair.WorkingCircularBacklash, 1e-9);
            Assert.Greater(pair.WorkingCircularBacklash, 0.0);
            Assert.AreEqual(expectedBacklash * Math.Cos(expectedAlphaW), pair.NormalBacklash, 1e-9);

            Assert.AreEqual((centre - 60.0) / 2.0, pair.CentreDistanceIncrementFactor, 1e-12);
            Assert.AreEqual(pair.Gear1.PitchDiameter * Math.Cos(alpha) / Math.Cos(expectedAlphaW), pair.Gear1WorkingPitchDiameter, 1e-9);
        }

        [Test]
        public void TestCentreDistance_Fixed_ChangesTheContactRatio()
        {
            var automatic = Pair(Gear("A", 20), Gear("B", 40)).Relations.First().Result.PairData.ContactRatio;
            var wider = Pair(Gear("A", 20), Gear("B", 40), 61.0).Relations.First().Result.PairData.ContactRatio;

            Assert.Less(wider, automatic, "Pulling the gears apart shortens the line of action.");
        }

        [Test]
        public void TestCentreDistance_TooSmall_IsRejectedByTheSolver()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => Pair(Gear("A", 20), Gear("B", 40), 40.0));
            StringAssert.Contains("too small", ex.Message);
        }

        [Test]
        public void TestCentreDistance_NotPositive_IsRejectedByThePairCalculation()
        {
            Assert.Throws<InvalidOperationException>(() => GearParameterCalculator.CalculatePair(
                20, 2.0, 20.0, 10.0, 0.0, false, 10.0, 40, 10.0, 0.0, false, 10.0, workingCentreDistance: -5.0));
        }

        [Test]
        public void TestCentreDistance_OnAMeshWithoutAdjustableCentre_IsRejected()
        {
            var pinion = Gear("A", 20);
            var rack = new RackComponent("R");
            var assembly = new GearAssembly();
            var s1 = new Shaft("s1");
            s1.Mount(pinion, 0.0);
            var s2 = new Shaft("s2");
            s2.Mount(rack, 0.0);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            var relation = assembly.AddRelation(pinion, rack, MeshType.Rack);
            relation.CentreDistanceOverride = 30.0;

            var ex = Assert.Throws<InvalidOperationException>(() => assembly.Solve());
            StringAssert.Contains("cannot be set", ex.Message);
        }

        [Test]
        public void TestBacklash_OfBothGears_AddsUpAtTheStandardDistance()
        {
            var a = Gear("A", 20);
            a.Backlash = 0.1;
            var b = Gear("B", 40);
            b.Backlash = 0.1;

            var pair = Pair(a, b).Relations.First().Result.PairData;

            Assert.AreEqual(0.2, pair.WorkingCircularBacklash, 1e-9);
            Assert.AreEqual(0.2 * Math.Cos(pair.WorkingPressureAngleRad), pair.NormalBacklash, 1e-9);
        }

        [Test]
        public void TestBacklash_InternalPair_AddsUpAndShrinksWithCentreDistance()
        {
            Func<double?, double> backlash = centre =>
            {
                var a = Gear("A", 20);
                a.Backlash = 0.1;
                var b = Gear("B", 40, true);
                b.Backlash = 0.1;
                return Pair(a, b, centre).Relations.First().Result.PairData.WorkingCircularBacklash;
            };

            Assert.AreEqual(0.2, backlash(null), 1e-9);

            // Unlike an external pair, an internal pair loses backlash as the centre distance grows: j = j0 - 2 a_w (inv(alpha_w) - inv(alpha)) at
            // zero profile shift, so a distance of 20.2 mm takes about 0.15 mm out of the 0.2 mm.
            var wider = backlash(20.2);
            Assert.Less(wider, backlash(null));
            Assert.AreEqual(0.0496, wider, 1e-3);
            Assert.Greater(backlash(19.9), backlash(null), "A smaller distance opens an internal mesh.");
        }

        [Test]
        public void TestClearance_ChangesTheRootCircleOfTheGeometry()
        {
            var small = Gear("A", 20);
            small.Clearance = 0.25;
            var large = Gear("A", 20);
            large.Clearance = 0.75;

            var smallRoot = small.BuildGeometry().Profile.Parameters;
            var largeRoot = large.BuildGeometry().Profile.Parameters;

            Assert.AreEqual(0.25, small.Clearance);
            Assert.Less(
                Convert.ToDouble(largeRoot["dedendumRadius"]),
                Convert.ToDouble(smallRoot["dedendumRadius"]),
                "More clearance cuts the tooth space deeper, so the root circle gets smaller.");
        }

        [Test]
        public void TestProfileShiftFitter_ExternalPair_ReproducesTheChosenCentreDistance()
        {
            var driver = Gear("A", 20);
            driver.ProfileShift = 0.2;
            var driven = Gear("B", 40);

            driven.ProfileShift = Math.Round(ProfileShiftFitter.RequiredDrivenProfileShift(driver, driven, 61.0), 7);
            var result = Pair(driver, driven).Relations.First().Result;

            Assert.AreEqual(61.0, result.CentreDistance, 1e-5);
            Assert.Greater(driven.ProfileShift, 0.0);
        }

        [Test]
        public void TestProfileShiftFitter_HelicalPair_ReproducesTheChosenCentreDistance()
        {
            var driver = Gear("A", 20);
            driver.HelixAngleDeg = 15.0;
            var driven = Gear("B", 40);
            driven.HelixAngleDeg = 15.0;
            driven.HelicalDirection = "L";
            var standard = ProfileShiftFitter.StandardCentreDistance(driver, driven);

            driven.ProfileShift = Math.Round(ProfileShiftFitter.RequiredDrivenProfileShift(driver, driven, standard + 1.0), 7);
            var result = Pair(driver, driven).Relations.First().Result;

            Assert.AreEqual(standard + 1.0, result.CentreDistance, 1e-5);
        }

        [Test]
        public void TestProfileShiftFitter_InternalPair_ReproducesTheChosenCentreDistance()
        {
            var driver = Gear("A", 20);
            var driven = Gear("B", 40, true);
            Assert.AreEqual(20.0, ProfileShiftFitter.StandardCentreDistance(driver, driven), Tol);

            driven.ProfileShift = Math.Round(ProfileShiftFitter.RequiredDrivenProfileShift(driver, driven, 20.4), 7);
            var result = Pair(driver, driven).Relations.First().Result;

            Assert.AreEqual(20.4, result.CentreDistance, 1e-5);
        }

        [Test]
        public void TestProfileShiftFitter_NoShiftNeededAtTheStandardDistance()
        {
            var driver = Gear("A", 20);
            var driven = Gear("B", 40);

            Assert.AreEqual(0.0, ProfileShiftFitter.RequiredDrivenProfileShift(driver, driven, 60.0), 1e-9);
        }

        [Test]
        public void TestProfileShiftFitter_TooSmallDistance_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => ProfileShiftFitter.RequiredDrivenProfileShift(Gear("A", 20), Gear("B", 40), 40.0));
        }

        [Test]
        public void TestReport_ShowsTheWorkingValuesOfEachStage()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 60.5);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);
            var stage = report.Stages.Single();

            Assert.IsTrue(stage.CentreDistanceFixed);
            Assert.AreEqual(60.5, stage.CentreDistance, 1e-7);
            Assert.Greater(stage.WorkingPressureAngleDeg.Value, 20.0);
            Assert.Greater(stage.WorkingBacklash.Value, 0.0);
            Assert.Greater(stage.ContactRatio.Value, 1.0);
            StringAssert.Contains("(fixed)", report.ToText());
            StringAssert.Contains("working backlash", report.ToText());
        }

        [Test]
        public void TestReport_WarnsWhenTheTeethJam()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 59.9);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);

            Assert.IsTrue(report.Warnings.Any(w => w.Contains("jam")), string.Join(" | ", report.Warnings));
            Assert.Less(report.Stages.Single().WorkingBacklash.Value, 0.0);
        }

        [Test]
        public void TestReport_WarnsWhenTheContactRatioDropsBelowOne()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 63.0);
            var report = GearTrainAnalyzer.Analyze(assembly, null, 1000.0);

            Assert.IsTrue(report.Warnings.Any(w => w.Contains("contact ratio below 1")), string.Join(" | ", report.Warnings));
        }

        [Test]
        public void TestSerializer_KeepsTheFixedCentreDistance()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 61.25);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            Assert.AreEqual(61.25, loaded.Relations.Single().CentreDistanceOverride);
            loaded.Solve();
            Assert.AreEqual(61.25, loaded.Shafts[1].Placement.Origin.X, 1e-7);
        }

        [Test]
        public void TestSerializer_AutomaticCentreDistance_StaysAutomatic()
        {
            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(Pair(Gear("A", 20), Gear("B", 40))));

            Assert.IsNull(loaded.Relations.Single().CentreDistanceOverride);
        }

        #region Parameter tables

        [Test]
        public void TestParameterTable_ForInvoluteGear_MatchesTheSingleGearCalculator()
        {
            var gear = Gear("A", 20);
            gear.ProfileShift = 0.25;

            var table = GearParameterTables.ForGear(gear);
            var expected = GearParameterCalculator.Calculate(20, 2.0, 20.0, gear.FaceWidth, 0.25, false, gear.RimThickness, gear.Clearance, gear.Backlash, 0.0, "R");

            Assert.AreEqual(expected.ParameterRows.Count, table.Rows.Count);
            Assert.Greater(table.Rows.Count, 10, "The table lists all gear parameters.");
            StringAssert.Contains("A", table.Title);
            foreach (var row in table.Rows)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.ParameterName), "Every row has a name.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.SymbolLatex), $"'{row.ParameterName}' has a symbol.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.FormulaLatex), $"'{row.ParameterName}' has a formula.");
                Assert.IsFalse(double.IsNaN(row.Value), $"'{row.ParameterName}' has a value.");
            }

            Assert.AreEqual(20.0, table.Rows.Single(r => r.SymbolLatex == "z").Value, Tol);
            Assert.AreEqual(0.25, table.Rows.Single(r => r.SymbolLatex == "x").Value, Tol);
        }

        [Test]
        public void TestParameterTable_ForHelicalGear_UsesNormalAndTransverseSymbols()
        {
            var gear = Gear("H", 24);
            gear.HelixAngleDeg = 15.0;

            var symbols = GearParameterTables.ForGear(gear).Rows.Select(r => r.SymbolLatex).ToList();

            CollectionAssert.Contains(symbols, "m_t");
            CollectionAssert.Contains(symbols, "\\beta");
        }

        [Test]
        public void TestParameterTable_ForMesh_ListsWorkingValuesOfThePair()
        {
            var assembly = Pair(Gear("A", 20), Gear("B", 40), 61.0);
            var relation = assembly.Relations.Single();

            var table = GearParameterTables.ForMesh(relation);

            Assert.Greater(table.Rows.Count, 5);
            Assert.AreEqual(61.0, table.Rows.Single(r => r.ParameterName == "Working Centre Distance").Value, 1e-7);
            Assert.AreEqual(2.0, table.Rows.Single(r => r.ParameterName == "Gear Ratio").Value, Tol);
            Assert.IsTrue(table.Rows.Any(r => r.ParameterName == "Working Circular Backlash"), "A fixed centre distance adds the working backlash.");
            Assert.IsTrue(table.Rows.All(r => !string.IsNullOrWhiteSpace(r.FormulaLatex)));
        }

        [Test]
        public void TestParameterTable_FindMeshFor_PrefersTheDrivingMeshAndHandlesUnmeshedGears()
        {
            var driver = Gear("A", 20);
            var driven = Gear("B", 40);
            var assembly = Pair(driver, driven);

            Assert.AreSame(assembly.Relations.Single(), GearParameterTables.FindMeshFor(assembly, driven));
            Assert.AreSame(assembly.Relations.Single(), GearParameterTables.FindMeshFor(assembly, driver));
            Assert.IsNull(GearParameterTables.FindMeshFor(assembly, Gear("C", 30)));
            Assert.IsNull(GearParameterTables.FindMeshFor(null, driver));
        }

        [Test]
        public void TestParameterTable_NullInputs_GiveAnEmptyTable()
        {
            Assert.AreEqual(0, GearParameterTables.ForGear(null).Rows.Count);
            Assert.AreEqual(0, GearParameterTables.ForMesh(null).Rows.Count);
            Assert.AreEqual(0, GearParameterTables.ForMesh(new GearMeshRelation(Gear("A", 20), Gear("B", 40), MeshType.Parallel)).Rows.Count,
                "A mesh that has not been solved has no pair data.");
        }

        #endregion

        #region Presenter

        private sealed class FakeView : IGearTrainView
        {
            public double InputSpeedRpm { get; set; } = 1000.0;
            public object Properties;
            public GearTrainReport Report;
            public string Message;
            public bool IsError;

            public void ShowTree(TrainTreeNode root, object selectedTag) { }
            public void ShowSelection(object selectedTag) { }
            public void ShowProperties(object properties) => Properties = properties;
            public void ShowScene(AssemblyScene scene, IReadOnlyCollection<object> selectedItems, ISet<GearComponent> hiddenGears) { }
            public void ShowReport(GearTrainReport report) => Report = report;

            public void ShowMessage(string message, bool isError)
            {
                Message = message;
                IsError = isError;
            }
        }

        private static GearTrainPresenter NewMeshedPresenter(out FakeView view, out InvoluteGearComponent driver, out InvoluteGearComponent driven)
        {
            view = new FakeView();
            var presenter = new GearTrainPresenter(view);
            presenter.NewTrain();
            driver = (InvoluteGearComponent)presenter.Selected;
            driven = presenter.AddMeshedGear(driver, 40, 0.0);
            return presenter;
        }

        [Test]
        public void TestPresenter_SetCentreDistance_MovesTheShaftAndUpdatesTheReport()
        {
            var presenter = NewMeshedPresenter(out var view, out _, out var driven);

            presenter.SetCentreDistance(driven, 61.0);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.AreEqual(61.0, presenter.Assembly.Shafts[1].Placement.Origin.X, 1e-7);
            Assert.IsTrue(view.Report.Stages.Single().CentreDistanceFixed);
            Assert.AreEqual(61.0, view.Report.Stages.Single().CentreDistance, 1e-7);

            presenter.SetCentreDistance(driven, 0.0);
            Assert.IsFalse(view.Report.Stages.Single().CentreDistanceFixed);
        }

        [Test]
        public void TestPresenter_SetCentreDistance_TooSmall_ShowsAMessageAndKeepsTheLastGoodReport()
        {
            var presenter = NewMeshedPresenter(out var view, out _, out var driven);
            var before = view.Report;

            Assert.DoesNotThrow(() => presenter.SetCentreDistance(driven, 20.0));

            Assert.IsTrue(view.IsError);
            StringAssert.Contains("too small", view.Message);
            Assert.AreSame(before, view.Report);
        }

        [Test]
        public void TestPresenter_FitProfileShift_ReplacesTheFixedDistanceByAProfileShift()
        {
            var presenter = NewMeshedPresenter(out var view, out var driver, out var driven);
            presenter.SetCentreDistance(driven, 61.0);

            presenter.FitProfileShiftToCentreDistance(driven);

            Assert.IsFalse(view.IsError, view.Message);
            Assert.IsNull(presenter.Assembly.FindDrivingRelation(driven).CentreDistanceOverride);
            Assert.Greater(driven.ProfileShift, 0.0);
            Assert.AreEqual(0.0, driver.ProfileShift, 0.0, "The driver is left alone.");
            Assert.AreEqual(61.0, presenter.Assembly.Shafts[1].Placement.Origin.X, 1e-5);
            Assert.AreEqual(0.0, view.Report.Stages.Single().WorkingBacklash.Value, 1e-5, "Fitting by profile shift keeps the mesh free of extra backlash.");
        }

        [Test]
        public void TestPresenter_FitProfileShift_NeedsADrivenGear()
        {
            var presenter = NewMeshedPresenter(out _, out var driver, out _);

            Assert.Throws<InvalidOperationException>(() => presenter.FitProfileShiftToCentreDistance(driver));
        }

        [Test]
        public void TestProperties_ExposeClearanceBacklashProfileShiftAndCentreDistance()
        {
            var presenter = NewMeshedPresenter(out var view, out _, out var driven);
            presenter.Select(driven);
            var properties = (GearNodeProperties)view.Properties;

            Assert.AreEqual(60.0, properties.CentreDistance, 1e-7, "The solved distance is shown while none is fixed.");
            Assert.IsFalse(properties.CentreDistanceFixed);
            Assert.AreEqual(60.0, properties.StandardCentreDistance, 1e-7);
            Assert.AreEqual(20.0, properties.WorkingPressureAngle, 1e-6);

            properties.Clearance = 0.3;
            properties.Backlash = 0.05;
            properties.ProfileShift = 0.1;
            Assert.AreEqual(0.3, driven.Clearance);
            Assert.AreEqual(0.05, driven.Backlash);
            Assert.AreEqual(0.1, driven.ProfileShift);

            properties.CentreDistance = 62.0;
            presenter.Refresh();
            presenter.Select(driven);
            properties = (GearNodeProperties)view.Properties;
            Assert.IsTrue(properties.CentreDistanceFixed);
            Assert.AreEqual(62.0, properties.CentreDistance, 1e-7);
            Assert.AreEqual(62.0, presenter.Assembly.Shafts[1].Placement.Origin.X, 1e-7);
            Assert.Greater(properties.WorkingBacklash, 0.05);

            properties.CentreDistanceFixed = false;
            presenter.Refresh();
            Assert.IsNull(presenter.Assembly.FindDrivingRelation(driven).CentreDistanceOverride);
        }

        #endregion
    }
}

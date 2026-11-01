using System;
using System.Linq;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Imperial reference conversions: diametral pitch P = 25.4 / m, circular pitch p = pi * m (mm) = pi / P (in),
    /// and lengths in inches. The design itself stays metric.
    /// </summary>
    [TestFixture]
    public class TestImperialUnits
    {
        private const double Tol = 1e-9;

        [TestCase(1.0, 25.4)]
        [TestCase(2.0, 12.7)]
        [TestCase(2.54, 10.0)]
        [TestCase(25.4, 1.0)]
        public void TestDiametralPitch_FromModule(double module, double expected)
        {
            Assert.AreEqual(expected, ImperialUnits.DiametralPitchFromModule(module), Tol);
            Assert.AreEqual(module, ImperialUnits.ModuleFromDiametralPitch(expected), Tol, "The conversion round-trips.");
        }

        [Test]
        public void TestCircularPitch_FromModule_InMillimetresAndInches()
        {
            Assert.AreEqual(Math.PI * 2.0, ImperialUnits.CircularPitchMmFromModule(2.0), Tol);
            Assert.AreEqual(Math.PI * 2.0 / 25.4, ImperialUnits.CircularPitchInchesFromModule(2.0), Tol);

            var dp = ImperialUnits.DiametralPitchFromModule(2.0);
            Assert.AreEqual(Math.PI / dp, ImperialUnits.CircularPitchInchesFromModule(2.0), Tol, "p = pi / P.");
            Assert.AreEqual(dp, ImperialUnits.DiametralPitchFromCircularPitchInches(ImperialUnits.CircularPitchInchesFromModule(2.0)), Tol, "P = pi / p.");
        }

        [Test]
        public void TestLengths_ConvertBothWays()
        {
            Assert.AreEqual(1.0, ImperialUnits.ToInches(25.4), Tol);
            Assert.AreEqual(25.4, ImperialUnits.ToMillimetres(1.0), Tol);
            Assert.AreEqual(3.75, ImperialUnits.ToInches(ImperialUnits.ToMillimetres(3.75)), Tol);
        }

        [Test]
        public void TestTransverseModule_UsesTheHelixAngle()
        {
            Assert.AreEqual(2.0 / Math.Cos(15.0 * Math.PI / 180.0), ImperialUnits.TransverseModule(2.0, 15.0), Tol);
            Assert.AreEqual(2.0, ImperialUnits.TransverseModule(2.0, 0.0), Tol);
        }

        [Test]
        public void TestInvalidInputs_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ImperialUnits.DiametralPitchFromModule(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImperialUnits.ModuleFromDiametralPitch(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImperialUnits.CircularPitchMmFromModule(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImperialUnits.CircularPitchInchesFromDiametralPitch(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ImperialUnits.DiametralPitchFromCircularPitchInches(0));
        }

        [Test]
        public void TestReferenceTable_ForSpurGear_ListsPitchesAndInchLengths()
        {
            var gear = new InvoluteGearComponent("A") { Module = 2.0, Teeth = 20 };

            var table = ImperialReference.ForGear(gear);

            var dp = table.Rows.Single(r => r.Name == "Diametral Pitch (D.P.)");
            Assert.AreEqual(2.0, dp.MetricValue, Tol);
            Assert.AreEqual(12.7, dp.ImperialValue, Tol);

            var cp = table.Rows.Single(r => r.Name == "Circular Pitch (C.P.)");
            Assert.AreEqual(Math.PI * 2.0, cp.MetricValue, Tol);
            Assert.AreEqual(Math.PI / 12.7, cp.ImperialValue, Tol);

            var pitchDiameter = table.Rows.Single(r => r.Name.IndexOf("Pitch Diameter", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.AreEqual(40.0, pitchDiameter.MetricValue, 1e-7);
            Assert.AreEqual(40.0 / 25.4, pitchDiameter.ImperialValue, 1e-7, "The pitch diameter equals N / P inches.");
            Assert.AreEqual(20.0 / 12.7, pitchDiameter.ImperialValue, 1e-7);

            Assert.IsTrue(table.Rows.All(r => !double.IsNaN(r.ImperialValue)), "Every row has a value.");
            Assert.IsFalse(table.Rows.Any(r => r.Name.IndexOf("Module", StringComparison.OrdinalIgnoreCase) >= 0 && r.ImperialUnit == "in"),
                "The module itself is not converted to inches.");
        }

        [Test]
        public void TestReferenceTable_ForHelicalGear_GivesNormalAndTransversePitches()
        {
            var gear = new InvoluteGearComponent("H") { Module = 2.0, Teeth = 24, HelixAngleDeg = 15.0 };

            var table = ImperialReference.ForGear(gear);

            var normal = table.Rows.Single(r => r.Name == "Normal Diametral Pitch (D.P.)");
            var transverse = table.Rows.Single(r => r.Name == "Transverse Diametral Pitch (D.P.)");
            Assert.AreEqual(12.7, normal.ImperialValue, Tol);
            Assert.AreEqual(12.7 * Math.Cos(15.0 * Math.PI / 180.0), transverse.ImperialValue, Tol);
            Assert.AreEqual(Math.PI * 2.0, table.Rows.Single(r => r.Name == "Normal Circular Pitch (C.P.)").MetricValue, Tol);
            Assert.AreEqual(Math.PI * 2.0 / Math.Cos(15.0 * Math.PI / 180.0),
                table.Rows.Single(r => r.Name == "Transverse Circular Pitch (C.P.)").MetricValue, Tol);
        }

        [Test]
        public void TestReferenceTable_ForOtherGearFamilies_AndNulls()
        {
            var rack = new RackComponent("R") { Module = 2.0 };
            Assert.AreEqual(12.7, ImperialReference.ForGear(rack).Rows.Single(r => r.Name == "Diametral Pitch (D.P.)").ImperialValue, Tol);

            var worm = new WormComponent("W") { Module = 2.0 };
            Assert.IsTrue(ImperialReference.ForGear(worm).Rows.Any(r => r.Name == "Axial Diametral Pitch (D.P.)"));

            Assert.AreEqual(0, ImperialReference.ForGear(null).Rows.Count);
            Assert.AreEqual(0, ImperialReference.ForMesh(null).Rows.Count);
        }

        [Test]
        public void TestReferenceTable_ForMesh_ConvertsTheCentreDistance()
        {
            var driver = new InvoluteGearComponent("A") { Module = 2.0, Teeth = 20 };
            var driven = new InvoluteGearComponent("B") { Module = 2.0, Teeth = 40 };
            var assembly = new GearAssembly();
            var shaft1 = new Shaft("s1");
            shaft1.Mount(driver, 0.0);
            var shaft2 = new Shaft("s2");
            shaft2.Mount(driven, 0.0);
            assembly.AddShaft(shaft1);
            assembly.AddShaft(shaft2);
            assembly.AddRelation(driver, driven);
            assembly.Solve();

            var relation = assembly.Relations.Single();
            var table = ImperialReference.ForMesh(relation);

            Assert.Greater(table.Rows.Count, 0);
            var centre = table.Rows.First(r => r.Name.IndexOf("Centre Distance", StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.AreEqual(centre.MetricValue / 25.4, centre.ImperialValue, Tol);
            Assert.AreEqual("in", centre.ImperialUnit);
        }
    }
}

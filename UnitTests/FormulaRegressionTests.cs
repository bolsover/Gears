using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Numeric regression guard for the NCalc formulas held in Nexus.db.
    /// Every variable of every GearType is calculated with the default design values and compared
    /// with a captured baseline, so rewriting an NCalc expression (e.g. replacing an inlined
    /// sub-expression with a reference to another variable) cannot silently change a result.
    /// </summary>
    [TestFixture]
    public class FormulaRegressionTests
    {
        private const double RelativeTolerance = 1e-9;

        private static string BaselinePath =>
            Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, @"..\..\Baselines\FormulaValues.csv"));

        /// <summary>
        /// Set this environment variable to 1 to re-capture the baseline from the current database.
        /// Do so only when a change of calculated values is intended.
        /// </summary>
        private const string RecaptureVariable = "GEARS_RECAPTURE_BASELINE";

        private static void CaptureBaseline()
        {
            var lines = CalculateAll()
                .Select(kvp => kvp.Key + "," + kvp.Value.ToString("R", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(Path.GetDirectoryName(BaselinePath)!);
            File.WriteAllLines(BaselinePath, new[] { "GearType|GearPart|VariableId,Value" }.Concat(lines));
        }

        [Test]
        public void CalculatedValuesMatchBaseline()
        {
            if (!File.Exists(BaselinePath) || Environment.GetEnvironmentVariable(RecaptureVariable) == "1")
            {
                CaptureBaseline();
                Assert.Inconclusive($"Baseline captured to {BaselinePath}");
            }

            var expected = File.ReadAllLines(BaselinePath).Skip(1)
                .Select(l => l.Split(','))
                .ToDictionary(p => p[0], p => double.Parse(p[1], CultureInfo.InvariantCulture));
            var actual = CalculateAll();

            var failures = new List<string>();
            foreach (var kvp in expected)
            {
                if (!actual.TryGetValue(kvp.Key, out var value))
                {
                    failures.Add($"{kvp.Key}: missing");
                    continue;
                }

                if (!AreClose(kvp.Value, value)) failures.Add($"{kvp.Key}: expected {kvp.Value:R}, actual {value:R}");
            }

            failures.AddRange(actual.Keys.Except(expected.Keys).Select(k => $"{k}: not in baseline"));
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        [Test]
        public void ReferencedCalculatedVariablesAreEvaluatedOnDemand()
        {
            var gear = new Gear(GearTypes.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
            gear.Calculate();
            var baseDiameter = gear.Gears[0].GetVariable(VariableIds.BaseDiameter);
            var inlined = baseDiameter.Value;

            // d_{b1} = d_{1} cos(alpha_t), referencing two calculated variables instead of inlining them
            baseDiameter.NCalc = "[d1] * Cos([alphat])";
            baseDiameter.Value = 0;

            Assert.That(gear.Gears[0].CalculateVariable(VariableIds.BaseDiameter), Is.EqualTo(inlined).Within(1e-9));
        }

        [Test]
        public void CircularReferenceIsReported()
        {
            var gear = new Gear(GearTypes.ProfileShiftedExternalNormalHelicalPair, CalculationMode.Automatic);
            gear.Gears[0].GetVariable(VariableIds.BaseDiameter).NCalc = "[d1] * Cos([alphat])";
            gear.Gears[0].GetVariable(VariableIds.PitchDiameter).NCalc = "[db1] / Cos([alphat])";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                gear.Gears[0].CalculateVariable(VariableIds.BaseDiameter));
            Assert.That(ex.Message, Does.Contain("Circular reference"));
        }

        private static bool AreClose(double expected, double actual)
        {
            if (double.IsNaN(expected) || double.IsNaN(actual)) return double.IsNaN(expected) && double.IsNaN(actual);
            return Math.Abs(expected - actual) <= RelativeTolerance * Math.Max(1.0, Math.Abs(expected));
        }

        private static SortedDictionary<string, double> CalculateAll()
        {
            var results = new SortedDictionary<string, double>(StringComparer.Ordinal);
            foreach (var context in GearVariableLoader.GetGearContexts())
            {
                var type = (GearTypes)Enum.Parse(typeof(GearTypes), context);
                var gear = new Gear(type, CalculationMode.Automatic);
                gear.Calculate();
                Collect(gear, type, results);
            }

            return results;
        }

        private static void Collect(Gear gear, GearTypes type, IDictionary<string, double> results)
        {
            foreach (var variable in gear.GearVariables.Values)
            {
                results[$"{type}|{variable.GearPart}|{variable.Id}"] = variable.Value;
            }

            foreach (var child in gear.Gears) Collect(child, type, results);
        }
    }
}

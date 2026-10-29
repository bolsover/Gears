using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class NCalcLatexRendererTests
    {
        private static readonly Dictionary<string, string> Symbols = new()
        {
            ["a"] = "a", ["b"] = "b", ["c"] = "c", ["m"] = "m", ["mn"] = "m_{n}", ["z1"] = "z_{1}", ["z2"] = "z_{2}",
            ["beta"] = @"\beta", ["alphan"] = @"\alpha_{n}", ["alphawt"] = @"\alpha_{wt}", ["db1"] = "d_{b1}",
            ["da1"] = "d_{a1}", ["d1"] = "d_{1}", ["alphat"] = @"\alpha_{t}", ["xsum"] = "x_{1}+x_{2}"
        };

        private static string Render(string nCalc) =>
            NCalcLatexRenderer.Render(nCalc, k => Symbols.TryGetValue(k, out var s) ? s : null);

        [TestCase("[db1] / Cos([alphawt])", @"\frac{d_{b1}}{\cos\alpha_{wt}}")]
        [TestCase("[d1] * Cos([alphat])", @"d_{1}\cos\alpha_{t}")]
        [TestCase("[z1] * [mn] / Cos([beta])", @"\frac{z_{1}m_{n}}{\cos\beta}")]
        [TestCase("([z1] * [m]) + 2 * [m]", "z_{1}m + 2m")]
        [TestCase("1.25 * [m]", "1.25m")]
        [TestCase("Atan(Tan([alphan]) / Cos([beta]))", @"\tan^{-1}\left(\frac{\tan\alpha_{n}}{\cos\beta}\right)")]
        [TestCase("[m] * ([z1] + [z2]) / 2", @"\frac{m\left(z_{1} + z_{2}\right)}{2}")]
        [TestCase("[a] - ([b] - [c])", @"a - \left(b - c\right)")]
        [TestCase("[a] - ([b] + [c])", @"a - \left(b + c\right)")]
        [TestCase("([a] + [b]) + [c]", "a + b + c")]
        [TestCase("Cos([beta]) * [mn]", @"\cos\beta \cdot m_{n}")]
        [TestCase("[mn] * 2", @"m_{n} \cdot 2")]
        [TestCase("[beta] * [m]", @"\beta m")]
        [TestCase("Cos([a] + [b])", @"\cos\left(a + b\right)")]
        [TestCase("Pow([a] + [b], 2)", @"\left(a + b\right)^{2}")]
        [TestCase("Pow([da1] / 2, 2)", @"\left(\frac{d_{a1}}{2}\right)^{2}")]
        [TestCase("Sqrt(Pow([a], 2) - Pow([b], 2))", @"\sqrt{a^{2} - b^{2}}")]
        [TestCase("[a] * -[b]", @"a\left(-b\right)")]
        [TestCase("-[a] * [b]", "-ab")]
        [TestCase("[xsum] * [m]", @"\left(x_{1}+x_{2}\right)m")]
        [TestCase("[pi] * [m]", @"\pi m")]
        [TestCase("[unknown]", @"\mathrm{unknown}")]
        public void RendersExpression(string nCalc, string expected)
        {
            Assert.That(Render(nCalc), Is.EqualTo(expected));
        }

        [Test]
        public void RenderedLatexParses()
        {
            foreach (var latex in new[] { Render("Pow([da1] / 2, 2)"), Render("Atan(Tan([alphan]) / Cos([beta]))") })
            {
                Assert.That(LaTeXUtils.TryParse(latex, out var error), Is.True, $"{latex}: {error}");
            }
        }

        private static IEnumerable<List<GearVariable>> AllGearTypes() =>
            GearVariableLoader.GetGearContexts()
                .Select(c => GearVariableLoader.LoadGearVariables((GearTypes)Enum.Parse(typeof(GearTypes), c)));

        private static IEnumerable<GearVariable> NCalcExpressions(IEnumerable<GearVariable> variables) =>
            variables.Where(v => v.CalcMethod == "NCalc" && !string.IsNullOrWhiteSpace(v.NCalc) &&
                                 !v.NCalc.TrimStart().StartsWith(@"\"));

        [Test]
        public void KeysAreUniquePerGearType()
        {
            var duplicates = AllGearTypes()
                .SelectMany(vs => vs.GroupBy(v => v.Key).Where(g => g.Count() > 1)
                    .Select(g => $"{g.First().GearType}: {g.Key} ({string.Join(", ", g.Select(v => v.Id))})"))
                .ToList();
            Assert.That(duplicates, Is.Empty, string.Join(Environment.NewLine, duplicates));
        }

        [Test]
        public void EveryNCalcReferenceResolvesToAKey()
        {
            var failures = new List<string>();
            foreach (var variables in AllGearTypes())
            {
                var keys = new HashSet<string>(variables.Select(v => v.Key)) { "pi" };
                foreach (var v in NCalcExpressions(variables))
                {
                    failures.AddRange(Regex.Matches(v.NCalc, @"\[(.*?)\]").Cast<Match>()
                        .Select(m => m.Groups[1].Value)
                        .Where(k => !keys.Contains(k))
                        .Select(k => $"{v.GearType}.{v.Id} ({v.GearPart}): [{k}]"));
                }
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// Every NCalc expression in the database renders to LaTeX that WpfMath can parse.
        /// The rendered output is written to the test log for review against the hand-written Formula.
        /// </summary>
        [Test]
        public void EveryDatabaseFormulaRendersToValidLatex()
        {
            var failures = new List<string>();
            foreach (var variables in AllGearTypes())
            {
                var symbols = variables.GroupBy(v => v.Key).ToDictionary(g => g.Key, g => g.First().Symbol);
                foreach (var v in NCalcExpressions(variables))
                {
                    string latex;
                    try
                    {
                        latex = NCalcLatexRenderer.Render(v.NCalc, k => symbols.TryGetValue(k, out var s) ? s : null);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{v.GearType}.{v.Id}: {ex.Message}");
                        continue;
                    }

                    if (!LaTeXUtils.TryParse(latex, out var error)) failures.Add($"{v.GearType}.{v.Id}: {latex}: {error}");
                    ConsoleIO.WriteLine($"{v.GearType}.{v.Id} [{v.Key}]{Environment.NewLine}" +
                                        $"  override : {v.FormulaOverride}{Environment.NewLine}  generated: {latex}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }
    }
}

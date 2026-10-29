using System;
using System.Collections.Generic;
using System.IO;
using Bolsover.Gears.Data;
using Bolsover.Gears.Model.Variable;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace UnitTests.VariableTests;

/// <summary>
/// Illustrates round-tripping a heterogeneous list of <see cref="IVariable"/> implementations
/// through JSON using <see cref="VariableSerializer"/>.
/// </summary>
[TestFixture]
public class VariableSerializerTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static List<IVariable> BuildSampleVariables() =>
    [
        new Addendum       { Value = 2.5,  Role = VariableRole.Calculated },
        new Module         { Value = 2.0,  Role = VariableRole.Design      },
        new Teeth          { Value = 24,   Role = VariableRole.Design      },
        new PressureAngle  { Value = 0.349, Role = VariableRole.Design     },
        new PitchDiameter  { Value = 48.0, Role = VariableRole.Calculated  },
    ];

    // ── ToJson ─────────────────────────────────────────────────────────────────

    [Test]
    public void ToJson_ProducesArrayWithCorrectEntries()
    {
        var variables = BuildSampleVariables();

        var json = VariableSerializer.ToJson(variables);

        var array = JArray.Parse(json);
        Assert.That(array.Count, Is.EqualTo(variables.Count));

        var first = array[0];
        Assert.That(first["typeName"]?.Value<string>(), Is.EqualTo("Addendum"));
        Assert.That(first["value"]?.Value<double>(),    Is.EqualTo(2.5).Within(1e-9));
        Assert.That(first["role"]?.Value<string>(),     Is.EqualTo("Calculated"));
    }

    [Test]
    public void ToJson_ThrowsOnNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => VariableSerializer.ToJson(null!));
    }

    // ── FromJson ───────────────────────────────────────────────────────────────

    [Test]
    public void FromJson_ReturnsCorrectConcreteTypes()
    {
        var json = VariableSerializer.ToJson(BuildSampleVariables());

        var loaded = VariableSerializer.FromJson(json);

        Assert.That(loaded[0], Is.InstanceOf<Addendum>());
        Assert.That(loaded[1], Is.InstanceOf<Module>());
        Assert.That(loaded[2], Is.InstanceOf<Teeth>());
        Assert.That(loaded[3], Is.InstanceOf<PressureAngle>());
        Assert.That(loaded[4], Is.InstanceOf<PitchDiameter>());
    }

    [Test]
    public void FromJson_ValuesAndRolesRoundTrip()
    {
        var original = BuildSampleVariables();
        var json     = VariableSerializer.ToJson(original);

        var loaded = VariableSerializer.FromJson(json);

        for (var i = 0; i < original.Count; i++)
        {
            Assert.That(loaded[i].Value, Is.EqualTo(original[i].Value).Within(1e-9),
                $"Value mismatch at index {i}");
            Assert.That(loaded[i].Role, Is.EqualTo(original[i].Role),
                $"Role mismatch at index {i}");
        }
    }

    [Test]
    public void FromJson_ReadOnlyMetadataIsPreservedByConcreteType()
    {
        // Metadata (Name, Symbol, Formula, NCalc) is not stored in JSON;
        // it comes from the concrete class. Verify it survives the round-trip.
        var json   = VariableSerializer.ToJson([new Addendum { Value = 1.0 }]);
        var loaded = VariableSerializer.FromJson(json);

        var addendum = loaded[0];
        Assert.That(addendum.Name,    Is.EqualTo("Addendum"));
        Assert.That(addendum.Symbol,  Is.EqualTo(@"h_{a}"));
        Assert.That(addendum.NCalc,   Is.EqualTo(@"1.00 * [m]"));
        Assert.That(addendum.Formula, Is.EqualTo(@"1.00m"));
    }

    [Test]
    public void FromJson_ThrowsOnUnknownTypeName()
    {
        const string badJson = """[{"TypeName":"UnknownType","Value":1.0,"Role":"Calculated"}]""";
        Assert.Throws<InvalidOperationException>(() => VariableSerializer.FromJson(badJson));
    }

    // ── Save / Load (file round-trip) ──────────────────────────────────────────

    [Test]
    public void SaveAndLoad_RoundTripsToFile()
    {
        var original = BuildSampleVariables();
        var path     = Path.GetTempFileName();

        try
        {
            VariableSerializer.Save(original, path);

            // Verify the file contains readable JSON
            var rawJson = File.ReadAllText(path);
            Assert.That(rawJson, Does.Contain("Addendum"));

            var loaded = VariableSerializer.Load(path);

            Assert.That(loaded.Count, Is.EqualTo(original.Count));
            Assert.That(loaded[0], Is.InstanceOf<Addendum>());
            Assert.That(loaded[0].Value, Is.EqualTo(2.5).Within(1e-9));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void SavedJson_IsHumanReadable()
    {
        var path = Path.GetTempFileName();
        try
        {
            VariableSerializer.Save([new Module { Value = 3.0 }], path);
            var json = File.ReadAllText(path);

            // Indented JSON will have newlines
            Assert.That(json, Does.Contain(Environment.NewLine).Or.Contain("\n"));
            ConsoleIO.WriteLine(json);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

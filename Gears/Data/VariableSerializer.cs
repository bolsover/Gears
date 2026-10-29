using System;
using System.Collections.Generic;
using System.IO;
using Bolsover.Gears.Model.Variable;
using Newtonsoft.Json;

namespace Bolsover.Gears.Data;

/// <summary>
/// Illustrates how a heterogeneous collection of <see cref="IVariable"/> implementations
/// can be round-tripped through JSON.
///
/// Because <see cref="IVariable"/> properties such as Name, Symbol, Formula and NCalc are
/// read-only constants defined by each concrete class, only the mutable state
/// (<c>value</c>, <c>role</c>) plus a <c>typeName</c> discriminator are stored in JSON.
/// On load the discriminator is used to instantiate the correct concrete type via a
/// static registry, after which <c>Value</c> and <c>Role</c> are applied.
///
/// JSON format (array of entries):
/// <code>
/// [
///   { "typeName": "Addendum",  "value": 2.5,  "role": "Calculated" },
///   { "typeName": "Module",    "value": 2.0,  "role": "Design"     }
/// ]
/// </code>
/// </summary>
public static class VariableSerializer
{
    // Registry maps the simple class name to a factory that creates a default instance.
    private static readonly Dictionary<string, Func<IVariable>> Registry = new()
    {
        [nameof(Addendum)]        = () => new Addendum(),
        [nameof(BaseDiameter)]    = () => new BaseDiameter(),
        [nameof(Dedendum)]        = () => new Dedendum(),
        [nameof(Module)]          = () => new Module(),
        [nameof(OutsideDiameter)] = () => new OutsideDiameter(),
        [nameof(PitchDiameter)]   = () => new PitchDiameter(),
        [nameof(PressureAngle)]   = () => new PressureAngle(),
        [nameof(RootDiameter)]    = () => new RootDiameter(),
        [nameof(Teeth)]           = () => new Teeth(),
    };

    // DTO used only for serialization / deserialization.
    private record VariableDto(string TypeName, double Value, string Role);

    /// <summary>Serializes <paramref name="variables"/> to an indented JSON string.</summary>
    public static string ToJson(IEnumerable<IVariable> variables)
    {
        if (variables == null) throw new ArgumentNullException(nameof(variables));

        var dtos = new List<VariableDto>();
        foreach (var v in variables)
            dtos.Add(new VariableDto(v.GetType().Name, v.Value, v.Role.ToString()));

        return JsonConvert.SerializeObject(dtos, Formatting.Indented);
    }

    /// <summary>
    /// Deserializes a JSON string produced by <see cref="ToJson"/> back to a list of
    /// <see cref="IVariable"/> instances, each holding the correct concrete type.
    /// </summary>
    public static List<IVariable> FromJson(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));

        var dtos = JsonConvert.DeserializeObject<List<VariableDto>>(json)
                   ?? throw new InvalidOperationException("JSON did not deserialize to a list.");

        var result = new List<IVariable>(dtos.Count);
        foreach (var dto in dtos)
        {
            if (!Registry.TryGetValue(dto.TypeName, out var factory))
                throw new InvalidOperationException($"Unknown variable type '{dto.TypeName}'.");

            var variable = factory();
            variable.Value = dto.Value;
            variable.Role  = Enum.Parse<VariableRole>(dto.Role);
            result.Add(variable);
        }

        return result;
    }

    /// <summary>Writes the JSON representation of <paramref name="variables"/> to <paramref name="filePath"/>.</summary>
    public static void Save(IEnumerable<IVariable> variables, string filePath)
        => File.WriteAllText(filePath, ToJson(variables));

    /// <summary>Loads <see cref="IVariable"/> instances from a JSON file written by <see cref="Save"/>.</summary>
    public static List<IVariable> Load(string filePath)
        => FromJson(File.ReadAllText(filePath));
}

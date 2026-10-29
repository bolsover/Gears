using System.Collections.Generic;
using Bolsover.Gears.Model.GearModels;
using Bolsover.Gears.Model.Variable;

namespace Bolsover.Gears.Model.GearBox;

public class PrototypeGear : IGear
{
    public string Id { get; set; }
    public GearTypes GearType { get; }
    public CalculationMode Mode { get; set; }
    public Dictionary<string, IVariable> GearVariables { get; set; }
    public IMesh Mesh { get; set; }
    public void Calculate()
    {
        GearCalc.Calculate(this);
    }

    public void Validate()
    {
        throw new System.NotImplementedException();
    }
}
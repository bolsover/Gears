using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Model.GearBox;
using Bolsover.Gears.Model.Variable;
using NCalc;

namespace Bolsover.Gears.Model.GearModels;

public class ExternalGear : IGear
{
    public string Id { get; set; }
    public GearTypes GearType { get; }
    public CalculationMode Mode { get; set; }
    public Dictionary<string, IVariable> GearVariables { get; set; } = new();
    public IMesh Mesh { get; set; }

    public ExternalGear()
    {
        GearVariables.Add("Teeth", new Teeth());
        GearVariables.Add("Module", new Module());
        GearVariables.Add("PressureAngle", new PressureAngle());
        GearVariables.Add("PitchDiameter", new PitchDiameter());
        GearVariables.Add("OutsideDiameter", new OutsideDiameter());
        GearVariables.Add("BaseDiameter", new BaseDiameter());
        GearVariables.Add("RootDiameter", new RootDiameter());
        GearVariables.Add("Dedendum", new Dedendum());
        GearVariables.Add("Addendum", new Addendum());
    }


    public void Calculate()
    {
        GearCalc.Calculate(this);
     
    }

    public void Validate()
    {
        throw new NotImplementedException();
    }

 
}
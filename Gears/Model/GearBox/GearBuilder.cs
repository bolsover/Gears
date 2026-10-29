using Bolsover.Gears.Model.GearModels;

namespace Bolsover.Gears.Model.GearBox;

public class GearBuilder
{
        public IGear Build(GearTypes type)
        {
            return type switch
            {
                GearTypes.External => new ExternalGear( ),
                // GearTypes.Prototype => new PrototypeGear { GearType = type, Mode = mode },
                // _ => throw new ArgumentException($"Unsupported gear type: {type}")
            };
        }
}
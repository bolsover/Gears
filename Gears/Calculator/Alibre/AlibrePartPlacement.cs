using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Works out where each exported part sits in an Alibre assembly, and converts a placement to the values of an Alibre transformation. Needs no Alibre.</summary>
    internal static class AlibrePartPlacement
    {
        internal static Transform3D GetBeltPlacement(Shaft driverShaft, ShaftMount driverMount)
        {
            if (driverShaft == null) throw new ArgumentNullException(nameof(driverShaft));
            if (driverMount == null) throw new ArgumentNullException(nameof(driverMount));

            return driverShaft.Placement.ToTransform().Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, driverMount.AxialOffset)));
        }

        internal static Transform3D GetShaftPlacement(Shaft shaft)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));

            return shaft.Placement.ToTransform()
                .Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, ShaftPartSketchBuilder.GetStart(shaft))));
        }

        internal static Transform3D GetPartPlacement(Shaft shaft, ShaftMount mount)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (mount == null) throw new ArgumentNullException(nameof(mount));

            if (mount.Component is BevelGearComponent bevel)
            {
                return shaft.Placement.ToTransform().Compose(bevel.GetLocalMotion(mount.PhaseRad)).Compose(GetBevelPartFrame(bevel, mount.AxialOffset));
            }

            // A herringbone part or a pulley is made about its sketch plane (half the face width on each side), so that plane is already the mid-plane.
            var centred = AlibreAssemblyPartSelector.IsHerringbone(mount.Component) || mount.Component is HtdPulleyComponent || mount.Component is SprocketComponent
                          || mount.Component is CycloidalGearComponent;
            var start = centred ? mount.AxialOffset : mount.AxialOffset - mount.Component.FaceWidth / 2.0;
            var placement = shaft.Placement.ToTransform().Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, start)));
            return placement.Compose(mount.Component.GetLocalMotion(mount.PhaseRad));
        }

        internal static Transform3D GetBevelPartFrame(BevelGearComponent gear, double axialOffset)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            var phase = Math.PI / gear.Teeth;
            var c = Math.Cos(phase);
            var s = Math.Sin(phase);
            return Transform3D.FromFrame(new Vector3D(0.0, 0.0, axialOffset + gear.BlankAxialOffset), new Vector3D(0.0, 0.0, -1.0),
                new Vector3D(c, s, 0.0), new Vector3D(s, -c, 0.0));
        }

        internal static double[] ToAlibreMatrix(Transform3D transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            var x = transform.TransformDirection(Vector3D.UnitX);
            var y = transform.TransformDirection(Vector3D.UnitY);
            var z = transform.TransformDirection(Vector3D.UnitZ);
            var t = transform.Translation;
            var k = SingleToothSketchBuilder.MmToAlibreUnits;
            return new[]
            {
                x.X, x.Y, x.Z, 0.0,
                y.X, y.Y, y.Z, 0.0,
                z.X, z.Y, z.Z, 0.0,
                t.X * k, t.Y * k, t.Z * k, 1.0
            };
        }
    }
}

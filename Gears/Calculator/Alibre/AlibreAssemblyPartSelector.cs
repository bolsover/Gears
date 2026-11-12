using System;
using System.Collections.Generic;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Chooses which gears, shafts and belts of a train go into an Alibre export, and names the parts. Needs no Alibre.</summary>
    internal static class AlibreAssemblyPartSelector
    {
        internal static IReadOnlyList<KeyValuePair<GearComponent, Transform3D>> GetAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<GearComponent, Transform3D>>();
            foreach (var shaft in train.Shafts)
            {
                foreach (var mount in shaft.Mounts)
                {
                    if (!IsIncluded(selection, mount.Component)) continue;
                    if (!IsExportable(mount.Component))
                    {
                        throw new InvalidOperationException(
                            $"Gear '{mount.Component.Id}' cannot be exported to Alibre yet; only involute gears, racks, worms, worm wheels, bevel gears and HTD pulleys can.");
                    }

                    parts.Add(new KeyValuePair<GearComponent, Transform3D>(mount.Component, AlibrePartPlacement.GetPartPlacement(shaft, mount)));
                }
            }

            return parts;
        }

        internal static void CheckPartCount(int count)
        {
            if (count < AlibreGearExporter.MinimumAssemblyParts)
            {
                throw new InvalidOperationException(
                    $"An assembly needs at least {AlibreGearExporter.MinimumAssemblyParts} parts; there are {count}. Use the single part export instead.");
            }
        }

        internal static bool IsIncluded(IReadOnlyCollection<object> selection, object item)
        {
            if (selection == null) return true;
            foreach (var candidate in selection)
            {
                if (ReferenceEquals(candidate, item)) return true;
            }

            return false;
        }

        internal static IReadOnlyList<KeyValuePair<Shaft, Transform3D>> GetShaftAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<Shaft, Transform3D>>();
            foreach (var shaft in train.Shafts)
            {
                if (!IsIncluded(selection, shaft)) continue;
                if (PlanetaryGearSet.IsCarrier(shaft) || ShaftPartSketchBuilder.IsExportable(shaft))
                {
                    parts.Add(new KeyValuePair<Shaft, Transform3D>(shaft, AlibrePartPlacement.GetShaftPlacement(shaft)));
                }
            }

            return parts;
        }

        internal static IReadOnlyList<KeyValuePair<TimingBelt, Transform3D>> GetBeltAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<TimingBelt, Transform3D>>();
            foreach (var belt in train.Belts)
            {
                if (!IsIncluded(selection, belt)) continue;
                var mount = train.FindMount(belt.Driver, out var shaft);
                if (mount == null) continue;
                parts.Add(new KeyValuePair<TimingBelt, Transform3D>(belt, AlibrePartPlacement.GetBeltPlacement(shaft, mount)));
            }

            return parts;
        }

        internal static bool IsExportable(GearComponent component)
        {
            return component is InvoluteGearComponent || component is RackComponent || component is WormComponent || component is WormWheelComponent
                || component is BevelGearComponent || component is HtdPulleyComponent || component is CycloidalGearComponent;
        }

        internal static bool IsHerringbone(GearComponent component)
        {
            return (component is InvoluteGearComponent gear && gear.IsHerringbone && gear.IsHelical)
                   || (component is RackComponent rack && rack.IsHerringbone && rack.IsHelical);
        }

        internal static string UniqueName(string name, ISet<string> used)
        {
            var candidate = name;
            for (var i = 2; !used.Add(candidate); i++) candidate = name + "_" + i;
            return candidate;
        }
    }
}

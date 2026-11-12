using System;
using System.Collections.Generic;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Gui.Presenters
{
    /// <summary>Builds the speed and ratio report of a planetary stage that is shown in the gear train editor.</summary>
    internal static class PlanetaryReportBuilder
    {
        /// <summary>Reports the speeds of every shaft and gear of <paramref name="assembly"/>, the posed assembly of <paramref name="set"/>.</summary>
        public static GearTrainReport Build(PlanetaryGearSet set, PlanetaryMember planetaryInput, double inputRpm, GearAssembly assembly)
        {
            var output = (PlanetaryMember)(3 - (int)planetaryInput - (int)set.Fixed);
            var speeds = set.Analyze(planetaryInput, inputRpm, output);

            double SpeedOf(Shaft shaft)
            {
                if (shaft.Id == set.Id + ".SunShaft") return speeds.SunRpm;
                if (shaft.Id == set.Id + ".RingShaft") return speeds.RingRpm;
                if (shaft.Id == set.Id + ".Carrier") return speeds.CarrierRpm;
                return speeds.PlanetRpm;
            }

            var shaftRows = new List<ShaftSpeedRow>();
            foreach (var shaft in assembly.Shafts)
            {
                var speed = SpeedOf(shaft);
                double? ratio = Math.Abs(speed) > 1e-12 ? GearPrecision.Round(inputRpm / speed) : (double?)null;
                shaftRows.Add(new ShaftSpeedRow(shaft.Id, speed, ratio));
            }

            var gearRows = new List<GearSpeedRow>();
            foreach (var shaft in assembly.Shafts)
            {
                foreach (var mount in shaft.Mounts)
                {
                    var gear = mount.Component;
                    var speed = SpeedOf(shaft);
                    var role = gear.Id == set.Id + ".Sun" ? "Sun" : gear.Id == set.Id + ".Ring" ? "Ring" : "Planet";
                    gearRows.Add(new GearSpeedRow(
                        gear.Id,
                        shaft.Id,
                        role,
                        gear.ToothCount,
                        GearPrecision.Round(gear.PitchDiameter),
                        speed,
                        GearPrecision.Round(Math.PI * gear.PitchDiameter * Math.Abs(speed) / 60000.0)));
                }
            }

            // Stage ratios are relative to the carrier, as for a gear train with a fixed carrier.
            var stages = new List<GearStageRow>
            {
                new GearStageRow(set.Id + ".Sun", set.Id + ".Planet1", GearPrecision.Round((double)set.PlanetTeeth / set.SunTeeth),
                    GearPrecision.Round(-(double)set.SunTeeth / set.PlanetTeeth), set.CentreDistance),
                new GearStageRow(set.Id + ".Planet1", set.Id + ".Ring", GearPrecision.Round((double)set.RingTeeth / set.PlanetTeeth),
                    GearPrecision.Round((double)set.PlanetTeeth / set.RingTeeth), set.CentreDistance)
            };

            var outputShaftId = set.Id + (output == PlanetaryMember.Sun ? ".SunShaft" : output == PlanetaryMember.Ring ? ".RingShaft" : ".Carrier");
            var inputShaftId = set.Id + (planetaryInput == PlanetaryMember.Sun ? ".SunShaft" : planetaryInput == PlanetaryMember.Ring ? ".RingShaft" : ".Carrier");
            return new GearTrainReport(inputShaftId, inputRpm, outputShaftId, speeds.OverallRatio, shaftRows, gearRows, stages, new List<string>());
        }
    }
}

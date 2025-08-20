using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator.Strategies
{
    internal class ExternalAutomaticToothProfileComputation : IToothProfileComputation
    {
        public ToothProfile Calculate(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            gear.Calculate();

            var rootDiameter = GetValueOrDefault(gear, VariableNames.RootDiameter);
            var baseDiameter = GetValueOrDefault(gear, VariableNames.BaseDiameter);
            var pitchDiameter = GetValueOrDefault(gear, VariableNames.PitchDiameter);
            var outsideDiameter = GetValueOrDefault(gear, VariableNames.OutsideDiameter);

            var rRoot = rootDiameter / 2.0;
            var rBase = baseDiameter / 2.0;
            var rPitch = pitchDiameter / 2.0;
            var rOutside = outsideDiameter / 2.0;

            var profile = new ToothProfile();
            var origin = new GearPoint(0, 0);

            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.RootCircle, origin, rRoot, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.BaseCircle, origin, rBase, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.PitchCircle, origin, rPitch, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.OutsideCircle, origin, rOutside, true);

            var flankStart = new GearPoint(rRoot, 0);
            var flankEnd = new GearPoint(rOutside, 0);
            var flank = new Feature(FeatureType.StraightLine, new List<GearPoint> { flankStart, flankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlank, flank);

            var mirrorFlankStart = new GearPoint(-rRoot, 0);
            var mirrorFlankEnd = new GearPoint(-rOutside, 0);
            var mirrorFlank = new Feature(FeatureType.StraightLine, new List<GearPoint> { mirrorFlankStart, mirrorFlankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlankMirror, mirrorFlank);

            return profile;
        }

        private static double GetValueOrDefault(Gear gear, VariableNames name)
        {
            var variable = gear.GetVariable(name);
            return variable?.Value ?? 0.0;
        }
    }
}

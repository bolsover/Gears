using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Calculator.Strategies
{
    internal class InternalAutomaticToothProfileComputation : IToothProfileComputation
    {
        public ToothProfile Calculate(Gear gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));
            gear.Calculate();

            var rootDiameter = GetValueOrDefault(gear, VariableIds.RootDiameter);
            var baseDiameter = GetValueOrDefault(gear, VariableIds.BaseDiameter);
            var pitchDiameter = GetValueOrDefault(gear, VariableIds.PitchDiameter);
            var outsideDiameter = GetValueOrDefault(gear, VariableIds.OutsideDiameter);

            var rRoot = rootDiameter / 2.0;
            var rBase = baseDiameter / 2.0;
            var rPitch = pitchDiameter / 2.0;
            var rOutside = outsideDiameter / 2.0;

            var profile = new ToothProfile();
            var origin = new GearPoint(0, 0);

            // For internal gears, the "outside" is inside the ring. We keep the same references for now,
            // but the placeholder flank is drawn along the negative X direction to hint different geometry.
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.RootCircle, origin, rRoot, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.BaseCircle, origin, rBase, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.PitchCircle, origin, rPitch, true);
            ToothProfileFeatureBuilder.AddCircle(profile, ToothFeatureKey.OutsideCircle, origin, rOutside, true);

            var flankStart = new GearPoint(-rRoot, 0);
            var flankEnd = new GearPoint(-rOutside, 0);
            var flank = new Feature(FeatureType.StraightLine, new List<GearPoint> { flankStart, flankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlank, flank);

            var mirrorFlankStart = new GearPoint(rRoot, 0);
            var mirrorFlankEnd = new GearPoint(rOutside, 0);
            var mirrorFlank = new Feature(FeatureType.StraightLine, new List<GearPoint> { mirrorFlankStart, mirrorFlankEnd });
            profile.AddFeature(ToothFeatureKey.ToothFlankMirror, mirrorFlank);

            return profile;
        }

        private static double GetValueOrDefault(Gear gear, VariableIds id)
        {
            var variable = gear.GetVariable(id);
            return variable?.Value ?? 0.0;
        }
    }
}

using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// The registration of the rollers of a <see cref="RollerChain"/> in the tooth gaps of its sprockets. Positions along the chain path are
    /// counted in chain pitches: a straight run of length L is L / p pitches, and the wrap round a sprocket with z teeth over an angle a is
    /// a / (2 pi / z) pitches, because a roller advances by one tooth step round the sprocket however the pitch circle compares with the
    /// polygon of the chain. The path is <c>U</c> pitches long and the chain has <see cref="RollerChain.LinkCount"/> rollers, so the rollers are
    /// <see cref="Spacing"/> = U / links pitches apart (close to one).
    /// <para>
    /// Convention: the path runs anticlockwise from the lower tangent point of the driven sprocket (round the driven sprocket, along the upper run,
    /// round the driver, along the lower run). A roller is put in the tooth gap of the driver that is nearest to the middle of the wrap round the
    /// driver, and the driven sprocket is clocked, by an offset of at most half a tooth step from the phase it has when it simply follows the
    /// driver at the ratio of the teeth, so that the roller nearest to the middle of the wrap round it sits in one of its gaps. Because
    /// positions are counted in tooth steps the offset does not depend on the driver phase and the driven sprocket follows the driver exactly at
    /// the ratio of the teeth.
    /// </para>
    /// </summary>
    public sealed class ChainRegistration
    {
        private ChainRegistration()
        {
        }

        /// <summary>Distance between rollers along the path, in pitches.</summary>
        public double Spacing { get; private set; }

        /// <summary>Position along the path (pitches, from the lower tangent point of the driven sprocket) of roller 0, in [0, <see cref="Spacing"/>).</summary>
        public double FirstRollerPosition { get; private set; }

        /// <summary>Registration offset (radians, in [-step/2, step/2) of the driven sprocket): the driven phase is the driver phase over the gear ratio plus this.</summary>
        public double DrivenOffsetRad { get; private set; }

        /// <summary>Length of the path in pitches.</summary>
        public double PathPitches { get; private set; }

        public double DrivenArcPitches { get; internal set; }

        public double RunPitches { get; internal set; }

        public double DriverArcPitches { get; internal set; }

        /// <summary>Registers the chain for the given total rotation of the driver sprocket (shaft rotation plus mount phase offset).</summary>
        public static ChainRegistration Compute(RollerChain chain, double driverPhaseRad)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));

            var radiusA = chain.Driver.PitchDiameter / 2.0;
            var radiusB = chain.Driven.PitchDiameter / 2.0;
            var distance = chain.CentreDistance;
            var theta = chain.CentreLineAngleDeg * Math.PI / 180.0;
            var pitch = chain.Profile.Pitch;
            var stepA = 2.0 * Math.PI / chain.Driver.Teeth;
            var stepB = 2.0 * Math.PI / chain.Driven.Teeth;

            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx);
            var lower = Math.Atan2(-ny, nx);
            var wrapA = lower + 2.0 * Math.PI - upper;

            var drivenArc = (upper - lower) / stepB;
            var run = distance * ny / pitch;
            var driverArc = wrapA / stepA;
            var total = drivenArc + 2.0 * run + driverArc;

            var count = chain.LinkCount;
            if (count < 2) throw new InvalidOperationException($"Chain '{chain.Id}' is too short to have rollers.");
            var spacing = total / count;

            // The driver gap nearest to the middle of the wrap round the driver (angles in the driver shaft's frame).
            var middleAngle = theta + upper + wrapA / 2.0;
            var gap = Math.Floor((middleAngle - driverPhaseRad) / stepA + 0.5);
            var gapAngle = driverPhaseRad + gap * stepA;
            var reference = drivenArc + run + (gapAngle - theta - upper) / stepA;
            var first = reference - spacing * Math.Floor(reference / spacing);
            if (first >= spacing) first = 0.0;

            // The driven sprocket is registered on the roller that is a fixed whole number of rollers away from the registration roller on the
            // driver: the one nearest to the middle of the wrap round the driven sprocket. Moving the driver by an angle moves the rollers by
            // that angle over the tooth step of the driver, and on the driven sprocket that is the same angle over the ratio of the teeth.
            var driverMiddle = drivenArc + run + driverArc / 2.0;
            var rollersBetween = Math.Floor((drivenArc / 2.0 - driverMiddle) / spacing + 0.5);
            var referenceAtZeroPhase = drivenArc + run + (-theta - upper) / stepA;
            var rollerAngle = theta + lower + (referenceAtZeroPhase + rollersBetween * spacing) * stepB;

            var offset = rollerAngle - stepB * Math.Floor(rollerAngle / stepB + 0.5);

            return new ChainRegistration
            {
                Spacing = spacing,
                FirstRollerPosition = first,
                DrivenOffsetRad = offset,
                PathPitches = total,
                DrivenArcPitches = drivenArc,
                RunPitches = run,
                DriverArcPitches = driverArc
            };
        }
    }

    /// <summary>
    /// Where the rollers of a <see cref="RollerChain"/> are, in the frame of the driver sprocket's shaft (the driver on the axis, the driven
    /// sprocket at the centre distance along the line of centres at the angle of the chain). The rollers are equally spaced along the path
    /// round the two pitch circles and registered on the tooth gaps by <see cref="ChainRegistration"/>.
    /// </summary>
    public sealed class RollerChainLayout
    {
        private RollerChainLayout(IReadOnlyList<GearPoint> rollers, ChainRegistration registration)
        {
            Rollers = rollers;
            Registration = registration;
        }

        /// <summary>The centres of the rollers, roller 0 first; link k joins roller k and roller k + 1 (the last joins roller n - 1 and roller 0).</summary>
        public IReadOnlyList<GearPoint> Rollers { get; }

        /// <summary>Distance between rollers along the path, in chain pitches (close to one).</summary>
        public double Spacing => Registration.Spacing;

        /// <summary>The registration of the chain on the sprockets for the driver phase of the layout.</summary>
        public ChainRegistration Registration { get; }

        /// <summary>Registers the chain for the given total rotation of the driver sprocket (shaft rotation plus mount phase offset).</summary>
        public static ChainRegistration Register(RollerChain chain, double driverPhaseRad) => ChainRegistration.Compute(chain, driverPhaseRad);

        public static RollerChainLayout Build(RollerChain chain, double driverPhaseRad = 0.0)
        {
            if (chain == null) throw new ArgumentNullException(nameof(chain));
            chain.Validate();

            var radiusA = chain.Driver.PitchDiameter / 2.0;
            var radiusB = chain.Driven.PitchDiameter / 2.0;
            var distance = chain.CentreDistance;
            var theta = chain.CentreLineAngleDeg * Math.PI / 180.0;
            var cosTheta = Math.Cos(theta);
            var sinTheta = Math.Sin(theta);
            var stepA = 2.0 * Math.PI / chain.Driver.Teeth;
            var stepB = 2.0 * Math.PI / chain.Driven.Teeth;

            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx);
            var lower = Math.Atan2(-ny, nx);

            var registration = ChainRegistration.Compute(chain, driverPhaseRad);
            var drivenArc = registration.DrivenArcPitches;
            var run = registration.RunPitches;
            var driverArc = registration.DriverArcPitches;
            var count = chain.LinkCount;

            GearPoint Rotate(double x, double y) => new GearPoint(x * cosTheta - y * sinTheta, x * sinTheta + y * cosTheta);

            var drivenUpper = new[] { distance + radiusB * Math.Cos(upper), radiusB * Math.Sin(upper) };
            var driverUpper = new[] { radiusA * Math.Cos(upper), radiusA * Math.Sin(upper) };
            var driverLower = new[] { radiusA * Math.Cos(lower), radiusA * Math.Sin(lower) };
            var drivenLower = new[] { distance + radiusB * Math.Cos(lower), radiusB * Math.Sin(lower) };

            // The point at a position along the path, in pitches.
            GearPoint PointAt(double u)
            {
                if (u <= drivenArc)
                {
                    var angle = lower + u * stepB;
                    return Rotate(distance + radiusB * Math.Cos(angle), radiusB * Math.Sin(angle));
                }

                u -= drivenArc;
                if (u <= run)
                {
                    var t = run > 1e-12 ? u / run : 0.0;
                    return Rotate(drivenUpper[0] + (driverUpper[0] - drivenUpper[0]) * t, drivenUpper[1] + (driverUpper[1] - drivenUpper[1]) * t);
                }

                u -= run;
                if (u <= driverArc)
                {
                    var angle = upper + u * stepA;
                    return Rotate(radiusA * Math.Cos(angle), radiusA * Math.Sin(angle));
                }

                u -= driverArc;
                var v = run > 1e-12 ? Math.Min(1.0, u / run) : 0.0;
                return Rotate(driverLower[0] + (drivenLower[0] - driverLower[0]) * v, driverLower[1] + (drivenLower[1] - driverLower[1]) * v);
            }

            var rollers = new List<GearPoint>(count);
            for (var k = 0; k < count; k++) rollers.Add(PointAt(registration.FirstRollerPosition + k * registration.Spacing));

            return new RollerChainLayout(rollers, registration);
        }
    }
}

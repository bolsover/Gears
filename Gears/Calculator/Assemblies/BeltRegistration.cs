using System;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// The registration of the teeth of a <see cref="TimingBelt"/> on the grooves of its pulleys. A belt tooth is the negative of a groove,
    /// so the centre of a tooth must lie on the centre of a groove: the tooth stands in the groove cavity, not on a land.
    /// <para>
    /// Convention: the path of the belt runs anticlockwise from the lower tangent point of the driven pulley (round the driven pulley, along
    /// the upper run, round the driver, along the lower run). A belt tooth is put on the centre of the driver groove that is nearest to the
    /// middle of the wrap round the driver, so the teeth are in register with the driver at the middle of its wrap. Groove k of a pulley is
    /// at the angle <c>phase + k * 2 pi / Teeth</c>. The driven pulley is then clocked, by a bounded offset <see cref="DrivenOffsetRad"/>
    /// (never more than half a groove step) from the phase it would have if it simply followed the driver at the ratio of the grooves, so that the belt tooth
    /// nearest to the middle of the wrap round it lies on one of its grooves.
    /// </para>
    /// <para>
    /// The belt has a whole number of teeth, <see cref="TimingBelt.BeltTeeth"/>, which is the path length over the pitch rounded, so the spacing
    /// along the path differs from the pitch by at most pitch / (2 * teeth). A tooth n teeth away from the registration tooth is therefore
    /// out by at most n times that: below a quarter of the pitch over half a wrap, and exactly in register everywhere when the path is a whole
    /// number of pitches.
    /// </para>
    /// <para>
    /// The result is a pure function of the belt and the driver phase, there is no state. When the driver turns through a groove the registration
    /// groove changes: the set of teeth then jumps by (pitch - spacing) along the belt. The driven pulley is registered on the tooth a fixed
    /// number of teeth away from the registration tooth of the driver, so <see cref="DrivenOffsetRad"/> is the same for every driver phase and
    /// the driven pulley follows the driver exactly at the ratio of the grooves. The offset is reduced modulo the groove step of the driven
    /// pulley; when it sits right on the edge of that range rounding can change it by a whole groove step, which leaves the pulley looking the same.
    /// </para>
    /// </summary>
    public sealed class BeltRegistration
    {
        private BeltRegistration()
        {
        }

        /// <summary>Distance between teeth along the belt path (mm): the path length over the number of teeth.</summary>
        public double Spacing { get; private set; }

        /// <summary>
        /// Position along the belt path (mm, from the lower tangent point of the driven pulley) of the first tooth, in [0, <see cref="Spacing"/>).
        /// Teeth are at this position plus whole spacings.
        /// </summary>
        public double FirstToothPosition { get; private set; }

        /// <summary>
        /// Phase (radians, in [0, step)) the driven pulley must have, modulo its groove step, to have a groove under the belt tooth nearest to
        /// the middle of its wrap.
        /// </summary>
        public double DrivenPhaseRad { get; private set; }

        /// <summary>
        /// Registration offset C (radians, in [-step/2, step/2) of the driven pulley): the driven phase is the driver phase over the gear ratio
        /// plus this offset, modulo the groove step. The driver term is not reduced, so the driven rotation stays unwrapped. The offset does not
        /// depend on the driver phase.
        /// </summary>
        public double DrivenOffsetRad { get; private set; }

        /// <summary>Registers the belt for the given total rotation of the driver pulley (shaft rotation plus mount phase offset).</summary>
        public static BeltRegistration Compute(TimingBelt belt, double driverPhaseRad)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));

            var radiusA = belt.Driver.PitchDiameter / 2.0;
            var radiusB = belt.Driven.PitchDiameter / 2.0;
            var distance = belt.CentreDistance;
            var theta = belt.CentreLineAngleDeg * Math.PI / 180.0;

            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx);
            var lower = Math.Atan2(-ny, nx);
            var run = distance * ny;

            var drivenArc = radiusB * (upper - lower);
            var wrapA = lower + 2.0 * Math.PI - upper;
            var total = drivenArc + 2.0 * run + radiusA * wrapA;

            var count = belt.BeltTeeth;
            if (count < 2) throw new InvalidOperationException($"Belt '{belt.Id}' is too short to have teeth.");
            var spacing = total / count;
            var stepA = 2.0 * Math.PI / belt.Driver.Teeth;
            var stepB = 2.0 * Math.PI / belt.Driven.Teeth;

            // The driver groove nearest to the middle of the wrap round the driver (angles in the driver shaft's frame).
            var middleAngle = theta + upper + wrapA / 2.0;
            var groove = Math.Floor((middleAngle - driverPhaseRad) / stepA + 0.5);
            var grooveAngle = driverPhaseRad + groove * stepA;
            var reference = drivenArc + run + (grooveAngle - theta - upper) * radiusA;
            var first = reference - spacing * Math.Floor(reference / spacing);
            if (first >= spacing) first = 0.0;

            // The driven pulley is registered on the belt tooth that is a fixed whole number of teeth away from the registration tooth on the
            // driver: the one nearest to the middle of the wrap round the driven pulley. The number of teeth does not depend on the driver phase,
            // so the registration tooth moves with the driver by exactly rA * d(phase) along the belt, and by one pitch when the registration
            // groove changes. On the driven pulley that is exactly 1 / ratio of the phase and one groove step respectively: the offset C below is
            // the same for every driver phase (modulo the groove step), the groove of the driver that is used cancels out of it.
            var driverMiddlePosition = drivenArc + run + wrapA * radiusA / 2.0;
            var teethBetween = Math.Floor((drivenArc / 2.0 - driverMiddlePosition) / spacing + 0.5);
            var referenceAtZeroPhase = drivenArc + run + (-theta - upper) * radiusA;
            var toothAngle = theta + lower + (referenceAtZeroPhase + teethBetween * spacing) / radiusB;

            var offset = toothAngle;
            offset -= stepB * Math.Floor(offset / stepB + 0.5);
            var drivenPhase = driverPhaseRad / belt.GearRatio + offset;
            drivenPhase -= stepB * Math.Floor(drivenPhase / stepB);

            return new BeltRegistration
            {
                Spacing = spacing,
                FirstToothPosition = first,
                DrivenPhaseRad = drivenPhase,
                DrivenOffsetRad = offset
            };
        }
    }
}

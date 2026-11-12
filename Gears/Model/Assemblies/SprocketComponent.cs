using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A sprocket for an ISO 606 (DIN 8187) B-series simplex roller chain. It has no meshing partner in the gear train: a roller chain
    /// connects sprockets.
    /// <para>
    /// Local frame: the sprocket axis is Z, the centre is the origin and the toothed width is centred on the Z = 0 mid-plane. Tooth gap 0
    /// is centred on the +X axis, so a roller seated in it has its centre on the pitch circle on the +X axis. The pitch diameter is
    /// <c>p / sin(180/z)</c> (the chain is a polygon, not a belt) and the root diameter is the pitch diameter minus the roller diameter.
    /// </para>
    /// Every gap is made of the roller seating arc (radius ri, angle alpha, centred on the roller) and on each side a tooth flank arc of
    /// radius re that is tangent to the seating arc and concave towards the gap, ending at the tip diameter. When the flanks of two
    /// neighbouring gaps meet below the tip diameter the tooth is pointed. The tooth form follows ISO 606, see <see cref="ChainProfile"/>.
    /// </summary>
    public class SprocketComponent : GearComponent
    {
        /// <summary>Number of segments of the half seating arc of a gap.</summary>
        public const int SeatArcSteps = 8;

        /// <summary>Number of segments of one tooth flank.</summary>
        public const int FlankSteps = 6;


        private GearGeometry _cachedGeometry;
        private string _cacheKey;

        public SprocketComponent(string id) : base(id)
        {
            Module = Profile.Pitch / Math.PI;
            FaceWidth = Profile.ToothWidth;
        }

        private ChainSeries _series = ChainSeries.B08;

        /// <summary>The chain series the sprocket is made for.</summary>
        public ChainSeries Series
        {
            get => _series;
            set
            {
                // A width that was left at the tooth width of the old series follows the new series; a width that was set stays.
                var followsSeries = Math.Abs(FaceWidth - Profile.ToothWidth) < 1e-9;
                _series = value;
                Module = Profile.Pitch / Math.PI;
                if (followsSeries) FaceWidth = Profile.ToothWidth;
            }
        }

        /// <summary>The dimensions of <see cref="Series"/>.</summary>
        public ChainProfile Profile => ChainProfile.Get(_series);

        /// <summary>Number of teeth.</summary>
        public int Teeth { get; set; } = 19;

        /// <summary>Which end of the ISO 606 tolerance band of the tooth form is drawn.</summary>
        public SprocketToothForm ToothForm { get; set; } = SprocketToothForm.Maximum;

        /// <summary>Diameter of the bore through the sprocket (mm); zero for a solid sprocket, as the shaft of the editor passes through it.</summary>
        public double BoreDiameter { get; set; }

        /// <summary>Diameter of the hub on the +Z side of the sprocket (mm); zero or a value not above the bore for no hub.</summary>
        public double HubDiameter { get; set; }

        /// <summary>Length of the hub beyond the toothed width (mm); zero for no hub.</summary>
        public double HubLength { get; set; }

        /// <summary>True when the sprocket has a hub: a diameter above the bore and a positive length.</summary>
        public bool HasHub => HubLength > 0.0 && HubDiameter > Math.Max(0.0, BoreDiameter);

        /// <summary>True when there is a bore or a hub, which gives the sprocket ring-shaped end faces.</summary>
        public bool HasBoreOrHub => BoreDiameter > 0.0 || HasHub;

        public override int ToothCount => Teeth;

        public override double PitchDiameter => Profile.PitchDiameter(Teeth);

        /// <summary>Diameter at the bottom of the roller seats, pitch diameter minus roller diameter (mm).</summary>
        public double RootDiameter => Profile.RootDiameter(Teeth);

        /// <summary>Tip diameter of the teeth for <see cref="ToothForm"/> (mm).</summary>
        public double OutsideDiameter => Profile.TipDiameter(Teeth, ToothForm);

        /// <summary>Radius of the circle that the roller centres lie on (mm); the pitch circle.</summary>
        public double RollerCentreRadius => PitchDiameter / 2.0;

        /// <summary>Width of the teeth (mm).</summary>
        public double ToothWidth => Profile.ToothWidth;

        /// <summary>True when the sprocket has fewer teeth than the chain makers recommend.</summary>
        public bool IsBelowRecommendedTeeth => Teeth < ChainProfile.RecommendedMinimumTeeth;

        public override string TypeName => "ISO 606 " + Profile.Name + " sprocket";

        public override void GetBodyAxialExtent(out double start, out double end)
        {
            start = -FaceWidth / 2.0;
            end = FaceWidth / 2.0 + (HasHub ? HubLength : 0.0);
        }

        public void Validate()
        {
            if (Teeth < ChainProfile.AbsoluteMinimumTeeth)
            {
                throw new InvalidOperationException($"Sprocket '{Id}': the number of teeth must be at least {ChainProfile.AbsoluteMinimumTeeth}.");
            }

            if (FaceWidth <= 0) throw new InvalidOperationException($"Sprocket '{Id}': the width must be positive.");

            if (BoreDiameter < 0 || HubDiameter < 0 || HubLength < 0)
            {
                throw new InvalidOperationException($"Sprocket '{Id}': the bore and hub dimensions must not be negative.");
            }

            var bottomDiameter = 2.0 * (RollerCentreRadius - Profile.SeatRadius(ToothForm));
            if (BoreDiameter > 0 && BoreDiameter >= bottomDiameter)
            {
                throw new InvalidOperationException(
                    $"Sprocket '{Id}': the bore of {BoreDiameter:0.###} mm must be smaller than the diameter of {bottomDiameter:0.###} mm at the bottom of the roller seats.");
            }
        }

        /// <summary>
        /// The right half of a tooth gap from the bottom of the roller seat to the tip of the tooth, in gap coordinates: x along the radius
        /// from the sprocket centre, y across the gap (the gap is on the x axis, the half is on the +y side). The first point is the bottom
        /// of the seat; the seating arc has <see cref="SeatArcSteps"/> segments and the flank <see cref="FlankSteps"/>. The last point is on
        /// the tip circle, or, when the flank meets the line half way to the next gap first, on that line (<paramref name="pointed"/>). The
        /// points are on the seating arc and the flank arc (see <see cref="FlankCentre"/>); the last one is the exact end of the flank.
        /// </summary>
        public static List<GearPoint> HalfGap(ChainProfile profile, int teeth, SprocketToothForm form, out bool pointed)
        {
            var name = profile.Name;
            if (teeth < ChainProfile.AbsoluteMinimumTeeth)
            {
                throw new InvalidOperationException($"A sprocket for chain {name} needs at least {ChainProfile.AbsoluteMinimumTeeth} teeth.");
            }

            var centreRadius = profile.PitchDiameter(teeth) / 2.0;
            var seatRadius = profile.SeatRadius(form);
            var seatAngle = ChainProfile.SeatAngle(teeth, form);
            var flankRadius = profile.FlankRadius(teeth, form);
            var tipRadius = profile.TipDiameter(teeth, form) / 2.0;
            var halfPitchAngle = Math.PI / teeth;

            // The seating arc ends where the radius from the roller centre has the angle thetaE from the +x axis.
            var thetaE = Math.PI - seatAngle / 2.0;
            var ux = Math.Cos(thetaE);
            var uy = Math.Sin(thetaE);
            var endX = centreRadius + seatRadius * ux;
            var endY = seatRadius * uy;
            if (centreRadius - seatRadius <= 0.0 || endY <= 0.0 || Math.Atan2(endY, endX) >= halfPitchAngle || Math.Sqrt(endX * endX + endY * endY) >= tipRadius)
            {
                throw new InvalidOperationException($"{teeth} teeth are too few for the ISO 606 tooth form of chain {name}.");
            }

            // The flank arc is tangent to the seating arc at its end; its centre is on the line from the roller centre through the end,
            // on the roller side (the flank is concave towards the gap). It is walked by the angle beta from the end.
            var flankCentreX = centreRadius + (seatRadius - flankRadius) * ux;
            var flankCentreY = (seatRadius - flankRadius) * uy;

            double FlankX(double beta) => flankCentreX + flankRadius * Math.Cos(thetaE - beta);
            double FlankY(double beta) => flankCentreY + flankRadius * Math.Sin(thetaE - beta);

            double Overshoot(double beta)
            {
                var x = FlankX(beta);
                var y = FlankY(beta);
                return Math.Max(Math.Sqrt(x * x + y * y) / tipRadius - 1.0, Math.Atan2(y, x) / halfPitchAngle - 1.0);
            }

            var betaMax = thetaE - 0.15;
            const int scan = 2000;
            var found = -1;
            for (var i = 1; i <= scan; i++)
            {
                if (Overshoot(betaMax * i / scan) >= 0.0)
                {
                    found = i;
                    break;
                }
            }

            if (found < 0)
            {
                throw new InvalidOperationException($"The ISO 606 tooth flank of chain {name} with {teeth} teeth does not reach the tip circle.");
            }

            var low = betaMax * (found - 1) / scan;
            var high = betaMax * found / scan;
            for (var i = 0; i < 60; i++)
            {
                var middle = (low + high) / 2.0;
                if (Overshoot(middle) >= 0.0) high = middle;
                else low = middle;
            }

            // The flank stops where it first reaches the tip circle, or, for a pointed tooth, the line half way to the next gap.
            var betaEnd = high;
            var tipX = FlankX(betaEnd);
            var tipY = FlankY(betaEnd);
            var tipAngle = Math.Atan2(tipY, tipX);
            var radiusAtTip = Math.Sqrt(tipX * tipX + tipY * tipY);
            pointed = tipAngle / halfPitchAngle >= radiusAtTip / tipRadius;

            var points = new List<GearPoint>();
            for (var i = 0; i <= SeatArcSteps; i++)
            {
                var theta = Math.PI - seatAngle / 2.0 * i / SeatArcSteps;
                points.Add(new GearPoint(centreRadius + seatRadius * Math.Cos(theta), i == 0 ? 0.0 : seatRadius * Math.Sin(theta)));
            }

            for (var i = 1; i <= FlankSteps; i++)
            {
                var beta = betaEnd * i / FlankSteps;
                points.Add(new GearPoint(FlankX(beta), FlankY(beta)));
            }

            // The last point is exactly on the tip circle, or on the line half way to the next gap for a pointed tooth.
            if (pointed)
            {
                points[points.Count - 1] = new GearPoint(radiusAtTip * Math.Cos(halfPitchAngle), radiusAtTip * Math.Sin(halfPitchAngle));
            }
            else
            {
                points[points.Count - 1] = new GearPoint(tipRadius * Math.Cos(tipAngle), tipRadius * Math.Sin(tipAngle));
            }

            return points;
        }

        /// <summary>
        /// The centre of the flank arc on the +y side of a gap, in the gap coordinates of <see cref="HalfGap"/> (the arc has the flank radius of
        /// the tooth form; the flank on the -y side is its mirror image). The flank starts at the end of the seating arc and is tangent to it.
        /// </summary>
        public static GearPoint FlankCentre(ChainProfile profile, int teeth, SprocketToothForm form)
        {
            var centreRadius = profile.PitchDiameter(teeth) / 2.0;
            var offset = profile.SeatRadius(form) - profile.FlankRadius(teeth, form);
            var thetaE = Math.PI - ChainProfile.SeatAngle(teeth, form) / 2.0;
            return new GearPoint(centreRadius + offset * Math.Cos(thetaE), offset * Math.Sin(thetaE));
        }

        /// <summary>True when the teeth are pointed: the flanks of neighbouring gaps meet below the tip diameter.</summary>
        public bool HasPointedTeeth
        {
            get
            {
                HalfGap(Profile, Teeth, ToothForm, out var pointed);
                return pointed;
            }
        }

        /// <summary>The number of outline points of one tooth gap and the tooth after it, and the index of the seat bottom in them.</summary>
        public static int PointsPerTooth(int halfGapCount, bool pointed) => pointed ? 2 * halfGapCount - 2 : 2 * halfGapCount;

        /// <summary>
        /// The closed, anticlockwise outline of the toothed part in the local XY plane (first point not repeated): for each tooth the gap
        /// from its left flank over the bottom of the seat to its right flank, then (unless the tooth is pointed) the middle of the tip
        /// between this gap and the next. The bottom of the seat of gap k is at index <c>k * block + HalfGap.Count - 1</c>.
        /// </summary>
        public List<GearPoint> BuildOutline()
        {
            Validate();

            var profile = Profile;
            var half = HalfGap(profile, Teeth, ToothForm, out var pointed);
            var tipRadius = profile.TipDiameter(Teeth, ToothForm) / 2.0;
            var step = 2.0 * Math.PI / Teeth;

            var gap = new List<GearPoint>();
            for (var i = half.Count - 1; i >= 1; i--) gap.Add(new GearPoint(half[i].X, -half[i].Y));
            for (var i = 0; i < half.Count - (pointed ? 1 : 0); i++) gap.Add(half[i]);

            var outline = new List<GearPoint>();
            for (var k = 0; k < Teeth; k++)
            {
                var rotation = k * step;
                var cos = Math.Cos(rotation);
                var sin = Math.Sin(rotation);
                foreach (var p in gap) outline.Add(new GearPoint(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos));

                if (!pointed)
                {
                    var middle = rotation + step / 2.0;
                    outline.Add(new GearPoint(tipRadius * Math.Cos(middle), tipRadius * Math.Sin(middle)));
                }
            }

            return outline;
        }

        /// <summary>The geometry is built once for a set of parameters and reused; the result is never modified.</summary>
        public override GearGeometry BuildGeometry()
        {
            var key = string.Join("|", _series, Teeth, ToothForm, FaceWidth, BoreDiameter, HubDiameter, HubLength);
            if (_cachedGeometry == null || _cacheKey != key)
            {
                _cachedGeometry = CreateGeometry();
                _cacheKey = key;
            }

            return _cachedGeometry;
        }

        private GearGeometry CreateGeometry()
        {
            var outline = BuildOutline();
            var closed = new List<GearPoint>(outline) { outline[0] };
            var polylines = new List<List<GearPoint>> { closed };

            if (HasBoreOrHub)
            {
                return CreateBoredGeometry(outline, polylines);
            }

            var mesh = SolidMeshBuilder.Loft(outline, FaceWidth / 2.0, 1.0, -FaceWidth / 2.0, 1.0, false);
            return new GearGeometry(null, polylines, mesh);
        }

        /// <summary>
        /// The geometry of a sprocket with a bore, a hub or both. Every circle (bore, hub) has the same number of segments, a multiple of the
        /// number of teeth, so the rings and cylinders join without gaps. The end faces of the toothed part are made of one cell per tooth
        /// (an ear-clipped polygon between the outline and a circle just inside the seat bottoms) and a ring from there to the bore.
        /// </summary>
        private GearGeometry CreateBoredGeometry(List<GearPoint> outline, List<List<GearPoint>> polylines)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            var halfWidth = FaceWidth / 2.0;
            var boreRadius = BoreDiameter / 2.0;
            var cellSteps = Math.Max(2, (int)Math.Ceiling(64.0 / Teeth));
            var segments = Teeth * cellSteps;

            var count = outline.Count;
            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                HtdPulleyComponent.AddQuad(mesh,
                    new GearPoint3D(outline[i].X, outline[i].Y, halfWidth), new GearPoint3D(outline[i].X, outline[i].Y, -halfWidth),
                    new GearPoint3D(outline[j].X, outline[j].Y, -halfWidth), new GearPoint3D(outline[j].X, outline[j].Y, halfWidth));
            }

            if (boreRadius > 0.0)
            {
                AddToothedCap(mesh, outline, halfWidth, true, boreRadius, cellSteps);
                AddToothedCap(mesh, outline, -halfWidth, false, boreRadius, cellSteps);
            }
            else
            {
                HtdPulleyComponent.AddPolygon(mesh, outline, halfWidth, true);
                HtdPulleyComponent.AddPolygon(mesh, outline, -halfWidth, false);
            }

            var low = -halfWidth;
            var high = halfWidth;
            if (HasHub)
            {
                var hubRadius = HubDiameter / 2.0;
                HtdPulleyComponent.AddWall(mesh, hubRadius, high, high + HubLength, segments, true);
                HtdPulleyComponent.AddRing(mesh, boreRadius, hubRadius, high + HubLength, segments, true);
                HtdPulleyComponent.AddRing(mesh, boreRadius, hubRadius, high, segments, false);
                high += HubLength;
                polylines.Add(HtdPulleyComponent.Circle(hubRadius, segments));
            }

            if (boreRadius > 0.0)
            {
                HtdPulleyComponent.AddWall(mesh, boreRadius, low, high, segments, false);
                polylines.Add(HtdPulleyComponent.Circle(boreRadius, segments));
            }

            mesh.CalculateNormals();
            return new GearGeometry(null, polylines, mesh);
        }

        /// <summary>The end face of the toothed part with a bore: a ring to a circle just inside the seat bottoms, and one cell per tooth outside it.</summary>
        private void AddToothedCap(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> outline, double z, bool facesUp, double boreRadius, int cellSteps)
        {
            var profile = Profile;
            var bottomRadius = RollerCentreRadius - profile.SeatRadius(ToothForm);
            var ringRadius = bottomRadius - Math.Min(0.5, (bottomRadius - boreRadius) / 2.0);
            var step = 2.0 * Math.PI / Teeth;
            var block = outline.Count / Teeth;
            var bottom = SeatArcSteps + FlankSteps; // index of the seat bottom within a block: the points of the left flank come first

            HtdPulleyComponent.AddRing(mesh, boreRadius, ringRadius, z, Teeth * cellSteps, facesUp);

            for (var k = 0; k < Teeth; k++)
            {
                var polygon = new List<GearPoint>();
                for (var i = bottom; i < block; i++) polygon.Add(outline[k * block + i]);
                var next = (k + 1) % Teeth;
                for (var i = 0; i <= bottom; i++) polygon.Add(outline[next * block + i]);
                for (var m = cellSteps; m >= 0; m--)
                {
                    var angle = k * step + step * m / cellSteps;
                    polygon.Add(new GearPoint(ringRadius * Math.Cos(angle), ringRadius * Math.Sin(angle)));
                }

                HtdPulleyComponent.AddPolygon(mesh, polygon, z, facesUp);
            }
        }
    }
}

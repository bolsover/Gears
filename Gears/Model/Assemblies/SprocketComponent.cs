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

        /// <summary>
        /// The tooth side relief of the sprocket catalogue (tooth radius r3, radius width C), the same one that the Alibre export cuts: the tooth
        /// side face is rounded by an arc of radius r3 that is tangent to it at <paramref name="startRadius"/> R0 and has moved in by C at the tip
        /// circle, so R0 = Ra - sqrt(2 r3 C - C^2). When R0 would be below the bottom of the roller seats (few teeth, short tooth form) the arc
        /// starts at the bottom radius instead and the cut-back at the tip circle is smaller than C. False when the chain has no relief, or when
        /// it is wider than half the tooth.
        /// </summary>
        public bool TrySideRelief(out double startRadius)
        {
            startRadius = 0.0;
            var profile = Profile;
            var radius = profile.SideRadius;
            var width = profile.SideReliefWidth;
            if (radius <= 0.0 || width <= 0.0 || width >= radius || width >= FaceWidth / 2.0) return false;

            var tip = OutsideDiameter / 2.0;
            var start = tip - Math.Sqrt(2.0 * radius * width - width * width);
            var bottom = RollerCentreRadius - profile.SeatRadius(ToothForm);
            if (start <= bottom)
            {
                start = bottom;
                var reach = tip - bottom;
                if (reach <= 0.0 || reach >= radius) return false;
            }

            startRadius = start;
            return true;
        }

        /// <summary>How far the tooth side is cut back at <paramref name="radius"/> by the side relief that starts at <paramref name="startRadius"/> (mm).</summary>
        public double SideDrop(double radius, double startRadius)
        {
            var r3 = Profile.SideRadius;
            var d = radius - startRadius;
            if (d <= 0.0) return 0.0;
            return r3 - Math.Sqrt(Math.Max(0.0, r3 * r3 - d * d));
        }

        /// <summary>Number of columns on each side of the tooth middle in the relief surface of a tooth.</summary>
        private const int ReliefHalfColumns = 2;

        /// <summary>The parts of a sprocket with side relief: the flat end face outline, the flat part of a gap and the levels of the relief surface.</summary>
        private sealed class ReliefShape
        {
            /// <summary>The closed outline of the flat part of the end face (the teeth cut off at the start radius of the relief).</summary>
            public List<GearPoint> FlatOutline;

            /// <summary>Points of the flat outline of one gap and the arc of the tooth after it.</summary>
            public int Block;

            /// <summary>Index of the seat bottom within a block.</summary>
            public int Bottom;

            /// <summary>The flat part of a gap, anticlockwise from its left flank over the seat bottom to its right flank (gap 0, on the +X axis).</summary>
            public List<GearPoint> Gap;

            /// <summary>Per level of the relief, from its start radius to the top of the tooth: radius, polar angle of the gap flank, half angle of the tooth and cut-back.</summary>
            public double[] Radius, Angle, Psi, Drop;
        }

        /// <summary>The relief shape for the side relief that starts at <paramref name="reliefStart"/>, or null when the tooth form does not allow the surface to be built.</summary>
        private ReliefShape BuildReliefShape(double reliefStart)
        {
            var half = HalfGap(Profile, Teeth, ToothForm, out _);
            var n = half.Count;
            var step = 2.0 * Math.PI / Teeth;
            var rho = new double[n];
            var ang = new double[n];
            for (var i = 0; i < n; i++)
            {
                rho[i] = Math.Sqrt(half[i].X * half[i].X + half[i].Y * half[i].Y);
                ang[i] = Math.Atan2(half[i].Y, half[i].X);
                if (i > 0 && rho[i] <= rho[i - 1] + 1e-9) return null;
            }

            if (reliefStart >= rho[n - 1] - 1e-6) return null;

            double AngleAt(double r)
            {
                for (var i = 0; i < n - 1; i++)
                {
                    if (r > rho[i + 1] + 1e-12) continue;
                    var t = Math.Min(1.0, Math.Max(0.0, (r - rho[i]) / (rho[i + 1] - rho[i])));
                    return Math.Atan2(half[i].Y + t * (half[i + 1].Y - half[i].Y), half[i].X + t * (half[i + 1].X - half[i].X));
                }

                return ang[n - 1];
            }

            var radii = new List<double> { reliefStart };
            for (var i = 0; i < n; i++)
            {
                if (rho[i] > reliefStart + 1e-9) radii.Add(rho[i]);
            }

            var levels = new List<double>();
            for (var i = 0; i < radii.Count - 1; i++)
            {
                levels.Add(radii[i]);
                levels.Add((radii[i] + radii[i + 1]) / 2.0);
            }

            levels.Add(radii[radii.Count - 1]);

            var shape = new ReliefShape
            {
                Radius = levels.ToArray(),
                Angle = new double[levels.Count],
                Psi = new double[levels.Count],
                Drop = new double[levels.Count]
            };
            for (var j = 0; j < levels.Count; j++)
            {
                shape.Angle[j] = AngleAt(levels[j]);
                var psi = step / 2.0 - shape.Angle[j];
                shape.Psi[j] = psi < 1e-9 ? 0.0 : psi;
                shape.Drop[j] = SideDrop(levels[j], reliefStart);
            }

            // The flat half gap: the seat bottom, the points below the start radius, and the point on the start radius.
            var flatHalf = new List<GearPoint> { half[0] };
            for (var i = 1; i < n && rho[i] < reliefStart - 1e-9; i++) flatHalf.Add(half[i]);
            if (reliefStart > rho[0] + 1e-9)
            {
                var a = shape.Angle[0];
                flatHalf.Add(new GearPoint(reliefStart * Math.Cos(a), reliefStart * Math.Sin(a)));
            }

            var gap = new List<GearPoint>();
            for (var i = flatHalf.Count - 1; i >= 1; i--) gap.Add(new GearPoint(flatHalf[i].X, -flatHalf[i].Y));
            gap.AddRange(flatHalf);
            shape.Gap = gap;
            shape.Bottom = flatHalf.Count - 1;

            var columns = 2 * ReliefHalfColumns;
            var outline = new List<GearPoint>();
            for (var k = 0; k < Teeth; k++)
            {
                var rotation = k * step;
                foreach (var p in gap) outline.Add(Rotate(p, rotation));
                for (var c = 1; c < columns; c++) outline.Add(ReliefPoint(shape, 0, k * step + step / 2.0, c));
            }

            shape.FlatOutline = outline;
            shape.Block = outline.Count / Teeth;
            return shape;
        }

        private static GearPoint Rotate(GearPoint p, double angle)
        {
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            return new GearPoint(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
        }

        private static GearPoint Polar(double radius, double angle) => new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle));

        /// <summary>The point at a level and column of the relief surface of the tooth with its middle at <paramref name="toothAngle"/>.</summary>
        private static GearPoint ReliefPoint(ReliefShape shape, int level, double toothAngle, int column)
        {
            var psi = shape.Psi[level];
            return Polar(shape.Radius[level], toothAngle - psi + 2.0 * psi * column / (2 * ReliefHalfColumns));
        }

        private static void AddReliefWall(GearMesh3DBuilder.Mesh3D mesh, GearPoint a, GearPoint b, double zA, double zB)
        {
            HtdPulleyComponent.AddQuad(mesh,
                new GearPoint3D(a.X, a.Y, zA), new GearPoint3D(a.X, a.Y, -zA), new GearPoint3D(b.X, b.Y, -zB), new GearPoint3D(b.X, b.Y, zB));
        }

        /// <summary>
        /// The walls of the toothed part and the relief surface on both end faces: the flat walls of the gaps, the relief edges of the flanks,
        /// the tip faces, and per tooth a grid of quads that follows the relief arc from its start radius to the top of the tooth.
        /// </summary>
        private void AddReliefSurfaces(GearMesh3DBuilder.Mesh3D mesh, ReliefShape shape)
        {
            var step = 2.0 * Math.PI / Teeth;
            var halfWidth = FaceWidth / 2.0;
            var columns = 2 * ReliefHalfColumns;
            var levels = shape.Radius.Length;
            var top = levels - 1;

            for (var k = 0; k < Teeth; k++)
            {
                var rotation = k * step;
                var toothAngle = rotation + step / 2.0;

                for (var i = 0; i + 1 < shape.Gap.Count; i++)
                {
                    AddReliefWall(mesh, Rotate(shape.Gap[i], rotation), Rotate(shape.Gap[i + 1], rotation), halfWidth, halfWidth);
                }

                for (var j = 0; j + 1 < levels; j++)
                {
                    AddReliefWall(mesh, Polar(shape.Radius[j], rotation + shape.Angle[j]), Polar(shape.Radius[j + 1], rotation + shape.Angle[j + 1]),
                        halfWidth - shape.Drop[j], halfWidth - shape.Drop[j + 1]);
                }

                var topZ = halfWidth - shape.Drop[top];
                for (var c = 0; c < columns; c++)
                {
                    var a = ReliefPoint(shape, top, toothAngle, c);
                    var b = ReliefPoint(shape, top, toothAngle, c + 1);
                    if (Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) > 1e-9) AddReliefWall(mesh, a, b, topZ, topZ);
                }

                for (var j = top; j >= 1; j--)
                {
                    AddReliefWall(mesh, Polar(shape.Radius[j], rotation + step - shape.Angle[j]), Polar(shape.Radius[j - 1], rotation + step - shape.Angle[j - 1]),
                        halfWidth - shape.Drop[j], halfWidth - shape.Drop[j - 1]);
                }

                for (var j = 0; j + 1 < levels; j++)
                {
                    var zLow = halfWidth - shape.Drop[j];
                    var zHigh = halfWidth - shape.Drop[j + 1];
                    for (var c = 0; c < columns; c++)
                    {
                        var p00 = ReliefPoint(shape, j, toothAngle, c);
                        var p01 = ReliefPoint(shape, j, toothAngle, c + 1);
                        var p10 = ReliefPoint(shape, j + 1, toothAngle, c);
                        var p11 = ReliefPoint(shape, j + 1, toothAngle, c + 1);
                        HtdPulleyComponent.AddQuad(mesh,
                            new GearPoint3D(p00.X, p00.Y, zLow), new GearPoint3D(p10.X, p10.Y, zHigh),
                            new GearPoint3D(p11.X, p11.Y, zHigh), new GearPoint3D(p01.X, p01.Y, zLow));
                        HtdPulleyComponent.AddQuad(mesh,
                            new GearPoint3D(p00.X, p00.Y, -zLow), new GearPoint3D(p01.X, p01.Y, -zLow),
                            new GearPoint3D(p11.X, p11.Y, -zHigh), new GearPoint3D(p10.X, p10.Y, -zHigh));
                    }
                }
            }
        }

        private GearGeometry CreateGeometry()
        {
            var outline = BuildOutline();
            var closed = new List<GearPoint>(outline) { outline[0] };
            var polylines = new List<List<GearPoint>> { closed };

            ReliefShape relief = null;
            if (TrySideRelief(out var reliefStart)) relief = BuildReliefShape(reliefStart);

            if (HasBoreOrHub)
            {
                return CreateBoredGeometry(outline, polylines, relief);
            }

            if (relief == null)
            {
                var plain = SolidMeshBuilder.Loft(outline, FaceWidth / 2.0, 1.0, -FaceWidth / 2.0, 1.0, false);
                return new GearGeometry(null, polylines, plain);
            }

            var mesh = new GearMesh3DBuilder.Mesh3D();
            AddReliefSurfaces(mesh, relief);
            HtdPulleyComponent.AddPolygon(mesh, relief.FlatOutline, FaceWidth / 2.0, true);
            HtdPulleyComponent.AddPolygon(mesh, relief.FlatOutline, -FaceWidth / 2.0, false);
            mesh.CalculateNormals();
            return new GearGeometry(null, polylines, mesh);
        }

        /// <summary>
        /// The geometry of a sprocket with a bore, a hub or both. Every circle (bore, hub) has the same number of segments, a multiple of the
        /// number of teeth, so the rings and cylinders join without gaps. The end faces of the toothed part are made of one cell per tooth
        /// (an ear-clipped polygon between the outline and a circle just inside the seat bottoms) and a ring from there to the bore.
        /// </summary>
        private GearGeometry CreateBoredGeometry(List<GearPoint> outline, List<List<GearPoint>> polylines, ReliefShape relief)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            var halfWidth = FaceWidth / 2.0;
            var boreRadius = BoreDiameter / 2.0;
            var cellSteps = Math.Max(2, (int)Math.Ceiling(64.0 / Teeth));
            var segments = Teeth * cellSteps;

            var count = outline.Count;
            if (relief != null)
            {
                AddReliefSurfaces(mesh, relief);
            }
            else
            {
                for (var i = 0; i < count; i++)
                {
                    var j = (i + 1) % count;
                    HtdPulleyComponent.AddQuad(mesh,
                        new GearPoint3D(outline[i].X, outline[i].Y, halfWidth), new GearPoint3D(outline[i].X, outline[i].Y, -halfWidth),
                        new GearPoint3D(outline[j].X, outline[j].Y, -halfWidth), new GearPoint3D(outline[j].X, outline[j].Y, halfWidth));
                }
            }

            // With relief the end faces are flat only up to the start radius of the relief; the teeth above it were built with the walls.
            var capOutline = relief != null ? relief.FlatOutline : outline;
            var block = relief != null ? relief.Block : outline.Count / Teeth;
            var bottomIndex = relief != null ? relief.Bottom : SeatArcSteps + FlankSteps;
            if (boreRadius > 0.0)
            {
                AddToothedCap(mesh, capOutline, halfWidth, true, boreRadius, cellSteps, block, bottomIndex);
                AddToothedCap(mesh, capOutline, -halfWidth, false, boreRadius, cellSteps, block, bottomIndex);
            }
            else
            {
                HtdPulleyComponent.AddPolygon(mesh, capOutline, halfWidth, true);
                HtdPulleyComponent.AddPolygon(mesh, capOutline, -halfWidth, false);
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
        private void AddToothedCap(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> outline, double z, bool facesUp, double boreRadius, int cellSteps,
            int block, int bottom)
        {
            var profile = Profile;
            var bottomRadius = RollerCentreRadius - profile.SeatRadius(ToothForm);
            var ringRadius = bottomRadius - Math.Min(0.5, (bottomRadius - boreRadius) / 2.0);
            var step = 2.0 * Math.PI / Teeth;

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

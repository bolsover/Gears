using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A pulley for an HTD (High Torque Drive) timing belt. It has no meshing partner in the gear train: a belt connects pulleys.
    /// <para>
    /// Local frame: the pulley axis is Z, the centre is the origin and the toothed width is centred on the Z = 0 mid-plane. Groove 0 is
    /// centred on the +X axis. The pitch diameter is <c>z p / pi</c> and the outside diameter is the pitch diameter minus twice the pitch
    /// line differential of the series (see <see cref="HtdProfile"/>).
    /// </para>
    /// Every groove is the negative of the belt tooth: a bottom arc of the head radius, two straight flanks with a 14 degree included angle
    /// that are tangent to it, and a rounded edge of the foot radius where each flank meets the outside diameter. The depth of a groove is
    /// the belt tooth height. Optional flanges (discs larger than the outside diameter) sit on both sides of the toothed width.
    /// </summary>
    public class HtdPulleyComponent : GearComponent
    {
        /// <summary>Number of segments of the half bottom arc of a groove; <see cref="HalfGroove"/> has this many plus one points on the arc.</summary>
        public const int BottomArcSteps = 6;

        /// <summary>Number of segments of the rounded groove edge.</summary>
        public const int EdgeArcSteps = 4;
        private const int FlangeSegments = 96;

        private GearGeometry _cachedGeometry;
        private string _cacheKey;

        /// <summary>The fewest grooves that are accepted; fewer leave no land between the grooves.</summary>
        public const int AbsoluteMinimumTeeth = 6;

        public HtdPulleyComponent(string id) : base(id)
        {
            Module = Profile.Pitch / Math.PI;
            FaceWidth = Profile.StandardBeltWidth;
        }

        private HtdSeries _series = HtdSeries.Htd5M;

        /// <summary>The HTD belt series the pulley is made for.</summary>
        public HtdSeries Series
        {
            get => _series;
            set
            {
                _series = value;
                Module = Profile.Pitch / Math.PI;
            }
        }

        /// <summary>The dimensions of <see cref="Series"/>.</summary>
        public HtdProfile Profile => HtdProfile.Get(_series);

        /// <summary>Number of grooves.</summary>
        public int Teeth { get; set; } = 20;

        /// <summary>True for a pulley with a flange on each side of the toothed width.</summary>
        public bool HasFlanges { get; set; }

        /// <summary>How far a flange reaches beyond the outside diameter (mm).</summary>
        public double FlangeHeight { get; set; } = 2.0;

        /// <summary>Thickness of one flange (mm), measured along the axis.</summary>
        public double FlangeThickness { get; set; } = 1.5;

        /// <summary>Diameter of the bore through the pulley (mm); zero for a solid pulley, as the shaft of the editor passes through it.</summary>
        public double BoreDiameter { get; set; }

        /// <summary>Diameter of the hub on the +Z side of the pulley (mm); zero or a value not above the bore for no hub.</summary>
        public double HubDiameter { get; set; }

        /// <summary>Length of the hub beyond the toothed width, or beyond the flange (mm); zero for no hub.</summary>
        public double HubLength { get; set; }

        /// <summary>True when the pulley has a hub: a diameter above the bore and a positive length.</summary>
        public bool HasHub => HubLength > 0.0 && HubDiameter > Math.Max(0.0, BoreDiameter);

        /// <summary>True when there is a bore or a hub, which gives the pulley ring-shaped end faces.</summary>
        public bool HasBoreOrHub => BoreDiameter > 0.0 || HasHub;

        public override int ToothCount => Teeth;

        public override double PitchDiameter => Profile.PitchDiameter(Teeth);

        /// <summary>Outside diameter of the toothed part (mm).</summary>
        public double OutsideDiameter => Profile.OutsideDiameter(Teeth);

        /// <summary>Diameter at the bottom of the grooves (mm).</summary>
        public double RootDiameter => Profile.RootDiameter(Teeth);

        /// <summary>Diameter of the flanges (mm); the outside diameter when the pulley has none.</summary>
        public double FlangeDiameter => OutsideDiameter + (HasFlanges ? 2.0 * FlangeHeight : 0.0);

        /// <summary>True when the pulley has fewer grooves than the belt makers recommend for the series.</summary>
        public bool IsBelowRecommendedTeeth => Teeth < Profile.MinimumTeeth;

        public override string TypeName => "HTD " + Profile.Name + " pulley";

        public override void GetBodyAxialExtent(out double start, out double end)
        {
            var half = FaceWidth / 2.0 + (HasFlanges ? FlangeThickness : 0.0);
            start = -half;
            end = half + (HasHub ? HubLength : 0.0);
        }

        public void Validate()
        {
            if (Teeth < AbsoluteMinimumTeeth)
            {
                throw new InvalidOperationException($"HTD pulley '{Id}': the number of grooves must be at least {AbsoluteMinimumTeeth}.");
            }

            if (FaceWidth <= 0) throw new InvalidOperationException($"HTD pulley '{Id}': the width must be positive.");
            if (HasFlanges && (FlangeHeight <= 0 || FlangeThickness <= 0))
            {
                throw new InvalidOperationException($"HTD pulley '{Id}': the flange height and thickness must be positive.");
            }

            if (RootDiameter <= 0)
            {
                throw new InvalidOperationException($"HTD pulley '{Id}': {Teeth} grooves are too few for the {Profile.Name} profile.");
            }

            if (BoreDiameter < 0 || HubDiameter < 0 || HubLength < 0)
            {
                throw new InvalidOperationException($"HTD pulley '{Id}': the bore and hub dimensions must not be negative.");
            }

            if (BoreDiameter > 0 && BoreDiameter >= RootDiameter)
            {
                throw new InvalidOperationException(
                    $"HTD pulley '{Id}': the bore of {BoreDiameter:0.###} mm must be smaller than the root diameter of {RootDiameter:0.###} mm.");
            }
        }

        /// <summary>
        /// The closed, anticlockwise outline of the toothed part in the local XY plane (first point not repeated): the grooves joined by
        /// arcs of the outside diameter.
        /// </summary>
        public List<GearPoint> BuildOutline()
        {
            Validate();

            var profile = Profile;
            var outsideRadius = OutsideDiameter / 2.0;
            var rootRadius = outsideRadius - profile.ToothHeight;
            var half = HalfGroove(profile);
            var halfWidthAtTop = half[half.Count - 1].X;
            var halfAngle = halfWidthAtTop / outsideRadius;
            var step = 2.0 * Math.PI / Teeth;
            var land = step - 2.0 * halfAngle;
            if (land <= 1e-4)
            {
                throw new InvalidOperationException(
                    $"HTD pulley '{Id}': {Teeth} grooves are too few for the {profile.Name} profile; the grooves would overlap.");
            }

            // The groove from its left edge to its right edge (increasing angle), in groove coordinates: u across, v up from the bottom.
            var groove = new List<GearPoint>();
            for (var i = half.Count - 1; i >= 1; i--) groove.Add(new GearPoint(-half[i].X, half[i].Y));
            groove.AddRange(half);

            var outline = new List<GearPoint>();
            for (var k = 0; k < Teeth; k++)
            {
                var centre = k * step;
                foreach (var p in groove)
                {
                    var radius = rootRadius + p.Y;
                    var angle = centre + p.X / radius;
                    outline.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
                }

                var landMiddle = centre + halfAngle + land / 2.0;
                outline.Add(new GearPoint(outsideRadius * Math.Cos(landMiddle), outsideRadius * Math.Sin(landMiddle)));
            }

            return outline;
        }

        /// <summary>
        /// The right half of a groove from its bottom (0, 0) to the end of the edge rounding on the outside diameter, in groove
        /// coordinates: u across the groove, v up from the bottom. The last point has v equal to the groove depth.
        /// </summary>
        public static List<GearPoint> HalfGroove(HtdProfile profile)
        {
            var headRadius = profile.HeadRadius;
            var depth = profile.ToothHeight;
            var theta0 = (90.0 - HtdProfile.FlankAngleDeg / 2.0) * Math.PI / 180.0;

            var points = new List<GearPoint>();
            for (var i = 0; i <= BottomArcSteps; i++)
            {
                var theta = theta0 * i / BottomArcSteps;
                points.Add(new GearPoint(headRadius * Math.Sin(theta), headRadius * (1.0 - Math.Cos(theta))));
            }

            // The flank leaves the bottom arc along its tangent and meets the outside diameter at the corner.
            var flankU = Math.Cos(theta0);
            var flankV = Math.Sin(theta0);
            var start = points[points.Count - 1];
            var flankLength = (depth - start.Y) / flankV;
            var cornerU = start.X + flankLength * flankU;

            // Rounded edge: an arc of the foot radius tangent to the flank and to the line v = depth.
            var downU = -flankU;
            var downV = -flankV;
            var alpha = Math.Acos(downU);
            var tangentHalf = Math.Tan(alpha / 2.0);
            var radius = Math.Min(profile.FootRadius, 0.9 * flankLength * tangentHalf);
            var tangentLength = radius / tangentHalf;

            var t1U = cornerU + tangentLength * downU;
            var t1V = depth + tangentLength * downV;
            var t2U = cornerU + tangentLength;
            var t2V = depth;

            var bisectU = downU + 1.0;
            var bisectV = downV;
            var bisectLength = Math.Sqrt(bisectU * bisectU + bisectV * bisectV);
            var centreDistance = radius / Math.Sin(alpha / 2.0);
            var centreU = cornerU + bisectU / bisectLength * centreDistance;
            var centreV = depth + bisectV / bisectLength * centreDistance;

            var a1 = Math.Atan2(t1V - centreV, t1U - centreU);
            var a2 = Math.Atan2(t2V - centreV, t2U - centreU);
            var sweep = a2 - a1;
            while (sweep > Math.PI) sweep -= 2.0 * Math.PI;
            while (sweep <= -Math.PI) sweep += 2.0 * Math.PI;

            for (var i = 0; i <= EdgeArcSteps; i++)
            {
                var a = a1 + sweep * i / EdgeArcSteps;
                points.Add(new GearPoint(centreU + radius * Math.Cos(a), centreV + radius * Math.Sin(a)));
            }

            // The last point is exactly on the outside diameter.
            points[points.Count - 1] = new GearPoint(points[points.Count - 1].X, depth);
            return points;
        }

        /// <summary>
        /// The geometry is built once for a set of parameters and reused, because the caps of the grooved body need an ear-clipping
        /// triangulation (the groove walls are not radial, so the outline cannot be seen from the axis). The result is never modified.
        /// </summary>
        public override GearGeometry BuildGeometry()
        {
            var key = string.Join("|", _series, Teeth, FaceWidth, HasFlanges, FlangeHeight, FlangeThickness, BoreDiameter, HubDiameter, HubLength);
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
            if (HasFlanges)
            {
                var flangeRadius = FlangeDiameter / 2.0;
                var circle = new List<GearPoint>();
                for (var i = 0; i < FlangeSegments; i++)
                {
                    var angle = 2.0 * Math.PI * i / FlangeSegments;
                    circle.Add(new GearPoint(flangeRadius * Math.Cos(angle), flangeRadius * Math.Sin(angle)));
                }

                Append(mesh, SolidMeshBuilder.Loft(circle, FaceWidth / 2.0 + FlangeThickness, 1.0, FaceWidth / 2.0, 1.0, true));
                Append(mesh, SolidMeshBuilder.Loft(circle, -FaceWidth / 2.0, 1.0, -FaceWidth / 2.0 - FlangeThickness, 1.0, true));
                mesh.CalculateNormals();
                polylines.Add(new List<GearPoint>(circle) { circle[0] });
            }

            return new GearGeometry(null, polylines, mesh);
        }

        /// <summary>
        /// The geometry of a pulley with a bore, a hub or both. Every circle (bore, hub, flanges) has the same number of segments, a multiple
        /// of the number of grooves, so the rings and cylinders join without gaps. The end faces of the toothed part are made of one cell per
        /// groove (an ear-clipped polygon between the groove outline and a circle just inside the roots) and a ring from there to the bore.
        /// </summary>
        private GearGeometry CreateBoredGeometry(List<GearPoint> outline, List<List<GearPoint>> polylines)
        {
            var mesh = new GearMesh3DBuilder.Mesh3D();
            var halfWidth = FaceWidth / 2.0;
            var boreRadius = BoreDiameter / 2.0;
            var cellSteps = Math.Max(2, (int)Math.Ceiling(64.0 / Teeth));
            var segments = Teeth * cellSteps;

            // Side faces of the toothed part, outward.
            var count = outline.Count;
            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                AddQuad(mesh,
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
                AddToothedCapWithoutBore(mesh, outline, halfWidth, true);
                AddToothedCapWithoutBore(mesh, outline, -halfWidth, false);
            }

            var low = -halfWidth;
            var high = halfWidth;
            if (HasFlanges)
            {
                var flangeRadius = FlangeDiameter / 2.0;
                // Each flange is a closed ring: the outer wall, the outer face and the inner face against the toothed part (which cancels the
                // end face of the toothed part where they touch and closes the gap between the outside diameter and the flange).
                AddWall(mesh, flangeRadius, halfWidth, halfWidth + FlangeThickness, segments, true);
                AddRing(mesh, boreRadius, flangeRadius, halfWidth + FlangeThickness, segments, true);
                AddRing(mesh, boreRadius, flangeRadius, halfWidth, segments, false);
                AddWall(mesh, flangeRadius, -halfWidth - FlangeThickness, -halfWidth, segments, true);
                AddRing(mesh, boreRadius, flangeRadius, -halfWidth - FlangeThickness, segments, false);
                AddRing(mesh, boreRadius, flangeRadius, -halfWidth, segments, true);
                low -= FlangeThickness;
                high += FlangeThickness;
                polylines.Add(Circle(flangeRadius, segments));
            }

            if (HasHub)
            {
                var hubRadius = HubDiameter / 2.0;
                AddWall(mesh, hubRadius, high, high + HubLength, segments, true);
                AddRing(mesh, boreRadius, hubRadius, high + HubLength, segments, true);
                AddRing(mesh, boreRadius, hubRadius, high, segments, false);
                high += HubLength;
                polylines.Add(Circle(hubRadius, segments));
            }

            if (boreRadius > 0.0)
            {
                AddWall(mesh, boreRadius, low, high, segments, false);
                polylines.Add(Circle(boreRadius, segments));
            }

            mesh.CalculateNormals();
            return new GearGeometry(null, polylines, mesh);
        }

        /// <summary>The end face of the toothed part with a bore: a ring to a circle just inside the roots, and one cell per groove outside it.</summary>
        private void AddToothedCap(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> outline, double z, bool facesUp, double boreRadius, int cellSteps)
        {
            var profile = Profile;
            var rootRadius = OutsideDiameter / 2.0 - profile.ToothHeight;
            var ringRadius = rootRadius - Math.Min(0.5, (rootRadius - boreRadius) / 2.0);
            var step = 2.0 * Math.PI / Teeth;
            var halfCount = HalfGroove(profile).Count;
            var block = 2 * halfCount; // the points of one groove and the middle of the land after it
            var bottom = halfCount - 1;

            AddRing(mesh, boreRadius, ringRadius, z, Teeth * cellSteps, facesUp);

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

                AddPolygon(mesh, polygon, z, facesUp);
            }
        }

        /// <summary>The end face of a toothed part without a bore (it only has a hub): the whole outline, ear-clipped.</summary>
        private static void AddToothedCapWithoutBore(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> outline, double z, bool facesUp)
        {
            AddPolygon(mesh, outline, z, facesUp);
        }

        private static void AddPolygon(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> polygon, double z, bool facesUp)
        {
            var start = mesh.Vertices.Count;
            foreach (var p in polygon) mesh.Vertices.Add(new GearPoint3D(p.X, p.Y, z));
            var triangles = SolidMeshBuilder.Triangulate(polygon);
            for (var t = 0; t + 2 < triangles.Count; t += 3)
            {
                if (facesUp) mesh.TriangleIndices.AddRange(new[] { start + triangles[t], start + triangles[t + 1], start + triangles[t + 2] });
                else mesh.TriangleIndices.AddRange(new[] { start + triangles[t], start + triangles[t + 2], start + triangles[t + 1] });
            }
        }

        /// <summary>A flat ring between two radii at height z; a disc when the inner radius is zero.</summary>
        private static void AddRing(GearMesh3DBuilder.Mesh3D mesh, double innerRadius, double outerRadius, double z, int segments, bool facesUp)
        {
            for (var i = 0; i < segments; i++)
            {
                var a = 2.0 * Math.PI * i / segments;
                var b = 2.0 * Math.PI * (i + 1) / segments;
                var outerA = new GearPoint3D(outerRadius * Math.Cos(a), outerRadius * Math.Sin(a), z);
                var outerB = new GearPoint3D(outerRadius * Math.Cos(b), outerRadius * Math.Sin(b), z);
                if (innerRadius <= 1e-9)
                {
                    var centre = new GearPoint3D(0.0, 0.0, z);
                    if (facesUp) AddTriangle(mesh, centre, outerA, outerB);
                    else AddTriangle(mesh, centre, outerB, outerA);
                    continue;
                }

                var innerA = new GearPoint3D(innerRadius * Math.Cos(a), innerRadius * Math.Sin(a), z);
                var innerB = new GearPoint3D(innerRadius * Math.Cos(b), innerRadius * Math.Sin(b), z);
                if (facesUp) AddQuad(mesh, innerA, outerA, outerB, innerB);
                else AddQuad(mesh, innerA, innerB, outerB, outerA);
            }
        }

        /// <summary>A cylinder wall between two heights; the faces point away from the axis when <paramref name="outward"/>, else towards it.</summary>
        private static void AddWall(GearMesh3DBuilder.Mesh3D mesh, double radius, double zLow, double zHigh, int segments, bool outward)
        {
            for (var i = 0; i < segments; i++)
            {
                var a = 2.0 * Math.PI * i / segments;
                var b = 2.0 * Math.PI * (i + 1) / segments;
                var highA = new GearPoint3D(radius * Math.Cos(a), radius * Math.Sin(a), zHigh);
                var highB = new GearPoint3D(radius * Math.Cos(b), radius * Math.Sin(b), zHigh);
                var lowA = new GearPoint3D(highA.X, highA.Y, zLow);
                var lowB = new GearPoint3D(highB.X, highB.Y, zLow);
                if (outward) AddQuad(mesh, highA, lowA, lowB, highB);
                else AddQuad(mesh, highA, highB, lowB, lowA);
            }
        }

        private static void AddQuad(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D a, GearPoint3D b, GearPoint3D c, GearPoint3D d)
        {
            var k = mesh.Vertices.Count;
            mesh.Vertices.AddRange(new[] { a, b, c, d });
            mesh.TriangleIndices.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }

        private static void AddTriangle(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D a, GearPoint3D b, GearPoint3D c)
        {
            var k = mesh.Vertices.Count;
            mesh.Vertices.AddRange(new[] { a, b, c });
            mesh.TriangleIndices.AddRange(new[] { k, k + 1, k + 2 });
        }

        private static List<GearPoint> Circle(double radius, int segments)
        {
            var circle = new List<GearPoint>(segments + 1);
            for (var i = 0; i <= segments; i++)
            {
                var angle = 2.0 * Math.PI * (i % segments) / segments;
                circle.Add(new GearPoint(radius * Math.Cos(angle), radius * Math.Sin(angle)));
            }

            return circle;
        }

        private static void Append(GearMesh3DBuilder.Mesh3D target, GearMesh3DBuilder.Mesh3D extra)
        {
            var offset = target.Vertices.Count;
            target.Vertices.AddRange(extra.Vertices);
            target.TriangleIndices.AddRange(extra.TriangleIndices.Select(i => i + offset));
        }
    }
}

using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator
{
    /// <summary>A point of the bevel blank sketch: X along the gear axis, Y radial (positive up), both in mm.</summary>
    public readonly struct BevelBlankPoint
    {
        public BevelBlankPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public static BevelBlankPoint operator +(BevelBlankPoint a, BevelBlankPoint b) => new BevelBlankPoint(a.X + b.X, a.Y + b.Y);
        public static BevelBlankPoint operator -(BevelBlankPoint a, BevelBlankPoint b) => new BevelBlankPoint(a.X - b.X, a.Y - b.Y);
        public static BevelBlankPoint operator *(BevelBlankPoint a, double s) => new BevelBlankPoint(a.X * s, a.Y * s);

        public double Length => Math.Sqrt(X * X + Y * Y);

        public override string ToString() => $"({X:0.####}, {Y:0.####})";
    }

    /// <summary>
    /// The closed profile sketch of a straight bevel gear blank and the solid made by revolving it about the X axis.
    /// The construction (all lengths in mm, positive X to the right, positive Y up):
    /// <list type="bullet">
    /// <item>refa: vertical line from the origin to pointA at half the pitch diameter.</item>
    /// <item>refb: from pointB on the negative X axis to pointA, at the pitch cone angle to the X axis.</item>
    /// <item>refc: from pointC on the positive X axis to pointA, perpendicular to refb (the back cone line).</item>
    /// <item>refd: from pointB to pointD on refc, a dedendum from pointA towards pointC (the root cone line).</item>
    /// <item>refe: from pointB to pointE on refc, a whole depth from pointD away from pointC (the tip cone line).</item>
    /// <item>The sketch is F-D (vertical), D-E (along refc), E-G (face width along refe towards pointB), G-H (perpendicular to
    /// refb, running down until it meets refd), H-I (vertical to the X axis) and I-F (along the X axis).</item>
    /// </list>
    /// </summary>
    public sealed class BevelBlankProfile
    {
        private const double AxisTolerance = 1e-9;

        /// <param name="teeth">Teeth of this gear.</param>
        /// <param name="mateTeeth">Teeth of the mating gear.</param>
        /// <param name="module">Module at the large end, mm.</param>
        /// <param name="faceWidth">Face width, mm, measured along the tip cone line.</param>
        /// <param name="shaftAngleDeg">Angle between the two shafts, degrees.</param>
        /// <param name="addendumFactor">Addendum as a multiple of the module (standard 1).</param>
        /// <param name="dedendumFactor">Dedendum as a multiple of the module (standard 1.25).</param>
        /// <param name="bevelType">Tooth system. For <see cref="BevelGearType.Gleason"/> the addendum and dedendum follow the Gleason
        /// formulas (long pinion addendum, short gear addendum, whole depth 2.188 m + 0.051 mm) and the two factors are ignored.</param>
        public BevelBlankProfile(int teeth, int mateTeeth, double module, double faceWidth, double shaftAngleDeg = 90.0,
            double addendumFactor = 1.0, double dedendumFactor = 1.25, BevelGearType bevelType = BevelGearType.Standard)
        {
            if (teeth < 1) throw new ArgumentException("The gear needs at least one tooth.", nameof(teeth));
            if (mateTeeth < 1) throw new ArgumentException("The mating gear needs at least one tooth.", nameof(mateTeeth));
            if (!(module > 0.0)) throw new ArgumentException("The module must be positive.", nameof(module));
            if (!(faceWidth > 0.0)) throw new ArgumentException("The face width must be positive.", nameof(faceWidth));
            if (!(shaftAngleDeg > 0.0 && shaftAngleDeg < 180.0)) throw new ArgumentException("The shaft angle must be between 0 and 180 degrees.", nameof(shaftAngleDeg));
            if (!(addendumFactor > 0.0 && dedendumFactor > 0.0)) throw new ArgumentException("The addendum and dedendum factors must be positive.");

            Teeth = teeth;
            MateTeeth = mateTeeth;
            Module = module;
            FaceWidth = faceWidth;
            ShaftAngleDeg = shaftAngleDeg;
            BevelType = bevelType;
            PitchDiameter = module * teeth;

            var sigma = shaftAngleDeg * Math.PI / 180.0;
            PitchConeAngleRad = Math.Atan2(Math.Sin(sigma), (double)mateTeeth / teeth + Math.Cos(sigma));
            BackConeAngleRad = Math.PI / 2.0 - PitchConeAngleRad;

            if (bevelType == BevelGearType.Gleason)
            {
                var mateCone = sigma - PitchConeAngleRad;
                if (!(Math.Cos(mateCone) > 1e-9)) throw new ArgumentException("The Gleason proportions need a mating pitch cone angle below 90 degrees.", nameof(shaftAngleDeg));
                VirtualToothRatio = (teeth / Math.Cos(PitchConeAngleRad)) / (mateTeeth / Math.Cos(mateCone));
                Clearance = GleasonClearanceFactor * module + BevelGearComponent.GleasonDepthConstant;
                WholeDepth = 2.0 * module + Clearance;
                Addendum = GleasonAddendum(module, VirtualToothRatio);
                Dedendum = WholeDepth - Addendum;
                ProfileShift = Addendum - module;
            }
            else
            {
                VirtualToothRatio = 1.0;
                Addendum = addendumFactor * module;
                Dedendum = dedendumFactor * module;
                WholeDepth = Addendum + Dedendum;
                Clearance = WholeDepth - 2.0 * module;
                ProfileShift = 0.0;
            }

            ConeDistance = PitchDiameter / 2.0 / Math.Sin(PitchConeAngleRad);

            var radius = PitchDiameter / 2.0;
            PointA = new BevelBlankPoint(0.0, radius);
            PointB = new BevelBlankPoint(-radius / Math.Tan(PitchConeAngleRad), 0.0);
            PointC = new BevelBlankPoint(radius / Math.Tan(BackConeAngleRad), 0.0);

            // refc, from pointA towards pointC.
            var refc = new BevelBlankPoint(Math.Cos(BackConeAngleRad), -Math.Sin(BackConeAngleRad));
            PointD = PointA + refc * Dedendum;
            PointE = PointD - refc * WholeDepth;
            PointF = new BevelBlankPoint(PointD.X, 0.0);

            var towardsB = PointB - PointE;
            PointG = PointE + towardsB * (faceWidth / towardsB.Length);

            // lineI is perpendicular to refb, so it runs parallel to refc; it ends where it meets the root cone line refd.
            var rootLine = PointD - PointB;
            var denominator = Cross(refc, rootLine);
            if (Math.Abs(denominator) < AxisTolerance) throw new InvalidOperationException("The toe face does not meet the root cone line.");
            var t = Cross(PointB - PointG, rootLine) / denominator;
            if (!(t > 0.0)) throw new InvalidOperationException("The face width is too large for this gear: the toe face does not meet the root cone line.");
            PointH = PointG + refc * t;
            if (!(PointH.Y > 0.0)) throw new InvalidOperationException("The face width is too large for this gear: the toe reaches the axis.");
            PointI = new BevelBlankPoint(PointH.X, 0.0);
        }

        private const double GleasonClearanceFactor = 0.188;

        /// <summary>
        /// Gleason addendum at the large end for a gear whose virtual tooth ratio to its mate is <paramref name="virtualRatio"/>:
        /// <c>0.54 m + 0.46 m / q</c> for the gear (q at least 1) and the rest of the working depth <c>2 m</c> for the pinion.
        /// </summary>
        public static double GleasonAddendum(double module, double virtualRatio)
        {
            return virtualRatio >= 1.0
                ? module * (0.54 + 0.46 / virtualRatio)
                : module * (2.0 - 0.54 - 0.46 * virtualRatio);
        }

        /// <summary>Tooth system of the gear.</summary>
        public BevelGearType BevelType { get; }

        /// <summary>Ratio of the virtual tooth counts of this gear and its mate (1 for the standard system, where it is not needed).</summary>
        public double VirtualToothRatio { get; }

        /// <summary>Bottom clearance at the large end, mm: whole depth less the working depth 2 m.</summary>
        public double Clearance { get; }

        /// <summary>
        /// Profile shift of the virtual spur gear, mm: addendum less module for Gleason (long and short addendum, equal and opposite on
        /// the two gears), zero otherwise. It makes the pinion tooth thicker and the gear tooth thinner at the pitch circle.
        /// </summary>
        public double ProfileShift { get; }

        public int Teeth { get; }
        public int MateTeeth { get; }
        public double Module { get; }
        public double FaceWidth { get; }
        public double ShaftAngleDeg { get; }
        public double Addendum { get; }
        public double Dedendum { get; }
        public double WholeDepth { get; }
        public double PitchDiameter { get; }
        public double PitchConeAngleRad { get; }
        public double PitchConeAngleDeg => PitchConeAngleRad * 180.0 / Math.PI;
        public double BackConeAngleRad { get; }
        public double BackConeAngleDeg => BackConeAngleRad * 180.0 / Math.PI;

        /// <summary>Distance from the cone apex (pointB) to pointA along the pitch cone.</summary>
        public double ConeDistance { get; }

        public BevelBlankPoint PointA { get; }
        public BevelBlankPoint PointB { get; }
        public BevelBlankPoint PointC { get; }
        public BevelBlankPoint PointD { get; }
        public BevelBlankPoint PointE { get; }
        public BevelBlankPoint PointF { get; }
        public BevelBlankPoint PointG { get; }
        public BevelBlankPoint PointH { get; }
        public BevelBlankPoint PointI { get; }

        /// <summary>The closed sketch: pointF, D, E, G, H, I. The last edge (I to F) lies on the X axis.</summary>
        public IReadOnlyList<BevelBlankPoint> Outline => new[] { PointF, PointD, PointE, PointG, PointH, PointI };

        /// <summary>The sketch lines as name, start and end, for display.</summary>
        public IEnumerable<KeyValuePair<string, BevelBlankPoint[]>> Lines
        {
            get
            {
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineF", new[] { PointF, PointD });
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineG", new[] { PointD, PointE });
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineH", new[] { PointE, PointG });
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineI", new[] { PointG, PointH });
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineJ", new[] { PointH, PointI });
                yield return new KeyValuePair<string, BevelBlankPoint[]>("lineK", new[] { PointI, PointF });
            }
        }

        /// <summary>Length of the back cone line from pointC to pointA: the pitch radius of the developed back cone.</summary>
        public double BackConeDistance => (PointA - PointC).Length;

        /// <summary>Scale about the apex (pointB) that carries the heel (back cone) end of the teeth to the toe end.</summary>
        public double ToeScale => (PointG - PointB).Length / (PointE - PointB).Length;

        /// <summary>The point on the back cone line (pointC towards pointA and beyond) at the given distance from pointC.</summary>
        public BevelBlankPoint BackConePoint(double distanceFromPointC) => PointC + (PointA - PointC) * (distanceFromPointC / BackConeDistance);

        /// <summary>The point at the toe that corresponds to a point at the heel: a scaling about the apex (pointB).</summary>
        public BevelBlankPoint ToToe(BevelBlankPoint heel) => PointB + (heel - PointB) * ToeScale;

        /// <summary>Revolves a sketch point about the X axis into 3D (Y radial at angle 0, rotating towards Z).</summary>
        public static GearPoint3D Revolve(BevelBlankPoint p, double angleRad) => new GearPoint3D(p.X, p.Y * Math.Cos(angleRad), p.Y * Math.Sin(angleRad));

        private static double Cross(BevelBlankPoint a, BevelBlankPoint b) => a.X * b.Y - a.Y * b.X;

        /// <summary>
        /// The blank with the teeth cut: a loft cut of the valley sketch from the back cone face to the toe, patterned about the axis.
        /// Every tooth line runs to the cone apex, so the loft is the valley sketch on the back cone and the same sketch scaled
        /// about pointB on the toe face. The cut valleys are removed from the revolved blank between the root cone (pointD to pointH)
        /// and the tip cone (pointE to pointG); everything below the root cone and the back and toe faces are those of the blank.
        /// </summary>
        /// <param name="sketch">The valley sketch made for this blank.</param>
        /// <param name="valleys">Number of valleys to cut (1 to the number of teeth); 0 or less cuts them all.</param>
        public GearMesh3DBuilder.Mesh3D BuildToothedMesh(BevelToothSketch sketch, int valleys = 0)
        {
            if (sketch == null) throw new ArgumentNullException(nameof(sketch));
            if (!ReferenceEquals(sketch.Profile, this)) throw new ArgumentException("The sketch belongs to a different blank.", nameof(sketch));

            var outline = sketch.BuildOutline(valleys <= 0 ? Teeth : valleys);
            var cosDelta = Math.Cos(PitchConeAngleRad);
            var count = outline.Count;
            var backFace = new GearPoint3D[count];
            var heelRoot = new GearPoint3D[count];
            var heel = new GearPoint3D[count];
            var toe = new GearPoint3D[count];
            var toeRoot = new GearPoint3D[count];
            var toeFace = new GearPoint3D[count];
            for (var i = 0; i < count; i++)
            {
                // The developed back cone is a full turn of 2 pi cos(delta); on the cone itself the turn is 2 pi.
                var angle = outline[i].Angle / cosDelta;
                var onBackCone = BackConePoint(outline[i].Radius);
                backFace[i] = Revolve(PointF, angle);
                heelRoot[i] = Revolve(PointD, angle);
                heel[i] = Revolve(onBackCone, angle);
                toe[i] = Revolve(ToToe(onBackCone), angle);
                toeRoot[i] = Revolve(PointH, angle);
                toeFace[i] = Revolve(PointI, angle);
            }

            // The same order as the sketch F, D, E, G, H, I: back face, heel (back cone), loft along the teeth, toe, toe face.
            var mesh = new GearMesh3DBuilder.Mesh3D();
            AddBand(mesh, backFace, heelRoot);
            AddBand(mesh, heelRoot, heel);
            AddBand(mesh, heel, toe);
            AddBand(mesh, toe, toeRoot);
            AddBand(mesh, toeRoot, toeFace);
            mesh.CalculateNormals();
            return mesh;
        }

        /// <summary>Joins two rings of equal length with triangles facing outwards, for a sketch that runs counter-clockwise.</summary>
        private static void AddBand(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D[] ring0, GearPoint3D[] ring1)
        {
            var count = ring0.Length;
            var first0 = mesh.Vertices.Count;
            mesh.Vertices.AddRange(ring0);
            var first1 = mesh.Vertices.Count;
            mesh.Vertices.AddRange(ring1);
            for (var j = 0; j < count; j++)
            {
                var k = (j + 1) % count;
                AddTriangleChecked(mesh, first0 + j, first1 + j, first0 + k);
                AddTriangleChecked(mesh, first1 + j, first1 + k, first0 + k);
            }
        }

        private static void AddTriangleChecked(GearMesh3DBuilder.Mesh3D mesh, int a, int b, int c)
        {
            if (SamePosition(mesh.Vertices[a], mesh.Vertices[b]) || SamePosition(mesh.Vertices[b], mesh.Vertices[c]) || SamePosition(mesh.Vertices[a], mesh.Vertices[c])) return;
            mesh.TriangleIndices.Add(a);
            mesh.TriangleIndices.Add(b);
            mesh.TriangleIndices.Add(c);
        }

        private static bool SamePosition(GearPoint3D a, GearPoint3D b)
        {
            return Math.Abs(a.X - b.X) < 1e-9 && Math.Abs(a.Y - b.Y) < 1e-9 && Math.Abs(a.Z - b.Z) < 1e-9;
        }

        /// <summary>Revolves the sketch 360 degrees about the X axis into a closed solid with outward facing triangles.</summary>
        /// <param name="segments">Number of steps around the axis.</param>
        public GearMesh3DBuilder.Mesh3D BuildMesh(int segments = 96)
        {
            if (segments < 3) throw new ArgumentException("At least 3 segments are needed to revolve the sketch.", nameof(segments));

            var outline = new List<BevelBlankPoint>(Outline);
            var area = 0.0;
            for (var i = 0; i < outline.Count; i++)
            {
                area += Cross(outline[i], outline[(i + 1) % outline.Count]);
            }

            // The winding below assumes a counter-clockwise sketch (X right, Y up).
            if (area < 0.0) outline.Reverse();

            var mesh = new GearMesh3DBuilder.Mesh3D();
            for (var e = 0; e < outline.Count; e++)
            {
                var p0 = outline[e];
                var p1 = outline[(e + 1) % outline.Count];
                if (p0.Y <= AxisTolerance && p1.Y <= AxisTolerance) continue; // an edge on the axis sweeps no surface

                var ring0 = AddRing(mesh, p0, segments);
                var ring1 = AddRing(mesh, p1, segments);
                for (var j = 0; j < segments; j++)
                {
                    var k = (j + 1) % segments;
                    AddTriangle(mesh, ring0[j], ring1[j], ring0[k]);
                    AddTriangle(mesh, ring1[j], ring1[k], ring0[k]);
                }
            }

            mesh.CalculateNormals();
            return mesh;
        }

        private static int[] AddRing(GearMesh3DBuilder.Mesh3D mesh, BevelBlankPoint p, int segments)
        {
            var ring = new int[segments];
            if (p.Y <= AxisTolerance)
            {
                mesh.Vertices.Add(new GearPoint3D(p.X, 0.0, 0.0));
                for (var j = 0; j < segments; j++) ring[j] = mesh.Vertices.Count - 1;
                return ring;
            }

            for (var j = 0; j < segments; j++)
            {
                var angle = 2.0 * Math.PI * j / segments;
                ring[j] = mesh.Vertices.Count;
                mesh.Vertices.Add(new GearPoint3D(p.X, p.Y * Math.Cos(angle), p.Y * Math.Sin(angle)));
            }

            return ring;
        }

        private static void AddTriangle(GearMesh3DBuilder.Mesh3D mesh, int a, int b, int c)
        {
            if (a == b || b == c || a == c) return; // collapsed on the axis
            mesh.TriangleIndices.Add(a);
            mesh.TriangleIndices.Add(b);
            mesh.TriangleIndices.Add(c);
        }
    }
}

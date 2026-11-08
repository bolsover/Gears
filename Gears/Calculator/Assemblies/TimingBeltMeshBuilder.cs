using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Builds the shape of a <see cref="TimingBelt"/>: a closed strip that runs round the two pulleys. The path is the open belt (external
    /// tangent lines and an arc round each pulley) in the plane of the pulleys' mid-planes. The strip lies between the outside diameter of
    /// the pulleys and the back of the belt, and is as wide as the belt; the HTD teeth (see <see cref="BeltToothPath"/>) stand on its
    /// pulley side and reach to the root diameter. The geometry is made in the frame of the driver's shaft, with the
    /// driver on the axis and the driven pulley at the centre distance along the line of centres, and then moved with the frame.
    /// </summary>
    public static class TimingBeltMeshBuilder
    {
        /// <summary>Angle of one segment of an arc round a pulley (radians).</summary>
        private const double ArcStep = Math.PI / 36.0;

        /// <summary>
        /// The plain (toothless) belt loop at an offset from the pitch line, anticlockwise, in the plane with the driver at the origin. The
        /// offset is positive away from the pulleys. <paramref name="offset"/> of -PLD is the outside diameter of the pulleys. Every loop has
        /// the same number of points.
        /// </summary>
        public static List<GearPoint> BuildLoop(TimingBelt belt, double offset)
        {
            if (belt == null) throw new ArgumentNullException(nameof(belt));
            belt.Validate();

            var radiusA = belt.Driver.PitchDiameter / 2.0;
            var radiusB = belt.Driven.PitchDiameter / 2.0;
            var distance = belt.CentreDistance;
            var theta = belt.CentreLineAngleDeg * Math.PI / 180.0;
            var cosTheta = Math.Cos(theta);
            var sinTheta = Math.Sin(theta);

            // In the frame with the line of centres along +X: the normal of the upper tangent line has x = (rA - rB) / C.
            var nx = (radiusA - radiusB) / distance;
            var ny = Math.Sqrt(Math.Max(0.0, 1.0 - nx * nx));
            var upper = Math.Atan2(ny, nx);
            var lower = Math.Atan2(-ny, nx);

            var points = new List<GearPoint>();

            void Add(double cx, double radius, double angle)
            {
                var x = cx + radius * Math.Cos(angle);
                var y = radius * Math.Sin(angle);
                points.Add(new GearPoint(x * cosTheta - y * sinTheta, x * sinTheta + y * cosTheta));
            }

            // Round the driven pulley from the lower to the upper tangent (anticlockwise), then round the driver from the upper to the lower.
            var stepsB = Math.Max(2, (int)Math.Ceiling((upper - lower) / ArcStep));
            for (var i = 0; i <= stepsB; i++) Add(distance, radiusB + offset, lower + (upper - lower) * i / stepsB);

            var lowerAround = lower + 2.0 * Math.PI;
            var stepsA = Math.Max(2, (int)Math.Ceiling((lowerAround - upper) / ArcStep));
            for (var i = 0; i <= stepsA; i++) Add(0.0, radiusA + offset, upper + (lowerAround - upper) * i / stepsA);

            return points;
        }

        /// <summary>
        /// Builds the world-space mesh and outlines of the belt, with its teeth. <paramref name="driverFrame"/> is the frame of the driver
        /// pulley's mid-plane (shaft placement and axial offset). <paramref name="driverPhaseRad"/> is the driver mount's phase: the teeth
        /// are placed in the grooves of the driver pulley at that phase (see <see cref="BeltRegistration"/>).
        /// </summary>
        public static void Build(TimingBelt belt, Transform3D driverFrame, out GearMesh3DBuilder.Mesh3D mesh, out List<List<GearPoint>> polylines,
            double driverPhaseRad = 0.0)
        {
            var loop = BeltToothPath.Build(belt, driverPhaseRad);
            var inner = new List<GearPoint>(loop.InnerRing);
            var outer = new List<GearPoint>(loop.OuterRing);
            var half = belt.Width / 2.0;
            var local = new GearMesh3DBuilder.Mesh3D();
            var count = inner.Count;

            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                var io = Point(outer[i], half);
                var jo = Point(outer[j], half);
                var ioBottom = Point(outer[i], -half);
                var joBottom = Point(outer[j], -half);
                var ii = Point(inner[i], half);
                var ji = Point(inner[j], half);
                var iiBottom = Point(inner[i], -half);
                var jiBottom = Point(inner[j], -half);

                AddQuad(local, io, ioBottom, joBottom, jo); // outer wall, outward
                AddQuad(local, ii, ji, jiBottom, iiBottom); // inner wall, towards the pulleys
                AddQuad(local, ii, io, jo, ji); // upper face
                AddQuad(local, iiBottom, jiBottom, joBottom, ioBottom); // lower face
            }

            local.CalculateNormals();
            mesh = GearAssemblySceneBuilder.TransformMesh(local, driverFrame);

            var outlines = new List<List<GearPoint>>
            {
                new List<GearPoint>(outer) { outer[0] },
                new List<GearPoint>(inner) { inner[0] }
            };
            polylines = GearAssemblySceneBuilder.TransformPolylines(outlines, driverFrame);
        }

        private static GearPoint3D Point(GearPoint p, double z) => new GearPoint3D(p.X, p.Y, z);

        private static void AddQuad(GearMesh3DBuilder.Mesh3D mesh, GearPoint3D a, GearPoint3D b, GearPoint3D c, GearPoint3D d)
        {
            var k = mesh.Vertices.Count;
            mesh.Vertices.AddRange(new[] { a, b, c, d });
            mesh.TriangleIndices.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
        }
    }
}

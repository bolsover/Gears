using System;
using System.Collections.Generic;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Assemblies
{
    /// <summary>
    /// Builds the shape of a <see cref="RollerChain"/>: for every roller a roller and a pin, and for every link two link plates. Link k joins
    /// roller k and roller k + 1; links with an even k are inner links (the plates close to the rollers, on the pins of the rollers) and links
    /// with an odd k are outer links (the plates on the outside of the inner plates). A plate is a stadium: a strip of the height of the
    /// plates between two semicircles round the pins. The geometry is made in the frame of the driver's shaft (see
    /// <see cref="RollerChainLayout"/>) and then moved with that frame. The XY plane is the mid-plane of the chain.
    /// </summary>
    public static class RollerChainMeshBuilder
    {
        private const int RollerSegments = 16;
        private const int PinSegments = 10;
        private const int PlateArcSegments = 8;

        /// <summary>The closed anticlockwise outline of the plate of the link that joins two roller centres (first point not repeated).</summary>
        public static List<GearPoint> PlateOutline(GearPoint from, GearPoint to, double height)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-9) throw new InvalidOperationException("Two rollers of the chain are at the same place.");
            var ux = dx / length;
            var uy = dy / length;
            var nx = -uy;
            var ny = ux;
            var half = height / 2.0;
            var minusNormal = Math.Atan2(-ny, -nx);

            var outline = new List<GearPoint>();

            // Round the far roller from the right side, through the direction of the link, to the left side.
            for (var i = 0; i <= PlateArcSegments; i++)
            {
                var angle = minusNormal + Math.PI * i / PlateArcSegments;
                outline.Add(new GearPoint(to.X + half * Math.Cos(angle), to.Y + half * Math.Sin(angle)));
            }

            // And round the near roller from the left side, against the direction of the link, to the right side.
            var plusNormal = Math.Atan2(ny, nx);
            for (var i = 0; i <= PlateArcSegments; i++)
            {
                var angle = plusNormal + Math.PI * i / PlateArcSegments;
                outline.Add(new GearPoint(from.X + half * Math.Cos(angle), from.Y + half * Math.Sin(angle)));
            }

            return outline;
        }

        /// <summary>A circle as a closed anticlockwise polygon (first point not repeated).</summary>
        public static List<GearPoint> Circle(GearPoint centre, double radius, int segments)
        {
            var circle = new List<GearPoint>(segments);
            for (var i = 0; i < segments; i++)
            {
                var angle = 2.0 * Math.PI * i / segments;
                circle.Add(new GearPoint(centre.X + radius * Math.Cos(angle), centre.Y + radius * Math.Sin(angle)));
            }

            return circle;
        }

        /// <summary>
        /// Builds the world-space mesh and outlines of the chain. <paramref name="driverFrame"/> is the frame of the driver sprocket's
        /// mid-plane (shaft placement and axial offset). <paramref name="driverPhaseRad"/> is the driver mount's phase: the rollers are placed
        /// in the tooth gaps of the driver sprocket at that phase (see <see cref="BeltRegistration"/>).
        /// </summary>
        public static void Build(RollerChain chain, Transform3D driverFrame, out GearMesh3DBuilder.Mesh3D mesh, out List<List<GearPoint>> polylines,
            double driverPhaseRad = 0.0)
        {
            var layout = RollerChainLayout.Build(chain, driverPhaseRad);
            var profile = chain.Profile;
            var rollers = layout.Rollers;
            var count = rollers.Count;

            var rollerHalf = profile.InnerWidth / 2.0;
            var innerOuterFace = rollerHalf + profile.InnerPlateThickness;
            var outerOuterFace = innerOuterFace + profile.OuterPlateThickness;

            var local = new GearMesh3DBuilder.Mesh3D();
            var outlines = new List<List<GearPoint>>();

            for (var k = 0; k < count; k++)
            {
                AddPrism(local, Circle(rollers[k], profile.RollerDiameter / 2.0, RollerSegments), -rollerHalf, rollerHalf);
                AddPrism(local, Circle(rollers[k], profile.PinDiameter / 2.0, PinSegments), -outerOuterFace, outerOuterFace);

                var plate = PlateOutline(rollers[k], rollers[(k + 1) % count], profile.PlateHeight);
                var inner = k % 2 == 0;
                var low = inner ? rollerHalf : innerOuterFace;
                var high = inner ? innerOuterFace : outerOuterFace;
                AddPrism(local, plate, low, high);
                AddPrism(local, plate, -high, -low);

                outlines.Add(new List<GearPoint>(plate) { plate[0] });
                outlines.Add(new List<GearPoint>(Circle(rollers[k], profile.RollerDiameter / 2.0, RollerSegments)) { new GearPoint(rollers[k].X + profile.RollerDiameter / 2.0, rollers[k].Y) });
            }

            local.CalculateNormals();
            mesh = GearAssemblySceneBuilder.TransformMesh(local, driverFrame);
            polylines = GearAssemblySceneBuilder.TransformPolylines(outlines, driverFrame);
        }

        /// <summary>A convex prism between two heights; the polygon must run anticlockwise. The caps are fans from the middle.</summary>
        private static void AddPrism(GearMesh3DBuilder.Mesh3D mesh, List<GearPoint> polygon, double zLow, double zHigh)
        {
            var count = polygon.Count;
            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                AddQuad(mesh,
                    new GearPoint3D(polygon[i].X, polygon[i].Y, zHigh), new GearPoint3D(polygon[i].X, polygon[i].Y, zLow),
                    new GearPoint3D(polygon[j].X, polygon[j].Y, zLow), new GearPoint3D(polygon[j].X, polygon[j].Y, zHigh));
            }

            var cx = 0.0;
            var cy = 0.0;
            foreach (var p in polygon)
            {
                cx += p.X;
                cy += p.Y;
            }

            cx /= count;
            cy /= count;
            for (var i = 0; i < count; i++)
            {
                var j = (i + 1) % count;
                AddTriangle(mesh, new GearPoint3D(cx, cy, zHigh), new GearPoint3D(polygon[i].X, polygon[i].Y, zHigh), new GearPoint3D(polygon[j].X, polygon[j].Y, zHigh));
                AddTriangle(mesh, new GearPoint3D(cx, cy, zLow), new GearPoint3D(polygon[j].X, polygon[j].Y, zLow), new GearPoint3D(polygon[i].X, polygon[i].Y, zLow));
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
    }
}

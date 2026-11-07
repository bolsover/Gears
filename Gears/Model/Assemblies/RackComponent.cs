using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A straight-toothed rack that meshes with an involute pinion.
    /// <para>
    /// Local frame: the rack length runs along X, the tooth tips point towards +Y, the datum (pitch) line is the
    /// line Y = 0 and the face width is centred on the Z = 0 mid-plane. The centre of a tooth space lies at X = 0
    /// when the rack is not displaced; the displacement of the rack is its phase (millimetres along X).
    /// </para>
    /// A rack is mounted on a "carrier" shaft whose placement is solved from the pinion, so the carrier axis is
    /// parallel to the pinion axis and is not drawn.
    /// <para>
    /// A helical rack has a <see cref="HelixAngleDeg"/> and a <see cref="HelicalDirection"/>. As for helical gears, the module and
    /// the pressure angle are the normal values; the end-face (transverse) section of the rack has the pitch pi m / cos(beta) and the
    /// pressure angle atan(tan(alpha) / cos(beta)), and each tooth is sheared along X by tan(beta) per unit of Z. A rack meshes with a
    /// pinion of the same helix angle and the opposite hand, as external helical gears do.
    /// </para>
    /// </summary>
    public class RackComponent : GearComponent
    {
        private string _helicalDirection = "R";

        /// <summary>Largest helix angle accepted, in degrees.</summary>
        public const double MaximumHelixAngleDeg = 60.0;

        public RackComponent(string id) : base(id)
        {
        }

        /// <summary>Helix angle in degrees; zero for a straight-toothed rack.</summary>
        public double HelixAngleDeg { get; set; }

        /// <summary>"R" (right) or "L" (left). Any value starting with L/l is treated as left.</summary>
        public string HelicalDirection
        {
            get => _helicalDirection;
            set => _helicalDirection = value?.StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
        }

        public bool IsHelical => HelixAngleDeg > 1e-6;

        /// <summary>
        /// True for a herringbone rack: a helical rack mirrored about its middle plane, so each half of the face width has the helix angle
        /// with the opposite hand. Ignored (the rack stays straight) when the helix angle is zero.
        /// </summary>
        public bool IsHerringbone { get; set; }

        /// <summary>Cosine of the helix angle (1 for a straight rack).</summary>
        private double CosHelix => IsHelical ? Math.Cos(HelixAngleDeg * Math.PI / 180.0) : 1.0;

        /// <summary>Pressure angle in the end-face section (radians): tan(alpha_t) = tan(alpha_n) / cos(beta).</summary>
        public double TransversePressureAngleRad => Math.Atan(Math.Tan(PressureAngleDeg * Math.PI / 180.0) / CosHelix);

        /// <summary>
        /// Sideways shift (X) of a tooth section per unit of Z. A right-hand rack meshes with a left-hand pinion, whose teeth move
        /// towards +X with +Z at the contact point, so a right-hand rack moves towards -X.
        /// </summary>
        public double ShearPerZ => !IsHelical ? 0.0 : (HelicalDirection == "R" ? -1.0 : 1.0) * Math.Tan(HelixAngleDeg * Math.PI / 180.0);

        /// <summary>Number of teeth along the rack.</summary>
        public int Teeth { get; set; } = 12;

        /// <summary>Bottom clearance (mm).</summary>
        public double Clearance => 0.25 * Module;

        /// <summary>Circular backlash (mm); the rack teeth are made thinner by half of it.</summary>
        public double Backlash { get; set; }

        /// <summary>Height of the rack body below the tooth roots (mm).</summary>
        public double BodyHeight { get; set; } = 10.0;

        /// <summary>Distance between two teeth along the rack in the end-face section (mm): pi m / cos(beta).</summary>
        public double Pitch => Math.PI * Module / CosHelix;

        /// <summary>Length of the toothed part of the rack (mm).</summary>
        public double Length => Teeth * Pitch;

        public override int ToothCount => Teeth;

        /// <summary>A rack has no pitch circle; zero is returned and the pitch line velocity is its linear speed.</summary>
        public override double PitchDiameter => 0.0;

        public override string TypeName => IsHelical ? (IsHerringbone ? "Herringbone rack" : "Helical rack") : "Rack";

        public override bool IsLinear => true;

        public override bool NeedsShaftMesh => false;

        public override Transform3D GetLocalMotion(double phase) => Transform3D.Translate(new Vector3D(phase, 0, 0));

        public void Validate()
        {
            if (Teeth < 1) throw new InvalidOperationException($"Rack '{Id}': the number of teeth must be at least 1.");
            if (Module <= 0) throw new InvalidOperationException($"Rack '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Rack '{Id}': the face width must be positive.");
            if (BodyHeight <= 0) throw new InvalidOperationException($"Rack '{Id}': the body height must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 60)
            {
                throw new InvalidOperationException($"Rack '{Id}': the pressure angle must be between 0 and 60 degrees.");
            }

            if (Backlash < 0 || Backlash >= Pitch / 2.0)
            {
                throw new InvalidOperationException($"Rack '{Id}': the backlash must be between 0 and half the pitch.");
            }

            if (HelixAngleDeg < 0 || HelixAngleDeg > MaximumHelixAngleDeg)
            {
                throw new InvalidOperationException(
                    $"Rack '{Id}': the helix angle must be between 0 (straight teeth) and {MaximumHelixAngleDeg:0} degrees.");
            }
        }

        /// <summary>The closed, anticlockwise outline of the rack in its local XY plane (first point not repeated).</summary>
        public List<GearPoint> BuildOutline()
        {
            Validate();

            var p = Pitch;
            var tanAlpha = Math.Tan(TransversePressureAngleRad);
            var addendum = Module;
            var dedendum = Module + Clearance;
            var thickness = p / 2.0 - Backlash / 2.0;

            var halfTip = thickness / 2.0 - addendum * tanAlpha;
            var halfRoot = Math.Min(thickness / 2.0 + dedendum * tanAlpha, p / 2.0);
            if (halfTip <= 0)
            {
                throw new InvalidOperationException(
                    $"Rack '{Id}': the pressure angle is too large for the tooth tip to have a flat top.");
            }

            var firstTooth = -(Teeth / 2);
            var left = (firstTooth) * p;
            var right = (firstTooth + Teeth) * p;
            var bottom = -dedendum - BodyHeight;

            var outline = new List<GearPoint>
            {
                new GearPoint(left, bottom),
                new GearPoint(right, bottom),
                new GearPoint(right, -dedendum)
            };

            for (var k = firstTooth + Teeth - 1; k >= firstTooth; k--)
            {
                var centre = (k + 0.5) * p;
                outline.Add(new GearPoint(centre + halfRoot, -dedendum));
                outline.Add(new GearPoint(centre + halfTip, addendum));
                outline.Add(new GearPoint(centre - halfTip, addendum));
                outline.Add(new GearPoint(centre - halfRoot, -dedendum));
            }

            outline.Add(new GearPoint(left, -dedendum));
            return RemoveDuplicates(outline);
        }

        public override GearGeometry BuildGeometry()
        {
            var outline = BuildOutline();
            var closed = new List<GearPoint>(outline) { outline[0] };
            GearMesh3DBuilder.Mesh3D mesh;
            if (IsHelical && IsHerringbone)
            {
                // Both halves lean the same way away from the mid-plane (x = shear * |z|), so the teeth form a V.
                mesh = SolidMeshBuilder.Loft(outline, FaceWidth / 2.0, 1.0, 0.0, 1.0, false, ShearPerZ);
                var lower = SolidMeshBuilder.Loft(outline, 0.0, 1.0, -FaceWidth / 2.0, 1.0, false, -ShearPerZ);
                var offset = mesh.Vertices.Count;
                mesh.Vertices.AddRange(lower.Vertices);
                mesh.TriangleIndices.AddRange(lower.TriangleIndices.Select(i => i + offset));
                mesh.CalculateNormals();
            }
            else
            {
                mesh = SolidMeshBuilder.Loft(outline, FaceWidth / 2.0, 1.0, -FaceWidth / 2.0, 1.0, false, ShearPerZ);
            }

            return new GearGeometry(null, new List<List<GearPoint>> { closed }, mesh);
        }

        private static List<GearPoint> RemoveDuplicates(List<GearPoint> points)
        {
            var result = new List<GearPoint>(points.Count);
            foreach (var point in points)
            {
                if (result.Count > 0)
                {
                    var last = result[result.Count - 1];
                    if (Math.Abs(last.X - point.X) < 1e-9 && Math.Abs(last.Y - point.Y) < 1e-9) continue;
                }

                result.Add(point);
            }

            if (result.Count > 1)
            {
                var first = result[0];
                var last = result[result.Count - 1];
                if (Math.Abs(last.X - first.X) < 1e-9 && Math.Abs(last.Y - first.Y) < 1e-9) result.RemoveAt(result.Count - 1);
            }

            return result;
        }
    }
}

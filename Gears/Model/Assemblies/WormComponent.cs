using System;
using System.Collections.Generic;
using Bolsover.Gears.Calculator;

namespace Bolsover.Gears.Model.Assemblies
{
    /// <summary>
    /// A cylindrical worm with one or more thread starts.
    /// <para>
    /// <see cref="GearComponent.Module"/> is the axial module (the thread pitch is pi times the module),
    /// <see cref="GearComponent.PressureAngleDeg"/> the normal pressure angle and <see cref="GearComponent.FaceWidth"/>
    /// the threaded length. The pitch diameter is <c>q * m</c> where q is the <see cref="DiameterQuotient"/>.
    /// </para>
    /// <para>
    /// The thread is swept as a helicoid: the radius at the angle theta and the axial position z follows the trapezoidal
    /// axial tooth profile of a rack at <c>u = z -/+ lead * theta / 2pi</c> (upper sign for a right-hand thread).
    /// Local frame: the axis is Z, centred on the Z = 0 mid-plane; at phase zero a thread centre crosses the +X side at
    /// Z = 0.
    /// </para>
    /// </summary>
    public class WormComponent : GearComponent
    {
        private string _helicalDirection = "R";

        public WormComponent(string id) : base(id)
        {
            FaceWidth = 40.0;
        }

        /// <summary>Number of thread starts (the "tooth count" of a worm).</summary>
        public int Starts { get; set; } = 1;

        /// <summary>Pitch diameter divided by the module (typically 7 to 17).</summary>
        public double DiameterQuotient { get; set; } = 10.0;

        /// <summary>"R" (right) or "L" (left). Any value starting with L/l is treated as left.</summary>
        public string HelicalDirection
        {
            get => _helicalDirection;
            set => _helicalDirection = value?.StartsWith("L", StringComparison.OrdinalIgnoreCase) == true ? "L" : "R";
        }

        public bool IsRightHand => HelicalDirection == "R";

        /// <summary>Bottom clearance (mm).</summary>
        public double Clearance => 0.25 * Module;

        public override int ToothCount => Starts;

        public override double PitchDiameter => DiameterQuotient * Module;

        public override string TypeName => "Worm";

        /// <summary>Axial pitch of the thread (mm).</summary>
        public double AxialPitch => Math.PI * Module;

        /// <summary>Lead: the axial advance per revolution (mm).</summary>
        public double Lead => Starts * AxialPitch;

        public double PitchRadius => PitchDiameter / 2.0;
        public double TipRadius => PitchRadius + Module;
        public double RootRadius => PitchRadius - Module - Clearance;

        /// <summary>Lead angle at the pitch cylinder in radians: tan(gamma) = starts / q.</summary>
        public double LeadAngleRad => Math.Atan2(Starts, DiameterQuotient);

        public double LeadAngleDeg => LeadAngleRad * 180.0 / Math.PI;

        /// <summary>Pressure angle in the axial plane (radians): tan(alpha_x) = tan(alpha_n) / cos(gamma).</summary>
        public double AxialPressureAngleRad => Math.Atan(Math.Tan(PressureAngleDeg * Math.PI / 180.0) / Math.Cos(LeadAngleRad));

        public void Validate()
        {
            if (Starts < 1 || Starts > 8) throw new InvalidOperationException($"Worm '{Id}': the number of starts must be between 1 and 8.");
            if (Module <= 0) throw new InvalidOperationException($"Worm '{Id}': the module must be positive.");
            if (FaceWidth <= 0) throw new InvalidOperationException($"Worm '{Id}': the threaded length must be positive.");
            if (DiameterQuotient <= 0) throw new InvalidOperationException($"Worm '{Id}': the diameter quotient must be positive.");
            if (PressureAngleDeg <= 0 || PressureAngleDeg >= 45)
            {
                throw new InvalidOperationException($"Worm '{Id}': the pressure angle must be between 0 and 45 degrees.");
            }

            if (RootRadius <= 0.1 * Module)
            {
                throw new InvalidOperationException(
                    $"Worm '{Id}': the diameter quotient ({DiameterQuotient}) is too small; the root radius would be {RootRadius} mm.");
            }
        }

        /// <summary>
        /// Radius of the thread surface at the angle <paramref name="thetaRad"/> and the axial position <paramref name="z"/>.
        /// </summary>
        public double RadiusAt(double thetaRad, double z)
        {
            var pitch = AxialPitch;
            var handSign = IsRightHand ? 1.0 : -1.0;
            var u = z - handSign * Lead * thetaRad / (2.0 * Math.PI);
            u -= pitch * Math.Round(u / pitch);

            var radius = PitchRadius + (pitch / 4.0 - Math.Abs(u)) / Math.Tan(AxialPressureAngleRad);
            return Math.Max(RootRadius, Math.Min(TipRadius, radius));
        }

        /// <summary>Number of samples around the circumference used for the outline and the mesh.</summary>
        public int AngularSegments => Math.Max(48, 24 * Starts);

        public override GearGeometry BuildGeometry()
        {
            Validate();

            var n = AngularSegments;
            var half = FaceWidth / 2.0;
            var zSteps = Math.Max(1, (int)Math.Ceiling(FaceWidth / (AxialPitch / 16.0)));

            var mesh = new GearMesh3DBuilder.Mesh3D();
            for (var k = 0; k <= zSteps; k++)
            {
                var z = -half + FaceWidth * k / zSteps;
                for (var i = 0; i < n; i++)
                {
                    var theta = 2.0 * Math.PI * i / n;
                    var r = RadiusAt(theta, z);
                    mesh.Vertices.Add(new GearPoint3D(r * Math.Cos(theta), r * Math.Sin(theta), z));
                }
            }

            for (var k = 0; k < zSteps; k++)
            {
                for (var i = 0; i < n; i++)
                {
                    var next = (i + 1) % n;
                    var v0 = k * n + i;
                    var v1 = k * n + next;
                    var v2 = (k + 1) * n + next;
                    var v3 = (k + 1) * n + i;
                    mesh.TriangleIndices.AddRange(new[] { v0, v1, v2, v0, v2, v3 });
                }
            }

            // Smooth normals for the thread surface are computed before the flat caps are added.
            mesh.CalculateNormals();
            AddCap(mesh, 0, -half, false);
            AddCap(mesh, zSteps, half, true);

            var outline = new List<GearPoint>(n + 1);
            for (var i = 0; i <= n; i++)
            {
                var theta = 2.0 * Math.PI * (i % n) / n;
                var r = RadiusAt(theta, 0.0);
                outline.Add(new GearPoint(r * Math.Cos(theta), r * Math.Sin(theta)));
            }

            return new GearGeometry(null, new List<List<GearPoint>> { outline }, mesh);
        }

        private void AddCap(GearMesh3DBuilder.Mesh3D mesh, int ringLevel, double z, bool facesUp)
        {
            var n = AngularSegments;
            var normalZ = facesUp ? 1.0 : -1.0;

            var ring = mesh.Vertices.Count;
            for (var i = 0; i < n; i++)
            {
                var source = mesh.Vertices[ringLevel * n + i];
                mesh.Vertices.Add(new GearPoint3D(source.X, source.Y, z));
                mesh.Normals.Add(new GearPoint3D(0, 0, normalZ));
            }

            var centre = mesh.Vertices.Count;
            mesh.Vertices.Add(new GearPoint3D(0, 0, z));
            mesh.Normals.Add(new GearPoint3D(0, 0, normalZ));

            for (var i = 0; i < n; i++)
            {
                var a = ring + i;
                var b = ring + (i + 1) % n;
                if (facesUp) mesh.TriangleIndices.AddRange(new[] { centre, a, b });
                else mesh.TriangleIndices.AddRange(new[] { centre, b, a });
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Bolsover.Gears.Calculator;
using Bolsover.Gears.Calculator.Alibre;
using Bolsover.Gears.Calculator.Assemblies;
using Bolsover.Gears.Model;
using Bolsover.Gears.Model.Assemblies;
using NUnit.Framework;

namespace UnitTests
{
    /// <summary>
    /// Tests of the rack, helical rack, worm and worm wheel export to Alibre, and of the helical rack that goes with it. The sketch
    /// plans are pure geometry, so they need no running Alibre; they are compared with the 3D geometry of the same components.
    /// </summary>
    [TestFixture]
    public class TestAlibreRackWormExport
    {
        private const double CmPerMm = 0.1;

        private static RackComponent CreateRack(int teeth = 12, double module = 2.0, double helixAngle = 0.0, string hand = "R",
            double faceWidth = 15.0, double backlash = 0.0)
        {
            return new RackComponent("R1")
            {
                Teeth = teeth,
                Module = module,
                PressureAngleDeg = 20.0,
                FaceWidth = faceWidth,
                Backlash = backlash,
                HelixAngleDeg = helixAngle,
                HelicalDirection = hand
            };
        }

        private static WormComponent CreateWorm(int starts = 1, double module = 2.0, double quotient = 10.0, string hand = "R", double length = 40.0)
        {
            return new WormComponent("W1")
            {
                Starts = starts,
                Module = module,
                PressureAngleDeg = 20.0,
                DiameterQuotient = quotient,
                HelicalDirection = hand,
                FaceWidth = length
            };
        }

        private static InvoluteGearComponent CreatePinion(int teeth = 20, double helixAngle = 0.0, string hand = "R")
        {
            return new InvoluteGearComponent("P1")
            {
                Teeth = teeth,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = 15.0,
                HelixAngleDeg = helixAngle,
                HelicalDirection = hand
            };
        }

        /// <summary>Area of the closed polygon through the figure start points, in square centimetres.</summary>
        private static double Area(System.Collections.Generic.IReadOnlyList<ToothSketchFigure> figures)
        {
            var area = 0.0;
            foreach (var f in figures) area += f.StartX * f.EndY - f.EndX * f.StartY;
            return Math.Abs(area) / 2.0;
        }

        private static double Volume(GearMesh3DBuilder.Mesh3D mesh)
        {
            var volume = 0.0;
            for (var i = 0; i + 2 < mesh.TriangleIndices.Count; i += 3)
            {
                var a = mesh.Vertices[mesh.TriangleIndices[i]];
                var b = mesh.Vertices[mesh.TriangleIndices[i + 1]];
                var c = mesh.Vertices[mesh.TriangleIndices[i + 2]];
                volume += (a.X * (b.Y * c.Z - b.Z * c.Y) - a.Y * (b.X * c.Z - b.Z * c.X) + a.Z * (b.X * c.Y - b.Y * c.X)) / 6.0;
            }

            return volume;
        }

        #region Rack

        [TestCase(12, 2.0, 0.0, 0.0)]
        [TestCase(5, 1.5, 0.0, 0.1)]
        [TestCase(20, 3.0, 0.0, 0.0)]
        [TestCase(12, 2.0, 15.0, 0.0)]
        [TestCase(8, 2.5, 30.0, 0.2)]
        public void RackSketch_IsClosedAndOnePitchWide(int teeth, double module, double helix, double backlash)
        {
            var rack = CreateRack(teeth, module, helix, "R", 15.0, backlash);
            var sketch = RackToothSketchBuilder.Build(rack);

            Assert.IsTrue(sketch.IsClosed(), "Every figure end point must meet exactly one other end point.");
            Assert.IsTrue(sketch.Figures.All(f => f.Kind == ToothSketchFigureKind.Line), "A rack tooth cell is made of lines only.");

            var xs = sketch.Figures.SelectMany(f => new[] { f.StartX, f.EndX }).ToList();
            Assert.AreEqual(0.0, xs.Min(), 1e-9, "The cell starts in the middle of a tooth space at X = 0.");
            Assert.AreEqual(rack.Pitch * CmPerMm, xs.Max(), 1e-6, "The cell is one transverse pitch wide, in centimetres.");
            Assert.AreEqual(rack.Pitch * CmPerMm, sketch.Pitch, 1e-6, "The pattern spacing is the transverse pitch.");
            Assert.AreEqual(teeth, sketch.Teeth);
            Assert.AreEqual(rack.FaceWidth * CmPerMm, sketch.ExtrusionDepth, 1e-9);
        }

        [Test]
        public void RackSketch_HeightsAreAddendumAndDedendum()
        {
            var rack = CreateRack(module: 2.0);
            var sketch = RackToothSketchBuilder.Build(rack);

            var ys = sketch.Figures.SelectMany(f => new[] { f.StartY, f.EndY }).ToList();
            Assert.AreEqual(rack.Module * CmPerMm, ys.Max(), 1e-9, "The tooth tip is one module above the datum line.");
            Assert.AreEqual((-(rack.Module + rack.Clearance) - rack.BodyHeight) * CmPerMm, ys.Min(), 1e-9, "The body bottom is below the root.");
            Assert.IsTrue(ys.Any(y => Math.Abs(y + (rack.Module + rack.Clearance) * CmPerMm) < 1e-9), "There are points at the root line.");
        }

        [TestCase(0.0, "R")]
        [TestCase(15.0, "R")]
        [TestCase(20.0, "L")]
        [TestCase(35.0, "R")]
        public void RackSketch_TimesTeethGivesTheAreaOfTheRackOutline(double helix, string hand)
        {
            var rack = CreateRack(10, 2.0, helix, hand);
            var sketch = RackToothSketchBuilder.Build(rack);

            var outlineArea = Math.Abs(SolidMeshBuilder.SignedArea(rack.BuildOutline())) * CmPerMm * CmPerMm;
            Assert.AreEqual(outlineArea, Area(sketch.Figures) * rack.Teeth, outlineArea * 1e-5,
                "The patterned cells must add up to the rack that is shown in the 3D view.");
        }

        [Test]
        public void StraightRack_IsNotHelical()
        {
            var sketch = RackToothSketchBuilder.Build(CreateRack());

            Assert.IsFalse(sketch.IsHelical);
            Assert.AreEqual(0.0, sketch.ShearPerDepth, 0.0);
            Assert.AreEqual(0.0, sketch.ShearTotal, 0.0);
            Assert.AreEqual("Rack_Z12_M2", sketch.PartName);
        }

        [Test]
        public void HelicalRack_ShearsByTanBetaAndHasTransversePitch()
        {
            const double beta = 20.0;
            var tan = Math.Tan(beta * Math.PI / 180.0);

            var left = RackToothSketchBuilder.Build(CreateRack(helixAngle: beta, hand: "L", faceWidth: 15.0));
            var right = RackToothSketchBuilder.Build(CreateRack(helixAngle: beta, hand: "R", faceWidth: 15.0));

            Assert.IsTrue(left.IsHelical);
            Assert.AreEqual(beta, left.HelixAngleDeg, 0.0);
            Assert.AreEqual(tan, left.ShearPerDepth, 1e-12, "A left-hand rack drifts towards +X with +Z.");
            Assert.AreEqual(-tan, right.ShearPerDepth, 1e-12, "A right-hand rack drifts towards -X with +Z.");
            Assert.AreEqual(tan * 15.0 * CmPerMm, left.ShearTotal, 1e-9, "The slanted path ends tan(beta) times the face width to the side.");
            Assert.IsFalse(left.RightHand);
            Assert.IsTrue(right.RightHand);

            var expectedPitch = Math.PI * 2.0 / Math.Cos(beta * Math.PI / 180.0) * CmPerMm;
            Assert.AreEqual(expectedPitch, left.Pitch, 1e-6, "The transverse pitch is pi m_n / cos(beta).");
            Assert.AreEqual("HelicalRack_Z12_M2_B20L", left.PartName);
            Assert.AreEqual("HelicalRack_Z12_M2_B20R", right.PartName);
        }

        [Test]
        public void HelicalRack_UsesTheTransversePressureAngle()
        {
            const double beta = 25.0;
            var rack = CreateRack(helixAngle: beta);

            var expected = Math.Atan(Math.Tan(20.0 * Math.PI / 180.0) / Math.Cos(beta * Math.PI / 180.0));
            Assert.AreEqual(expected, rack.TransversePressureAngleRad, 1e-12);

            // The flank of the sketch leans at the transverse pressure angle: dx / dy of the first flank.
            var sketch = RackToothSketchBuilder.Build(rack);
            var flank = sketch.Figures.First(f => Math.Abs(f.EndY - f.StartY) > 1e-6 && Math.Abs(f.EndX - f.StartX) > 1e-6);
            Assert.AreEqual(Math.Tan(expected), Math.Abs((flank.EndX - flank.StartX) / (flank.EndY - flank.StartY)), 1e-5);
        }

        [Test]
        public void HelicalRack_RejectsBadHelixAngle()
        {
            Assert.Throws<InvalidOperationException>(() => RackToothSketchBuilder.Build(CreateRack(helixAngle: 70.0)));
            Assert.Throws<InvalidOperationException>(() => RackToothSketchBuilder.Build(CreateRack(helixAngle: -5.0)));
        }

        [Test]
        public void RackSketch_RejectsAFlatlessTooth()
        {
            var rack = CreateRack();
            rack.PressureAngleDeg = 55.0;

            Assert.Throws<InvalidOperationException>(() => RackToothSketchBuilder.Build(rack));
        }

        [Test]
        public void HelicalRack_Mesh_IsShearedAndKeepsItsVolume()
        {
            var rack = CreateRack(6, 2.0, 20.0, "L", 15.0);
            var mesh = rack.BuildGeometry().Mesh;

            Assert.Greater(Volume(mesh), 0.0, "Faces must point outwards.");

            var top = mesh.Vertices.Where(v => Math.Abs(v.Z - 7.5) < 1e-9).Select(v => v.X).Min();
            var bottom = mesh.Vertices.Where(v => Math.Abs(v.Z + 7.5) < 1e-9).Select(v => v.X).Min();
            Assert.AreEqual(rack.ShearPerZ * 15.0, top - bottom, 1e-9, "The far face is shifted by tan(beta) times the face width.");

            var straight = CreateRack(6, 2.0, 20.0, "L", 15.0).BuildOutline();
            Assert.AreEqual(Math.Abs(SolidMeshBuilder.SignedArea(straight)) * 15.0, Volume(mesh), 1e-6, "Shearing does not change the volume.");
        }

        [Test]
        public void StraightRack_Mesh_IsNotSheared()
        {
            var mesh = CreateRack(6).BuildGeometry().Mesh;

            var top = mesh.Vertices.Where(v => Math.Abs(v.Z - 7.5) < 1e-9).Select(v => v.X).Min();
            var bottom = mesh.Vertices.Where(v => Math.Abs(v.Z + 7.5) < 1e-9).Select(v => v.X).Min();
            Assert.AreEqual(0.0, top - bottom, 1e-12);
        }

        #endregion

        #region Rack and pinion mesh

        [Test]
        public void HelicalRackShear_FollowsTheToothDriftOfTheMeshingPinion()
        {
            // A pinion tooth turns by tan(beta) z / r about Z (right hand: anticlockwise), so at the contact point on the +X side it
            // moves by tan(beta) z along +Y, which is the rack's +X. The meshing rack must drift the same way.
            const double beta = 15.0;
            var rack = CreateRack(helixAngle: beta, hand: "L");

            Assert.AreEqual(Math.Tan(beta * Math.PI / 180.0), rack.ShearPerZ, 1e-12);
        }

        [Test]
        public void HelicalPinionAndRack_SolveWithTransversePitchRadius()
        {
            const double beta = 15.0;
            var pinion = CreatePinion(20, beta, "R");
            var rack = CreateRack(10, 2.0, beta, "L");

            var result = new RackMeshSolver().Solve(pinion, rack, 0.5);

            var radius = 2.0 * 20 / (2.0 * Math.Cos(beta * Math.PI / 180.0));
            Assert.AreEqual(GearPrecision.Round(radius), result.CentreDistance, 1e-7, "The datum line is a transverse pitch radius from the axis.");
            Assert.AreEqual(GearPrecision.Round(radius * 0.5), result.DrivenPhaseRad, 1e-7, "The rack slides by r_t times the pinion angle.");
            Assert.AreEqual(0.5, result.GearRatio, 1e-7);
            Assert.AreEqual(2.0 * Math.PI * radius, new RackMeshSolver().SpeedFactor(pinion, rack), 1e-9);
        }

        [Test]
        public void SpurPinionAndStraightRack_StillSolveWithTheSpurPitchRadius()
        {
            var result = new RackMeshSolver().Solve(CreatePinion(20), CreateRack(10), 1.0);

            Assert.AreEqual(20.0, result.CentreDistance, 1e-7);
            Assert.AreEqual(20.0, result.DrivenPhaseRad, 1e-7);
        }

        [Test]
        public void HelicalPinionAndRack_NeedOppositeHandsAndTheSameAngle()
        {
            var solver = new RackMeshSolver();

            Assert.Throws<InvalidOperationException>(() => solver.Solve(CreatePinion(20, 15.0, "R"), CreateRack(10, 2.0, 15.0, "R"), 0.0),
                "Two right-hand helical members cannot mesh.");
            Assert.Throws<InvalidOperationException>(() => solver.Solve(CreatePinion(20, 15.0, "R"), CreateRack(10, 2.0, 20.0, "L"), 0.0),
                "The helix angles must be equal.");
            Assert.Throws<InvalidOperationException>(() => solver.Solve(CreatePinion(20, 15.0, "R"), CreateRack(10), 0.0),
                "A helical pinion needs a helical rack.");
            Assert.Throws<InvalidOperationException>(() => solver.Solve(CreatePinion(20), CreateRack(10, 2.0, 15.0, "L"), 0.0),
                "A spur pinion needs a straight rack.");
        }

        [Test]
        public void HelicalRack_SurvivesSaveAndLoad()
        {
            var pinion = CreatePinion(20, 15.0, "R");
            var rack = CreateRack(10, 2.0, 15.0, "L");

            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(pinion);
            var s2 = new Shaft("S2");
            s2.Mount(rack);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(pinion, rack, MeshType.Rack, 0.0);

            var loaded = AssemblySerializer.FromJson(AssemblySerializer.ToJson(assembly));

            var loadedRack = loaded.Shafts.SelectMany(s => s.Mounts).Select(m => m.Component).OfType<RackComponent>().Single();
            Assert.AreEqual(15.0, loadedRack.HelixAngleDeg, 0.0);
            Assert.AreEqual("L", loadedRack.HelicalDirection);
            Assert.IsTrue(loadedRack.IsHelical);
        }

        [Test]
        public void StraightRack_LoadsWithoutHelixFields()
        {
            var pinion = CreatePinion(20);
            var rack = CreateRack(10);

            var assembly = new GearAssembly();
            var s1 = new Shaft("S1");
            s1.Mount(pinion);
            var s2 = new Shaft("S2");
            s2.Mount(rack);
            assembly.AddShaft(s1);
            assembly.AddShaft(s2);
            assembly.AddRelation(pinion, rack, MeshType.Rack, 0.0);

            var json = AssemblySerializer.ToJson(assembly);
            var loaded = AssemblySerializer.FromJson(json);

            var loadedRack = loaded.Shafts.SelectMany(s => s.Mounts).Select(m => m.Component).OfType<RackComponent>().Single();
            Assert.IsFalse(loadedRack.IsHelical);
        }

        #endregion

        #region Worm

        [TestCase(1, 2.0, 10.0, "R", 40.0)]
        [TestCase(2, 2.0, 10.0, "L", 40.0)]
        [TestCase(3, 1.5, 12.0, "R", 50.0)]
        [TestCase(4, 3.0, 8.0, "R", 60.0)]
        public void WormSketch_IsClosedAndMatchesTheComponentRadii(int starts, double module, double quotient, string hand, double length)
        {
            var worm = CreateWorm(starts, module, quotient, hand, length);
            var sketch = WormThreadSketchBuilder.Build(worm);

            Assert.IsTrue(sketch.IsClosed(), "Every figure end point must meet exactly one other end point.");

            var radii = sketch.Figures.Where(f => f.Kind != ToothSketchFigureKind.Line)
                .SelectMany(FigurePoints).Select(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)).ToList();
            Assert.AreEqual(worm.TipRadius * CmPerMm, radii.Max(), 1e-6, "The thread tip is at the tip radius.");
            Assert.AreEqual(worm.RootRadius * CmPerMm, radii.Min(), 1e-6, "The thread root is at the root radius.");
            Assert.AreEqual(length * CmPerMm, sketch.Length, 1e-9);
            Assert.AreEqual(starts, sketch.Starts);
            Assert.AreEqual(starts > 1 ? 2 : 0, sketch.Figures.Count(f => f.Kind == ToothSketchFigureKind.Line),
                "A multi-start wedge is closed by two radial lines; a single start closes on itself.");
        }

        [TestCase(1, 2.0, 10.0, "R", 40.0)]
        [TestCase(2, 2.0, 10.0, "L", 40.0)]
        [TestCase(3, 1.5, 12.0, "R", 50.0)]
        public void WormSketch_PointsLieOnTheComponentThreadSurface(int starts, double module, double quotient, string hand, double length)
        {
            var worm = CreateWorm(starts, module, quotient, hand, length);
            var sketch = WormThreadSketchBuilder.Build(worm);

            foreach (var point in sketch.Figures.Where(f => f.Kind != ToothSketchFigureKind.Line).SelectMany(FigurePoints))
            {
                var x = point.X / CmPerMm;
                var y = point.Y / CmPerMm;
                var expected = worm.RadiusAt(Math.Atan2(y, x), -length / 2.0);
                Assert.AreEqual(expected, Math.Sqrt(x * x + y * y), 1e-5, "The sketch is the end-face section of the thread.");
            }
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void WormSketch_WedgeIsOneStartWide(int starts)
        {
            var sketch = WormThreadSketchBuilder.Build(CreateWorm(starts, 2.0, 10.0, "R", 40.0));

            var curves = sketch.Figures.Where(f => f.Kind != ToothSketchFigureKind.Line).ToList();
            var startAngle = Math.Atan2(curves.First().StartY, curves.First().StartX);
            var endAngle = Math.Atan2(curves.Last().EndY, curves.Last().EndX);
            var span = endAngle - startAngle;
            while (span <= 0) span += 2.0 * Math.PI;

            Assert.AreEqual(2.0 * Math.PI / starts, span, 1e-5, "The wedge spans 360 degrees divided by the number of starts.");
            Assert.AreEqual(2.0 * Math.PI / starts, sketch.PatternAngle, 1e-12);
        }

        [Test]
        public void WormSketch_SingleStartGoesAllTheWayRound()
        {
            var sketch = WormThreadSketchBuilder.Build(CreateWorm(1, 2.0, 10.0, "R", 40.0));

            var first = sketch.Figures.First();
            var last = sketch.Figures.Last();
            Assert.AreEqual(first.StartX, last.EndX, 1e-12, "The loop closes on its own start point.");
            Assert.AreEqual(first.StartY, last.EndY, 1e-12);
        }

        private static IEnumerable<GearPoint> FigurePoints(ToothSketchFigure figure)
        {
            if (figure.Points != null) return figure.Points;
            return new[] { new GearPoint(figure.StartX, figure.StartY), new GearPoint(figure.EndX, figure.EndY) };
        }

        [Test]
        public void WormSketch_HelixHasTheWormLeadAndHand()
        {
            var right = WormThreadSketchBuilder.Build(CreateWorm(2, 2.0, 10.0, "R"));
            var left = WormThreadSketchBuilder.Build(CreateWorm(2, 2.0, 10.0, "L"));

            Assert.IsTrue(right.Helix.RightHand);
            Assert.IsFalse(left.Helix.RightHand);
            Assert.AreEqual(2 * Math.PI * 2.0 * CmPerMm, right.Helix.Lead, 1e-6, "Lead is starts times pi times the axial module.");
            Assert.AreEqual(Math.PI, right.PatternAngle, 1e-12, "Two starts are half a turn apart.");
            Assert.AreEqual(right.Helix.Height / right.Helix.Lead, right.Helix.Revolutions, 1e-12);
            Assert.AreEqual("Worm_S2_M2_Q10_R", right.PartName);
            Assert.AreEqual("Worm_S2_M2_Q10_L", left.PartName);
        }

        [Test]
        public void WormSketch_HelixRunsTheWholeThreadedLength()
        {
            var worm = CreateWorm(1, 2.0, 10.0, "R", 40.0);
            var sketch = WormThreadSketchBuilder.Build(worm);

            Assert.AreEqual(40.0 * CmPerMm, sketch.Helix.Height, 1e-9, "The helical boss runs for the whole threaded length.");
            Assert.AreEqual(sketch.Length, sketch.Helix.Height, 1e-12);
            Assert.AreEqual(40.0 / worm.Lead, sketch.Helix.Revolutions, 1e-6, "Turns = length / lead, within the 7 decimal rounding of the lead.");
            Assert.AreEqual(worm.LeadAngleDeg, sketch.Helix.AngleDeg, 1e-9);
        }

        [Test]
        public void WormSketch_HandMirrorsTheSection()
        {
            var right = WormThreadSketchBuilder.Build(CreateWorm(1, 2.0, 10.0, "R", 40.0));
            var left = WormThreadSketchBuilder.Build(CreateWorm(1, 2.0, 10.0, "L", 40.0));

            // The tip arc is centred on the thread centre; for the opposite hand the thread centre is at the mirrored angle.
            ToothSketchFigure Tip(WormThreadSketch s) => s.Figures.Single(f => f.Name == "Tip arc");
            double Mid(ToothSketchFigure f) => (Math.Atan2(f.StartY, f.StartX) + Math.Atan2(f.EndY, f.EndX)) / 2.0;

            Assert.AreEqual(Math.Sin(Mid(Tip(right))), -Math.Sin(Mid(Tip(left))), 1e-6, "The left-hand section is the mirror image of the right-hand one.");
            Assert.AreEqual(Math.Cos(Mid(Tip(right))), Math.Cos(Mid(Tip(left))), 1e-6);
        }

        [Test]
        public void WormSketch_RejectsInvalidWorms()
        {
            Assert.Throws<InvalidOperationException>(() => WormThreadSketchBuilder.Build(CreateWorm(starts: 0)));
            Assert.Throws<InvalidOperationException>(() => WormThreadSketchBuilder.Build(CreateWorm(quotient: 0.5)));
        }

        #endregion

        #region Worm wheel

        [Test]
        public void WormWheel_IsAHelicalGearWithTheLeadAngle()
        {
            var worm = CreateWorm(2, 2.0, 10.0, "L");
            var wheel = new WormWheelComponent("G1")
            {
                Teeth = 30,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = 12.0,
                LeadAngleDeg = worm.LeadAngleDeg,
                HelicalDirection = worm.HelicalDirection
            };

            var gear = wheel.ToInvoluteGear();

            Assert.AreEqual(30, gear.Teeth);
            Assert.AreEqual(worm.LeadAngleDeg, gear.HelixAngleDeg, 1e-12);
            Assert.AreEqual("L", gear.HelicalDirection);
            Assert.AreEqual(2.0 * Math.Cos(worm.LeadAngleRad), gear.Module, 1e-12, "The wheel teeth use the normal module m cos(gamma).");
            Assert.AreEqual(12.0, gear.FaceWidth, 0.0);
            Assert.IsTrue(gear.IsHelical);
        }

        [Test]
        public void WormWheel_SketchIsTheHelicalToothWithAWheelName()
        {
            var wheel = new WormWheelComponent("G1")
            {
                Teeth = 30,
                Module = 2.0,
                PressureAngleDeg = 20.0,
                FaceWidth = 12.0,
                LeadAngleDeg = 11.25,
                HelicalDirection = "R"
            };

            var name = WormThreadSketchBuilder.PartName(wheel);
            var sketch = SingleToothSketchBuilder.Build(wheel.ToInvoluteGear(), name);

            Assert.AreEqual("WormWheel_Z30_M2_L11p25R", name);
            Assert.AreEqual(name, sketch.PartName);
            Assert.IsTrue(sketch.IsClosed());
            Assert.IsTrue(sketch.IsHelical);
            Assert.AreEqual(11.25, sketch.Helix.AngleDeg, 1e-12);
            Assert.IsTrue(sketch.Helix.RightHand);
            Assert.AreEqual(12.0 * CmPerMm, sketch.Helix.Height, 1e-9);
        }

        [Test]
        public void WormWheel_SketchBuilderKeepsTheDefaultNameWithoutOne()
        {
            var sketch = SingleToothSketchBuilder.Build(CreatePinion(20, 15.0, "R"));

            Assert.AreEqual("HelicalGear_Z20_M2_B15R", sketch.PartName);
        }

        #endregion
    }
}

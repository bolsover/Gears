using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AlibreX;
using Bolsover.Gears.Model.Assemblies;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>
    /// Creates a simple involute gear (external or internal, spur or helical) as a new Alibre part, the way a user would:
    /// 1. open a new part with a 2D sketch on the XY plane,
    /// 2. draw the closed sketch of one tooth with lines, arcs and splines,
    /// 3. form a single tooth: extrude the sketch to the face width for a spur gear, or, for a helical gear, sweep it along a helix
    ///    about the Z axis with a helical boss (the sketch turns through tan(beta) times the face width over the pitch radius),
    /// 4. make a circular pattern of the tooth feature around the Z axis to form the gear.
    /// For an internal gear the tooth is a wedge of the ring, between the tooth contour and the rim.
    /// The geometry comes from <see cref="SingleToothSketchBuilder"/>; this class only drives the Alibre API.
    /// </summary>
    public sealed class AlibreGearExporter
    {
        /// <summary>Name of the default plane of a new Alibre part that the tooth is sketched on.</summary>
        public const string SketchPlaneName = "XY-Plane";

        /// <summary>Name of the default axis of a new Alibre part that the tooth is patterned about.</summary>
        public const string PatternAxisName = "Z-Axis";

        /// <summary>
        /// True so that the helical boss keeps the tooth sketch parallel to the gear axis (the sketch stays in its own plane, the end-face
        /// section) instead of turning it normal to the helix path. The sketch is the transverse section, so the profile, the module and
        /// the lead (2 pi r / tan(beta)) are unaffected.
        /// </summary>
        public const bool HelicalBossParallelOriented = true;

        /// <summary>Name of the default axis of a new Alibre part along which the rack tooth is linear-patterned.</summary>
        public const string RackPatternAxisName = "X-Axis";

        /// <summary>
        /// Name of the default axis of a new Alibre part used as the second pattern direction of the rack pattern. Alibre rejects a linear
        /// pattern without a valid second axis ("The given second axis object is not valid") even when the second direction has a single
        /// instance, so the rack pattern is made two-directional with one instance in this direction.
        /// </summary>
        public const string RackPatternSecondAxisName = "Y-Axis";
        
       
        /// <summary>
        /// Name of the default plane that holds the sweep path of a helical rack tooth: it lies in a plane that contains the part
        /// axis. Sketch x is the global X; sketch y is the global Z when <see cref="AxialPlaneSketchYIsNegativeZ"/> is false.
        /// </summary>
        private const string AxialSketchPlaneName = "ZX-Plane";

        /// <summary>
        /// Set to true when Alibre's sketch y axis on the ZX plane points along -Z, so that the helical rack sweep path is drawn
        /// mirrored in y and still runs along +Z. This has been checked inside Alibre.
        /// </summary>
        public bool AxialPlaneSketchYIsNegativeZ { get; set; }

        public const string SketchName = "ToothProfile";
        public const string ShaftSketchName = "ShaftSection";
        public const string ShaftFeatureName = "Shaft";
        public const string CarrierFeatureName = "CarrierPlate";
        public const string PathSketchName = "ToothPath";
        public const string ThreadPatternFeatureName = "ThreadPattern";
        public const string RackPatternFeatureName = "RackTeeth";
        public const string ToothFeatureName = "Tooth";
        public const string PatternFeatureName = "ToothPattern";
        public const string MirrorFeatureName = "ToothMirror";
        public const string FlangeSketchName = "FlangeSection";
        public const string FlangeFeatureName = "Flange";
        public const string BeltSketchName = "BeltSection";
        public const string BeltFeatureName = "Belt";
        public const string PulleyAxisName = "Z-Axis";

        /// <summary>Name of the default plane of a new Alibre part that the back cone plane of a bevel gear is first offset from.</summary>
        public const string BevelOffsetFromPlaneName = "YZ-Plane";

        /// <summary>Name of the default axis of a new Alibre part that the bevel gear blank is revolved and the valleys are patterned about.</summary>
        public const string BevelGearAxisName = "X-Axis";

        /// <summary>Name of the default plane of a new Alibre part that the axis of the back cone plane is made on (with the offset plane); the axis then runs along Z through pointC.</summary>
        public const string BevelAxisFromPlaneName = "ZX-Plane";

        public const string BevelBlankSketchName = "GearBlankProfile";
        public const string BevelBlankFeatureName = "GearBlank";
        public const string BevelOffsetPlaneName = "BackConeOffsetPlane";
        public const string BevelConeAxisName = "BackConeAxis";
        public const string BevelConePlaneName = "BackConePlane";
        public const string BevelToothSketchName = "ToothValleyProfile";
        public const string BevelApexPointName = "PitchConeApex";
        public const string BevelValleyFeatureName = "ToothValley";
        public const string BevelPatternFeatureName = "ToothValleyPattern";

        /// <summary>Largest distance, in Alibre units (centimetres), between where a new plane should be and where Alibre put it.</summary>
        private const double PlaneTolerance = 1e-4;

        private readonly IADRoot _root;

        /// <param name="root">The Alibre root object (<c>IADRoot</c>) that new parts are created in.</param>
        public AlibreGearExporter(IADRoot root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// True to pattern the geometry of the tooth directly, which is faster; false (the default) to regenerate the tooth
        /// feature for every instance.
        /// </summary>
        public bool PatternGeometry { get; set; }

        /// <summary>
        /// Creates the gear in a new Alibre part and returns the part session.
        /// Throws <see cref="InvalidOperationException"/> when the gear is not supported or Alibre rejects a step.
        /// </summary>
        public IADPartSession Export(InvoluteGearComponent gear)
        {
            return Export(gear, null);
        }

        /// <summary>
        /// Creates a worm wheel in a new Alibre part. A worm wheel is a helical involute gear (normal module m cos(gamma), helix angle
        /// equal to the lead angle of the worm, same hand), so it is made like any helical gear: one tooth swept along a helix, then
        /// patterned. The throat of an enveloping wheel is not modelled.
        /// </summary>
        public IADPartSession Export(WormWheelComponent wheel)
        {
            if (wheel == null) throw new ArgumentNullException(nameof(wheel));
            if (wheel.LeadAngleDeg <= 0.0)
            {
                throw new InvalidOperationException(
                    $"Worm wheel '{wheel.Id}' has no lead angle yet: mesh it with a worm first, so that its helix angle is known.");
            }

            return Export(wheel.ToInvoluteGear(), WormThreadSketchBuilder.PartName(wheel));
        }

        /// <summary>
        /// Creates a straight or helical rack in a new Alibre part: a closed sketch of one tooth cell on the XY plane, a single tooth
        /// formed by an extrusion to the face width (or, for a helical rack, a rigid sweep along a slanted path that shears the
        /// section), and a linear pattern of that tooth along the X axis. A herringbone rack is swept over half the face width, mirrored
        /// across the sketch plane, and the tooth and its mirror are patterned together.
        /// </summary>
        public IADPartSession Export(RackComponent rack)
        {
            var plan = RackToothSketchBuilder.Build(rack);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The tooth sketch of rack '{rack.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the tooth profile";
                var closed = DrawFigures(sketch, plan.Figures, null);

                IADPartFeature tooth;
                try
                {
                    if (plan.IsHelical)
                    {
                        stage = "drawing the sweep path of the helical tooth";
                        var pathPlane = session.DesignPlanes.Item(AxialSketchPlaneName);
                        var pathSketch = session.Sketches.AddSketch(null, pathPlane, PathSketchName);
                        var ySign = AxialPlaneSketchYIsNegativeZ ? -1.0 : 1.0;
                        pathSketch.BeginChange();
                        try
                        {
                            pathSketch.Figures.AddLine(0.0, 0.0, plan.ShearTotal, ySign * plan.SweepDepth);
                        }
                        finally
                        {
                            pathSketch.EndChange();
                        }

                        stage = "sweeping the tooth along its helical path";
                        var path = _root.NewObjectCollector();
                        path.Add(pathSketch);
                        tooth = session.Features.AddSweptBoss(sketch, path, true, ADPartFeatureEndCondition.AD_ENTIRE_PATH, null, null, 0.0, 0.0, false,
                            ToothFeatureName);
                    }
                    else
                    {
                        stage = "extruding the tooth";
                        tooth = session.Features.AddExtrudedBoss(sketch, plan.ExtrusionDepth, ADPartFeatureEndCondition.AD_TO_DEPTH, null, null, 0.0,
                            ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, ToothFeatureName, "", "");
                    }
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the tooth sketch is not closed: " + ex.Message, ex);
                }

                IADMirrorFeature mirror = null;
                if (plan.IsHerringbone)
                {
                    // The tooth was swept over half the depth from the sketch plane; its mirror across that plane is the other half.
                    stage = "mirroring the tooth across the " + SketchPlaneName;
                    mirror = AddHerringboneMirror(session, tooth, plane);
                }

                if (plan.Teeth > 1)
                {
                    stage = "patterning the tooth along the rack";
                    AddRackPattern(session, tooth, plan, mirror);
                }

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of rack '{rack.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a worm in a new Alibre part the way a helical gear is made: a closed sketch of the end-face section of one thread
        /// start on the XY plane, a single thread formed by sweeping that section along a helix about the Z axis with a helical boss
        /// (height = threaded length, pitch = lead), and, for several starts, a circular pattern of the thread about the Z axis.
        /// </summary>
        public IADPartSession Export(WormComponent worm)
        {
            var plan = WormThreadSketchBuilder.Build(worm);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The thread sketch of worm '{worm.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the thread section";
                var closed = DrawFigures(sketch, plan.Figures, plan.ReferenceCircles);

                var axis = session.DesignAxes.Item(PatternAxisName);
                IADPartFeature thread;
                try
                {
                    stage = "sweeping the thread along its helix";
                    thread = AddHelicalTooth(session, sketch, axis, plan.Helix);
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the thread sketch is not closed: " + ex.Message, ex);
                }

                CheckFeature(thread, "The helical thread");

                if (plan.Starts > 1)
                {
                    stage = "patterning the thread for each start";
                    var targets = _root.NewObjectCollector();
                    targets.Add(thread);
                    var pattern = session.Features.AddCircularFeaturePattern(targets, plan.Starts, plan.PatternAngle, null, axis, false,
                        PatternGeometry, ThreadPatternFeatureName);
                    CheckFeature(pattern, "The circular pattern of the thread");
                }

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of worm '{worm.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Patterns the single rack tooth along the rack. Alibre needs a valid second axis and a non-zero second spacing even for a pattern
        /// in one direction (it rejects a null second axis, a zero second spacing and counts passed as doubles), so the second direction is the
        /// <see cref="RackPatternSecondAxisName"/> with a single instance. Alibre can accept the call and still report the pattern feature as
        /// failed, which leaves a single tooth, so each attempt is checked and the next form is tried: first as configured, then patterning
        /// the geometry directly (or the feature, if the geometry was tried first).
        /// Throws <see cref="InvalidOperationException"/> when no attempt gives a valid pattern.
        /// </summary>
        private void AddRackPattern(IADPartSession session, IADPartFeature tooth, RackToothSketch plan, IADMirrorFeature mirror = null)
        {
            var axis = session.DesignAxes.Item(RackPatternAxisName);
            var secondAxis = session.DesignAxes.Item(RackPatternSecondAxisName);
            var failures = new List<string>();

            for (var attempt = 0; attempt < 2; attempt++)
            {
                IADLinearPatternFeature pattern = null;
                try
                {
                    var targets = _root.NewObjectCollector();
                    targets.Add(tooth);
                    if (mirror != null) targets.Add(mirror);

                    var patternGeometry = attempt == 1 ? !PatternGeometry : PatternGeometry;

                    pattern = session.Features.AddLinearFeaturePattern(targets, plan.Teeth, plan.Pitch, null, axis, false, 1, plan.Pitch,
                        null, secondAxis, false, patternGeometry, RackPatternFeatureName);

                    if (pattern != null && !pattern.HasError) return;
                    failures.Add($"attempt {attempt + 1}: Alibre reports an error in the pattern feature");
                }
                catch (Exception ex)
                {
                    failures.Add($"attempt {attempt + 1}: {ex.Message}");
                }

                try
                {
                    pattern?.Delete();
                }
                catch (Exception)
                {
                    // A pattern that cannot be removed is left for the user to see; the next attempt still runs.
                }
            }

            throw new InvalidOperationException(
                $"the linear pattern of {plan.Teeth} teeth along {RackPatternAxisName} was not accepted ({string.Join("; ", failures)})");
        }

        /// <summary>
        /// Creates a straight bevel gear in a new Alibre part, following the construction of the 3D view in the editor:
        /// 1. a sketch of the closed gear blank profile on the XY plane (gear axis along X),
        /// 2. a revolve of the blank about the X axis, and a design point at the pitch cone apex (created with <c>IADDesignPoints.CreatePoint</c>),
        /// 3. a plane offset from the YZ plane to the point where the back cone meets the X axis, an axis where that plane meets the ZX
        ///    plane (it runs along Z through that point), and a plane turned about that axis to lie on the back cone,
        /// 4. a sketch of one tooth valley (Tredgold, with trochoidal root fillets) on the back cone plane,
        /// 5. a loft cut from the valley sketch to the apex design point,
        /// 6. a circular pattern of the cut about the X axis, which forms all the teeth.
        /// The bore and hub of the editor model are not part of the export.
        /// </summary>
        public IADPartSession Export(BevelGearComponent gear)
        {
            var plan = BevelGearSketch.Build(gear);

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);
                var xyPlane = session.DesignPlanes.Item(SketchPlaneName);
                var gearAxis = session.DesignAxes.Item(BevelGearAxisName);

                stage = "adding the gear blank sketch on the " + SketchPlaneName;
                var blankSketch = session.Sketches.AddSketch(null, xyPlane, BevelBlankSketchName);

                stage = "drawing the gear blank profile";
                var closed = DrawFigures(blankSketch, plan.BlankFigures, null);

                stage = "revolving the gear blank about the " + BevelGearAxisName;
                IADPartFeature blank;
                try
                {
                    blank = session.Features.AddRevolvedBoss(blankSketch, null, gearAxis, 2.0 * Math.PI, BevelBlankFeatureName);
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the gear blank sketch is not closed: " + ex.Message, ex);
                }

                CheckFeature(blank, "The revolved gear blank");

                stage = "adding the design point at the apex of the pitch cone";
                var apexPoint = session.DesignPoints.CreatePoint(plan.ApexNodeX, 0.0, 0.0, BevelApexPointName);
                if (apexPoint == null) throw new InvalidOperationException("The design point at the pitch cone apex was not created.");

                stage = "adding a plane offset from the " + BevelOffsetFromPlaneName + " to the back cone apex";
                var offsetPlane = AddBackConeOffsetPlane(session, plan);

                stage = "adding an axis where the offset plane meets the " + BevelAxisFromPlaneName;
                var zxPlane = session.DesignPlanes.Item(BevelAxisFromPlaneName);
                var coneAxis = session.DesignAxes.CreateBy2Planes(null, offsetPlane, null, zxPlane, BevelConeAxisName);

                stage = "adding a plane on the back cone";
                var conePlane = AddBackConePlane(session, offsetPlane, coneAxis, plan);

                stage = "adding the tooth valley sketch on the back cone plane";
                var toothSketch = session.Sketches.AddSketch(null, conePlane, BevelToothSketchName);

                stage = "locating the axes of the back cone plane sketch";
                var map = SketchMap.Create(session, toothSketch, plan);

                stage = "drawing the tooth valley profile";
                var toothClosed = DrawMappedFigures(toothSketch, plan.ToothFigures, map);

                stage = "cutting a tooth valley with a loft to the apex design point";
                IADPartFeature valley;
                try
                {
                    valley = AddValleyLoftCut(session, toothSketch, apexPoint);
                }
                catch (InvalidOperationException) when (!toothClosed)
                {
                    throw new InvalidOperationException("Alibre reports that the tooth valley sketch is not closed.");
                }

                stage = "patterning the tooth valley about the " + BevelGearAxisName;
                var targets = _root.NewObjectCollector();
                targets.Add(valley);
                var pattern = session.Features.AddCircularFeaturePattern(targets, plan.Teeth, plan.PatternAngle, null, gearAxis, false,
                    PatternGeometry, BevelPatternFeatureName);
                CheckFeature(pattern, "The circular pattern of the tooth valley");

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of bevel gear '{gear.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// Adds the plane offset from the YZ plane to the point where the back cone meets the X axis. Alibre's offset direction is not
        /// known in advance, so the plane is read back and, when it is on the wrong side, replaced by the plane offset the other way.
        /// </summary>
        private static IADDesignPlane AddBackConeOffsetPlane(IADPartSession session, BevelGearSketch plan)
        {
            var yzPlane = session.DesignPlanes.Item(BevelOffsetFromPlaneName);
            string found = null;
            foreach (var sign in new[] { 1.0, -1.0 })
            {
                var plane = session.DesignPlanes.CreateAtOffsetToPlane(null, yzPlane, sign * plan.BackConeApexX, BevelOffsetPlaneName);
                if (!TryReadPlane(plane, out _, out var point) || Math.Abs(point[0] - plan.BackConeApexX) <= PlaneTolerance) return plane;

                found = string.Format(CultureInfo.InvariantCulture, "{0:0.######}", point[0]);
                DeleteQuietly(plane);
            }

            throw new InvalidOperationException(
                $"the plane offset from the {BevelOffsetFromPlaneName} is at X = {found} instead of {plan.BackConeApexX.ToString("0.######", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Adds the plane that lies on the back cone: the offset plane turned about the axis by the pitch cone angle so that it holds the
        /// back cone line from pointC to pointA. The direction of the new axis is not known in advance, so the plane is read back and,
        /// when it does not hold the back cone line, replaced by the plane turned the other way.
        /// </summary>
        private static IADDesignPlane AddBackConePlane(IADPartSession session, IADDesignPlane offsetPlane, IADDesignAxis axis, BevelGearSketch plan)
        {
            plan.GetBackConeDirection(out var dx, out var dy);
            foreach (var sign in new[] { 1.0, -1.0 })
            {
                var plane = session.DesignPlanes.CreateAtAngleToPlane(null, offsetPlane, null, axis, sign * plan.BackConePlaneAngleRad, BevelConePlaneName);
                if (!TryReadPlane(plane, out var normal, out var point)) return plane;

                var length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
                if (length > 0.0)
                {
                    var nx = normal[0] / length;
                    var ny = normal[1] / length;
                    var nz = normal[2] / length;
                    var alongCone = nx * dx + ny * dy;
                    var offPlane = (point[0] - plan.BackConeApexX) * nx + point[1] * ny + point[2] * nz;
                    if (Math.Abs(alongCone) < 1e-6 && Math.Abs(nz) < 1e-6 && Math.Abs(offPlane) <= PlaneTolerance) return plane;
                }

                DeleteQuietly(plane);
            }

            throw new InvalidOperationException("the plane turned about the new axis does not lie on the back cone");
        }

        /// <summary>Reads the normal and a point of a plane; false when Alibre does not give them.</summary>
        private static bool TryReadPlane(IADDesignPlane plane, out double[] normal, out double[] point)
        {
            normal = null;
            point = null;
            try
            {
                plane.GetGeometry(out var p1, out _, out _);
                var n = plane.Normal;
                if (p1 == null || n == null) return false;

                point = new[] { p1.X, p1.Y, p1.Z };
                normal = new[] { n.X, n.Y, n.Z };
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void DeleteQuietly(IADDesignPlane plane)
        {
            try
            {
                plane.Delete();
            }
            catch (Exception)
            {
                // A plane that cannot be removed is left for the user to see; the next attempt uses another name.
            }
        }

        /// <summary>
        /// Where the developed back cone (pointC as origin, x along the back cone line towards pointA, y across the tooth) lies in the
        /// sketch on the back cone plane. Alibre decides the origin and axes of a new plane's sketch, so they are found by mapping three
        /// world points that lie on the plane into the sketch.
        /// </summary>
        private sealed class SketchMap
        {
            private double _originX;
            private double _originY;
            private double _ux;
            private double _uy;
            private double _vx;
            private double _vy;

            public bool Mirrored => _ux * _vy - _uy * _vx < 0.0;

            public double X(double u, double v) => _originX + u * _ux + v * _vx;

            public double Y(double u, double v) => _originY + u * _uy + v * _vy;

            public static SketchMap Create(IADPartSession session, IADSketch sketch, BevelGearSketch plan)
            {
                var factory = session.GeometryFactory;
                MapPoint(sketch, factory, plan.BackConeApexX, 0.0, 0.0, out var cx, out var cy);
                MapPoint(sketch, factory, 0.0, plan.PointAY, 0.0, out var ax, out var ay);
                MapPoint(sketch, factory, plan.BackConeApexX, 0.0, 1.0, out var zx, out var zy);

                var uLength = Math.Sqrt((ax - cx) * (ax - cx) + (ay - cy) * (ay - cy));
                var vLength = Math.Sqrt((zx - cx) * (zx - cx) + (zy - cy) * (zy - cy));
                if (!(uLength > 1e-9) || !(vLength > 1e-9)) throw new InvalidOperationException("the back cone line has no length in the sketch");

                // The sketch is in the same units as the world, so the scale is 1; it is measured anyway.
                var scale = uLength / plan.BackConeDistance;
                var u = new[] { (ax - cx) / uLength, (ay - cy) / uLength };
                var v = new[] { (zx - cx) / vLength, (zy - cy) / vLength };
                if (Math.Abs(u[0] * v[0] + u[1] * v[1]) > 1e-6) throw new InvalidOperationException("the sketch axes of the back cone plane are not perpendicular");

                return new SketchMap { _originX = cx, _originY = cy, _ux = u[0] * scale, _uy = u[1] * scale, _vx = v[0] * scale, _vy = v[1] * scale };
            }

            private static void MapPoint(IADSketch sketch, IADGeometryFactory factory, double x, double y, double z, out double sx, out double sy)
            {
                sketch.MapFromWorldToSketch(factory.CreatePoint(x, y, z), out var a, out var b);
                sx = a;
                sy = b;
            }
        }

        /// <summary>Draws figures given in the developed back cone into a sketch through <paramref name="map"/>; returns whether the sketch is closed.</summary>
        private static bool DrawMappedFigures(IADSketch sketch, IEnumerable<ToothSketchFigure> profile, SketchMap map)
        {
            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                foreach (var figure in profile)
                {
                    switch (figure.Kind)
                    {
                        case ToothSketchFigureKind.Line:
                            figures.AddLine(map.X(figure.StartX, figure.StartY), map.Y(figure.StartX, figure.StartY),
                                map.X(figure.EndX, figure.EndY), map.Y(figure.EndX, figure.EndY));
                            break;
                        case ToothSketchFigureKind.Arc:
                        {
                            // A mirrored sketch turns an anticlockwise arc into a clockwise one, so start and end change places.
                            var startX = map.Mirrored ? figure.EndX : figure.StartX;
                            var startY = map.Mirrored ? figure.EndY : figure.StartY;
                            var endX = map.Mirrored ? figure.StartX : figure.EndX;
                            var endY = map.Mirrored ? figure.StartY : figure.EndY;
                            figures.AddCircularArcByCenterStartEnd(map.X(figure.CentreX, figure.CentreY), map.Y(figure.CentreX, figure.CentreY),
                                map.X(startX, startY), map.Y(startX, startY), map.X(endX, endY), map.Y(endX, endY));
                            break;
                        }
                        default:
                            Array interpolation = new double[figure.Points.Count * 2];
                            var index = 0;
                            foreach (var point in figure.Points)
                            {
                                interpolation.SetValue(map.X(point.X, point.Y), index++);
                                interpolation.SetValue(map.Y(point.X, point.Y), index++);
                            }

                            figures.AddBsplineByInterpolation(ref interpolation);
                            break;
                    }
                }
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch.IsClosed;
        }

        /// <summary>
        /// Cuts one tooth valley with a loft from the valley sketch to the design point at the pitch cone apex. Alibre can accept the
        /// call and still report the feature as failed, so each attempt is checked and the next form is tried: the point with null tangent
        /// and guide collectors, then the same with empty collectors.
        /// Throws <see cref="InvalidOperationException"/> when no attempt gives a valid loft cut.
        /// </summary>
        private IADPartFeature AddValleyLoftCut(IADPartSession session, IADSketch toothSketch, IADDesignPoint apexPoint)
        {
            var failures = new List<string>();

            IADPartFeature Attempt(string label, object node, bool emptyCollectors)
            {
                IADLoftFeature loft = null;
                try
                {
                    var sections = _root.NewObjectCollector();
                    sections.Add(toothSketch);
                    sections.Add(node);
                    IObjectCollector tangents = null;
                    IObjectCollector magnitudes = null;
                    IObjectCollector angles = null;
                    IObjectCollector guides = null;
                    if (emptyCollectors)
                    {
                        tangents = _root.NewObjectCollector();
                        magnitudes = _root.NewObjectCollector();
                        angles = _root.NewObjectCollector();
                        guides = _root.NewObjectCollector();
                    }

                    loft = session.Features.AddLoftCut(sections, tangents, magnitudes, angles, guides, ADLoftGuideType.AD_NONE, false, false, false, false,
                        BevelValleyFeatureName);
                    if (loft != null && !loft.HasError) return loft;
                    failures.Add(label + ": Alibre reports an error in the loft cut");
                }
                catch (Exception ex)
                {
                    failures.Add(label + ": " + ex.Message);
                }

                try
                {
                    loft?.Delete();
                }
                catch (Exception)
                {
                    // A loft that cannot be removed is left for the user to see; the next attempt still runs.
                }

                return null;
            }

            var result = Attempt("design point", apexPoint, false) ?? Attempt("design point, empty collectors", apexPoint, true);
            if (result != null) return result;

            throw new InvalidOperationException($"the loft cut of the tooth valley to the apex design point was not accepted ({string.Join("; ", failures)})");
        }

        /// <summary>Creates the new Alibre part that a component is built in.</summary>
        private IADPartSession CreatePart(string name)
        {
            return _root.CreateEmptyPartEx(name, false, true);
        }

        /// <summary>The smallest number of gears for which an assembly export makes sense.</summary>
        public const int MinimumAssemblyParts = 2;

        /// <summary>Name given to the Alibre assembly that a gear train is exported to.</summary>
        public const string DefaultAssemblyName = "GearTrain";

        /// <summary>
        /// Folder that the parts of an exported assembly are saved to before they are added to it. Null (the default) uses a new folder
        /// under the temporary folder of the user.
        /// </summary>
        public string AssemblyFolder { get; set; }

        /// <summary>
        /// Exports every gear of the train to a new Alibre assembly. Each gear is built as a new part exactly like the single-part export
        /// (so it gets its full set of teeth), saved to <see cref="AssemblyFolder"/> and added to the assembly with
        /// <c>IADOccurrences.Add</c> (the Alibre API has no <c>AddPart</c>) at its shaft placement and axial offset in the train.
        /// Throws <see cref="InvalidOperationException"/> when the train has fewer than <see cref="MinimumAssemblyParts"/> gears,
        /// holds a gear family that cannot be exported yet, or Alibre rejects a step.
        /// </summary>
        public IADAssemblySession ExportAssembly(GearAssembly train, string assemblyName = DefaultAssemblyName, AssemblyExportOptions options = null)
        {
            var parts = GetAssemblyParts(train);
            var shaftParts = GetShaftAssemblyParts(train, options);
            var beltParts = GetBeltAssemblyParts(train, options);

            var stage = "creating the assembly";
            var folder = string.IsNullOrWhiteSpace(AssemblyFolder)
                ? Path.Combine(Path.GetTempPath(), "GearsAlibre", DateTime.Now.ToString("yyyyMMdd_HHmmss"))
                : AssemblyFolder;
            var log = new List<string>();
            LastExportLog = null;
            try
            {
                stage = "checking the Alibre save location";
                if (IsUsingVault())
                {
                    throw new InvalidOperationException(
                        "Alibre is set to save designs to the Alibre Vault (PDM). The Alibre API cannot change this option, and parts " +
                        "cannot be saved to a Windows folder while it is on. In Alibre Design, change the system option to save files " +
                        "to the Windows file system, then export again.");
                }

                stage = "creating the assembly";
                Directory.CreateDirectory(folder);

                // Every part is built and saved before the assembly is created: creating a part opens it as the active window, and
                // IADOccurrences.Add only works while the assembly (its root occurrence) is the active session.
                var saved = new List<IADPartSession>();
                var index = 0;
                foreach (var part in parts)
                {
                    var current = part.Key.Id;
                    index++;

                    stage = "building part '" + current + "'";
                    var session = ExportComponent(part.Key);
                    log.Add("built part '" + current + "'");
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                foreach (var shaftPart in shaftParts)
                {
                    var current = shaftPart.Key.Id;
                    index++;

                    stage = "building " + (PlanetaryGearSet.IsCarrier(shaftPart.Key) ? "carrier '" : "shaft '") + current + "'";
                    var session = Export(shaftPart.Key);
                    log.Add("built " + stage.Substring("building ".Length));
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                foreach (var beltPart in beltParts)
                {
                    var current = beltPart.Key.Id;
                    index++;

                    stage = "building belt '" + current + "'";
                    var session = Export(beltPart.Key);
                    log.Add("built belt '" + current + "'");
                    saved.Add(session);
                    SavePart(session, current, folder, index, log);
                }

                stage = "creating the assembly";
                var assembly = _root.CreateEmptyAssemblyEx(string.IsNullOrWhiteSpace(assemblyName) ? DefaultAssemblyName : assemblyName, true);
                log.Add("created the assembly");
                var rootOccurrence = assembly.RootOccurrence;
                try
                {
                    assembly.ActiveOccurrence = rootOccurrence;
                }
                catch (Exception activeError)
                {
                    log.Add("setting the active occurrence failed: " + activeError.Message);
                }

                var occurrences = rootOccurrence.Occurrences;
                var factory = assembly.GeometryFactory;

                for (var i = 0; i < parts.Count; i++)
                {
                    var current = parts[i].Key.Id;
                    stage = "adding part '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(parts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[i], transformation);
                    log.Add("added part '" + current + "' to the assembly");
                }

                for (var i = 0; i < shaftParts.Count; i++)
                {
                    var current = shaftParts[i].Key.Id;
                    stage = "adding shaft part '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(shaftParts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[parts.Count + i], transformation);
                    log.Add("added shaft part '" + current + "' to the assembly");
                }

                for (var i = 0; i < beltParts.Count; i++)
                {
                    var current = beltParts[i].Key.Id;
                    stage = "adding belt '" + current + "' to the assembly";
                    Array matrix = ToAlibreMatrix(beltParts[i].Value);
                    var transformation = factory.CreateTransform(ref matrix);
                    AddPart(occurrences, saved[parts.Count + shaftParts.Count + i], transformation);
                    log.Add("added belt '" + current + "' to the assembly");
                }

                stage = "regenerating the assembly";
                assembly.RegenerateDesign(true);
                log.Add("regenerated the assembly");
                WriteLog(folder, log);
                return assembly;
            }
            catch (Exception ex)
            {
                log.Add("FAILED while " + stage + ": " + ex);
                var logPath = WriteLog(folder, log);
                throw new InvalidOperationException(
                    $"Alibre export of the gear train to an assembly failed while {stage}: {ex.Message}"
                    + (logPath == null ? "" : " (details: " + logPath + ")"), ex);
            }
        }

        /// <summary>
        /// Saves a part to its own numbered folder, as a gear train often holds several parts with the same name. A rejected save is not fatal:
        /// the part is then added to the assembly from its open session.
        /// </summary>
        private static void SavePart(IADPartSession session, string name, string folder, int index, List<string> log)
        {
            try
            {
                var partFolder = Path.Combine(folder, index.ToString("00"));
                Directory.CreateDirectory(partFolder);
                object destination = partFolder;
                session.SaveNew(ref destination);
                log.Add("saved part '" + name + "' to " + partFolder);
            }
            catch (Exception saveError)
            {
                log.Add("saving part '" + name + "' failed: " + saveError.Message);
            }
        }

        /// <summary>
        /// True when Alibre is set to save designs to the Alibre Vault instead of the Windows file system (<c>IADVaultInfo.IsUsingVault</c>,
        /// read-only in the API). False when it is not, or when the option cannot be read.
        /// </summary>
        private bool IsUsingVault()
        {
            try
            {
                return _root.VaultInfo.IsUsingVault;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Path of the log file written by the last <see cref="ExportAssembly"/>, or null if none could be written.</summary>
        public string LastExportLog { get; private set; }

        private string WriteLog(string folder, List<string> lines)
        {
            try
            {
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, "export.log");
                File.WriteAllLines(path, lines);
                LastExportLog = path;
                return path;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Adds a saved part to the assembly by its file path, falling back to the open part session if the path is rejected.</summary>
        private static void AddPart(IADOccurrences occurrences, IADPartSession session, IADTransformation transformation)
        {
            var path = session.FilePath;
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    object file = path;
                    occurrences.Add(ref file, transformation);
                    return;
                }
                catch (Exception pathError)
                {
                    try
                    {
                        object design = session;
                        occurrences.Add(ref design, transformation);
                        return;
                    }
                    catch (Exception sessionError)
                    {
                        throw new InvalidOperationException(
                            $"by file '{path}': {pathError.Message}; by part session: {sessionError.Message}", sessionError);
                    }
                }
            }

            object part = session;
            occurrences.Add(ref part, transformation);
        }

        /// <summary>
        /// The gears of a train with the location of each one in the assembly, in shaft and mount order. Throws
        /// <see cref="InvalidOperationException"/> when the train has fewer than <see cref="MinimumAssemblyParts"/> gears or holds a gear that
        /// cannot be exported. Needs no Alibre, so it is checked before anything is created there.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<GearComponent, Transform3D>> GetAssemblyParts(GearAssembly train)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<GearComponent, Transform3D>>();
            foreach (var shaft in train.Shafts)
            {
                foreach (var mount in shaft.Mounts)
                {
                    if (!IsExportable(mount.Component))
                    {
                        throw new InvalidOperationException(
                            $"Gear '{mount.Component.Id}' cannot be exported to Alibre yet; only involute gears, racks, worms, worm wheels, bevel gears and HTD pulleys can.");
                    }

                    parts.Add(new KeyValuePair<GearComponent, Transform3D>(mount.Component, GetPartPlacement(shaft, mount)));
                }
            }

            if (parts.Count < MinimumAssemblyParts)
            {
                throw new InvalidOperationException(
                    $"An assembly needs at least {MinimumAssemblyParts} gears; the train has {parts.Count}. Use the single gear export instead.");
            }

            return parts;
        }

        /// <summary>
        /// The shafts and planetary carriers of a train that <paramref name="options"/> asks for, with the location of each one in the assembly
        /// (the shaft placement, then the start of the shaft along its axis). A shaft that only carries a rack has no part. Needs no Alibre.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<Shaft, Transform3D>> GetShaftAssemblyParts(GearAssembly train, AssemblyExportOptions options)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<Shaft, Transform3D>>();
            if (options == null || (!options.IncludeShafts && !options.IncludeCarriers)) return parts;

            foreach (var shaft in train.Shafts)
            {
                var include = PlanetaryGearSet.IsCarrier(shaft) ? options.IncludeCarriers : options.IncludeShafts && ShaftPartSketchBuilder.IsExportable(shaft);
                if (include) parts.Add(new KeyValuePair<Shaft, Transform3D>(shaft, GetShaftPlacement(shaft)));
            }

            return parts;
        }

        /// <summary>
        /// The timing belts of a train, with the location of each one in the assembly, when <paramref name="options"/> asks for them. The belt
        /// part has the driver pulley's axis at its origin and lies on its mid-plane, so it is placed like the driver pulley: the driver shaft
        /// placement, then the axial offset of the driver mount. A belt whose driver is not mounted has no part. Needs no Alibre.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<TimingBelt, Transform3D>> GetBeltAssemblyParts(GearAssembly train, AssemblyExportOptions options)
        {
            if (train == null) throw new ArgumentNullException(nameof(train));

            var parts = new List<KeyValuePair<TimingBelt, Transform3D>>();
            if (options == null || !options.IncludeBelts) return parts;

            foreach (var belt in train.Belts)
            {
                var mount = train.FindMount(belt.Driver, out var shaft);
                if (mount == null) continue;
                parts.Add(new KeyValuePair<TimingBelt, Transform3D>(belt, GetBeltPlacement(shaft, mount)));
            }

            return parts;
        }

        /// <summary>Location of an exported belt part: the shaft placement of the driver pulley, then the axial offset of its mid-plane.</summary>
        public static Transform3D GetBeltPlacement(Shaft driverShaft, ShaftMount driverMount)
        {
            if (driverShaft == null) throw new ArgumentNullException(nameof(driverShaft));
            if (driverMount == null) throw new ArgumentNullException(nameof(driverMount));

            return driverShaft.Placement.ToTransform().Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, driverMount.AxialOffset)));
        }

        /// <summary>
        /// Creates a timing belt in a new Alibre part: a sketch of the outer loop of the belt (straight runs and an arc round each pulley)
        /// and the inner loop with the HTD teeth (splines, lines and arcs) on the XY plane, extruded mid-plane to the belt width. The driver pulley's axis is the
        /// Z axis and the driven pulley lies on the line of centres in the XY plane. Throws <see cref="InvalidOperationException"/> when the
        /// belt cannot be drawn or Alibre rejects a step.
        /// </summary>
        public IADPartSession Export(TimingBelt belt)
        {
            var plan = BeltSketchBuilder.Build(belt);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The belt sketch of '{belt.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, BeltSketchName);

                stage = "drawing the belt loops";
                var closed = DrawFigures(sketch, plan.Figures, null);

                stage = "extruding the belt";
                IADPartFeature body;
                try
                {
                    // Mid-plane: half the width on each side of the sketch plane, like the pulleys the belt runs on.
                    body = session.Features.AddExtrudedBoss(sketch, plan.Width, ADPartFeatureEndCondition.AD_MID_PLANE, null, null, 0.0,
                        ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, BeltFeatureName, "", "");
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the belt sketch is not closed: " + ex.Message, ex);
                }

                CheckFeature(body, "The extruded belt");

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of belt '{belt.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Location of an exported shaft or carrier part in the assembly: the shaft placement, then the start of the shaft along its axis,
        /// because the part is extruded from the sketch plane up to its length.
        /// </summary>
        public static Transform3D GetShaftPlacement(Shaft shaft)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));

            return shaft.Placement.ToTransform()
                .Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, ShaftPartSketchBuilder.GetStart(shaft))));
        }

        /// <summary>
        /// Creates a shaft, or the plate of a planetary carrier, in a new Alibre part: a circle on the XY plane extruded along Z to the length of
        /// the shaft. Throws <see cref="InvalidOperationException"/> when the shaft has no positive size or Alibre rejects a step.
        /// </summary>
        public IADPartSession Export(Shaft shaft)
        {
            var plan = ShaftPartSketchBuilder.Build(shaft);

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, ShaftSketchName);

                stage = "drawing the circle";
                sketch.BeginChange();
                try
                {
                    sketch.Figures.AddCircle(0.0, 0.0, plan.Radius);
                }
                finally
                {
                    sketch.EndChange();
                }

                stage = "extruding the " + (plan.IsCarrier ? "carrier plate" : "shaft");
                var body = session.Features.AddExtrudedBoss(sketch, plan.Length, ADPartFeatureEndCondition.AD_TO_DEPTH, null, null, 0.0,
                    ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, plan.IsCarrier ? CarrierFeatureName : ShaftFeatureName, "", "");
                CheckFeature(body, plan.IsCarrier ? "The carrier plate" : "The shaft");

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of shaft '{shaft.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>True for the gear families that <see cref="ExportAssembly"/> and the single-part export can build.</summary>
        public static bool IsExportable(GearComponent component)
        {
            return component is InvoluteGearComponent || component is RackComponent || component is WormComponent || component is WormWheelComponent
                || component is BevelGearComponent || component is HtdPulleyComponent || component is CycloidalGearComponent;
        }

        /// <summary>
        /// Location of an exported part in the assembly: the shaft placement, then the axial offset along the shaft. An exported part is
        /// extruded from the sketch plane up to its face width, while the offset locates the mid-plane, hence the half face width.
        /// A herringbone part is mirrored about the sketch plane, and an HTD pulley is extruded mid-plane about it, so both already lie on
        /// the mid-plane and have no such shift.
        /// </summary>
        public static Transform3D GetPartPlacement(Shaft shaft, ShaftMount mount)
        {
            if (shaft == null) throw new ArgumentNullException(nameof(shaft));
            if (mount == null) throw new ArgumentNullException(nameof(mount));

            if (mount.Component is BevelGearComponent bevel)
            {
                return shaft.Placement.ToTransform().Compose(GetBevelPartFrame(bevel, mount.AxialOffset));
            }

            // A herringbone part or a pulley is made about its sketch plane (half the face width on each side), so that plane is already the mid-plane.
            var centred = IsHerringbone(mount.Component) || mount.Component is HtdPulleyComponent || mount.Component is CycloidalGearComponent;
            var start = centred ? mount.AxialOffset : mount.AxialOffset - mount.Component.FaceWidth / 2.0;
            return shaft.Placement.ToTransform().Compose(Transform3D.Translate(new Vector3D(0.0, 0.0, start)));
        }

        /// <summary>True for a herringbone gear or rack that is exported with a mirror: a helical one with the herringbone option set.</summary>
        public static bool IsHerringbone(GearComponent component)
        {
            return (component is InvoluteGearComponent gear && gear.IsHerringbone && gear.IsHelical)
                   || (component is RackComponent rack && rack.IsHerringbone && rack.IsHelical);
        }

        /// <summary>
        /// Location of an exported bevel gear part in the local frame of its shaft mount (axis Z, apex on +Z, the face on the mid-plane at the
        /// axial offset). The part has its axis along X with the apex towards -X, and a valley, not a tooth, on +Y. This is the rotation
        /// (x, y, z) to (y, -z, -x) that the editor model uses, with the same axial shift and the same half pitch turn, so a tooth is on the
        /// local +X axis and the bevel gear meshes as in the editor.
        /// </summary>
        public static Transform3D GetBevelPartFrame(BevelGearComponent gear, double axialOffset)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            var phase = Math.PI / gear.Teeth;
            var c = Math.Cos(phase);
            var s = Math.Sin(phase);
            return Transform3D.FromFrame(new Vector3D(0.0, 0.0, axialOffset + gear.BlankAxialOffset), new Vector3D(0.0, 0.0, -1.0),
                new Vector3D(c, s, 0.0), new Vector3D(s, -c, 0.0));
        }

        /// <summary>
        /// The 16 values of an Alibre transformation (<c>IADGeometryFactory.CreateTransform</c>): the X, Y and Z vectors of the frame in
        /// elements 0-2, 4-6 and 8-10 and the translation in 12-14; elements 3, 7 and 11 are 0 and element 15 is 1. The transform is in
        /// millimetres; Alibre works in centimetres (<see cref="SingleToothSketchBuilder.MmToAlibreUnits"/>), so the translation is converted.
        /// </summary>
        public static double[] ToAlibreMatrix(Transform3D transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            var x = transform.TransformDirection(Vector3D.UnitX);
            var y = transform.TransformDirection(Vector3D.UnitY);
            var z = transform.TransformDirection(Vector3D.UnitZ);
            var t = transform.Translation;
            var k = SingleToothSketchBuilder.MmToAlibreUnits;
            return new[]
            {
                x.X, x.Y, x.Z, 0.0,
                y.X, y.Y, y.Z, 0.0,
                z.X, z.Y, z.Z, 0.0,
                t.X * k, t.Y * k, t.Z * k, 1.0
            };
        }

        /// <summary>Makes a part name unique in the assembly (a gear train often holds several gears with the same tooth count and module).</summary>
        public static string UniqueName(string name, ISet<string> used)
        {
            var candidate = name;
            for (var i = 2; !used.Add(candidate); i++) candidate = name + "_" + i;
            return candidate;
        }

        private IADPartSession ExportComponent(GearComponent component)
        {
            switch (component)
            {
                case InvoluteGearComponent gear:
                    return Export(gear);
                case RackComponent rack:
                    return Export(rack);
                case WormComponent worm:
                    return Export(worm);
                case WormWheelComponent wheel:
                    return Export(wheel);
                case BevelGearComponent bevel:
                    return Export(bevel);
                case HtdPulleyComponent pulley:
                    return Export(pulley);
                case CycloidalGearComponent cycloidal:
                    return Export(cycloidal);
                default:
                    throw new InvalidOperationException($"Gear '{component.Id}' cannot be exported to Alibre yet.");
            }
        }

        /// <summary>Throws when Alibre reports an error in the feature that was just created.</summary>
        private static void CheckFeature(IADPartFeature feature, string description)
        {
            if (feature == null) throw new InvalidOperationException(description + " was not created.");
            if (feature.HasError) throw new InvalidOperationException(description + " has an error in Alibre, so it was not formed.");
        }

        /// <summary>
        /// Creates an HTD timing belt pulley in a new Alibre part: a closed sketch of one groove cell (a wedge one pitch wide) on the XY plane,
        /// extruded mid-plane (half the width to each side of the plane), a circular pattern of that cell about the Z axis, and, for a pulley
        /// with flanges, a revolve of the flange on the positive side (sketched on the plane that holds the Z axis) about the Z axis and a
        /// mirror of that flange across the XY plane for the other one. The body runs from z = -width / 2 to z = width / 2, so the sketch
        /// plane is the mid-plane; the bore and hub are not part of the export.
        /// </summary>
        public IADPartSession Export(HtdPulleyComponent pulley)
        {
            var plan = PulleySketchBuilder.Build(pulley);
            var cell = plan.Tooth;
            if (!cell.IsClosed()) throw new InvalidOperationException($"The groove sketch of pulley '{pulley.Id}' is not closed, so it cannot be exported.");
            if (!plan.IsFlangeClosed()) throw new InvalidOperationException($"The flange sketch of pulley '{pulley.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(cell.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the groove cell";
                var closed = DrawFigures(sketch, cell.Figures, cell.ReferenceCircles);

                stage = "extruding the groove cell";
                IADPartFeature tooth;
                try
                {
                    // Mid-plane: half the width on each side of the sketch plane, so the pulley is centred on z = 0.
                    tooth = session.Features.AddExtrudedBoss(sketch, cell.ExtrusionDepth, ADPartFeatureEndCondition.AD_MID_PLANE, null, null, 0.0,
                        ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, ToothFeatureName, "", "");
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the groove sketch is not closed: " + ex.Message, ex);
                }

                CheckFeature(tooth, "The extruded groove cell");

                var axis = session.DesignAxes.Item(PulleyAxisName);
                var targets = _root.NewObjectCollector();
                targets.Add(tooth);

                stage = "patterning the groove cell";
                session.Features.AddCircularFeaturePattern(targets, cell.Teeth, cell.PatternAngle, null, axis, false, PatternGeometry, PatternFeatureName);

                if (plan.HasFlanges)
                {
                    var ySign = AxialPlaneSketchYIsNegativeZ ? -1.0 : 1.0;
                    var axialPlane = session.DesignPlanes.Item(AxialSketchPlaneName);

                    stage = "drawing flange 1";
                    var flangeSketch = session.Sketches.AddSketch(null, axialPlane, FlangeSketchName + "1");
                    DrawFigures(flangeSketch, plan.Flange, null, ySign);

                    stage = "revolving flange 1";
                    var flange = session.Features.AddRevolvedBoss(flangeSketch, null, axis, 2.0 * Math.PI, FlangeFeatureName + "1");
                    CheckFeature(flange, "Flange 1");

                    // Flange 2 is the mirror image of flange 1 across the sketch plane, which is the mid-plane of the pulley.
                    stage = "mirroring flange 1 across the " + SketchPlaneName;
                    AddMirror(session, flange, plane, FlangeFeatureName + "2", "Flange 2");
                }

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of pulley '{pulley.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a cycloidal gear in a new Alibre part: a closed sketch of the whole gear outline (tip arcs, radial flanks and root arcs of
        /// every tooth, a reference circle for the pitch diameter and, when set, a circle for the centre hole) on the XY plane, extruded
        /// mid-plane (half the face width to each side of the plane). The body runs from z = -width / 2 to z = width / 2, so the sketch
        /// plane is the mid-plane. The gear is made for the wheel or pinion role and the mating tooth count that the gear has.
        /// </summary>
        public IADPartSession Export(CycloidalGearComponent gear)
        {
            var plan = CycloidalSketchBuilder.Build(gear);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The outline sketch of cycloidal gear '{gear.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the gear outline";
                var closed = DrawFigures(sketch, plan.Figures, plan.ReferenceCircles, 1.0, plan.CentreHoleRadius);

                stage = "extruding the gear";
                IADPartFeature body;
                try
                {
                    // Mid-plane: half the width on each side of the sketch plane, so the gear is centred on z = 0.
                    body = session.Features.AddExtrudedBoss(sketch, plan.Width, ADPartFeatureEndCondition.AD_MID_PLANE, null, null, 0.0,
                        ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, ToothFeatureName, "", "");
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the gear outline sketch is not closed: " + ex.Message, ex);
                }

                CheckFeature(body, "The extruded gear");

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of cycloidal gear '{gear.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        private IADPartSession Export(InvoluteGearComponent gear, string partName)
        {
            var plan = SingleToothSketchBuilder.Build(gear, partName);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The tooth sketch of gear '{gear.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = CreatePart(plan.PartName);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the tooth profile";
                var closed = DrawFigures(sketch, plan.Figures, plan.ReferenceCircles);

                var axis = session.DesignAxes.Item(PatternAxisName);

                IADPartFeature tooth;
                try
                {
                    if (plan.IsHelical)
                    {
                        stage = "sweeping the tooth along its helix";
                        tooth = AddHelicalTooth(session, sketch, axis, plan.Helix);
                    }
                    else
                    {
                        stage = "extruding the tooth";
                        tooth = session.Features.AddExtrudedBoss(sketch, plan.ExtrusionDepth, ADPartFeatureEndCondition.AD_TO_DEPTH, null, null, 0.0,
                            ADDirectionType.AD_ALONG_NORMAL, null, null, false, 0.0, false, ToothFeatureName, "", "");
                    }
                }
                catch (Exception ex) when (!closed)
                {
                    throw new InvalidOperationException("Alibre reports that the tooth sketch is not closed: " + ex.Message, ex);
                }

                var targets = _root.NewObjectCollector();
                targets.Add(tooth);
                if (plan.IsHerringbone)
                {
                    // The tooth was swept over half the face width from the sketch plane; mirroring it across that plane gives the
                    // other half with the opposite hand, and both halves are patterned together.
                    stage = "mirroring the tooth across the " + SketchPlaneName;
                    var mirror = AddHerringboneMirror(session, tooth, plane);
                    targets.Add(mirror);
                }

                stage = "patterning the tooth";
                session.Features.AddCircularFeaturePattern(targets, plan.Teeth, plan.PatternAngle, null, axis, false, PatternGeometry, PatternFeatureName);

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of gear '{gear.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Mirrors the half-width helical tooth of a herringbone gear across the plane it was sketched on (<c>IADPartFeatures.AddMirrorFeaturePattern</c>).
        /// The mirror of a right-hand helix is a left-hand helix, so the two halves form the V of a herringbone gear and meet on the plane.
        /// Alibre can accept the call and still report the feature as failed, so the result is checked.
        /// </summary>
        private IADMirrorFeature AddHerringboneMirror(IADPartSession session, IADPartFeature tooth, IADDesignPlane mirrorPlane)
        {
            return AddMirror(session, tooth, mirrorPlane, MirrorFeatureName, "The mirror of the helical tooth");
        }

        /// <summary>Mirrors one feature across a plane (<c>IADPartFeatures.AddMirrorFeaturePattern</c>) and checks the result.</summary>
        private IADMirrorFeature AddMirror(IADPartSession session, IADPartFeature feature, IADDesignPlane mirrorPlane, string name, string description)
        {
            var toMirror = _root.NewObjectCollector();
            toMirror.Add(feature);

            // The second argument is the occurrence, null in a part (as for the circular pattern).
            var mirror = session.Features.AddMirrorFeaturePattern(toMirror, null, mirrorPlane, PatternGeometry, name);
            CheckFeature(mirror, description);
            return mirror;
        }

        /// <summary>
        /// Forms the tooth of a helical gear with a helical boss: the profile sketch (in the plane perpendicular to the axis) is swept
        /// along a constant-pitch helix about the gear axis. The helix is given by its height (the face width) and pitch (the lead);
        /// the revolutions are passed as well. A right-hand gear gives a clockwise helix. The profile is kept parallel oriented to the
        /// axis (<see cref="HelicalBossParallelOriented"/>), so it stays the transverse section of the tooth along the whole sweep.
        /// </summary>
        private static IADPartFeature AddHelicalTooth(IADPartSession session, IADSketch sketch, object axis, SingleToothHelix helix)
        {
            return session.Features.AddHelicalBoss(sketch, HelicalBossParallelOriented, axis, ADHelixType.AD_Height_Pitch, helix.RightHand, false,
                helix.Height, helix.Lead, helix.Revolutions, ADPitchType.AD_Constant, 1.0, helix.Lead, 0.0,
                ADHelixConditionType.AD_Natural, 0.0, 0.0, ADHelixConditionType.AD_Natural, 0.0, 0.0,
                ToothFeatureName, "", "", "", "", "", "", "", "", "");
        }

        /// <summary>
        /// Draws the closed profile and the reference circles (when given); returns whether Alibre reports the sketch as closed.
        /// <paramref name="ySign"/> mirrors the sketch y coordinates (-1) for a plane whose sketch y axis points the other way.
        /// </summary>
        private static bool DrawFigures(IADSketch sketch, IEnumerable<ToothSketchFigure> profile, IEnumerable<ToothSketchReferenceCircle> referenceCircles,
            double ySign = 1.0, double centreHoleRadius = 0.0)
        {
            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                foreach (var figure in profile)
                {
                    switch (figure.Kind)
                    {
                        case ToothSketchFigureKind.Line:
                            figures.AddLine(figure.StartX, ySign * figure.StartY, figure.EndX, ySign * figure.EndY);
                            break;
                        case ToothSketchFigureKind.Arc:
                            figures.AddCircularArcByCenterStartEnd(figure.CentreX, figure.CentreY, figure.StartX, figure.StartY, figure.EndX, figure.EndY);
                            break;
                        default:
                            Array interpolation = new double[figure.Points.Count * 2];
                            var index = 0;
                            foreach (var point in figure.Points)
                            {
                                interpolation.SetValue(point.X, index++);
                                interpolation.SetValue(point.Y, index++);
                            }

                            figures.AddBsplineByInterpolation(ref interpolation);
                            break;
                    }
                }

                if (referenceCircles != null)
                {
                    foreach (var circle in referenceCircles)
                    {
                        figures.AddCircle(0.0, 0.0, circle.Radius).IsReference = true;
                    }
                }

                // A hole through the centre: a closed circle inside the outline, which Alibre extrudes as a hole.
                if (centreHoleRadius > 0.0) figures.AddCircle(0.0, 0.0, centreHoleRadius);
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch.IsClosed;
        }
    }
}

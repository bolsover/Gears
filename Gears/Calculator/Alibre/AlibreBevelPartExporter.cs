using System;
using System.Collections.Generic;
using System.Globalization;
using AlibreX;
using Bolsover.Gears.Model.Assemblies;
using static Bolsover.Gears.Calculator.Alibre.AlibreGearExporter;
using static Bolsover.Gears.Calculator.Alibre.AlibreSketchDrawer;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Builds the Alibre part of a straight, spiral, zerol or hypoid bevel gear.</summary>
    internal sealed class AlibreBevelPartExporter
    {
        private readonly AlibreGearExporter _owner;
        private readonly IADRoot _root;

        public AlibreBevelPartExporter(AlibreGearExporter owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _root = owner.Root;
        }

        private bool PatternGeometry => _owner.PatternGeometry;

        private IADPartSession CreatePart(string name) => _owner.CreatePart(name);

        private int CurvedBevelSectionCount => _owner.CurvedBevelSectionCount;

        internal IADPartSession Export(BevelGearComponent gear)
        {
            if (gear is SpiralBevelGearComponent spiral) return ExportCurvedBevel(spiral);

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
                var map = AlibreSketchDrawer.SketchMap.Create(session, toothSketch, plan);

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

        internal IADPartSession ExportCurvedBevel(SpiralBevelGearComponent gear)
        {
            if (gear == null) throw new ArgumentNullException(nameof(gear));

            var plan = BevelGearSketch.Build(gear);
            var sections = SpiralToothSpaceSections.Build(gear, plan, CurvedBevelSectionCount, true);

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

                plan.GetBackConeDirection(out var dx, out var dy);
                var sketches = new List<IADSketch>();
                for (var k = 0; k < sections.Count; k++)
                {
                    var section = sections[k];
                    var number = k + 1;
                    var label = $"section {number} of {sections.Count}";

                    // The section plane holds the back cone line of the scaled section turned about the X axis by the twist (right hand
                    // rule about +X, the way the 3D model turns its sections); the tooth space is symmetric across that line.
                    var cos = Math.Cos(section.TwistRad);
                    var sin = Math.Sin(section.TwistRad);
                    var length = plan.BackConeDistance * section.Scale;
                    var origin = new[] { section.PlaneOriginX, 0.0, 0.0 };
                    var along = new[] { section.PlaneOriginX + length * dx, length * dy * cos, length * dy * sin };
                    var across = new[] { section.PlaneOriginX, -length * sin, length * cos };

                    stage = "adding the design points of the plane of " + label;
                    var pointOrigin = AddSectionPoint(session, origin, $"{BevelSectionPointName}{number}A");
                    var pointAlong = AddSectionPoint(session, along, $"{BevelSectionPointName}{number}B");
                    var pointAcross = AddSectionPoint(session, across, $"{BevelSectionPointName}{number}C");

                    stage = "adding the plane of " + label;
                    var plane = session.DesignPlanes.CreateBy3Points(null, pointOrigin, null, pointAlong, null, pointAcross, $"{BevelSectionPlaneName}{number}");
                    if (plane == null) throw new InvalidOperationException($"The plane of {label} was not created.");

                    stage = "adding the tooth space sketch of " + label;
                    var sketch = session.Sketches.AddSketch(null, plane, $"{BevelSectionSketchName}{number}");

                    stage = "locating the axes of the sketch of " + label;
                    var map = AlibreSketchDrawer.SketchMap.CreateFromPoints(session, sketch, origin, along, across, length);

                    stage = "drawing the tooth space profile of " + label;
                    if (!DrawMappedFigures(sketch, section.Figures, map))
                    {
                        throw new InvalidOperationException($"Alibre reports that the tooth space sketch of {label} is not closed.");
                    }

                    sketches.Add(sketch);
                }

                stage = $"cutting a tooth space with a loft through {sections.Count} sections, including one section past the toe";
                var valley = AddSectionLoftCut(session, sketches);

                stage = "patterning the tooth space about the " + BevelGearAxisName;
                var targets = _root.NewObjectCollector();
                targets.Add(valley);
                var pattern = session.Features.AddCircularFeaturePattern(targets, plan.Teeth, plan.PatternAngle, null, gearAxis, false,
                    PatternGeometry, BevelPatternFeatureName);
                CheckFeature(pattern, "The circular pattern of the tooth space");

                stage = "regenerating the part";
                session.RegenerateAll();
                return session;
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Alibre export of {gear.TypeName.ToLowerInvariant()} '{gear.Id}' failed while {stage}: {ex.Message}", ex);
            }
        }

        private static IADDesignPoint AddSectionPoint(IADPartSession session, double[] position, string name)
        {
            var point = session.DesignPoints.CreatePoint(position[0], position[1], position[2], name);
            if (point == null) throw new InvalidOperationException($"The design point {name} was not created.");
            return point;
        }

        private IADPartFeature AddSectionLoftCut(IADPartSession session, IReadOnlyList<IADSketch> sketches)
        {
            var failures = new List<string>();

            IADPartFeature Attempt(string label, bool emptyCollectors, bool minimizeTwist)
            {
                IADLoftFeature loft = null;
                try
                {
                    var sections = _root.NewObjectCollector();
                    foreach (var sketch in sketches) sections.Add(sketch);
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

                    loft = session.Features.AddLoftCut(sections, tangents, magnitudes, angles, guides, ADLoftGuideType.AD_NONE, minimizeTwist, false, false, false,
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

            var result = Attempt("minimised twist", false, true) ?? Attempt("minimised twist, empty collectors", true, true) ?? Attempt("natural twist", false, false);
            if (result != null) return result;

            throw new InvalidOperationException(
                $"the loft cut of the tooth space through {sketches.Count} sections was not accepted ({string.Join("; ", failures)}). " +
                "Use more sections (CurvedBevelSectionCount), a smaller spiral angle or a narrower face.");
        }

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
    }
}

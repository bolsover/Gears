using System;
using System.Collections.Generic;
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
        public const string PathSketchName = "ToothPath";
        public const string ThreadPatternFeatureName = "ThreadPattern";
        public const string RackPatternFeatureName = "RackTeeth";
        public const string ToothFeatureName = "Tooth";
        public const string PatternFeatureName = "ToothPattern";

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
        /// section), and a linear pattern of that tooth along the X axis.
        /// </summary>
        public IADPartSession Export(RackComponent rack)
        {
            var plan = RackToothSketchBuilder.Build(rack);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The tooth sketch of rack '{rack.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = _root.CreateEmptyPartEx(plan.PartName, false, true);

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
                            pathSketch.Figures.AddLine(0.0, 0.0, plan.ShearTotal, ySign * plan.ExtrusionDepth);
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

                if (plan.Teeth > 1)
                {
                    stage = "patterning the tooth along the rack";
                    AddRackPattern(session, tooth, plan);
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
                var session = _root.CreateEmptyPartEx(plan.PartName, false, true);

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
        private void AddRackPattern(IADPartSession session, IADPartFeature tooth, RackToothSketch plan)
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

        /// <summary>Throws when Alibre reports an error in the feature that was just created.</summary>
        private static void CheckFeature(IADPartFeature feature, string description)
        {
            if (feature == null) throw new InvalidOperationException(description + " was not created.");
            if (feature.HasError) throw new InvalidOperationException(description + " has an error in Alibre, so it was not formed.");
        }

        private IADPartSession Export(InvoluteGearComponent gear, string partName)
        {
            var plan = SingleToothSketchBuilder.Build(gear, partName);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The tooth sketch of gear '{gear.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = _root.CreateEmptyPartEx(plan.PartName, false, true);

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

                stage = "patterning the tooth";
                var targets = _root.NewObjectCollector();
                targets.Add(tooth);
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
            double ySign = 1.0)
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
            }
            finally
            {
                sketch.EndChange();
            }

            return sketch.IsClosed;
        }
    }
}

using System;
using System.Collections.Generic;
using AlibreX;
using Bolsover.Gears.Model.Assemblies;
using static Bolsover.Gears.Calculator.Alibre.AlibreGearExporter;
using static Bolsover.Gears.Calculator.Alibre.AlibreSketchDrawer;

namespace Bolsover.Gears.Calculator.Alibre
{
    /// <summary>Builds the Alibre parts of a rack and of a worm.</summary>
    internal sealed class AlibreRackWormPartExporter
    {
        private readonly AlibreGearExporter _owner;
        private readonly IADRoot _root;

        public AlibreRackWormPartExporter(AlibreGearExporter owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _root = owner.Root;
        }

        private bool PatternGeometry => _owner.PatternGeometry;

        private IADPartSession CreatePart(string name) => _owner.CreatePart(name);

        private bool AxialPlaneSketchYIsNegativeZ => _owner.AxialPlaneSketchYIsNegativeZ;

        private IADMirrorFeature AddHerringboneMirror(IADPartSession session, IADPartFeature tooth, IADDesignPlane mirrorPlane)
            => _owner.AddHerringboneMirror(session, tooth, mirrorPlane);

        internal IADPartSession Export(RackComponent rack)
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

        internal IADPartSession Export(WormComponent worm)
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
    }
}

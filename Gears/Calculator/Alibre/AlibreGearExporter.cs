using System;
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

        public const string SketchName = "ToothProfile";
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
            var plan = SingleToothSketchBuilder.Build(gear);
            if (!plan.IsClosed()) throw new InvalidOperationException($"The tooth sketch of gear '{gear.Id}' is not closed, so it cannot be exported.");

            var stage = "creating the part";
            try
            {
                var session = _root.CreateEmptyPartEx(plan.PartName, false, true);

                stage = "adding the sketch on the " + SketchPlaneName;
                var plane = session.DesignPlanes.Item(SketchPlaneName);
                var sketch = session.Sketches.AddSketch(null, plane, SketchName);

                stage = "drawing the tooth profile";
                var closed = DrawProfile(sketch, plan);

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

        /// <summary>Draws the closed profile and the reference circles; returns whether Alibre reports the sketch as closed.</summary>
        private static bool DrawProfile(IADSketch sketch, SingleToothSketch plan)
        {
            sketch.BeginChange();
            try
            {
                var figures = sketch.Figures;
                foreach (var figure in plan.Figures)
                {
                    switch (figure.Kind)
                    {
                        case ToothSketchFigureKind.Line:
                            figures.AddLine(figure.StartX, figure.StartY, figure.EndX, figure.EndY);
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

                foreach (var circle in plan.ReferenceCircles)
                {
                    figures.AddCircle(0.0, 0.0, circle.Radius).IsReference = true;
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

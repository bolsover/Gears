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
        internal const string AxialSketchPlaneName = "ZX-Plane";

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
        public const string BevelSectionPointName = "ToothSpacePoint";
        public const string BevelSectionPlaneName = "ToothSpacePlane";
        public const string BevelSectionSketchName = "ToothSpaceSection";

        /// <summary>
        /// Number of sections of the loft cut of a spiral, zerol or hypoid bevel gear (2 or more); zero or less chooses it from the twist of
        /// the tooth trace (5 to 8, see <see cref="SpiralToothSpaceSections.SectionCountFor"/>). More sections follow the curved tooth more
        /// closely but take longer in Alibre. One more section past the toe, towards the apex, is added to this count so that the loft cuts
        /// through the conical toe face of the blank completely (see <see cref="SpiralToothSpaceSections.ToeExtensionScale"/>).
        /// </summary>
        public int CurvedBevelSectionCount { get; set; }

        /// <summary>Largest distance, in Alibre units (centimetres), between where a new plane should be and where Alibre put it.</summary>
        internal const double PlaneTolerance = 1e-4;

        private readonly IADRoot _root;

        /// <param name="root">The Alibre root object (<c>IADRoot</c>) that new parts are created in.</param>
        public AlibreGearExporter(IADRoot root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
        }

        internal IADRoot Root => _root;

        private AlibreRackWormPartExporter RackWormExporter => new AlibreRackWormPartExporter(this);

        private AlibreBevelPartExporter BevelExporter => new AlibreBevelPartExporter(this);

        private AlibreAssemblyBuilder AssemblyBuilder => new AlibreAssemblyBuilder(this);

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
            => RackWormExporter.Export(rack);

        /// <summary>
        /// Creates a worm in a new Alibre part the way a helical gear is made: a closed sketch of the end-face section of one thread
        /// start on the XY plane, a single thread formed by sweeping that section along a helix about the Z axis with a helical boss
        /// (height = threaded length, pitch = lead), and, for several starts, a circular pattern of the thread about the Z axis.
        /// </summary>
        public IADPartSession Export(WormComponent worm)
            => RackWormExporter.Export(worm);

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
        /// A spiral, zerol or hypoid bevel gear is made by <see cref="ExportCurvedBevel"/> instead, with a multi-section loft cut.
        /// </summary>
        public IADPartSession Export(BevelGearComponent gear)
            => BevelExporter.Export(gear);


        /// <summary>
        /// Creates a spiral, zerol or hypoid bevel gear in a new Alibre part. The blank is revolved as for the straight bevel gear; the tooth
        /// space is a loft cut through several sections (<see cref="SpiralToothSpaceSections"/>), which is patterned about the gear axis:
        /// 1. the gear blank sketch and its revolve about the X axis,
        /// 2. for every section a plane through three design points (the point on the X axis where the scaled back cone meets it, a point along
        ///    the back cone line and a point across it, both turned about the X axis by the twist of the section), a sketch of the tooth
        ///    space on that plane, scaled about the pitch cone apex,
        /// 3. a loft cut through all the sketches, from the heel to the toe and one section past it, because the flat plane of the toe section
        ///    touches the conical toe face of the blank along one line only,
        /// 4. a circular pattern of the cut about the X axis, the axis of the gear itself (a hypoid gear's axis does not pass through the apex of its mate).
        /// The number of sections from the heel to the toe is <see cref="CurvedBevelSectionCount"/>; the section past the toe is in addition to it.
        /// The bore and hub of the editor model are not part of the export.
        /// </summary>
        public IADPartSession ExportCurvedBevel(SpiralBevelGearComponent gear)
            => BevelExporter.ExportCurvedBevel(gear);

        /// <summary>Creates the new Alibre part that a component is built in.</summary>
        internal IADPartSession CreatePart(string name)
        {
            return _root.CreateEmptyPartEx(name, false, true);
        }

        /// <summary>The smallest number of parts (gears, shafts, carriers and belts together) for which an assembly export makes sense.</summary>
        public const int MinimumAssemblyParts = 2;

        /// <summary>Name given to the Alibre assembly that a gear train is exported to.</summary>
        public const string DefaultAssemblyName = "GearTrain";

        /// <summary>
        /// Folder that the parts of an exported assembly are saved to before they are added to it. Null (the default) uses a new folder
        /// under the temporary folder of the user.
        /// </summary>
        public string AssemblyFolder { get; set; }

        /// <summary>
        /// Exports the train to a new Alibre assembly: every gear, every shaft (a shaft that only carries a rack has none), every planetary carrier
        /// and every timing belt, or only the items of <paramref name="selection"/> when one is given. Each gear is built as a new part exactly like the single-part export
        /// (so it gets its full set of teeth), saved to <see cref="AssemblyFolder"/> and added to the assembly with
        /// <c>IADOccurrences.Add</c> (the Alibre API has no <c>AddPart</c>) at its shaft placement and axial offset in the train.
        /// Throws <see cref="InvalidOperationException"/> when there are fewer than <see cref="MinimumAssemblyParts"/> parts in total,
        /// a gear family cannot be exported yet, Alibre rejects a step, or Alibre is not set to save to the Windows file system (the Vault, a
        /// PDM repository and an AD repository are refused before anything is created, see <see cref="AlibreSaveTargetReader"/>).
        /// </summary>
        /// <param name="selection">The gears, shafts and belts to export; null exports everything.</param>
        public IADAssemblySession ExportAssembly(GearAssembly train, string assemblyName = DefaultAssemblyName, IReadOnlyCollection<object> selection = null)
            => AssemblyBuilder.ExportAssembly(train, assemblyName, selection);

        /// <summary>Path of the log file written by the last <see cref="ExportAssembly"/>, or null if none could be written.</summary>
        public string LastExportLog { get; internal set; }

        /// <summary>
        /// The gears of a train (or only the gears of <paramref name="selection"/> when one is given) with the location of each one in the
        /// assembly, in shaft and mount order. Throws <see cref="InvalidOperationException"/> when one of them cannot be exported.
        /// Needs no Alibre, so it is checked before anything is created there.
        /// </summary>
        /// <param name="selection">The items to export; null returns every gear.</param>
        public static IReadOnlyList<KeyValuePair<GearComponent, Transform3D>> GetAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
            => AlibreAssemblyPartSelector.GetAssemblyParts(train, selection);

        internal static void CheckPartCount(int count)
            => AlibreAssemblyPartSelector.CheckPartCount(count);


        /// <summary>
        /// The shafts and planetary carriers of a train (or only those of <paramref name="selection"/> when one is given), with the location of
        /// each one in the assembly (the shaft placement, then the start of the shaft along its axis). A shaft that only carries a rack has no
        /// part. Needs no Alibre.
        /// </summary>
        /// <param name="selection">The items to export; null returns every exportable shaft and carrier.</param>
        public static IReadOnlyList<KeyValuePair<Shaft, Transform3D>> GetShaftAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
            => AlibreAssemblyPartSelector.GetShaftAssemblyParts(train, selection);

        /// <summary>
        /// The timing belts of a train (or only those of <paramref name="selection"/> when one is given), with the location of each one in the assembly. The belt
        /// part has the driver pulley's axis at its origin and lies on its mid-plane, so it is placed like the driver pulley: the driver shaft
        /// placement, then the axial offset of the driver mount. A belt whose driver is not mounted has no part. Needs no Alibre.
        /// </summary>
        public static IReadOnlyList<KeyValuePair<TimingBelt, Transform3D>> GetBeltAssemblyParts(GearAssembly train, IReadOnlyCollection<object> selection = null)
            => AlibreAssemblyPartSelector.GetBeltAssemblyParts(train, selection);

        /// <summary>Location of an exported belt part: the shaft placement of the driver pulley, then the axial offset of its mid-plane.</summary>
        public static Transform3D GetBeltPlacement(Shaft driverShaft, ShaftMount driverMount)
            => AlibrePartPlacement.GetBeltPlacement(driverShaft, driverMount);

        /// <summary>
        /// Creates a timing belt in a new Alibre part: a sketch of the outer loop of the belt (straight runs and an arc round each pulley)
        /// and the inner loop with the HTD teeth (splines, lines and arcs) on the XY plane, extruded mid-plane to the belt width. The driver pulley's axis is the
        /// Z axis and the driven pulley lies on the line of centres in the XY plane. Throws <see cref="InvalidOperationException"/> when the
        /// belt cannot be drawn or Alibre rejects a step. <paramref name="driverPhaseRad"/> is the phase of the driver pulley's mount (the
        /// teeth are placed in its grooves); a caller without a mount phase gets phase 0.
        /// </summary>
        public IADPartSession Export(TimingBelt belt, double driverPhaseRad = 0.0)
        {
            var plan = BeltSketchBuilder.Build(belt, driverPhaseRad);
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
            => AlibrePartPlacement.GetShaftPlacement(shaft);

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
            => AlibreAssemblyPartSelector.IsExportable(component);

        /// <summary>
        /// Location of an exported part in the assembly: the shaft placement, then the axial offset along the shaft. An exported part is
        /// extruded from the sketch plane up to its face width, while the offset locates the mid-plane, hence the half face width.
        /// A herringbone part is mirrored about the sketch plane, and an HTD pulley is extruded mid-plane about it, so both already lie on
        /// the mid-plane and have no such shift.
        /// Every part is then moved by the local motion of its mount phase, as in the 3D scene: turned about the
        /// shaft axis by the phase, or slid along its length for a rack. The phase includes the half tooth that the mesh solvers give a driven
        /// gear, the angle of the line of centres and, for the driver, the rotation of its own shaft, so a meshed train is exported in the pose
        /// shown in the application. An HTD pulley is turned the same way so that its grooves meet the teeth of the belt (see
        /// <see cref="Bolsover.Gears.Calculator.Assemblies.BeltRegistration"/>). At phase 0 the placement is only the shaft placement and the
        /// axial translation. A bevel gear is turned about the shaft axis by the phase before its part frame (see <see cref="GetBevelPartFrame"/>);
        /// the axial shift is along that axis, so it commutes with the turn and the order is that of the scene.
        /// </summary>
        public static Transform3D GetPartPlacement(Shaft shaft, ShaftMount mount)
            => AlibrePartPlacement.GetPartPlacement(shaft, mount);

        /// <summary>True for a herringbone gear or rack that is exported with a mirror: a helical one with the herringbone option set.</summary>
        public static bool IsHerringbone(GearComponent component)
            => AlibreAssemblyPartSelector.IsHerringbone(component);

        /// <summary>
        /// Location of an exported bevel gear part in the local frame of its shaft mount (axis Z, apex on +Z, the face on the mid-plane at the
        /// axial offset). The part has its axis along X with the apex towards -X, and a valley, not a tooth, on +Y. This is the rotation
        /// (x, y, z) to (y, -z, -x) that the editor model uses, with the same axial shift and the same half pitch turn, so a tooth is on the
        /// local +X axis and the bevel gear meshes as in the editor.
        /// </summary>
        public static Transform3D GetBevelPartFrame(BevelGearComponent gear, double axialOffset)
            => AlibrePartPlacement.GetBevelPartFrame(gear, axialOffset);

        /// <summary>
        /// The 16 values of an Alibre transformation (<c>IADGeometryFactory.CreateTransform</c>): the X, Y and Z vectors of the frame in
        /// elements 0-2, 4-6 and 8-10 and the translation in 12-14; elements 3, 7 and 11 are 0 and element 15 is 1. The transform is in
        /// millimetres; Alibre works in centimetres (<see cref="SingleToothSketchBuilder.MmToAlibreUnits"/>), so the translation is converted.
        /// </summary>
        public static double[] ToAlibreMatrix(Transform3D transform)
            => AlibrePartPlacement.ToAlibreMatrix(transform);

        /// <summary>Makes a part name unique in the assembly (a gear train often holds several gears with the same tooth count and module).</summary>
        public static string UniqueName(string name, ISet<string> used)
            => AlibreAssemblyPartSelector.UniqueName(name, used);

        internal IADPartSession ExportComponent(GearComponent component)
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
        internal static void CheckFeature(IADPartFeature feature, string description)
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
        internal IADMirrorFeature AddHerringboneMirror(IADPartSession session, IADPartFeature tooth, IADDesignPlane mirrorPlane)
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
        internal static IADPartFeature AddHelicalTooth(IADPartSession session, IADSketch sketch, object axis, SingleToothHelix helix)
        {
            return session.Features.AddHelicalBoss(sketch, HelicalBossParallelOriented, axis, ADHelixType.AD_Height_Pitch, helix.RightHand, false,
                helix.Height, helix.Lead, helix.Revolutions, ADPitchType.AD_Constant, 1.0, helix.Lead, 0.0,
                ADHelixConditionType.AD_Natural, 0.0, 0.0, ADHelixConditionType.AD_Natural, 0.0, 0.0,
                ToothFeatureName, "", "", "", "", "", "", "", "", "");
        }

        private static bool DrawFigures(IADSketch sketch, IEnumerable<ToothSketchFigure> profile, IEnumerable<ToothSketchReferenceCircle> referenceCircles,
            double ySign = 1.0, double centreHoleRadius = 0.0)
            => AlibreSketchDrawer.DrawFigures(sketch, profile, referenceCircles, ySign, centreHoleRadius);
    }
}

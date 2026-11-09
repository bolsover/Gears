# Alibre export of spiral, zerol and hypoid bevel gears

This page records what the Alibre automation API (reference in `Reference\API Documentation`) offers for making curved-tooth bevel gears, why the idea of subtracting a solid tooth space from the blank with a **design boolean** cannot be used from code, and which method the exporter follows. The straight bevel export (see [Gear assemblies](gear-assemblies.md)) is unchanged; the geometry of the curved teeth is described in [Spiral, zerol and hypoid bevel gears](spiral-zerol-hypoid-bevel-gears.md).

## Why the straight bevel method is not enough

The straight bevel export makes the blank with `AddRevolvedBoss`, sketches one valley on the back cone plane and cuts it with a **two-section loft** (`AddValleyLoftCut`) from the heel valley to the cone apex, then repeats it with `AddCircularFeaturePattern`. A loft between a scaled heel section and the apex point has straight tooth lines, so it cannot follow the circular arc of a spiral or zerol tooth.

## Comparison of methods

| Approach | API | Verdict |
|---|---|---|
| **Design boolean** (unite, subtract, intersect solids) | `IADDesignBooleanFeature`, `ADPartFeatureType.AD_DESIGNBOOLEAN_FEATURE` | The interface is documented as a placeholder: *"This interface has no methods or properties. It is a placeholder for future expansion of the API"*. `IADPartFeatures` has no call that creates it from two solids. It is a user interface operation only, so **it cannot be used from automation**. |
| **Mesh boolean** | `IADDesignMeshes.CreateFromFile(fileName, meshName, units)`, then `IADPartFeatures.AddMeshBooleanFeature(designMesh, designMeshOcc, ADBooleanOperator, name)` | Works from code and accepts union, subtraction and intersection. The result is a faceted mesh, not exact BRep, and a mesh file is needed (the 3D builder can write STL). Usable as a quick fallback; weak for CAD work. |
| **Multi-section loft cut (recommended)** | `IADPartFeatures.AddLoftCut(CrossSections, Tangents, TangentMagnitudes, TangentAngles, GuideCurves, GuideCurveType, MinimizeTwist, MinimizeCurvature, SimplifySurface, ConnectEnds, Name)` | Takes two or more sketches, faces or design points as sections and optional 3D sketch guide curves. Sections placed at several cone distances and turned by the spiral twist give one curved tooth space as exact BRep. |
| **Swept cut** | `AddSweptCutout(profile, path, ...)` | The tooth-space profile is swept along a 3D sketch path. A plain sweep keeps the profile size, but the tooth space tapers with the cone distance, so it is only a fallback for a constant-section approximation. |
| **Helical cut** | `AddHelicalCutout` | A helix is not the circular arc of a face-milled cutter. Not suitable. |

## Subtracting a tooth-space solid

The idea of *subtract a solid tooth space from the blank, then pattern it* is achieved by the loft cut: a cut feature is the subtraction of the lofted solid of its sections from the blank. The solid design boolean is therefore not needed, and the feature stays parametric and exact. The circular pattern then repeats the tooth space about the gear axis.

## Recommended route

1. Make the blank as for the straight bevel gear (sketch the blank profile on the XY plane and revolve it about the X axis).
2. Take the tooth-space sections of `SpiralToothSpaceSections` (pure geometry, unit tested): the valley figures of `BevelToothSketch` at the heel, several intermediate cone distances and the toe. Each section is scaled about the apex by `R / R_heel` and turned about the gear axis by the spiral twist `SpiralToothTrace.TwistAt(R)`. This is what `BevelBlankProfile` does for the 3D view, so the Alibre part matches the screen model.
3. Put each section on its own plane, made with `IADDesignPlanes.CreateBy3Points` through three design points: the point on the X axis where the scaled back cone meets it, a point along the back cone line and a point across it, the last two turned about the X axis by the twist of the section. The sketch axes of each plane are found by mapping those three world points into the sketch (as for the straight bevel plane), the section figures are drawn into it, and the lofted tooth space is cut with `AddLoftCut` (`MinimizeTwist` true, then the same with empty collectors, then without minimising the twist). Guide curves (3D sketch splines through matching points of each section) are not made; they are the next step if the plain loft twists.
4. Check the feature (`HasError`) and pattern it with `AddCircularFeaturePattern` about the gear axis.
5. **Hypoid:** the same sections are used. `HypoidGearComponent` gives its own pitch cone angle and cone distance (from `HypoidGeometry`) to its blank profile, so the planes lie on the hypoid pitch cone of the gear, and the pattern axis is the gear's own axis, which does not pass through the apex of its mate.

The number of sections is the `CurvedBevelSectionCount` property of `AlibreGearExporter` (zero chooses 5 to 8 from the twist, one interval for every half tooth pitch of twist). A loft between two sections that turn by half a pitch or more would fold over, so the export is refused with a message that asks for more sections, a smaller spiral angle or a narrower face. Each section adds three design points and a plane to the part.

## The section past the toe

The last section of the loft is a flat sketch on a plane that lies on the toe face of the blank. That face (the blank line G to H, perpendicular to the pitch cone line) is revolved about the gear axis, so it is a **cone**, not a plane. A plane touches a cone along one line only; away from the centre line of the tooth space the cone lies on the material side of the plane. A loft that ends on that plane stops before it reaches the cone and leaves a thin sliver of uncut material at the sides of each tooth space on the toe face. The straight bevel export has no such sliver because its loft runs to the apex point, and the heel end is not affected because the back face lies on the other side of its tangent plane.

To cut the toe face through completely, `SpiralToothSpaceSections.Build` is asked (`extendPastToe`, used by the exporter) for **one more section past the toe, towards the apex**, in addition to `CurvedBevelSectionCount`. It is the same valley sketch at a smaller scale about the apex, turned by the twist that the tooth trace has at that cone distance, and it is marked `IsExtension`. Its length along the pitch cone comes from `SpiralToothSpaceSections.ToeExtensionScale`:

* A flat plane touching a cone at its centre line leaves a gap of about `s^2 / (2 r)` at a sideways distance `s`, where `r` is the pitch radius at the toe. With `s` one circular pitch at the toe, `pi r / teeth`, and a factor of two for margin, the length is `pi^2 r / teeth^2`.
* A floor of 2 percent and a cap of 25 percent of the toe cone distance apply (`ToeExtensionMinFraction`, `ToeExtensionMaxFraction`); they are named constants that can be tuned after an Alibre check.
* The cutter trace must reach the cone distance (`SpiralToothTrace.Reaches`). If it does not, the extension is shortened to the nearest distance it reaches, and it is left out when nothing beyond the toe remains.
* The twist step from the toe section to the extension section is covered by the half-pitch check of the other sections.

Beyond the toe there is no material, so a longer extension does no harm to the part; it only makes the loft longer. The default list of `Build` does not have the extension section.

## Mesh boolean fallback

Write the tooth space (or the whole gear) as STL with the 3D builder, import it with `CreateFromFile`, and apply `AddMeshBooleanFeature` with `AD_SUBTRACTION`. Limits: the result is faceted, the features cannot be edited as sketches, mesh booleans are slow for many teeth, and the units of the file must be given correctly.

## Risks and limits

* **Loft twist or self-intersection** with a large spiral angle or few sections: raise the number of sections, keep `MinimizeTwist` on, or add guide curves.
* **Tredgold approximation:** the heel profile is used along the face, as in the 3D view, so the tooth space is approximate.
* **Plane orientation:** a plane through three points is fixed in space, but Alibre decides the origin and axes of its sketch, so the exporter maps three world points into each sketch to find them (the straight bevel export does this too). Which way the twist turns in Alibre is checked by comparing with the 3D view.
* The Alibre calls cannot run without a licensed Alibre session; only the section geometry is covered by automated tests.

## Checks inside Alibre

The Alibre calls cannot run in the automated tests. After *File > Export selected to Alibre…* of a spiral, zerol or hypoid gear:

1. Open the part and check that the feature tree has `GearBlank`, `ToothValley` and `ToothValleyPattern`, none of them with an error.
2. Check that all the teeth are there (the pattern count is the number of teeth) and that the tooth lines are arcs, not cone elements.
3. Compare the part with the 3D view of the editor: the same hand, the same curvature, a tooth space that is central at the middle of the face.
4. If the loft twists or folds, export again with more sections (`CurvedBevelSectionCount`), a smaller spiral angle or a narrower face; the next fallbacks are guide curves through matching points of the sections, and then the mesh boolean.
5. For a hypoid gear, check that the teeth are patterned about the axis of the gear itself.
6. Check that the toe face is cut through completely: no thin sliver of uncut material is left at the sides of the tooth spaces on the toe face. If a sliver remains, increase `ToeExtensionMinFraction`; the loft may need guide curves when the cutter trace allows only a tiny extension.

## To be confirmed inside Alibre

* The installed type library has no solid boolean creation call (the reference is the only evidence so far).
* `AddLoftCut` accepts the sketches of all sections and cuts a tooth space that follows the arc without twisting.
* The circular pattern of the lofted feature gives all teeth without errors and the result matches the 3D view.
* `CreateBy3Points` accepts the design points of the section planes, and a three-point plane turned about the X axis takes the sketch with the axes that the sketch map finds.

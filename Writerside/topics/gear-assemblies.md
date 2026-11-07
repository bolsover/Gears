# Gear assemblies: shafts, gears and meshes

A gear train is a `GearAssembly`: shafts carrying gears, plus *meshing relations* between gears.
`GearAssembly.Solve()` keeps the first shaft where it is and places every other shaft from the shaft that drives it.
All computed lengths and coordinates are given to **7 decimal places**; angles keep full precision.

## Gear families

| Family | Component | Mesh type | Notes |
|---|---|---|---|
| Spur, helical, internal | `InvoluteGearComponent` | `Parallel` | Parallel shafts. A pair uses `GearParameterCalculator.CalculatePair`. |
| Rack and pinion | `RackComponent` | `Rack` | A spur or helical pinion drives a straight or helical rack mounted on a carrier shaft. |
| Bevel | `BevelGearComponent` | `Bevel` | Straight bevel gears, Tredgold approximation, any shaft angle. |
| Worm and wheel | `WormComponent`, `WormWheelComponent` | `Worm` | Crossed shafts at 90 degrees. |
| Planetary | `PlanetaryGearSet` | none | Sun, planets, ring and carrier plate; builds an ordinary assembly. |

A component's *phase* is its rotation about its axis in radians. For a rack the phase is the linear displacement in mm.

### Rack and pinion

* The rack datum line lies at `m z / 2 + x m` from the pinion axis (x is the profile shift), parallel to the pinion axis
  and perpendicular to the line of centres, with the tooth tips pointing at the pinion.
* Turning the pinion by `phi` slides the rack by `r * phi`, where `r` is the transverse pitch radius (`m z / 2` for a spur pinion). The linear speed is `2 pi r` mm per revolution.
* The pinion must be an external gear. A spur pinion needs a straight rack.
* **Helical rack:** the rack has a *Helix angle* and a *Helix hand*. As for helical gears, the module and pressure angle are the normal values. In the end-face section the rack has the pitch `p_t = pi m_n / cos(beta)` and the pressure angle `tan(alpha_t) = tan(alpha_n) / cos(beta)`, and the pinion datum radius is `m_n z / (2 cos(beta))`. Each tooth section is sheared along the rack by `tan(beta)` per mm of face width. A helical pinion needs a rack of the **same helix angle and the opposite hand**, as external helical gears do; the solver reports an error otherwise. *Add mate* gives the new rack the pinion's helix angle and the opposite hand.

### Bevel gears (Tredgold)

* Pitch cone angle: `tan(delta1) = sin(sigma) / (z2 / z1 + cos(sigma))`, and `delta1 + delta2 = sigma` (the shaft angle).
* The tooth profile is the involute profile of a virtual spur gear with `Zv = z / cos(delta)` teeth, mapped onto the large-end
  plane (radial distances from the pitch circle times `cos(delta)`, angle times `Zv / z`), then scaled towards the cone apex by
  `(Re - b) / Re` at the small end. `Re` is the cone distance and `b` the face width along the cone.
* Both cone apexes coincide. The tooth phase is the same as for an external spur pair, and the speed ratio is `-z1 / z2`.
* A bevel gear can mesh with one mate only: the solver sets the mate's tooth count, shaft angle and bevel type on both gears.
* The property grid also reports the quantities of the usual bevel gear tables (for example KHK / HZPT): dedendum angle `atan(hf / Re)`, addendum angle (Standard `atan(ha / Re)`; Gleason: the dedendum angle of the mate), tip cone angle `delta + theta_a`, root cone angle `delta - theta_f`, tip diameter `d + 2 ha cos(delta)`, pitch apex to crown `Re cos(delta) - ha sin(delta)` and the recommended face width limit (the smaller of `Re / 3` and `10 m`).
* The 3D solid and the 2D outline are built from the same large-end profile, so the tip and root radii are exactly `d/2 + ha cos(delta)` and `d/2 - hf cos(delta)`. At the large end every profile point lies on the back cone (the surface perpendicular to the pitch cone), so the tooth tips sit nearer the apex than the pitch circle, by `ha sin(delta)`, as in the usual section drawings. The small end is the large end scaled about the pitch apex, so the tooth edges, tip lines and root lines all run to the apex. For Standard teeth this gives exactly the tip and root cones of the tables. For Gleason teeth the tables use a constant-clearance taper; the solid keeps the proportional taper, so the tooth depth at the small end differs slightly (a few hundredths of a millimetre for a face width of one third of the cone distance).

#### Back cone, hub and bore

The 3D solid of a bevel gear is the toothed slab between the back cone (the surface perpendicular to the pitch cone at the cone distance `Re`) and the front cone, a face width `b` nearer the apex. The solid has a bore through the slab and a hub projecting behind the back cone. The back cone face runs from the teeth in to the hub, so the teeth rest on a cone and not on a flat disc. The 2D outline shows the hub and the bore as circles inside the teeth.

* Back cone angle: `90° - delta`, and back cone distance `d / (2 cos(delta))`. The virtual tooth count `Zv = z / cos(delta)` is `2 * back cone distance / m`. Both appear in the property grid under "Cone".
* Bore diameter (property grid, "Hub and bore"): `0` uses the diameter of the shaft that carries the gear, so the shaft runs exactly through the bore. A gear that is not mounted on a shaft has no bore until a value is entered. The bore must not exceed 0.6 times the root diameter `d - 2 hf cos(delta)`; an automatic bore is limited to that value, a larger entered value is rejected.
* Hub: the hub switch turns the hub on or off. Hub diameter `0` gives 1.8 times the bore (0.4 times the root diameter when there is no bore); an entered value must be larger than the bore and not more than 0.85 times the root diameter. Hub length is the projection behind the back cone along the axis; `0` gives 0.75 times the bore, but at least half the face width.
* The grid also reports the bore, hub diameter and hub length actually used, and the overall length along the axis from the back of the hub to the front of the teeth.
* The shaft is sized from the whole body of the gear, hub included, so the shaft still covers the hub. Spacing and overlap checks between gears on one shaft still use the face width.
* The bore, hub switch, hub diameter and hub length are saved with the gear. Files saved earlier have none of them and open with the automatic values and a hub.

#### Standard and Gleason tooth proportions

The *Bevel type* property (and the *Start with* menu entries *Bevel gear (standard)* and *Bevel gear (Gleason)*) selects the tooth depth system. Set it on the driving gear; the meshed gear follows.

| | Standard | Gleason |
|---|---|---|
| Addendum `a` | `m` on both gears | gear `0.54 m + 0.46 m / q`, pinion `2 m - a(gear)` |
| Clearance `c` | `0.25 m` | `0.188 m + 0.051 mm` |
| Whole depth `h` | `2.25 m` | `2.188 m + 0.051 mm` |

`q` is the ratio of the virtual (Tredgold) tooth numbers, `(z1 / cos(delta1)) / (z2 / cos(delta2))`, taken as at least 1 for the gear. For a 90 degree pair it is the square of the tooth ratio, so the formula equals the usual `0.54 m + 0.46 m / u^2` with `u = z2 / z1`.
A 2:1 pair therefore has a gear addendum of `0.655 m` and a pinion addendum of `1.345 m`. The long and short addenda are applied as equal and opposite profile shifts of the virtual spur gears, so the working depth stays `2 m` and the teeth are thicker on the pinion. The property grid shows the addendum, dedendum, whole depth and clearance. Files saved before this option load as Standard.

### Worm gear sets

* The module is the axial module. `q` (the diameter quotient) gives the pitch diameter `q m`. The lead angle is
  `tan(gamma) = starts / q`. The thread is swept as a helicoid from the trapezoidal axial rack profile
  (axial pressure angle `tan(alpha_x) = tan(alpha_n) / cos(gamma)`).
* The wheel is a helical involute gear with helix angle `gamma` and normal module `m cos(gamma)`, so its pitch diameter is `m z`.
  It has no throat; an enveloping (globoid) wheel is not modelled.
* The centre distance is `(d_worm + d_wheel) / 2`. The wheel turns at `starts / z` of the worm speed, with the sign set by the hand.

### Planetary sets

* All gears share the module, so the ring has `Zr = Zs + 2 Zp` teeth. Equal spacing needs `(Zs + Zr) / N` to be a whole number.
* The Willis equation `Zs * ws + Zr * wr - (Zs + Zr) * wc = 0` links the three members; any one can be held fixed.
  With the ring fixed and the sun driving, the carrier turns at `Zs / (Zs + Zr)` of the sun speed.
* `PlanetaryGearSet.BuildAssembly()` returns the posed stage as an assembly without relations (one shaft for the sun, each planet,
  the ring and the carrier plate), so the scene builder, viewer, export and serialiser work on it.

## Gear train editor

The editor (`GearTrainForm`, the main window of the program) starts a train with a spur gear, a bevel gear, a worm or a planetary stage.
*Add meshed gear* adds a parallel gear; *Add mate* adds a rack, bevel gear or worm wheel to the selected gear.
The *File* menu saves and loads a train as JSON and exports the scene as STL or OBJ. *Animate* turns the input shaft
at the chosen speed; the report shows speeds in rpm (mm/min for a rack).

### Choosing and sorting the properties

The property grid lists the properties of the selected shaft, gear or planetary stage in categories. The bar above it controls what is listed:

* **Sort** orders the list: *By category (A-Z)* (the default), *Alphabetical (one list)* or *Custom order*, which keeps the categories and uses the order you chose.
* **Choose properties...** opens a dialog for the selected kind of item (spur gear, rack, bevel gear, worm, worm wheel, planetary stage or shaft). Tick the properties to show, move them up or down, or use *Sort A-Z* (which switches to custom order). *Show all*, *Show none* and *Reset* help to start again.
* **Notes** are items you add yourself: type a name and a value in the empty last row of the notes table (select a row and press Delete to remove one). Each note appears in the grid in the *Notes* category and its value can be edited there. Names must differ from each other and from the property names.
* **Save as default** (also *Apply and save as default* in the dialog) keeps the layout, the sort mode and the notes in `%APPDATA%\Gears\property-layout.json`. The file is read when the editor starts; a missing or damaged file gives the built-in layout.

A layout belongs to a kind of item, so all spur gears share one layout. Hiding a property only removes it from the grid; its value still takes part in the calculation. Properties added in a later version of the program appear at the end of the list.

### Clearance, backlash, profile shift and centre distance

Select an involute gear in the editor to change these in the property grid. Meshing involute gears (spur, helical, internal) use all four:

* **Clearance** and **Backlash** (per gear) and **Profile shift** `x` (per gear) change the tooth profile. Backlash thins the tooth; the working
  backlash of a pair is the circular backlash measured on the working pitch circles.
* **Centre distance (mm)** of a driven gear sets the distance to its driver's shaft. Entering a value fixes it (*Fixed centre distance* turns on);
  entering 0, or turning the flag off, goes back to the distance that follows from the profile shifts.
  With a fixed distance `a_w` the working pressure angle follows from `a_w cos(alpha_wt) = a cos(alpha_t)`, and the working pitch diameters,
  contact ratio and backlash are recalculated. For an unshifted pair without backlash the working backlash is `2 a_w (inv alpha_w - inv alpha)`:
  a larger distance adds backlash to an external pair and takes it away from an internal pair.
  A distance below `a cos(alpha_t)` is rejected because the base circles would overlap.
* **Fit profile shift** (*Build* menu) gives the selected driven gear the profile shift that makes the mesh run at the current centre distance with no
  fixed distance, so the distance follows from the profile shifts. The driver is not changed.
* The *Mesh* group of the property grid and the *Stages* tab show the standard and working centre distance, the working pressure angle, the working
  backlash and the contact ratio. The report warns when the working backlash is negative (the teeth jam) or the contact ratio is below 1.
* A fixed centre distance is saved in the JSON file. Only parallel-axis meshes support it; rack, bevel and worm meshes report an error.

## Files and export

* **JSON**: `AssemblySerializer` stores shafts (placement, size, input rotation), gears with their parameters and mounts, and relations.
  Derived results are solved again when the file is loaded.
* **STL and OBJ**: `MeshExporter` writes one solid or object per gear (and optionally per shaft), 7 decimal places, invariant culture.
* **Animation**: `GearTrainAnimator` advances the input shaft angle; the scene is rebuilt from the mesh relations.

## Limits

* Bevel and worm geometry is an approximation intended for visualisation, DXF and STL hand-off, not a stress calculation.
* Enveloping worm wheels and idler bevel gears are not supported.
* A planetary stage is edited through its properties; other gears cannot be added to it.


## Gear and mesh parameter tables

The Gear Train form has two tabs that list every calculated parameter, with its symbol, formula, value and unit. They use the table calculated by `GearParameterCalculator`, with the symbols and formulae of *Elements of Metric Gear Technology*.

- **Gear parameters** shows the table of the selected gear: module, pressure angle, tooth count, profile shift, pitch, base, tip and root diameters, addendum, dedendum, tooth thickness, and for helical gears the normal and transverse values. Select a gear in the tree or in the 3D view.
- **Mesh parameters** shows the table of the mesh that drives the selected gear, or the first mesh it drives: standard and working centre distance, working pressure angle, centre distance increment factor, working involute function, contact ratios, gear ratio and, for a fixed centre distance, the working circular backlash.

Both tables update when you change a gear property or the centre distance. Symbols and formulae are drawn from LaTeX; hover over a cell to see the LaTeX source. Gear families other than involute spur and helical gears show their basic geometry only, and bevel, worm and rack meshes have no involute pair table.


## Imperial reference

All design parameters and calculations are metric. The **Imperial reference** tab of the Gear Train form shows the imperial equivalents of the selected gear and of its mesh, for reference only. Nothing shown in inches changes the design.

| Quantity | Formula |
|---|---|
| Diametral pitch (D.P.), teeth per inch | `P = 25.4 / m` |
| Module from D.P. | `m = 25.4 / P` |
| Circular pitch (C.P.) in mm | `p = pi * m` |
| Circular pitch (C.P.) in inches | `p = pi / P = pi * m / 25.4` |
| Length in inches | `inches = mm / 25.4` |

* The gear table starts with the diametral pitch and circular pitch of the module. A helical gear shows both the normal values (from `m_n`) and the transverse values (from `m_t = m_n / cos(beta)`); a worm shows the axial values.
* Every other length of the gear table (pitch, base, tip and root diameters, addendum, dedendum, face width, clearance, backlash and so on) follows, in millimetres and inches. The pitch diameter in inches equals `N / P`.
* The lower table converts the lengths of the mesh (standard and working centre distance, working pitch diameters and so on).
* The conversions are in `ImperialUnits` and `ImperialReference` (`Gears\Calculator\Assemblies\ImperialUnits.cs`). The values follow the relationships in *Elements of Metric Gear Technology*.



## 2D view

The area above the report tabs of the Gear Train form has two tabs: **3D view** (unchanged) and **2D view**. The 2D view draws the outline of every visible gear in the world X/Y plane, using the same solved scene as the 3D view.

* Each gear is coloured by its shaft. The selected gear or shaft is drawn in orange. Gears hidden in the tree are not drawn.
* Shafts are drawn as their projected outline (a circle when the shaft axis is perpendicular to the view, a bar when it is angled), controlled by **View > Shafts**.
* **Pitch circles** shows the dashed pitch circle of each spur and helical gear, and **Labels** shows the gear names. Both *View* menu items apply to the 2D view only.
* The mouse wheel zooms about the cursor, dragging pans, and **View > Fit view** fits both views. The status line at the bottom shows the cursor position in millimetres and the scale.
* Clicking a gear or shaft selects it in the tree, the property grid and the parameter tabs, as in the 3D view. A click near an outline also selects that gear.
* The view follows the animation and every edit, so meshing gears can be checked as they turn.

For bevel and worm gears the 2D view is the projection of the outline onto the X/Y plane. Pitch circles are only drawn for gears whose axis is perpendicular to the view.



## Export a gear to Alibre

When the editor is opened from the Alibre add-on, **File > Export gear to Alibre…** creates the selected gear as a new Alibre part. The item is disabled in the stand-alone program, which has no Alibre session. **Involute spur and helical gears (external and internal), racks (straight and helical), worms and worm wheels** are exported; bevel gears, planetary stages and whole gear trains are not (yet).

The export follows the usual manual steps, using the Alibre automation API (`AlibreGearExporter`):

1. A new part is created (`CreateEmptyPartEx`) and a 2D sketch is added on the `XY-Plane`.
2. The sketch gets **one closed tooth**: spline curves for the two trochoids and two involutes, arcs for the addendum and root, and two radial lines to the axis. The outline from `AlibreGearGenerator` is exactly one pitch wide, so the two radial lines close it into a wedge, and the patterned wedges tile into a solid disc. The pitch, base, root and outside circles are added as reference circles.
3. A single tooth is formed: a spur gear's sketch is extruded to the face width (`AddExtrudedBoss`); a helical gear's sketch is swept along a helix (`AddHelicalBoss`, see below).
4. The tooth feature is patterned about the `Z-Axis` (`AddCircularFeaturePattern`) with *z* instances, at `2 pi / z` radians apart.

### Internal gears

The single tooth of an internal gear is a wedge of the ring, one pitch wide. The sketch is the inner tooth contour (inside arcs, involute splines and the root arc), closed by two radial lines out to the rim circle and a rim arc between them. The rim circle is the root circle plus the rim thickness (never less than 1 mm). Nothing touches the axis, so the bore stays open. The tip arcs never go below the base circle, because an involute cannot start there. The part is named `InternalGear_Z{z}_M{m}`.

### Helical gears

A helical gear uses the same transverse (end-face) tooth sketch, made with the transverse module `m / cos(beta)`. The single tooth is formed with a **helical boss** (`AddHelicalBoss`) about the `Z-Axis` instead of a straight extrusion. The sketch is not parallel to the axis, and the helix is a constant-pitch helix given by height and pitch:

* height *H* = the face width;
* pitch (lead) *L* = `2 pi r / tan(beta)`, with *r* the transverse pitch radius including profile shift;
* revolutions = *H* / *L*, so the tooth section turns through `H tan(beta) / r`, the same rule the 3D preview uses;
* a right-hand gear gives a clockwise helix and a left-hand gear a counter-clockwise one.

The circular pattern then copies the helical tooth. Helix angles from 0 to 60 degrees are accepted. The part is named `HelicalGear_Z{z}_M{m}_B{beta}{R|L}`, or `InternalHelicalGear_...` for an internal gear.

The sketch, helix lead, turn angle and hand are tested without Alibre. How Alibre orients the swept sketch (the direction of the sweep along the axis, and whether `bClockwise` matches the preview's hand) has not been checked inside Alibre.

Alibre sketches and features are in **centimetres**, so each length is rounded to 7 decimal places in millimetres and then multiplied by 0.1. `SingleToothSketchBuilder` builds the sketch plan without calling Alibre, so the closure and pitch span can be tested without a running Alibre; it refuses an outline whose pieces do not join or do not span one pitch. If Alibre rejects a step, the error message names the step.

### Racks

The rack is built from **one tooth cell** and a **linear pattern**. `RackToothSketchBuilder` makes the plan:

1. A sketch on the `XY-Plane` holds the closed cell: the rack length runs along X, the tooth tips point towards +Y, and the cell spans X = 0 to one pitch, with the tooth in the middle and the rack body below it. The cell starts and ends in the middle of a tooth space, like the rack in the 3D view, and the cells add up to exactly that rack. The cell is made of lines only.
2. A straight rack's tooth is extruded to the face width (`AddExtrudedBoss`).
3. The tooth is patterned along the `X-Axis` (`AddLinearFeaturePattern`) with *z* instances, one transverse pitch apart.

The installed `AlibreX.dll` has a different `AddLinearFeaturePattern` signature from the documentation: the instance count and distance come first, then the optional occurrence and axis. The code uses the installed one.

Alibre can accept the call and still mark the pattern feature as failed (`IADLinearPatternFeature.HasError`), which leaves a single tooth. The exporter therefore checks the pattern it gets back and, if it has an error, deletes it and tries the next argument form: (1) the plain one-direction pattern with integer counts; (2) the same with counts passed as real numbers and a real spacing for the unused second direction; (3) the same, patterning the geometry directly (the opposite of `PatternGeometry`). If no form is accepted, the export stops with a message that lists each failure instead of silently leaving one tooth.

A **helical rack** uses the transverse section: pitch `pi m_n / cos(beta)` and pressure angle `atan(tan(alpha_n) / cos(beta))`. The tooth is formed with a **rigid sweep** (`AddSweptBoss`, `IsRigid = true`, so the sketch stays parallel to its plane) along a straight path sketched on the `XZ-Plane`. The path runs from the origin to `(tan(beta) * face width, face width)` with the sign of the hand, so each section of the tooth moves along X by `tan(beta)` per unit of depth. Parts are named `Rack_Z{z}_M{m}` and `HelicalRack_Z{z}_M{m}_B{beta}{R|L}`.

### Worm wheels

A worm wheel is exported as the helical involute gear it is made of (`WormWheelComponent.ToInvoluteGear()`): normal module `m cos(gamma)`, helix angle equal to the lead angle, and the hand of the worm. It follows exactly the helical gear steps above. The wheel needs its lead angle first, so mesh it with a worm before exporting; otherwise the export says so. The part is named `WormWheel_Z{z}_M{m}_L{gamma}{R|L}`. The throat of an enveloping wheel is not modelled.

### Worms

A worm is made exactly like a helical gear. `WormThreadSketchBuilder` makes the plan:

1. A sketch on the `XY-Plane` holds the closed **end-face section of one thread start**. A worm's section repeats every `360 / starts` degrees, so the sketch is a wedge one start wide: the root arc, the spiral flank (a spline through points of `WormComponent.RadiusAt`), the tip arc, the other flank and the root arc again, closed by two radial lines to the axis. A single-start worm goes all the way round, so its section closes on itself without radial lines. The root, pitch and tip circles are added as reference circles.
2. The single thread is formed with a **helical boss** (`AddHelicalBoss`, parallel oriented) about the `Z-Axis`, as for a helical gear: height = the threaded length, pitch = the lead `starts * pi * m`, a clockwise helix for a right-hand worm. The section turns through `2 pi` per lead.
3. For several starts the thread is circular-patterned about the `Z-Axis` (`AddCircularFeaturePattern`), one instance per start, `2 pi / starts` apart.

There is no separate core cylinder: the swept section includes the root cylinder. The section is taken at the end of the part, at the angle the 3D view has there (Z = -L/2), so the thread positions agree with the 3D view, and the tests check that every sketch point lies on the thread surface of the component. The exporter checks that the helical boss and the pattern are not in error in Alibre. The part is named `Worm_S{starts}_M{m}_Q{q}_{R|L}`.

### Alibre values not checked inside Alibre

* How the sketch y axis of the `XZ-Plane` is oriented. The sweep path of a helical rack assumes that sketch y is the global **+Z**. If Alibre's y points along -Z, set `AlibreGearExporter.AxialPlaneSketchYIsNegativeZ` to true (an instance property of the exporter, not yet on a menu); the path is then drawn mirrored in y.
* Whether the sweep path of a helical rack starts on the profile plane as Alibre needs, and whether the swept tooth keeps its section parallel to the end face (the documented meaning of `IsRigid`).
* Whether the linear pattern instance count includes the original tooth (the same assumption as for the circular pattern), and whether the default axis is named `X-Axis`.
* Whether `bClockwise = true` gives a right-hand thread, as the documentation says (the worm uses the same helical boss settings as the helical gears).
* Adjacent patterned rack teeth share a face; Alibre should merge them into one solid.

Export one straight rack, one helical rack (for example 12 teeth, module 2, 15 degrees, left hand), one worm and one worm wheel first, and check the results before relying on them.

## Alibre add-on

The Gear Train editor can be loaded inside Alibre Design as a managed add-on. The `GearsAddOn` project (a class library, .NET Framework 4.8.1, x64) references `GearsUI` and `Gears`, so the add-on opens the same `GearTrainForm` as the stand-alone program. The stand-alone program is unchanged.

In Alibre a **Gears** menu appears with one command, **Gear Train**. The command opens the editor as a modeless window owned by the Alibre main window, so Alibre stays usable. Choosing the command again brings the open window forward; closing the window and choosing the command again opens a new one. The command is enabled on the home window and in every part, assembly and drawing workspace. The editor does not read or change Alibre documents; gear trains are saved to their own JSON files.

### Files

| File | Purpose |
| --- | --- |
| `AlibreAddOn.cs` | The entry points that Alibre calls: `AddOnLoad`, `AddOnInvoke`, `AddOnUnload` and `GetAddOnInterface`. The namespace `AlibreAddOnAssembly` and the class name `AlibreAddOn` must stay as they are. It also loads the add-on's own libraries by name from the add-on folder, because Alibre does not apply the binding redirects of the add-on. |
| `GearTrainAddOn.cs` | Implements `IAlibreAddOn`: the menu tree, tool tips and enabled state, and `InvokeCommand`, which opens the editor. |
| `GearsAddOn.adc` | The add-on configuration that Alibre reads: managed DLL, loaded at startup, menu text, workspace and the identifier `{05A0F2F2-61D4-48B3-A737-BD23152F6FFF}`. |
| `addon.json`, `GearsAddOn.ico` | Add-on metadata and icon. |
| `Install-GearsAddOn.ps1` | Copies the build output into the Alibre add-ons folder and registers it. |

### Build and install

1. Build the `GearsAddOn` project (it is part of `Gears.sln`). The output folder `GearsAddOn\bin\Debug` holds the add-on DLL, the `.adc` file, `GearsUI.exe`, `Gears.dll` and every library they need.
2. Close Alibre Design. In an administrator PowerShell, run `Install-GearsAddOn.ps1` from the output folder. It copies the files to `C:\ProgramData\Alibre AddOns\GearsAddOn` and writes the value `{05A0F2F2-61D4-48B3-A737-BD23152F6FFF}` = that folder under `HKEY_LOCAL_MACHINE\SOFTWARE\Alibre Design Add-Ons`. This is the registration that Alibre looks for.
3. Start Alibre Design. The **Gears** menu appears.

While developing, `Install-GearsAddOn.ps1 -InPlace` registers the build output folder itself, so a rebuild followed by an Alibre restart is enough. `Install-GearsAddOn.ps1 -Uninstall` removes the registration and the copied files.

### Limits

* The identifier in `GearsAddOn.adc`, `addon.json`, `GearTrainAddOn.AddOnId` and the install script must be the same; tests check this.
* The add-on needs the Alibre assemblies `AlibreAddOn.dll` and `AlibreX.dll`; the project takes them from the `AlibreLib` folder, as the other Alibre projects in this solution do.

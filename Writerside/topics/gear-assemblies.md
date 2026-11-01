# Gear assemblies: shafts, gears and meshes

A gear train is a `GearAssembly`: shafts carrying gears, plus *meshing relations* between gears.
`GearAssembly.Solve()` keeps the first shaft where it is and places every other shaft from the shaft that drives it.
All computed lengths and coordinates are given to **7 decimal places**; angles keep full precision.

## Gear families

| Family | Component | Mesh type | Notes |
|---|---|---|---|
| Spur, helical, internal | `InvoluteGearComponent` | `Parallel` | Parallel shafts. A pair uses `GearParameterCalculator.CalculatePair`. |
| Rack and pinion | `RackComponent` | `Rack` | A spur pinion drives a straight rack mounted on a carrier shaft. |
| Bevel | `BevelGearComponent` | `Bevel` | Straight bevel gears, Tredgold approximation, any shaft angle. |
| Worm and wheel | `WormComponent`, `WormWheelComponent` | `Worm` | Crossed shafts at 90 degrees. |
| Planetary | `PlanetaryGearSet` | none | Sun, planets, ring and carrier plate; builds an ordinary assembly. |

A component's *phase* is its rotation about its axis in radians. For a rack the phase is the linear displacement in mm.

### Rack and pinion

* The rack datum line lies at `m z / 2 + x m` from the pinion axis (x is the profile shift), parallel to the pinion axis
  and perpendicular to the line of centres, with the tooth tips pointing at the pinion.
* Turning the pinion by `phi` slides the rack by `m z / 2 * phi`. The linear speed is `2 pi m z / 2` mm per revolution.
* The pinion must be an external spur gear; helical racks are not supported.

### Bevel gears (Tredgold)

* Pitch cone angle: `tan(delta1) = sin(sigma) / (z2 / z1 + cos(sigma))`, and `delta1 + delta2 = sigma` (the shaft angle).
* The tooth profile is the involute profile of a virtual spur gear with `Zv = z / cos(delta)` teeth, mapped onto the plane
  perpendicular to the axis (radius times `z / Zv`, angle times `Zv / z`), then scaled towards the cone apex by
  `(Re - b) / Re` at the small end. `Re` is the cone distance and `b` the face width along the cone.
* Both cone apexes coincide. The tooth phase is the same as for an external spur pair, and the speed ratio is `-z1 / z2`.
* A bevel gear can mesh with one mate only: the solver sets the mate's tooth count, shaft angle and bevel type on both gears.

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
* Helical racks, enveloping worm wheels and idler bevel gears are not supported.
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

When the editor is opened from the Alibre add-on, **File > Export gear to Alibre…** creates the selected gear as a new Alibre part. The item is disabled in the stand-alone program, which has no Alibre session. **Involute spur and helical gears, external and internal,** are exported; other gear families and whole gear trains are not (yet).

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

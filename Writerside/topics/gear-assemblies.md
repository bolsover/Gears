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

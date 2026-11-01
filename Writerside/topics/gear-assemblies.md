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
* A bevel gear can mesh with one mate only: the solver sets the mate's tooth count and shaft angle on both gears.

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

The editor (menu *Gear Train*) starts a train with a spur gear, a bevel gear, a worm or a planetary stage.
*Add meshed gear* adds a parallel gear; *Add mate* adds a rack, bevel gear or worm wheel to the selected gear.
The *File* menu saves and loads a train as JSON and exports the scene as STL or OBJ. *Animate* turns the input shaft
at the chosen speed; the report shows speeds in rpm (mm/min for a rack).

## Files and export

* **JSON**: `AssemblySerializer` stores shafts (placement, size, input rotation), gears with their parameters and mounts, and relations.
  Derived results are solved again when the file is loaded.
* **STL and OBJ**: `MeshExporter` writes one solid or object per gear (and optionally per shaft), 7 decimal places, invariant culture.
* **Animation**: `GearTrainAnimator` advances the input shaft angle; the scene is rebuilt from the mesh relations.

## Limits

* Bevel and worm geometry is an approximation intended for visualisation, DXF and STL hand-off, not a stress calculation.
* Helical racks, enveloping worm wheels and idler bevel gears are not supported.
* A planetary stage is edited through its properties; other gears cannot be added to it.

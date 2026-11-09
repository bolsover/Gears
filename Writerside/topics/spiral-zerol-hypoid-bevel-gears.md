# Spiral, zerol and hypoid bevel gears

The editor models three curved-tooth bevel gears besides the straight bevel gear of [Gear assemblies](gear-assemblies.md). Straight bevel gears (Standard and Gleason proportions) are unchanged. This page reviews the geometry, describes how it is modelled and lists the limits.

| Family | Component | Mesh type | Shafts |
|---|---|---|---|
| Spiral bevel | `SpiralBevelGearComponent` | `Bevel` | Intersecting, any shaft angle |
| Zerol bevel | `ZerolBevelGearComponent` | `Bevel` | Intersecting, any shaft angle |
| Hypoid | `HypoidGearComponent` | `Hypoid` | Offset, usually 90 degrees |

## Geometry review

### Spiral bevel

The tooth trace is a circular arc of a face-mill cutter of radius `rc` instead of a cone element. The **mean spiral angle** `beta_m` (typically 35 degrees) is the angle between the tooth line and the cone element at the mean cone distance `R_m`, the middle of the face. The spiral angle changes along the face. With the cutter centre at the distance `R0` from the apex:

* `R0^2 = R_m^2 + rc^2 - 2 R_m rc sin(beta_m)`
* spiral angle at the cone distance `R`: `sin(beta) = (R^2 + rc^2 - R0^2) / (2 R rc)`
* polar angle of the trace seen from the apex: `cos(phi) = (R^2 + R0^2 - rc^2) / (2 R R0)`

The pitch cone develops into a sector of angle `2 pi sin(delta)`, so the angle about the gear axis is the polar angle divided by `sin(delta)`. A section at the cone distance `R` is therefore turned about the axis by the **twist** `(phi(R) - phi(R_m)) / sin(delta)`. It is zero at the mean section, and the tooth line makes the angle `beta(R)` with the cone element everywhere (`r d(psi) = tan(beta) dR`). Pinion and gear have **opposite hands** and the same `beta_m`, cutter radius, module and pressure angle.

The profile in the plane perpendicular to the axis has the transverse pressure angle `tan(alpha_t) = tan(alpha_n) / cos(beta_m)`.

The **hand** is right when, looking from the heel towards the apex along the axis, the tooth line advances counter-clockwise from the heel to the toe, like the thread of a right hand screw.

### Zerol bevel

Zerol gears have the same arc trace with `beta_m = 0`. The tooth line is a cone element at the middle of the face, so the pitch cones, the cone angles, the apex positions and the thrust are those of a straight bevel pair. At the heel and the toe the spiral angle is small and not zero, so the teeth are curved.

### Hypoid

A hypoid pair has spiral teeth on shafts that do not intersect: the pinion shaft is offset by `E` from the gear shaft along their common perpendicular (the shaft angle is usually 90 degrees). The pitch surfaces touch at the mean point only, the cone apexes do not coincide, and pinion and gear have **different spiral angles**. The model uses these relations at the mean point:

* the gear pitch radius is `r2 = m z2 / 2` (`m` is the mean transverse module of the gear);
* the offset angle `eps` in the pitch plane follows from the offset, `sin(eps) = E / r2`, and `beta1 - beta2 = eps`;
* equal normal pitch gives the pinion pitch radius `r1 / r2 = z1 cos(beta2) / (z2 cos(beta1))`, so the pinion is larger than its tooth count suggests;
* the pitch cone half angles `delta1` and `delta2` are solved numerically so that the shafts are at the shaft angle and the distance `E` apart, with the cones touching at the mean point (the outward normals are opposite);
* the speed ratio stays `z2 / z1`.

With these relations the sliding velocity at the mean point lies along the tooth line (the normal pitch of both gears agrees) and the surfaces move the same way. Without an offset the solution is the intersecting pair: `r1 / r2 = z1 / z2`, equal spiral angles and one common apex.

`Module` of a hypoid gear is the mean transverse module of the gear, the same on both components. The *mean spiral angle* is the pinion's, also the same on both; the gear's is `beta1 - eps`. The tooth depth is that of the gear at its large end on both gears, and only the standard proportions are used. `HypoidGeometry.Solve` returns the pair with the mean point and the apexes in a frame with the gear apex at the origin.

## Model and mesh

* **Tooth rendering.** The valley profile of the straight bevel gear (involute flanks, trochoidal fillets, Tredgold back cone) is kept and swept along the arc: the profile is placed on several sections between the heel and the toe (8 by default, more when the twist needs them), each scaled about the cone apex and turned about the axis by the twist of its cone distance. The flank surfaces between neighbouring sections are ruled. A straight trace uses only the heel and toe sections and produces exactly the mesh of the straight bevel gear. The 2D outline is the heel section.
* **Cone geometry.** For spiral and zerol gears the module is the transverse module at the large end, so the cone angles, the cone distance and the apex position are those of a straight bevel pair with the same teeth.
* **Mesh solving.** A spiral or zerol pair uses the `Bevel` mesh and the placement of the straight pair: both apexes coincide and the tooth phase at mid face is the straight bevel phase (the twist is zero there). The solver refuses a pair of different families and checks module, pressure angle, mean spiral angle, cutter radius and hands; each message gives the expected value, for example *both have Right hand teeth; the mate of a Right hand gear must be Left hand*. A hypoid pair uses the `Hypoid` mesh: both gears are placed so that their mean points coincide, each with the mean point on its +X axis at its mid-plane, and the pinion shaft is offset by the relation's offset. A fixed centre distance is not available.
* **Hands of a hypoid pair.** The tooth line through the mean point must be the same line for both gears. For a positive offset the gear is right hand and the pinion left hand; a negative offset is the mirror image and swaps the hands. Changing the offset in the editor sets the hands of both gears.
* **File format.** Gear types `SpiralBevel`, `ZerolBevel` and `Hypoid` with the optional fields `MeanSpiralAngleDeg`, `CutterRadius`, `Hand`, and for hypoid gears `OffsetMm` and `IsPinion`; the relation has the optional `OffsetMm`. Files saved earlier load unchanged.

## Validation

* The mean spiral angle is from 0 to 60 degrees, the cutter radius cannot be negative, and the transverse pressure angle must stay below 45 degrees.
* The cutter must reach the whole face: with the cutter radius `rc`, the trace covers the cone distances from `|R0 - rc|` to `R0 + rc`. An automatic cutter radius is the mean cone distance.
* A spiral twist between two sections of more than half a tooth pitch would fold the flanks and is refused.
* A hypoid pair needs a non-zero offset that is smaller than the gear mean pitch radius, one pinion and one gear, equal modules, pressure angles and cutter radii, and the hands that the side of the offset gives.

## Editor

*Bevel > Spiral*, *Bevel > Zerol* and *Bevel > Hypoid* start a train with a spiral bevel gear, a zerol bevel gear or a hypoid pinion. *Bevel > Add meshed bevel gear* adds the mate of a straight, spiral or zerol gear (a spiral or zerol mate gets the opposite hand and the same spiral angle and cutter radius). *Bevel > Add hypoid gear or pinion* adds the other gear of the pair of the selected hypoid gear on an offset shaft; the default offset is a quarter of the gear mean pitch radius. The property grid shows the spiral angle, hand, cutter radius and, for a hypoid gear, the offset and the values of the pair (pitch radii, spiral angles and cone angles).

## Limits

* The face-milled cutter generation model (cutter tilt, swivel, machine settings) and the Gleason or Klingelnberg basic settings are not modelled; the tooth surface is the swept Tredgold profile. The **transverse profile at the heel is used along the whole face** although the spiral angle changes, so the flanks are approximate.
* The hypoid cone angles come from the offset angle `sin(eps) = E / r2`, which is an approximation; the real design chooses the mean point from more conditions.
* No contact pattern, load capacity or strength calculation.
* **The Alibre export of these gears is a prototype.** The tooth space is cut with a loft through several sections that follow the arc (see [Alibre export of spiral, zerol and hypoid bevel gears](alibre-curved-bevel-export.md), which also compares the other methods of the Alibre API). It has not yet been checked inside Alibre; use the STL or OBJ export of the 3D model if the loft is not accepted.

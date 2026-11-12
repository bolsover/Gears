# ISO 606 roller chain sprockets and chains

The editor models sprockets and simplex roller chains of the **ISO 606 (DIN 8187) B-series**. A `SprocketComponent` is a sprocket with the ISO tooth form, a `RollerChain` joins two sprockets, and the tooth form, the chain data and the Alibre export are covered by tests. This page lists the sources, the formulae, how the shapes are built and what is not modelled. The belt equivalents are described in [Gear assemblies](gear-assemblies.md).

## Scope

* **Simplex chains only** (one strand, ISO 606 "-1"), **B-series only**. The ANSI B29.1 and the ISO 606 A-series chains are not modelled.
* Ten chains are in the table: 06B, 08B, 10B, 12B, 16B, 20B, 24B, 28B, 32B and 40B. The chain makers' tables give 04B, 05B and 48B to 72B too, but the pin, plate height and plate thickness of those were not found in a source that could be read consistently, so they are left out.
* The sprocket is a plain toothed wheel with an optional bore and hub. Tooth chamfers, tooth side relief and the tooth side radius of ISO 606 are not modelled.

## Sources

The dimensions and formulae are those of ISO 606:2004 as published by the chain and sprocket makers (SIT, DH Chain, Renold, Ametric, ZMC and HZPT tables), compared with each other. They were found by web search, because the repository has no chain data.

| Chain | Pitch `p` | Roller `d1` (max) | Inner width `b1` (min) | Pin | Plate height | Inner plate | Outer plate |
|---|---|---|---|---|---|---|---|
| 06B-1 | 9.525 | 6.35 | 5.72 | 3.28 | 8.26 | 1.30 | 1.04 |
| 08B-1 | 12.70 | 8.51 | 7.75 | 4.45 | 11.81 | 1.55 | 1.55 |
| 10B-1 | 15.875 | 10.16 | 9.65 | 5.08 | 14.73 | 1.55 | 1.55 |
| 12B-1 | 19.05 | 12.07 | 11.68 | 5.72 | 16.13 | 1.80 | 1.80 |
| 16B-1 | 25.40 | 15.88 | 17.02 | 8.28 | 21.08 | 4.11 | 3.10 |
| 20B-1 | 31.75 | 19.05 | 19.56 | 10.19 | 26.42 | 4.62 | 3.61 |
| 24B-1 | 38.10 | 25.40 | 25.40 | 14.63 | 33.40 | 6.10 | 5.08 |
| 28B-1 | 44.45 | 27.94 | 30.99 | 15.90 | 37.08 | 7.62 | 6.35 |
| 32B-1 | 50.80 | 29.21 | 30.99 | 17.81 | 42.29 | 7.11 | 6.35 |
| 40B-1 | 63.50 | 39.37 | 38.10 | 22.89 | 52.96 | 8.13 | 8.13 |

All values are in millimetres and are in `ChainProfile`; **Sprocket > ISO 606 chain data…** lists them. The pin, the plate height and the plate thicknesses are manufacturers' maximum values and only serve to draw the chain; the sprocket does not depend on them.

## Sprocket tooth form

With `z` teeth, chain pitch `p` and roller diameter `d1`:

| Quantity | Formula |
|---|---|
| Pitch diameter | `d = p / sin(180° / z)` |
| Root diameter | `d_f = d - d1` |
| Roller seating radius `r_i` | minimum `0.505 d1`, maximum `0.505 d1 + 0.069 d1^(1/3)` |
| Roller seating angle `alpha` | minimum `120° - 90° / z`, maximum `140° - 90° / z` |
| Tooth flank radius `r_e` | minimum `0.12 d1 (z + 2)`, maximum `0.008 d1 (z² + 180)` |
| Tip diameter `d_a` | minimum `d + p (1 - 1.6 / z) - d1`, maximum `d + 1.25 p - d1` |
| Tooth width | `0.93 b1` for `p` up to 12.7 mm, `0.95 b1` above (simplex) |

The standard groups the seating radius, the seating angle and the flank radius as "minimum dimensions" and "maximum dimensions". `SprocketToothForm.Maximum` (the default) uses the dimensions of the maximum group (the largest seating radius and flank radius, the smallest seating angle, the largest tip diameter), `SprocketToothForm.Minimum` those of the minimum group. The chain pitch polygon, not the pitch circle, is what the chain follows, so the pitch diameter is `p / sin(180° / z)` and not `z p / pi`. `GearComponent.Module` has no meaning for a sprocket; it is set to `p / pi` only to keep the base class consistent.

### How a gap is drawn

Gap 0 is centred on the +X axis, so the roller centre of that gap is on the pitch circle on the +X axis. Each side of a gap is made of two circular arcs, so the end points are exact (`SprocketComponent.HalfGap`, `SprocketComponent.FlankCentre`):

1. the **seating arc**, of radius `r_i` about the roller centre, from the bottom of the seat over half the seating angle;
2. the **flank arc**, of radius `r_e`, tangent to the seating arc at its end, with its centre on the line from the roller centre through that end on the roller side (the flank is concave towards the gap, like the working curve of the ANSI form).

The flank ends where it reaches the tip circle, or, when it meets the line half way to the next gap first, there: the tooth is then **pointed** and ends below the tip diameter (`HasPointedTeeth`). Otherwise the tip is an arc of the tip circle. The tests check, for every chain and both tooth forms from 6 to 100 teeth, that the roller (diameter `d1`) fits in the gap without cutting into a tooth.

### 3D solid and 2D outline

The outline has the same number of points for every tooth (8 segments on the seating arc, 6 on each flank), runs anticlockwise and is symmetric about the gap axis. The solid is built like the HTD pulley: a prism between the faces, with an ear-clipped cap, or, with a bore or a hub, one cell per tooth between the outline and a ring just inside the seat bottoms, plus the bore and hub cylinders. The bore must be smaller than the diameter at the bottom of the seats.

## Roller chain

A `RollerChain` joins two sprockets of the same chain series on different, parallel shafts. Like a mesh relation or a belt it places a shaft: the driven sprocket's shaft is put `CentreDistance` from the driver's shaft along the line of centres at `CentreLineAngleDeg`, with the mid-planes in line. Both sprockets turn the same way, the driven one at `z_driver / z_driven` of the driver speed. The placement and the phase use the same solver as the timing belt (`GearAssembly.SolveConnector`).

* **Link count:** the length of the chain in pitches `2 C cos(a) / p + (z_big (pi + 2a) + z_small (pi - 2a)) / (2 pi)`, with `sin(a) = (d_big - d_small) / (2 C)`, rounded to the nearest **even** number (at least 4), so that inner and outer links alternate and no offset link is needed. `ChainLength` is links times pitch.
* **Wrap:** the angle of the smaller sprocket that the chain wraps; the property grid flags less than 120°, below which chain makers advise against the drive. The centre distance must exceed half the sum of the tip diameters; the property grid shows the minimum, and 30 to 50 pitches is usual.
* **Roller positions** (`RollerChainLayout`, `ChainRegistration`): the path is the open chain round the two pitch circles, from the lower tangent point of the driven sprocket round it, along the upper run, round the driver and back along the lower run. Positions along it are counted in **pitches**, with a straight run of length `L` counting `L / p` and the wrap round a sprocket counting one pitch per tooth step. The rollers are equally spaced (very nearly one pitch apart). A roller is put in the gap of the driver that is nearest to the middle of its wrap, and the driven sprocket is clocked, by an offset of at most half a tooth step, so that the roller nearest to the middle of its wrap sits in one of its gaps. Because positions are counted in tooth steps the offset does not depend on the driver phase, and the driven sprocket follows the driver exactly at the tooth ratio. This is different from the belt, whose positions are lengths on the pitch circle: for a chain the pitch circle is not in proportion to the tooth count, so the belt method would let the phasing drift.
* **Shape** (`RollerChainMeshBuilder`): a roller and a pin at every roller position, and for every link two stadium-shaped plates (a strip of the plate height with half circles round the pins). Even links are **inner links**, with the plates against the rollers, odd links are **outer links**, with the plates outside them. Rollers, pins and plates are separate solids, and the 2D view draws the plate outlines and the roller circles.

## In the editor

The **Sprocket** menu starts a train with a sprocket of any chain (*New train with sprocket*), adds a sprocket to the selected shaft, adds a **chained sprocket** to the selected sprocket (a second sprocket of the same chain and tooth form on a new shaft, joined by a chain; the default centre distance is 30 pitches, or more when the sprockets would be closer than 10 mm), removes the selected chain, and shows the chain data. A sprocket can be given any number of teeth from 6; fewer than 9 is flagged. Select a chain to change its centre distance and the direction of the line of centres. Removing a chained sprocket removes its chain and leaves its shaft, as for a belted pulley. The tree, the 2D and the 3D view show chains, and chains can be selected together with other parts.

The assembly file stores a sprocket as type `Sprocket` (chain series, teeth, tooth form, bore, hub and the common fields) and the chains in a `Chains` list (identifier, name, driver, driven, centre distance, line of centres angle). Older files have none and open as before.

## Alibre export

* **Sprocket:** one sketch of one tooth cell on the `XY-Plane`: a wedge one pitch wide, centred on the gap, with two radial lines to the axis, two arcs of the tip circle (none for a pointed tooth), the two flank arcs and the roller seating arc (`SprocketSketchBuilder`). It is extruded mid-plane to the tooth width and patterned `z` times about the `Z-Axis`. A **hub** is a rectangle on the `ZX-Plane` that is revolved about the axis, and a **bore** is a circle on the `XY-Plane` that is cut through the whole body.
* **Chain:** no offset planes are needed. All steps are mid-plane extrusions of sketches on the `XY-Plane`, in an order that leaves the right material at every height (`ChainSketch`): a boss of all outer links over the full width, a cut of the same outlines over the width of the inner plates, a boss of all inner links over that width, a cut of the inner outlines over the roller width, then a boss of all rollers and one of all pins. The chain part has the driver sprocket's axis at its origin and lies on its mid-plane, so it is placed like the driver sprocket and the rollers are in the gaps of the driver at the phase it has in the scene.
* *Export selected to Alibre…* exports a sprocket or a chain as a part; *Export all to Alibre…* adds every sprocket, shaft and chain to the assembly.

The Alibre steps were checked against the Alibre API reference and by tests of the sketch plans (closed loops, arc radii, areas, counts and placement). They have not been run against a live Alibre session.

## Not modelled

* Duplex and triplex chains, the A-series (ANSI-equivalent) chains and ANSI B29.1 sprockets.
* Tooth chamfer, tooth side relief and side radius, and the tooth width of multi-strand sprockets.
* Chain wear, slack and tension, offset links, and chain strength and life (no power rating is calculated).
* A chain between sprockets that are not on parallel shafts, and an idler or tensioner sprocket.

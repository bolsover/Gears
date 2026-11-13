# Gears Technical Reference

<link-summary>Parameter calculation formulae and engineering requirements for gears and power transmission components.</link-summary>

This reference documents the gear and power transmission components that **Gears** can calculate, draw and export.
For every component it gives the **parameter calculation formulae**, a **worked example** and the **engineering
requirements** that the geometry must satisfy.

All dimensions are in millimetres and all angles in degrees unless a topic states otherwise.
The formulae follow the calculators in the `Gears` library, so the tables in these topics are the same
quantities that the gear editor and the assembly viewer show.

## Component families

| Family | Components | Topic |
|--------|------------|-------|
| Parallel shaft involute gears | Spur, internal, profile shifted gears | [Involute Spur Gears](Involute-Spur-Gears.md) |
| | Helical and herringbone gears, crossed helical pairs | [Helical Gears](Helical-Gears.md) |
| | Mesh geometry: centre distance, working pressure angle, backlash, contact ratio | [Gear Pair Calculations](Gear-Pair-Calculations.md) |
| | Straight and helical racks | [Racks](Racks.md) |
| Intersecting shaft gears | Straight bevel gears (standard and Gleason proportions) | [Straight Bevel Gears](Straight-Bevel-Gears.md) |
| | Spiral and zerol bevel gears | [Spiral and Zerol Bevel Gears](Spiral-and-Zerol-Bevel-Gears.md) |
| Non-intersecting shaft gears | Hypoid gears | [Hypoid Gears](Hypoid-Gears.md) |
| | Worm and worm wheel | [Worm Gears](Worm-Gears.md) |
| Other gears | Cycloidal wheel and pinion | [Cycloidal Gears](Cycloidal-Gears.md) |
| | Planetary (epicyclic) stage | [Planetary Gear Sets](Planetary-Gear-Sets.md) |
| Flexible drives | HTD pulleys and timing belts | [HTD Pulleys and Timing Belts](HTD-Pulleys-and-Timing-Belts.md) |
| | ISO 606 roller chain and sprockets | [Roller Chain Sprockets](Roller-Chain-Sprockets.md) |

The [Symbols](Symbols.md) topic lists every symbol used, and [Design Limits](Design-Limits.md) summarises the
numerical limits that are enforced when a gear or drive is validated.

## How to read the topics

Each topic uses the same structure:

1. **Formulae**: a table of *Item*, *Symbol* and *Formula*, with the values of a worked example.
2. **Engineering requirements**: the conditions that a sound design must meet. These are general engineering practice.
3. **Limits enforced by Gears**: the conditions that the software rejects with an error message.

<note>
The formulae give <b>geometry</b>. They do not replace a strength rating (for example ISO 6336, AGMA 2001, AGMA 2003,
ISO 13050 or ISO 606 load ratings). Always rate a gear or drive for load, life and lubrication before it is manufactured.
</note>

## Conventions

* Metric module system. The module `m` is in millimetres. For helical gears `m` is the **normal** module `m_n` unless
  a subscript `t` (transverse, also called radial) is shown.
* Pressure angle `α` is the normal pressure angle for helical gears; `α_t` is the transverse pressure angle.
* Subscript `1` is the pinion (driver, smaller gear) and `2` is the gear (driven). Subscript `w` is a working
  value (on the working pitch circle of a meshing pair).
* External gears have positive tooth counts. Internal gears use the same tooth count symbols; their formulae
  differ in sign and are shown explicitly.

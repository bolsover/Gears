# Planetary Gear Sets

<link-summary>Formulae and engineering requirements for simple planetary (epicyclic) gear stages.</link-summary>

A simple planetary stage has a **sun** gear at the centre, $N$ equally spaced **planet** gears that mesh with both the
sun and an internal **ring** gear, and a **carrier** that holds the planet axes. The three members (sun, ring and carrier)
share the same axis. Holding one member fixed and using another as the input gives a compact, coaxial drive
with a high ratio and equal sharing of the load between the planets.

In Gears all gears of the stage have the same module and pressure angle. The ring is an internal involute gear. A helical
stage (with the planets of the opposite hand to the sun, and the ring of the same hand as the planets) and a herringbone
stage are also possible.

## Geometry

| No | Item | Symbol | Formula | Example |
|----|------|--------|---------|---------|
| 1 | Module | $m$ | - | 2 |
| 2 | Sun teeth | $z_s$ | - | 24 |
| 3 | Planet teeth | $z_p$ | - | 18 |
| 4 | Number of planets | $N$ | - | 3 |
| 5 | Ring teeth | $z_r$ | $z_s + 2 z_p$ | 60 |
| 6 | Centre distance (sun to planet axis) | $a$ | $\dfrac{m (z_s + z_p)}{2}$ | 42.000 |
| 7 | Centre distance (helical) | $a$ | $\dfrac{m (z_s + z_p)}{2 \cos\beta}$ | |
| 8 | Planet tip diameter | $d_{ap}$ | $m z_p + 2 m$ | 40.000 |
| 9 | Distance between neighbouring planet centres | $l$ | $2 a \sin\dfrac{\pi}{N}$ | 72.746 |

For a helical stage the module is the normal module and the pitch diameters are $m z / \cos\beta$.

## Assembly conditions

| Condition | Formula | Example |
|-----------|---------|---------|
| Coaxial gears | $z_r = z_s + 2 z_p$ | $60 = 24 + 36$ |
| Equal planet spacing | $\dfrac{z_s + z_r}{N}$ must be a whole number | $\dfrac{84}{3} = 28$ |
| No planet collision | $2 a \sin\dfrac{\pi}{N} > d_{ap}$ | $72.746 > 40$ |

## Kinematics

The speeds of the three members follow the **Willis equation**, which is the condition that the planets roll without
slipping on the sun and on the ring:

$$
z_s\,\omega_s + z_r\,\omega_r - (z_s + z_r)\,\omega_c = 0
$$

The speed of a planet about its own axis relative to the carrier is

$$
\omega_{p/c} = -\frac{z_s}{z_p} (\omega_s - \omega_c)
$$

Ratios for the three standard arrangements ($z_s = 24$, $z_r = 60$):

| Fixed member | Input | Output | Ratio $\omega_{in} / \omega_{out}$ | Formula | Example |
|--------------|-------|--------|------------------------------------|---------|---------|
| Ring | Sun | Carrier | reduction, same direction | $1 + \dfrac{z_r}{z_s}$ | 3.5 |
| Sun | Ring | Carrier | reduction, same direction | $1 + \dfrac{z_s}{z_r}$ | 1.4 |
| Carrier | Sun | Ring | reduction, reversed direction | $-\dfrac{z_r}{z_s}$ | -2.5 |

The ratio is the input speed divided by the output speed, signed so that a negative sign means a reversal of
rotation. If the output is the fixed member the ratio is not defined.

## Engineering requirements

* **Assembly condition.** $(z_s + z_r) / N$ must be a whole number, otherwise the planets cannot be placed at equal
  angles and mesh with both gears at once. Choose $N$ and tooth numbers accordingly.
* **Planet clearance.** Neighbouring planets must not touch. Check the distance between planet centres against the planet
  tip diameter, including the space needed for the carrier.
* **Load sharing.** Equal sharing between planets needs accurate positions of the planet pins and a floating sun or
  ring. Typical practice allows a sharing factor of 1.1 to 1.2 for three planets in a rating calculation.
* **Ratio range.** A single stage with the ring fixed gives a ratio of about 3 to 10; for higher ratios, put stages
  in series.
* **Ring rim.** The rim of the ring gear must be thick enough to resist deformation; see
  [Involute Spur Gears](Involute-Spur-Gears.md).
* **Contact ratio and interference.** Each sun-planet and planet-ring mesh must meet the requirements of
  [Gear Pair Calculations](Gear-Pair-Calculations.md). The planet-ring mesh is internal: check the tooth difference
  $z_r - z_p$ and the tip clearance.
* **Helical stages.** Planets of a helical stage have the opposite hand to the sun, and the ring has the hand of the
  planets. A helical stage produces axial thrust on all members; a herringbone stage cancels it.
* **Bearings and carrier.** The carrier must be stiff and the planets must be able to rotate freely in their bearings.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Sun teeth, planet teeth | at least 6 |
| Number of planets | at least 1 |
| Module, face width | greater than 0 |
| Pressure angle | greater than 0° and less than 90° |
| Helix angle | from 0° to below 90° |
| Helix hand | R or L |
| Equal spacing | $(z_s + z_r) / N$ must be a whole number |
| Planet collision | $2 a \sin(\pi / N) >$ planet tip diameter for $N > 1$ |
| Input member | cannot be the fixed member |

<seealso>
    <category ref="gears">
        <a href="Involute-Spur-Gears.md">Involute Spur Gears</a>
        <a href="Helical-Gears.md">Helical Gears</a>
        <a href="Gear-Pair-Calculations.md">Gear Pair Calculations</a>
    </category>
</seealso>

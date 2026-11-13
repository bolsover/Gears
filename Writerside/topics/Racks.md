# Racks

<link-summary>Formulae and engineering requirements for straight and helical racks meshing with an involute pinion.</link-summary>

A rack is a gear of infinite pitch diameter: its pitch circle becomes a straight **datum line** and its involute
flanks become straight lines. It converts rotation of the pinion into linear motion. A rack has no pitch diameter;
the speed of the rack is the pitch line velocity of the pinion.

## Straight rack

| No | Item | Symbol | Formula | Example ($m = 2$, $z = 12$) |
|----|------|--------|---------|------------------------------|
| 1 | Module | $m$ | - | 2 |
| 2 | Pressure angle | $\alpha$ | - | 20° |
| 3 | Number of teeth | $z$ | - | 12 |
| 4 | Pitch | $p$ | $\pi m$ | 6.283 |
| 5 | Length of toothed part | $l$ | $z p$ | 75.398 |
| 6 | Addendum | $h_a$ | $m$ | 2.000 |
| 7 | Dedendum | $h_f$ | $m + c$, with $c = 0.25\,m$ | 2.500 |
| 8 | Whole depth | $h$ | $h_a + h_f$ | 4.500 |
| 9 | Tooth thickness on the datum line | $s$ | $\dfrac{p}{2} - \dfrac{j_t}{2}$ | 3.142 (no backlash) |
| 10 | Half width of tooth tip | | $\dfrac{s}{2} - h_a \tan\alpha$ | 0.843 |
| 11 | Half width of tooth root | | $\min\left(\dfrac{s}{2} + h_f \tan\alpha,\ \dfrac{p}{2}\right)$ | 2.481 |

The flanks are straight lines inclined at the pressure angle $\alpha$ to the line perpendicular to the datum line.

## Rack and pinion

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Pinion pitch diameter | $d$ | $m z_p$ |
| 2 | Pitch line velocity = rack speed | $v$ | $\dfrac{\pi d n}{60\,000}$ in m/s with $n$ in rpm and $d$ in mm |
| 3 | Rack travel per pinion revolution | $s_r$ | $\pi m z_p$ |
| 4 | Centre height (standard, $x = 0$) | $H$ | $\dfrac{d}{2}$, the distance from the pinion axis to the datum line |
| 5 | Centre height (shifted pinion) | $H$ | $\dfrac{d}{2} + x m$ |
| 6 | Contact ratio | $\varepsilon_\alpha$ | $\dfrac{\sqrt{r_a^2 - r_b^2} - r \sin\alpha + \dfrac{h_a}{\sin\alpha}}{\pi m \cos\alpha}$ with $r = d / 2$ |

In the contact ratio formula $r_a$ is the pinion tip radius and $r_b$ the pinion base radius, and the formula holds
for a standard rack ($h_a = m$). The first two terms of the numerator are the length of the path of contact on the
pinion side of the pitch point, the last term the length on the rack side.

## Helical rack

A helical rack has a helix angle $\beta$ and a hand. The module and pressure angle are normal values; in the end
face (transverse) section the rack has

$$
p_t = \frac{\pi m_n}{\cos\beta}, \qquad
\alpha_t = \arctan\frac{\tan\alpha_n}{\cos\beta}
$$

Each tooth section is sheared along the rack length by $\tan\beta$ per unit of face width. The rack meshes with a
helical pinion of the **same helix angle and opposite hand**. A herringbone rack is a helical rack mirrored about its
middle plane.

## Engineering requirements

* **Pinion undercut.** Use at least 17 teeth on a 20° pinion, or give the pinion a positive profile shift
  $x \ge (17 - z_p) / 17$.
* **Backlash.** The rack teeth are thinned by half the backlash; the pinion by the other half. Avoid negative
  backlash; allow for thermal growth on long racks.
* **Pitch accuracy.** On a long rack the cumulative pitch error accumulates. Specify a maximum cumulative pitch
  error for the whole length.
* **Support.** The rack must be backed by a rigid guide, and the pinion shaft must be held at the correct centre
  height $H$; a small error changes backlash directly.
* **Helical racks.** The pinion shaft must be parallel to the rack face. Provide for the axial thrust
  $F_a = F_t \tan\beta$ in the pinion bearings.
* **Joining racks.** Hold the pitch across a joint to the pitch tolerance of a single rack and use alignment pins
  or a joining tooth form so that the teeth line up.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Number of teeth | at least 1 |
| Module, face width, body height | greater than 0 |
| Pressure angle | greater than 0° and less than 60° |
| Backlash | at least 0 and less than half the pitch |
| Helix angle | from 0° to 60° |
| Tooth tip | must keep a flat top: $\dfrac{s}{2} > h_a \tan\alpha_t$ |

<seealso>
    <category ref="gears">
        <a href="Involute-Spur-Gears.md">Involute Spur Gears</a>
        <a href="Helical-Gears.md">Helical Gears</a>
    </category>
</seealso>

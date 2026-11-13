# Helical Gears

<link-summary>Formulae and engineering requirements for helical, herringbone and crossed helical gears.</link-summary>

A helical gear has teeth cut at a helix angle $\beta$ to the axis. The module $m_n$ and pressure angle $\alpha_n$
are given in the **normal** plane (perpendicular to the tooth). The profile in the **transverse** plane (perpendicular
to the axis) has a larger module and pressure angle. Helical teeth engage gradually, so the drive is smoother and
quieter than a spur drive, but the helix creates an axial thrust.

A **herringbone** (double helical) gear is a helical gear mirrored about its middle plane. The two halves have
opposite hands, so the axial thrusts cancel.

## Parallel helical gear

| No | Item | Symbol | Formula | Pinion | Gear |
|----|------|--------|---------|--------|------|
| 1 | Normal module | $m_n$ | - | 2 | 2 |
| 2 | Normal pressure angle | $\alpha_n$ | - | 20° | 20° |
| 3 | Helix angle | $\beta$ | - | 15° | 15° |
| 4 | Number of teeth | $z$ | - | 20 | 40 |
| 5 | Transverse module | $m_t$ | $\dfrac{m_n}{\cos\beta}$ | 2.0706 | 2.0706 |
| 6 | Transverse pressure angle | $\alpha_t$ | $\arctan\dfrac{\tan\alpha_n}{\cos\beta}$ | 20.647° | 20.647° |
| 7 | Centre distance | $a$ | $\dfrac{m_t (z_1 + z_2)}{2}$ | 62.117 | |
| 8 | Pitch diameter | $d$ | $m_t z$ | 41.411 | 82.822 |
| 9 | Base diameter | $d_b$ | $d \cos\alpha_t$ | 38.751 | 77.503 |
| 10 | Addendum | $h_a$ | $m_n (1 + x)$ | 2.000 | 2.000 |
| 11 | Dedendum | $h_f$ | $m_n (1 - x) + c$ | 2.500 | 2.500 |
| 12 | Outside diameter | $d_a$ | $d + 2 h_a$ | 45.411 | 86.822 |
| 13 | Root diameter | $d_f$ | $d - 2 h_f$ | 36.411 | 77.822 |
| 14 | Transverse circular pitch | $p_t$ | $\pi m_t$ | 6.505 | 6.505 |
| 15 | Base pitch | $p_b$ | $\pi m_t \cos\alpha_t$ | 6.087 | 6.087 |
| 16 | Axial pitch | $p_x$ | $\dfrac{\pi m_n}{\sin\beta}$ | 24.276 | 24.276 |
| 17 | Lead | $p_z$ | $z \cdot p_x$ | 485.527 | 971.055 |
| 18 | Transverse contact ratio ($b = 30$) | $\varepsilon_\alpha$ | see [Gear Pair Calculations](Gear-Pair-Calculations.md) | 1.561 | |
| 19 | Axial contact ratio ($b = 30$) | $\varepsilon_\beta$ | $\dfrac{b \sin\beta}{\pi m_n}$ | 1.236 | |
| 20 | Total contact ratio | $\varepsilon_\gamma$ | $\varepsilon_\alpha + \varepsilon_\beta$ | 2.797 | |

Profile shift, clearance and tooth thickness use the same formulae as the spur gear, with the normal module:

$$
s_t = \frac{m_n \left(\dfrac{\pi}{2} + 2 x \tan\alpha_n\right) - j_t}{\cos\beta}
$$

The involute function and the working pressure angle use the **transverse** pressure angle,
$\mathrm{inv}\,\alpha_t = \tan\alpha_t - \alpha_t$.

### Hand of helix

| Mesh | Hands |
|------|-------|
| External parallel pair | opposite hands, equal helix angles ($\beta_1 = \beta_2$) |
| Internal pair | same hand, equal helix angles |
| Rack and helical pinion | opposite hands, equal helix angles |

A right hand helix rises to the right when the gear is viewed from the side with the axis horizontal.

### Axial thrust

For a single helical gear transmitting a tangential force $F_t$, the axial force is

$$
F_a = F_t \tan\beta
$$

and the radial force is $F_r = F_t \tan\alpha_n / \cos\beta$. A herringbone gear has no net axial force.

## Crossed helical gears

Two helical gears can mesh on shafts that are neither parallel nor intersecting. The gears touch at a **point**
and the teeth slide along their length, so the load capacity is low. Each gear has its own transverse module and
pitch diameter:

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Transverse module of each gear | $m_{t1}$, $m_{t2}$ | $\dfrac{m_n}{\cos\beta_1}$, $\dfrac{m_n}{\cos\beta_2}$ |
| 2 | Pitch diameter of each gear | $d_1$, $d_2$ | $m_{t1} z_1$, $m_{t2} z_2$ |
| 3 | Centre distance | $a$ | $\dfrac{d_1 + d_2}{2}$ |
| 4 | Shaft angle, same hands | $\Sigma$ | $\beta_1 + \beta_2$ |
| 5 | Shaft angle, opposite hands | $\Sigma$ | $\lvert \beta_1 - \beta_2 \rvert$ |
| 6 | Gear ratio | $i$ | $\dfrac{z_2}{z_1}$ |

<note>
The speed ratio is determined by the <b>tooth numbers</b> $z_2 / z_1$, not by the pitch diameters, because the
helix angles may differ. A shaft angle of $0°$ with opposite hands and equal helix angles is the ordinary parallel
helical mesh.
</note>

## Engineering requirements

* **Helix angle.** Typical helix angles are 8° to 20° for single helical gears. Angles up to 45° are used for
  crossed drives and high-speed light-load drives. Large angles give high axial thrust and need thrust bearings.
* **Herringbone angle.** A herringbone gear usually uses 25° to 45° because the thrust is cancelled. A relief groove
  between the two halves is normally required for the cutter to run out.
* **Face width.** For smooth running the face width should give an overlap ratio $\varepsilon_\beta \ge 1$,
  that is $b \ge \pi m_n / \sin\beta$ (the axial pitch).
* **Undercut limit.** The minimum number of teeth without undercut is lower than for a spur gear:
  $z_{min} = \dfrac{2 \cos\beta}{\sin^2\alpha_t}$.
* **Equal helix angles.** For a parallel mesh both helix angles must be equal in magnitude and opposite in hand
  (same hand for an internal gear). A mismatch gives edge contact and noise.
* **Shaft angle.** For a crossed pair the shaft angle must equal $\beta_1 \pm \beta_2$ (see the table above).
  Otherwise the teeth cannot mesh.
* **Crossed gears.** Crossed helical gears are limited to light loads and low speeds because of point contact and
  sliding wear. Use equal materials with good lubrication, and consider a worm drive for higher ratios.
* **Bearings.** Select thrust bearings for $F_a = F_t \tan\beta$ at the pinion and the gear.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Helix angle of a helical gear | greater than or equal to 0° and less than 90° |
| Helix angle of a crossed helical pair | 0° to below 90° for both gears |
| Profile shift of a crossed pair | exactly 0 for both gears |
| Internal gears in a crossed pair | not allowed |
| Teeth and module of a crossed pair | positive |
| Shaft angle of a crossed pair | must equal the value from the helix angles and hands to within 0.01° |
| Planetary stage helix angle | from 0° to below 90° |

<seealso>
    <category ref="gears">
        <a href="Involute-Spur-Gears.md">Involute Spur Gears</a>
        <a href="Gear-Pair-Calculations.md">Gear Pair Calculations</a>
        <a href="Racks.md">Racks</a>
    </category>
</seealso>

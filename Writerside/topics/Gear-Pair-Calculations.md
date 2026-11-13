# Gear Pair Calculations

<link-summary>Centre distance, working pressure angle, backlash and contact ratio of meshing involute gears.</link-summary>

When two involute gears mesh, the pitch circles of the individual gears are replaced by the **working pitch circles**.
Their size depends on the centre distance at which the gears run. At the standard centre distance, and with a total
profile shift of zero, the working circles coincide with the reference circles.

The formulae hold for external pairs (both gears external) and for internal pairs (one external gear running inside
an internal gear). For helical gears use the transverse values $m_t$, $\alpha_t$ in place of $m$, $\alpha$; see
[Helical Gears](Helical-Gears.md).

## Centre distance and working pressure angle

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Sum of teeth | $z_\Sigma$ | external pair: $z_1 + z_2$ <br/> internal pair: $z_{int} - z_{ext}$ |
| 2 | Sum of profile shifts | $x_\Sigma$ | external pair: $x_1 + x_2$ <br/> internal pair: $x_{int} - x_{ext}$ |
| 3 | Standard centre distance | $a$ | $\dfrac{m (z_1 + z_2)}{2}$ (external) <br/> $\dfrac{m (z_{int} - z_{ext})}{2}$ (internal) |
| 4 | Working involute function | $\mathrm{inv}\,\alpha_w$ | $2 \tan\alpha \dfrac{x_\Sigma}{z_\Sigma} + \mathrm{inv}\,\alpha$ |
| 5 | Working pressure angle | $\alpha_w$ | solve $\tan\alpha_w - \alpha_w = \mathrm{inv}\,\alpha_w$ by Newton-Raphson iteration |
| 6 | Working centre distance | $a_w$ | $a \dfrac{\cos\alpha}{\cos\alpha_w}$ |
| 7 | Centre distance increment factor | $y$ | $\dfrac{a_w - a}{m}$ |
| 8 | Working pitch diameter | $d_{w}$ | $d \dfrac{\cos\alpha}{\cos\alpha_w}$ |
| 9 | Gear ratio | $i$ | $\dfrac{z_2}{z_1}$ |

If the designer fixes the centre distance $a_w$, the working pressure angle follows directly from
$\cos\alpha_w = a \cos\alpha / a_w$ and the required total profile shift is found from the same involute equation
solved the other way round.

### Worked example

The shifted pair of [Involute Spur Gears](Involute-Spur-Gears.md): $m = 2.5$, $\alpha = 20°$, $z_1 = 18$,
$z_2 = 25$, $x_1 = 0.4$, $x_2 = 0.2$.

| No | Item | Symbol | Value |
|----|------|--------|-------|
| 1 | Standard centre distance | $a$ | 53.750 |
| 2 | Working involute function | $\mathrm{inv}\,\alpha_w$ | 0.025062 |
| 3 | Working pressure angle | $\alpha_w$ | 23.621° |
| 4 | Working centre distance | $a_w$ | 55.127 |
| 5 | Centre distance increment factor | $y$ | 0.551 |
| 6 | Working pitch diameters | $d_{w1}$, $d_{w2}$ | 46.153, 64.102 |
| 7 | Radial contact ratio | $\varepsilon_\alpha$ | 1.446 |

Note that $y < x_\Sigma$ ($0.551 < 0.6$). The difference $x_\Sigma - y$ is the tip shortening that the usual
gear standards apply to keep the bottom clearance when the centre distance is increased. Gears uses the full
addendum $m (1 + x)$, so check the tip clearance yourself when you run a shifted pair at its working centre distance.

## Backlash

Circular backlash is the amount by which the tooth space of one gear exceeds the tooth thickness of the mate, measured
along the working pitch circle. In Gears each gear carries its own backlash allowance $j_{t1}$, $j_{t2}$, which thins
the tooth.

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Tooth thickness with allowance | $s$ | $m \left(\dfrac{\pi}{2} + 2 x \tan\alpha\right) - j_t$ |
| 2 | Total circular backlash | $j_t$ | $j_{t1} + j_{t2}$ |
| 3 | Normal backlash (spur) | $j_n$ | $j_t \cos\alpha_w$ |
| 4 | Normal backlash (helical) | $j_n$ | $j_t \cos\alpha_{wt} \cos\beta$ |
| 5 | Working circular backlash (centre distance fixed) | $j_{tw}$ | $p_w - s_{w1} - s_{w2}$ |

The tooth thickness on the working pitch circle is found from the thickness at the reference circle with the
involute function:

$$
s_w = d_w \left( \frac{s}{d} + \mathrm{inv}\,\alpha - \mathrm{inv}\,\alpha_w \right)
$$

For an internal pair the working backlash is the space width of the internal gear minus the tooth thickness of the
external gear, both carried to the working circle in the same way.

## Contact ratio

The contact ratio is the average number of tooth pairs in contact. The transverse (radial) contact ratio of an
external pair is

$$
\varepsilon_\alpha = \frac{\sqrt{\left(\frac{d_{a1}}{2}\right)^2 - \left(\frac{d_{b1}}{2}\right)^2}
+ \sqrt{\left(\frac{d_{a2}}{2}\right)^2 - \left(\frac{d_{b2}}{2}\right)^2} - a_w \sin\alpha_w}{\pi m \cos\alpha}
$$

For an internal pair the second root and the centre distance term change sign:

$$
\varepsilon_\alpha = \frac{\sqrt{\left(\frac{d_{a1}}{2}\right)^2 - \left(\frac{d_{b1}}{2}\right)^2}
- \sqrt{\left(\frac{d_{a2}}{2}\right)^2 - \left(\frac{d_{b2}}{2}\right)^2} + a_w \sin\alpha_w}{\pi m \cos\alpha}
$$

For helical gears replace $m$ by $m_t$, $\alpha$ by $\alpha_t$ and $\alpha_w$ by $\alpha_{wt}$, and add the axial contact
ratio:

$$
\varepsilon_\beta = \frac{b \sin\beta}{\pi m_n}, \qquad \varepsilon_\gamma = \varepsilon_\alpha + \varepsilon_\beta
$$

where $b$ is the smaller of the two face widths. The standard 18/25 pair of
[Involute Spur Gears](Involute-Spur-Gears.md) has $\varepsilon_\alpha = 1.571$.

## Engineering requirements

* **Contact ratio.** Keep the transverse contact ratio $\varepsilon_\alpha$ at 1.2 or more, and preferably above 1.4.
  A ratio below 1.0 means the next pair of teeth is not yet in contact when the leading pair leaves, so the drive
  runs rough and with shock.
* **Overlap ratio.** For helical gears a total contact ratio $\varepsilon_\gamma \ge 2$ gives smooth, quiet running.
  An integer $\varepsilon_\beta$ (1, 2, ...) gives the least variation in mesh stiffness.
* **Base circles.** The centre distance must exceed half the sum of the base diameters,
  $a_w > (d_{b1} + d_{b2}) / 2$; otherwise the base circles overlap and no involute action is possible.
* **Bottom clearance.** At the working centre distance, the clearance between the tip of one gear and the root of the
  other must remain positive: $a_w - \dfrac{d_{a1} + d_{f2}}{2} > 0$ and $a_w - \dfrac{d_{a2} + d_{f1}}{2} > 0$.
* **Backlash.** Backlash is needed for thermal expansion, lubricant film and manufacturing error. Avoid negative
  working backlash; the tooth thickness allowance must cover the combined tolerance of centre distance and tooth
  thickness.
* **Equal base pitch.** Both gears must have the same base pitch $p_b = \pi m \cos\alpha$, which in practice means
  the same module and pressure angle.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Fixed centre distance | positive and finite |
| Fixed centre distance | not less than $a \cos\alpha$, because the base circles would overlap |
| Contact ratio of a crossed helical pair | not calculated (point contact), stays 0 |

<seealso>
    <category ref="gears">
        <a href="Involute-Spur-Gears.md">Involute Spur Gears</a>
        <a href="Helical-Gears.md">Helical Gears</a>
    </category>
</seealso>

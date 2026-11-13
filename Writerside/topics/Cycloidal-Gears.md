# Cycloidal Gears

<link-summary>Formulae and engineering requirements for cycloidal wheels and leaved pinions, as used in clocks and instruments.</link-summary>

Cycloidal gears are used in clocks, watches and instruments. Unlike involute gears, the tooth shape of a cycloidal gear
depends on **both** gears of the pair, so a cycloidal gear is always designed as a **wheel and a pinion**. The
tooth sizes in Gears follow the tables for clock wheels and leaves of the cycloidal gear tables of
[csparks.com](http://www.csparks.com/watchmaking/CycloidalGears/index.jhtml), as in the Alibre cycloidal gear add-on.

* The **wheel** is the larger gear. It has teeth with a rounded tip made of two circular arcs and radial flanks, half a
  circular pitch wide at the pitch circle.
* The **pinion** is the smaller gear, a leaved pinion. Its teeth (leaves) are narrower and the profile depends on the
  number of leaves.

## Common formulae

| No | Item | Symbol | Formula | Wheel 30 | Pinion 8 |
|----|------|--------|---------|----------|----------|
| 1 | Module | $m$ | - | 1 | 1 |
| 2 | Number of teeth | $z$ | - | 30 | 8 |
| 3 | Pitch diameter | $d$ | $m z$ | 30.000 | 8.000 |
| 4 | Circular pitch | $p$ | $\pi m$ | 3.142 | 3.142 |
| 5 | Gear ratio of the pair | $i$ | $\dfrac{z_w}{z_p}$ | 3.75 | |
| 6 | Outside diameter | $d_a$ | $d + 2 h_a$ | 32.848 | 9.340 |
| 7 | Root diameter | $d_f$ | $d - 2 h_f$ | 26.858 | 4.352 |

## Wheel

The wheel addendum follows from an **addendum factor** $k$ that depends on the tooth numbers of the wheel and of the pinion
it works with, so that the tip of a wheel tooth meets the flank of the pinion.

| No | Item | Symbol | Formula | Example (30/8) |
|----|------|--------|---------|----------------|
| 1 | Ratio parameter | $r_2$ | $\dfrac{2 z_w}{z_p}$ | 7.5 |
| 2 | Addendum factor | $k$ | see below | 1.49919 |
| 3 | Addendum | $h_a$ | $0.95\,m\,k$ | 1.424 |
| 4 | Addendum arc radius | $r_a$ | $1.4\,m\,k$ | 2.099 |
| 5 | Dedendum | $h_f$ | $\dfrac{\pi m}{2}$ | 1.571 |
| 6 | Half tooth angle at the pitch circle | | $\dfrac{\pi}{2 z_w}$ | 0.0524 rad |

The addendum factor is the result of an iteration. Start with $t_1 = 0$, set $t_0 = t_1$ and repeat until
$\lvert t_1 - t_0 \rvert < 10^{-6}$:

$$
b = \operatorname{atan2}\left(\sin t_0,\ 1 + r_2 - \cos t_0\right), \qquad
t_1 = \frac{\pi}{z_p} + r_2\, b
$$

then, with $K = 1 + r_2$:

$$
D = \sqrt{1 + K^2 - 2 K \cos t_1}, \qquad k = \frac{z_p}{4}\,\left(1 - K + D\right)
$$

## Pinion (leaves)

The pinion addendum and the radius of its tooth tip arcs depend on the number of leaves:

| Leaves | Tooth form | Addendum $h_a$ | Tip arc radius $r_a$ |
|--------|------------|----------------|----------------------|
| 6 to 7 | high ogival | $0.855\,m$ | $1.05\,m$ |
| 8 to 9 | medium ogival | $0.67\,m$ | $0.7\,m$ |
| 10 | round top, small tooth | $0.525\,m$ | $0.525\,m$ |
| 11 or more | round top, wider tooth | $0.625\,m$ | $0.625\,m$ |

| No | Item | Symbol | Formula | Example (8 leaves, mate 30) |
|----|------|--------|---------|------------------------------|
| 1 | Dedendum | $h_f$ | $m (0.95\,k + 0.4)$, with $k$ for the mating wheel | 1.824 |
| 2 | Dedendum with a chosen clearance | $h_f$ | $0.95\,m\,k + s$, where $s$ is the custom clearance | |
| 3 | Half tooth angle at the pitch circle | | $\dfrac{1.05}{z_p}$ for up to 10 leaves, $\dfrac{1.25}{z_p}$ above | 0.1313 rad |

## Engineering requirements

* **Wheel and pinion pair.** A cycloidal gear works only with the mate it was designed for. Changing the tooth number
  of either gear changes the tooth profile of both. Use the same module for both gears.
* **Pinion size.** Use at least six leaves. A pinion with few leaves needs a tall, narrow (ogival) tooth, which is
  weak; more leaves give a sturdier, smoother-running pinion.
* **Driving direction.** Cycloidal wheels and pinions are designed as a pair, normally with the **wheel driving the
  pinion** as in a clock going from the barrel towards the escapement. A pinion driving a wheel gives less favourable
  tooth action and a lower efficiency.
* **Centre distance.** The profile is only correct at the standard centre distance $a = m (z_w + z_p) / 2$. Hold this
  distance to a tight tolerance; the tooth shape does not allow the centre distance adjustment of involute gears.
* **Constant ratio.** Cycloidal gears keep a constant angular velocity ratio only when correctly spaced, and so need
  accurate pitch. Use the profile for light, low-speed drives and clocks, not for heavy power transmission.
* **Tooth width.** The teeth must not touch at the pitch circle. The wheel tooth is half a pitch wide there, and the
  pinion tooth is narrower, which leaves the space between the teeth.
* **Root diameter.** Keep the root diameter positive and larger than the centre hole.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Number of teeth | at least 6 |
| Mate teeth, when set | at least 6 |
| Module, face width | greater than 0 |
| Custom clearance, when used | not negative |
| Centre hole | not negative and smaller than the root diameter |
| Root diameter | greater than 0 |
| Teeth at the pitch circle | must not touch ($2 \cdot$ half tooth angle $< 2\pi / z$) |

<seealso>
    <category ref="gears">
        <a href="Involute-Spur-Gears.md">Involute Spur Gears</a>
        <a href="Planetary-Gear-Sets.md">Planetary Gear Sets</a>
    </category>
</seealso>

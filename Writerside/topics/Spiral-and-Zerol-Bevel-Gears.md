# Spiral and Zerol Bevel Gears

<link-summary>Formulae and engineering requirements for spiral and zerol bevel gears.</link-summary>

A **spiral bevel gear** is a bevel gear whose teeth are curved along a circular arc, the trace of a face-mill cutter.
The teeth engage gradually, so spiral bevel gears run more smoothly and quietly and carry more load than straight
bevel gears of the same size.

A **zerol bevel gear** has curved teeth with a mean spiral angle of **zero**. The tooth line is a cone element at the
middle of the face, so the pitch cones, cone angles and thrust are those of a straight bevel pair. At the heel
and the toe the spiral angle is small but not zero.

In Gears the pitch cones, cone angles, cone distance and tooth proportions of a spiral or zerol gear are those of
a straight bevel pair with the same teeth; see [Straight Bevel Gears](Straight-Bevel-Gears.md). The module is the
transverse module at the large end.

## Spiral geometry

| No | Item | Symbol | Formula | Example |
|----|------|--------|---------|---------|
| 1 | Normal pressure angle | $\alpha_n$ | - | 20° |
| 2 | Mean spiral angle | $\beta_m$ | - (0° for zerol) | 35° |
| 3 | Transverse pressure angle | $\alpha_t$ | $\arctan\dfrac{\tan\alpha_n}{\cos\beta_m}$ | 23.957° |
| 4 | Outer cone distance | $R_e$ | $\dfrac{d}{2 \sin\delta}$ | 54.083 |
| 5 | Face width | $b$ | - | 15.000 |
| 6 | Mean cone distance | $R_m$ | $R_e - \dfrac{b}{2}$ | 46.583 |
| 7 | Cutter radius | $r_c$ | automatic: $R_m$ | 46.583 |
| 8 | Distance of apex to cutter centre | $R_c$ | $\sqrt{R_m^2 + r_c^2 - 2 R_m r_c \sin\beta_m}$ | 43.020 |
| 9 | Spiral angle at cone distance $R$ | $\beta(R)$ | $\arcsin\dfrac{R^2 + r_c^2 - R_c^2}{2 R r_c}$ | see below |

The spiral angle changes along the face. For the example above (outer cone distance 54.083 mm, face width 15 mm):

| Position | Cone distance | Spiral angle |
|----------|---------------|--------------|
| Toe (small end) | 39.083 | 30.477° |
| Mean point | 46.583 | 35.000° |
| Heel (large end) | 54.083 | 40.081° |

A zerol gear with cutter radius $r_c = R_m$ has $\beta_m = 0$. The formula then gives a small positive spiral
angle at the heel and a small negative one at the toe.

### Hand of spiral

| Gear | Hand |
|------|------|
| Right hand | Looking from the heel towards the apex along the axis, the tooth line advances counter-clockwise from heel to toe, like a right hand screw thread |
| Mate | Always the **opposite** hand, with the same mean spiral angle, cutter radius, module and pressure angle |

### Contact ratio

The total contact ratio of a spiral bevel pair is the sum of the transverse contact ratio of the virtual spur gears and
the **face contact ratio** (overlap) that results from the spiral:

$$
\varepsilon_\gamma = \varepsilon_\alpha + \varepsilon_\beta, \qquad
\varepsilon_\beta \approx \frac{b \tan\beta_m}{p_{mt}}
$$

where $p_{mt}$ is the circular pitch at the mean point.

## Engineering requirements

* **Spiral angle.** The usual mean spiral angle is 35°. Smaller angles (down to zerol) reduce axial thrust but also the
  overlap; larger angles increase thrust and overlap.
* **Face contact ratio.** For smooth running the face contact ratio should be at least 1.25, and the total contact
  ratio about 2 or more.
* **Hand.** The hand and the rotation direction decide whether the axial thrust on the pinion is directed towards or
  away from the gear. Choose the hand and rotation so that the thrust tends to move the pinion **out of mesh**; thrust
  that pulls the pinion into mesh can close the backlash through bearing clearance and jam the pair.
* **Cutter radius.** The cutter radius must be large enough for the tooth trace to run across the whole face: a
  small cutter on a wide face cannot reach both heel and toe. The standard cutter radius equals the mean cone distance.
* **Transverse pressure angle.** The transverse pressure angle grows with the spiral angle; keep it well below 45°
  to avoid pointed teeth.
* **Face width.** As for straight bevel gears: $b \le R_e / 3$ and $\le 10\,m$.
* **Mounting.** The cone apexes must coincide. Spiral bevel gears are more sensitive to mounting error than straight
  gears because the lengthwise crowning is meant to localise the contact.
* **Pairs.** Always manufacture and run spiral and zerol bevel gears as a **matched set**.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Mean spiral angle | 0° to 60° |
| Spiral angle at the middle of the face | not more than 60° |
| Cutter radius | not negative; automatic ($R_m$) when 0 |
| Transverse pressure angle | below 45° |
| Tooth trace | must cover the cone distances from the toe to the heel |
| All limits of | [Straight Bevel Gears](Straight-Bevel-Gears.md) |

<seealso>
    <category ref="gears">
        <a href="Straight-Bevel-Gears.md">Straight Bevel Gears</a>
        <a href="Hypoid-Gears.md">Hypoid Gears</a>
    </category>
</seealso>

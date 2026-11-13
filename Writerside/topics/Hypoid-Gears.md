# Hypoid Gears

<link-summary>Formulae and engineering requirements for hypoid gears: spiral bevel gears on offset shafts.</link-summary>

A **hypoid gear** pair is a spiral bevel pair whose shafts do **not intersect**: the pinion axis is displaced from the
gear axis by an **offset** $E$. The offset lets the pinion be larger and stronger than a spiral bevel pinion of the
same ratio, runs quietly, and allows a shaft to pass under the gear, as in a vehicle rear axle. Hypoid gears
slide along the tooth more than spiral bevel gears and need a special hypoid lubricant.

Gears solves the pair at the **mean point**. The module $m$ is the mean transverse module of the **gear** (the large
wheel) and is the same on both components. The mean spiral angle is that of the **pinion**.

## Pitch geometry at the mean point

| No | Item | Symbol | Formula | Example |
|----|------|--------|---------|---------|
| 1 | Pinion teeth | $z_1$ | - | 10 |
| 2 | Gear teeth | $z_2$ | - | 40 |
| 3 | Mean module of the gear | $m$ | - | 4 |
| 4 | Offset | $E$ | - | 20 |
| 5 | Shaft angle | $\Sigma$ | - | 90° |
| 6 | Pinion mean spiral angle | $\beta_1$ | - | 45° |
| 7 | Gear mean pitch radius | $r_2$ | $\dfrac{m z_2}{2}$ | 80.000 |
| 8 | Offset angle | $\varepsilon$ | $\arcsin\dfrac{E}{r_2}$ | 14.478° |
| 9 | Gear mean spiral angle | $\beta_2$ | $\beta_1 - \varepsilon$ | 30.523° |
| 10 | Pinion mean pitch radius | $r_1$ | $r_2 \dfrac{z_1 \cos\beta_2}{z_2 \cos\beta_1}$ | 24.365 |
| 11 | Pinion pitch cone angle | $\delta_1$ | solved numerically (see below) | |
| 12 | Gear pitch cone angle | $\delta_2$ | solved numerically (see below) | |
| 13 | Mean cone distance, each member | $R_m$ | $\dfrac{r}{\sin\delta}$ | |

The pinion mean pitch radius follows from **equal normal pitch** of the two members. Because $\cos\beta_2 / \cos\beta_1$
is larger than 1, the pinion pitch radius is larger than the simple tooth ratio $z_1 / z_2$ would suggest. In the example
the pinion pitch diameter is 48.730 mm, which is a mean module of 4.873 mm.

The two pitch cones touch at the mean point only. Their half angles $\delta_1$ and $\delta_2$ are found numerically
so that

* the axes make the shaft angle $\Sigma$, expressed in the frame at the mean point as
  $\cos\delta_1 \cos\delta_2 \cos\varepsilon - \sin\delta_1 \sin\delta_2 = \cos\Sigma$, and
* the shortest distance between the two axes is the offset $E$.

For $E = 0$ the solution reduces to the intersecting-axes pair of the same pitch radii, with $\delta_2$ from
$\tan\delta_2 = \sin\Sigma / (r_1 / r_2 + \cos\Sigma)$ and $\delta_1 = \Sigma - \delta_2$.

## Tooth proportions

The tooth depth is that of the **gear** at its large end for both members, with the standard proportions:

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Depth module | $m_d$ | $m \dfrac{R_{m2} + b/2}{R_{m2}}$, the module scaled to the large end of the gear |
| 2 | Addendum | $h_a$ | $m_d$ |
| 3 | Clearance | $c$ | $0.25\,m_d$ |
| 4 | Dedendum | $h_f$ | $h_a + c = 1.25\,m_d$ |
| 5 | Whole depth | $h$ | $2 m_d + c$ |
| 6 | Cone distance (large end) | $R_e$ | $R_m + \dfrac{b}{2}$ |
| 7 | Pitch diameter at the large end | $d$ | $2 R_e \sin\delta$ |
| 8 | Pressure angle | $\alpha_n$ | normal pressure angle, transverse angle $\arctan\dfrac{\tan\alpha_n}{\cos\beta}$ at the member's own spiral angle |

The spiral angle of each member is its own: $\beta_1$ for the pinion and $\beta_2$ for the gear. The hand of the
two members is opposite.

## Engineering requirements

* **Offset.** The offset must be less than the gear mean pitch radius, and a practical offset is a modest fraction of the
  gear pitch diameter. A large offset gives a large pinion but a lot of sliding and a low efficiency.
* **Offset direction.** The offset is signed: the pinion can lie above or below the gear axis. The side decides the hand
  of the pinion and the thrust direction, so a pair must be built with the same sign of offset on both members.
* **Spiral angle.** Hypoid pinions typically use a mean spiral angle of 45° to 50°. The gear spiral angle
  $\beta_1 - \varepsilon$ is smaller than the pinion spiral angle by the offset angle.
* **Lubrication.** Because of the high sliding speed along the tooth, use an extreme pressure (hypoid) gear oil.
* **Efficiency.** A hypoid pair is less efficient than a spiral bevel pair because of the extra sliding, and the
  efficiency falls as the offset increases.
* **Mounting.** Both the mounting distances and the offset must be held; a small error changes the contact pattern.
* **Pairs.** Hypoid gears are made and lapped as a matched set.
* **Face width and spiral limits.** The face width limits of [Straight Bevel Gears](Straight-Bevel-Gears.md) and the
  spiral angle limits of [Spiral and Zerol Bevel Gears](Spiral-and-Zerol-Bevel-Gears.md) apply.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Offset | not zero (use a spiral bevel gear for intersecting shafts) |
| Offset | less than $0.9999\,r_2$ |
| Teeth | positive for both members |
| Module | greater than 0 |
| Shaft angle | greater than 0° and less than 180° |
| Pinion mean spiral angle | 0° to below 85°, and not more than 60° for the tooth trace |
| Gear spiral angle $\beta_1 - \varepsilon$ | absolute value below 90° |
| Pitch cones | a solution must exist for the teeth, offset, shaft angle and spiral angle |
| Tooth proportions | standard only (no Gleason proportions) |
| All limits of | [Spiral and Zerol Bevel Gears](Spiral-and-Zerol-Bevel-Gears.md) |

<seealso>
    <category ref="gears">
        <a href="Spiral-and-Zerol-Bevel-Gears.md">Spiral and Zerol Bevel Gears</a>
        <a href="Worm-Gears.md">Worm Gears</a>
    </category>
</seealso>

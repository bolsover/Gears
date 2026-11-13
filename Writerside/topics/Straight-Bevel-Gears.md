# Straight Bevel Gears

<link-summary>Formulae and engineering requirements for straight bevel gears with standard and Gleason tooth proportions.</link-summary>

A bevel gear transmits motion between shafts whose axes intersect, usually at a shaft angle $\Sigma = 90°$.
The pitch surfaces are cones that roll on each other without slipping. Straight bevel gears have teeth that are
straight lines through the cone apex.

Gears builds the tooth form with the **Tredgold (back cone) approximation**: the profile at the large end is the involute
profile of a virtual spur gear with $z_v = z / \cos\delta$ teeth, mapped onto the plane perpendicular to the axis.
The module $m$ is the module at the **large end**, and the tooth shrinks linearly towards the apex.

## Pitch cone geometry

| No | Item | Symbol | Formula | Pinion | Gear |
|----|------|--------|---------|--------|------|
| 1 | Module (large end) | $m$ | - | 3 | 3 |
| 2 | Pressure angle | $\alpha$ | - | 20° | 20° |
| 3 | Shaft angle | $\Sigma$ | - | 90° | 90° |
| 4 | Number of teeth | $z$ | - | 20 | 30 |
| 5 | Pitch diameter | $d$ | $m z$ | 60.000 | 90.000 |
| 6 | Pitch cone angle | $\delta$ | $\arctan\dfrac{\sin\Sigma}{z_2 / z_1 + \cos\Sigma}$, and $\delta_2 = \Sigma - \delta_1$ | 33.690° | 56.310° |
| 7 | Outer cone distance | $R_e$ | $\dfrac{d}{2 \sin\delta}$ | 54.083 | 54.083 |
| 8 | Virtual number of teeth | $z_v$ | $\dfrac{z}{\cos\delta}$ | 24.037 | 54.083 |
| 9 | Back cone distance | $R_b$ | $\dfrac{d}{2 \cos\delta}$ | 36.056 | 81.125 |
| 10 | Maximum recommended face width | $b_{max}$ | $\min\left(\dfrac{R_e}{3},\ 10 m\right)$ | 18.028 | 18.028 |

For a general shaft angle the cone angle of the pinion is

$$
\delta_1 = \arctan\frac{\sin\Sigma}{\dfrac{z_2}{z_1} + \cos\Sigma}
$$

For $\Sigma = 90°$ this is $\tan\delta_1 = z_1 / z_2$.

## Tooth proportions

Two tooth systems are available.

### Standard

Equal addendum on both gears and a clearance of $0.25\,m$.

| No | Item | Symbol | Formula | Pinion | Gear |
|----|------|--------|---------|--------|------|
| 1 | Addendum | $h_a$ | $m$ | 3.000 | 3.000 |
| 2 | Clearance | $c$ | $0.25\,m$ | 0.750 | 0.750 |
| 3 | Dedendum | $h_f$ | $h_a + c = 1.25\,m$ | 3.750 | 3.750 |
| 4 | Whole depth | $h$ | $2 m + c$ | 6.750 | 6.750 |
| 5 | Addendum angle | $\theta_a$ | $\arctan\dfrac{h_a}{R_e}$ | 3.175° | 3.175° |
| 6 | Dedendum angle | $\theta_f$ | $\arctan\dfrac{h_f}{R_e}$ | 3.966° | 3.966° |
| 7 | Tip cone angle | $\delta_a$ | $\delta + \theta_a$ | 36.865° | 59.485° |
| 8 | Root cone angle | $\delta_f$ | $\delta - \theta_f$ | 29.724° | 52.343° |
| 9 | Outside diameter | $d_a$ | $d + 2 h_a \cos\delta$ | 64.992 | 93.328 |
| 10 | Root diameter | $d_f$ | $d - 2 h_f \cos\delta$ | 53.760 | 85.840 |

### Gleason

Long and short addendum, with a whole depth and clearance that include the constant $0.051$ mm:

| No | Item | Symbol | Formula |
|----|------|--------|---------|
| 1 | Whole depth | $h$ | $2.188\,m + 0.051$ |
| 2 | Clearance | $c$ | $0.188\,m + 0.051$ |
| 3 | Virtual tooth ratio of a gear and its mate | $q$ | $\dfrac{z / \cos\delta}{z_{mate} / \cos\delta_{mate}}$, equal to $(z / z_{mate})^2$ at $\Sigma = 90°$ |
| 4 | Addendum of the gear ($q \ge 1$) | $h_{a2}$ | $m \left(0.54 + \dfrac{0.46}{q}\right)$ |
| 5 | Addendum of the pinion | $h_{a1}$ | $2 m - h_{a2}$ |
| 6 | Dedendum | $h_f$ | $h - h_a$ |
| 7 | Addendum angle | $\theta_a$ | $\arctan\dfrac{c + h_a}{R_e}$, so the tip cone runs parallel to the root cone of the mate |
| 8 | Profile shift of the virtual spur gear | $x m$ | $h_a - m$ |

With $m = 3$ and $z_1 / z_2 = 20 / 30$ the virtual ratio of the gear is $q = 2.25$, giving a whole depth of
$6.615$, a clearance of $0.615$, a gear addendum of $2.233$ and a pinion addendum of $3.767$.

## Engineering requirements

* **Face width.** Keep the face width $b \le R_e / 3$ and not more than $10\,m$. A wider face puts the toe too close to
  the apex, where the teeth become very small, and makes the load very sensitive to shaft deflection.
* **Mounting distance.** Bevel gears must be mounted so the cone apexes coincide. Specify the mounting distance
  (apex to locating face) and allow for axial adjustment of both gears.
* **Minimum teeth.** Use at least 12 teeth on a bevel pinion and keep the virtual tooth count $z_v$ at 17 or more
  for 20° pressure angle, or use the Gleason long-and-short addendum system, which gives the pinion a positive
  profile shift and avoids undercut.
* **Axial thrust.** A straight bevel pinion pushes **away** from the apex; the axial force on a pinion is
  $F_a = F_t \tan\alpha \sin\delta$ and the radial force $F_r = F_t \tan\alpha \cos\delta$.
* **Shaft deflection.** The gears must be supported on both sides where possible; an overhung pinion needs stiff, widely spaced bearings.
* **Bore and hub.** The bore must leave a rim under the teeth, with a bore of at most $0.6\,d_f$ at the large end.
  A hub of at most $0.85\,d_f$ leaves a land between the hub and the back cone.
* **Pairs.** A bevel gear meshes with one mate only: the pitch cone angle depends on the tooth numbers of both gears.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Number of teeth | at least 5 |
| Module, face width | greater than 0 |
| Pressure angle | greater than 0° and less than 45° |
| Shaft angle | greater than 0° and less than 180° |
| Backlash | not negative |
| Rack tip radius factor | 0 to below 1 module |
| Face width | smaller than the cone distance $R_e$ (one third of $R_e$ is usual) |
| Bore diameter | not more than $0.6\,d_f$ (large end) |
| Hub diameter | larger than the bore, not more than $0.85\,d_f$ |
| Bore, hub diameter, hub length | not negative |

<seealso>
    <category ref="gears">
        <a href="Spiral-and-Zerol-Bevel-Gears.md">Spiral and Zerol Bevel Gears</a>
        <a href="Hypoid-Gears.md">Hypoid Gears</a>
        <a href="Design-Limits.md">Design Limits</a>
    </category>
</seealso>

# HTD Pulleys and Timing Belts

<link-summary>Formulae and engineering requirements for HTD (High Torque Drive) pulleys and timing belts.</link-summary>

An HTD belt has a **curvilinear** tooth: a circular head and a rounded root. It carries more torque than a trapezoidal timing belt and runs with
little backlash. The pulley groove is the negative of the belt tooth, so the groove bottom has the belt head radius
and the groove edge at the outside diameter has the belt foot radius. The belt pitch is the distance between two teeth
along the **pitch line**, which lies inside the belt in the tension cord, so the pitch diameter is larger than the
outside diameter of the pulley.

The series are named after the pitch in millimetres: 3M, 5M, 8M and 14M.

## Belt series data

All dimensions in millimetres.

| Series | Pitch $p$ | Pitch line differential $PLD$ | Tooth height $h_t$ | Head radius $R_1$ | Foot radius $R_2$ | Tooth width $w$ | Belt thickness | Recommended minimum grooves $z_{min}$ | Common belt width |
|--------|-----------|------------------------------|--------------------|-------------------|-------------------|-----------------|----------------|------------------------------------|-------------------|
| 3M | 3 | 0.381 | 1.14 | 0.88 | 0.25 | 1.96 | 2.41 | 10 | 9 |
| 5M | 5 | 0.5715 | 2.06 | 1.49 | 0.43 | 3.05 | 3.80 | 14 | 15 |
| 8M | 8 | 0.6858 | 3.38 | 2.46 | 0.70 | 5.15 | 5.60 | 22 | 20 |
| 14M | 14 | 1.397 | 6.02 | 4.50 | 1.25 | 9.40 | 10.00 | 28 | 40 |

The included angle between the two flanks of a groove is $14°$ for all series.

<note>
The 3M head radius, foot radius and tooth width were not found in the published belt data; they are scaled from the
5M tooth and are approximate. Check them against the data of the belt maker before manufacturing a 3M pulley.
</note>

## Pulley

| No | Item | Symbol | Formula | 5M, 20 grooves | 5M, 40 grooves |
|----|------|--------|---------|----------------|----------------|
| 1 | Belt pitch | $p$ | - | 5 | 5 |
| 2 | Number of grooves | $z$ | - | 20 | 40 |
| 3 | Pitch diameter | $d$ | $\dfrac{z p}{\pi}$ | 31.831 | 63.662 |
| 4 | Pitch line differential | $PLD$ | - | 0.5715 | 0.5715 |
| 5 | Outside diameter | $d_o$ | $d - 2\,PLD$ | 30.688 | 62.519 |
| 6 | Groove depth (belt tooth height) | $h_t$ | - | 2.06 | 2.06 |
| 7 | Root diameter | $d_r$ | $d_o - 2 h_t$ | 26.568 | 58.399 |
| 8 | Groove bottom radius | $R_1$ | - | 1.49 | 1.49 |
| 9 | Groove edge radius | $R_2$ | - | 0.43 | 0.43 |
| 10 | Flange diameter (flanged pulley) | $d_f$ | $d_o + 2 h_f$ | | |

The flange height $h_f$ is the distance that a flange reaches beyond the outside diameter. Without flanges the flange
diameter is the outside diameter.

## Belt and drive

| No | Item | Symbol | Formula | Example (5M, 20/40, $C = 150$) |
|----|------|--------|---------|--------------------------------|
| 1 | Speed ratio | $i$ | $\dfrac{z_2}{z_1}$ | 2 |
| 2 | Tangent line angle | $\alpha$ | $\arcsin\dfrac{d_2 - d_1}{2 C}$ | 6.091° |
| 3 | Wrap angle on the small pulley | $\theta_1$ | $\pi - 2\alpha$ | 167.82° |
| 4 | Pitch path length | $L_p$ | $2 C \cos\alpha + \dfrac{(\pi + 2\alpha)\,d_2}{2} + \dfrac{(\pi - 2\alpha)\,d_1}{2}$ | 451.69 |
| 5 | Number of belt teeth | $z_b$ | $\operatorname{round}\dfrac{L_p}{p}$ | 90 |
| 6 | Pitch length of the belt as made | $L$ | $z_b\,p$ | 450.00 |
| 7 | Belt back thickness | | belt thickness $- h_t$ | 1.74 |
| 8 | Smallest centre distance | $C_{min}$ | $\dfrac{d_{f1} + d_{f2}}{2}$ (largest diameters, with flanges) | 46.6 |
| 9 | Belt width | $b$ | the narrower of the two pulley widths | |

Here $d_1$ and $d_2$ are the pitch diameters of the small and the large pulley. Both pulleys turn in the same
direction.

## Engineering requirements

* **Same series.** Belt and both pulleys must be the same series (pitch and tooth form). A pulley of one series cannot
  carry a belt of another.
* **Minimum grooves.** Use at least the recommended minimum number of grooves for the series. A pulley with fewer
  grooves shortens belt life by bending the belt too tightly and increases the loading on each tooth.
* **Teeth in mesh.** At least six belt teeth should be in mesh on the small pulley. The number of teeth in mesh is
  approximately $z_1 \theta_1 / 360°$. In the example, $20 \times 167.8 / 360 = 9.3$ teeth.
* **Wrap angle.** Keep the wrap angle on the small pulley above about 120°; a smaller angle reduces the number of teeth in mesh.
* **Centre distance.** The centre distance must exceed the sum of the largest radii (including the flanges) so that the
  pulleys do not touch. Allow adjustment of the centre distance for installing and tensioning the belt.
* **Belt length.** The belt has a whole number of teeth, so the centre distance for a standard belt length differs
  slightly from the design value. Solve the path length equation for $C$ with the chosen belt tooth count.
* **Flanges.** A belt tends to track to one side, so flange the small pulley on both sides, or one pulley on both sides.
  When the centre distance is more than about 8 times the small pulley diameter, or the shafts are vertical, flange
  both pulleys on both sides.
* **Pretension.** HTD belts need correct installation tension. Too little causes tooth jump (ratcheting), too much
  shortens the life of belt and bearings.
* **Alignment.** Align the pulley shafts parallel and the pulleys in line.
* **Rating.** The belt width and the power rating come from the belt maker's tables; the geometry here does not
  give a power rating.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Number of grooves | at least 6; fewer than the recommended minimum for the series is accepted but flagged |
| Pulley width | greater than 0 |
| Flange height and thickness | greater than 0 when flanges are on |
| Root diameter | greater than 0 |
| Groove land | the grooves must not overlap |
| Bore, hub diameter, hub length | not negative |
| Bore | smaller than the root diameter |
| Belt series | driver and driven pulley must be the same series |
| Belt centre distance | greater than half the sum of the largest pulley diameters (flange included) |

<seealso>
    <category ref="gears">
        <a href="Roller-Chain-Sprockets.md">Roller Chain Sprockets</a>
        <a href="Design-Limits.md">Design Limits</a>
    </category>
</seealso>

# Roller Chain Sprockets

<link-summary>Formulae and engineering requirements for ISO 606 roller chain sprockets and chain drives.</link-summary>

A roller chain drive transmits power between parallel shafts through a **chain** that wraps two **sprockets**. Gears covers
the simplex (single strand) chains of the ISO 606 (DIN 8187) **B series**. The designation number is the pitch in sixteenths
of an inch, so 08B has a pitch of $8 / 16$ in $= 12.7$ mm.

## Chain series data

All dimensions in millimetres (roller diameter is the maximum, inner width the minimum).

| Series | Pitch $p$ | Roller diameter $d_1$ | Inner width $b_1$ | Pin diameter | Plate height | Inner plate thickness | Outer plate thickness |
|--------|-----------|-----------------------|-------------------|--------------|--------------|-----------------------|-----------------------|
| 06B | 9.525 | 6.35 | 5.72 | 3.28 | 8.26 | 1.30 | 1.04 |
| 08B | 12.70 | 8.51 | 7.75 | 4.45 | 11.81 | 1.55 | 1.55 |
| 10B | 15.875 | 10.16 | 9.65 | 5.08 | 14.73 | 1.55 | 1.55 |
| 12B | 19.05 | 12.07 | 11.68 | 5.72 | 16.13 | 1.80 | 1.80 |
| 16B | 25.40 | 15.88 | 17.02 | 8.28 | 21.08 | 4.11 | 3.10 |
| 20B | 31.75 | 19.05 | 19.56 | 10.19 | 26.42 | 4.62 | 3.61 |
| 24B | 38.10 | 25.40 | 25.40 | 14.63 | 33.40 | 6.10 | 5.08 |
| 28B | 44.45 | 27.94 | 30.99 | 15.90 | 37.08 | 7.62 | 6.35 |
| 32B | 50.80 | 29.21 | 30.99 | 17.81 | 42.29 | 7.11 | 6.35 |
| 40B | 63.50 | 39.37 | 38.10 | 22.89 | 52.96 | 8.13 | 8.13 |

The width over the outer plates of a simplex chain is $b_1 + 2 (t_{inner} + t_{outer})$.

## Sprocket tooth form

ISO 606 gives the roller seating radius, the seating angle, the tooth flank radius and the tip diameter as a **minimum** and a
**maximum**. Gears offers two tooth forms:

* **Maximum**: the largest seating and flank radius, the smallest seating angle and the largest tip diameter. It gives the widest gap and the tallest tooth.
* **Minimum**: the smallest seating and flank radius, the largest seating angle and the smallest tip diameter. It gives the tightest gap and the shortest tooth.

| No | Item | Symbol | Formula (Maximum form) | Formula (Minimum form) |
|----|------|--------|------------------------|------------------------|
| 1 | Pitch diameter | $d$ | $\dfrac{p}{\sin(180° / z)}$ | same |
| 2 | Root diameter | $d_f$ | $d - d_1$ | same |
| 3 | Tip diameter | $d_a$ | $d + 1.25\,p - d_1$ | $d + p \left(1 - \dfrac{1.6}{z}\right) - d_1$ |
| 4 | Roller seating radius | $r_i$ | $0.505\,d_1 + 0.069 \sqrt[3]{d_1}$ | $0.505\,d_1$ |
| 5 | Roller seating angle | $\alpha$ | $120° - \dfrac{90°}{z}$ | $140° - \dfrac{90°}{z}$ |
| 6 | Tooth flank radius | $r_e$ | $0.008\,d_1 (z^2 + 180)$ | $0.12\,d_1 (z + 2)$ |
| 7 | Tooth width (simplex) | $b_f$ | $0.93\,b_1$ for $p \le 12.7$ mm, otherwise $0.95\,b_1$ | same |

### Worked example: 08B

| No | Item | Symbol | $z = 19$ | $z = 38$ |
|----|------|--------|----------|----------|
| 1 | Pitch diameter | $d$ | 77.159 | 153.791 |
| 2 | Root diameter | $d_f$ | 68.649 | 145.281 |
| 3 | Tip diameter, maximum form | $d_a$ | 84.524 | 161.156 |
| 4 | Tip diameter, minimum form | $d_a$ | 80.280 | 157.447 |
| 5 | Seating radius, maximum / minimum | $r_i$ | 4.438 / 4.298 | 4.438 / 4.298 |
| 6 | Seating angle, maximum form / minimum form | $\alpha$ | 115.26° / 135.26° | 117.63° / 137.63° |
| 7 | Flank radius, maximum form / minimum form | $r_e$ | 36.831 / 21.445 | 110.562 / 40.848 |
| 8 | Tooth width | $b_f$ | 7.208 | 7.208 |

The roller diameter is $d_1 = 8.51$ and the inner width $b_1 = 7.75$.

<note>
The maximum form pairs the largest seating radius and flank radius with the <b>smallest</b> seating angle ($120° - 90° / z$),
and the minimum form pairs the smallest radii with the largest angle ($140° - 90° / z$), as ISO 606 groups the values.
</note>

## Chain drive

| No | Item | Symbol | Formula | Example (08B, 19/38, $C = 400$) |
|----|------|--------|---------|---------------------------------|
| 1 | Speed ratio | $i$ | $\dfrac{z_2}{z_1}$ | 2 |
| 2 | Tangent line angle | $\alpha$ | $\arcsin\dfrac{d_2 - d_1}{2 C}$ | 5.497° |
| 3 | Wrap angle on the small sprocket | $\theta_1$ | $\pi - 2\alpha$ | 169.01° |
| 4 | Chain length in pitches | $L_p / p$ | $\dfrac{2 C \cos\alpha}{p} + \dfrac{z_2 (\pi + 2\alpha) + z_1 (\pi - 2\alpha)}{2\pi}$ | 91.783 |
| 5 | Number of links | $X$ | $L_p / p$ rounded to the nearest **even** number | 92 |
| 6 | Chain length | $L$ | $X p$ | 1168.4 |
| 7 | Smallest centre distance | $C_{min}$ | $\dfrac{d_{a1} + d_{a2}}{2}$ | |

Here $d_1$, $d_2$ are the pitch diameters of the small and large sprocket and $z_1$, $z_2$ their tooth numbers. The number of links is always even so
that inner and outer links alternate without an offset (cranked) link.

## Engineering requirements

* **Minimum teeth.** Use at least 9 teeth on a sprocket (the recommended minimum). Small sprockets give a strong polygon
  effect (chordal action): the chain speed varies by $1 - \cos(180° / z)$ per tooth, which is $1.4\%$ at 19 teeth and $6.0\%$ at 9 teeth, and wears the chain faster.
* **Maximum teeth.** Keep the number of teeth on the large sprocket to about 120 or fewer. As the chain wears and lengthens, it rides higher on
  the teeth, and on a very large sprocket a small elongation lets it climb off the teeth.
* **Wrap angle.** The wrap angle on the small sprocket should be at least 120°. Gears flags a drive below this value.
* **Even link count.** Use an even number of links. An odd number needs an offset link, which is weaker.
* **Centre distance.** A good centre distance is 30 to 50 pitches. The chain must be tensioned, either by adjusting the centre distance
  or with an idler or tensioner.
* **Tooth width.** The sprocket tooth width is slightly narrower than the inner width of the chain so that the chain
  enters the tooth gap.
* **Alignment.** Align the sprockets in line and the shafts parallel; misalignment wears the sprocket flanks and the chain side plates.
* **Speed and lubrication.** Lubricate according to the chain speed; select the chain and lubrication method from the maker's power rating, not from the geometry.
* **Same chain.** Chain and both sprockets must be for the same series; the pitch must be the same.

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Sprocket teeth | at least 6 (9 is recommended) |
| Sprocket width | greater than 0 |
| Bore, hub diameter, hub length | not negative |
| Bore | smaller than the diameter at the bottom of the roller seats |
| Tooth form | the flank must reach the tip circle; too few teeth are rejected |
| Chain series | driver and driven sprocket must be the same series |
| Chain centre distance | greater than half the sum of the sprocket tip diameters |
| Number of links | at least 4, always even |
| Wrap angle on the small sprocket | below 120° is flagged as below the recommended wrap |

<seealso>
    <category ref="gears">
        <a href="HTD-Pulleys-and-Timing-Belts.md">HTD Pulleys and Timing Belts</a>
        <a href="Design-Limits.md">Design Limits</a>
    </category>
</seealso>

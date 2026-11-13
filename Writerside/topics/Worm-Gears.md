# Worm Gears

<link-summary>Formulae and engineering requirements for cylindrical worms and worm wheels.</link-summary>

A worm drive transmits motion between **crossed, non-intersecting shafts**, normally at 90°. The **worm** is a screw with
one or more thread starts; the **worm wheel** is a gear that the thread drives. A single stage gives a high ratio
(5:1 to over 100:1) and can be self-locking, but the sliding contact makes it less efficient than a gear drive.

In Gears:

* the **module** $m$ is the **axial module** of the worm, and so the transverse module of the wheel;
* the **pressure angle** is the normal pressure angle $\alpha_n$;
* the wheel is a **non-throated helical gear** with a helix angle equal to the lead angle $\gamma$ of the worm.

## Worm

| No | Item | Symbol | Formula | Example |
|----|------|--------|---------|---------|
| 1 | Axial module | $m$ | - | 2 |
| 2 | Normal pressure angle | $\alpha_n$ | - | 20° |
| 3 | Thread starts | $z_1$ | - | 2 |
| 4 | Diameter quotient | $q$ | - (typically 7 to 17) | 10 |
| 5 | Pitch diameter | $d_1$ | $q m$ | 20.000 |
| 6 | Axial pitch | $p_x$ | $\pi m$ | 6.283 |
| 7 | Lead | $L$ | $z_1 p_x = z_1 \pi m$ | 12.566 |
| 8 | Lead angle | $\gamma$ | $\arctan\dfrac{z_1}{q}$ | 11.310° |
| 9 | Axial pressure angle | $\alpha_x$ | $\arctan\dfrac{\tan\alpha_n}{\cos\gamma}$ | 20.364° |
| 10 | Addendum | $h_a$ | $m$ | 2.000 |
| 11 | Bottom clearance | $c$ | $0.25\,m$ | 0.500 |
| 12 | Dedendum | $h_f$ | $m + c$ | 2.500 |
| 13 | Tip diameter | $d_{a1}$ | $d_1 + 2 h_a$ | 24.000 |
| 14 | Root diameter | $d_{f1}$ | $d_1 - 2 h_f$ | 15.000 |

The thread is a trapezoid in the axial section: the radius at an axial position $u$ from the thread centre is

$$
r(u) = r_1 + \frac{p_x / 4 - \lvert u \rvert}{\tan\alpha_x}
$$

limited between the root radius $r_{f1}$ and the tip radius $r_{a1}$, where the pitch radius is $r_1 = d_1 / 2$.


## Worm wheel

The wheel is a helical involute gear with the normal module $m_n = m \cos\gamma$, helix angle $\gamma$ and the
same hand as the worm.

| No | Item | Symbol | Formula | Example |
|----|------|--------|---------|---------|
| 1 | Number of teeth | $z_2$ | - | 40 |
| 2 | Normal module | $m_n$ | $m \cos\gamma$ | 1.961 |
| 3 | Helix angle | $\beta_2$ | $\gamma$ | 11.310° |
| 4 | Pitch diameter | $d_2$ | $m z_2$ | 80.000 |
| 5 | Tip diameter | $d_{a2}$ | $d_2 + 2 m_n$ | 83.922 |
| 6 | Root diameter | $d_{f2}$ | $d_2 - 2.5\,m_n$ | 75.097 |
| 7 | Centre distance | $a$ | $\dfrac{d_1 + d_2}{2} = \dfrac{m (q + z_2)}{2}$ | 50.000 |
| 8 | Speed ratio | $i$ | $\dfrac{z_2}{z_1}$ | 20 |

## Efficiency and self-locking

With a friction angle $\rho' = \arctan\dfrac{\mu}{\cos\alpha_n}$, the efficiency of the worm drive (driving by the worm) is

$$
\eta = \frac{\tan\gamma}{\tan(\gamma + \rho')}
$$

For the example with a friction coefficient $\mu = 0.05$ (illustrative): $\rho' = 3.05°$ and $\eta = 0.78$.

The drive is **self-locking** (the wheel cannot drive the worm) when the lead angle is smaller than the friction
angle, $\gamma < \rho'$. Self-locking drives have an efficiency below 50%. Do not rely on self-locking for safety
where vibration can occur; add a brake.

## Engineering requirements

* **Lead angle.** A larger lead angle gives a higher efficiency but a lower ratio. The lead angle for a given $q$ and
  number of starts is $\tan\gamma = z_1 / q$.
* **Number of starts.** Use 1 to 4 starts, and not more than 8. Multiple starts give a lower ratio and higher efficiency.
* **Diameter quotient.** Typical $q$ is 7 to 17. A small $q$ gives a high lead angle and a thin, flexible worm; a large $q$
  gives a stiff worm and a low efficiency.
* **Wheel teeth.** At least 10 teeth are needed on the wheel for the geometry to work; power drives usually
  use 24 or more teeth.
* **Threaded length.** The worm thread should be long enough to cover the wheel: a common rule for $z_1 \le 2$ is
  $b_1 \ge (11 + 0.06 z_2)\,m$. For the example, $b_1 \ge 26.8$ mm.
* **Wheel face width.** About $b_2 \le 0.75\,d_{a1}$ for worms with up to three starts. A wider wheel does not carry
  more load.
* **Materials.** Use a hardened, ground steel worm with a bronze wheel to reduce wear and scuffing.
* **Axial thrust.** The worm carries a large axial force $F_{a1} = F_{t2}$, which equals the tangential force on the
  wheel. Use thrust bearings.
* **Lubrication and heat.** The low efficiency turns power into heat. Check the thermal rating of the housing.
* **Hand.** The worm and the wheel have the same hand and their shafts are at 90° (for equal lead angle and helix angle).

## Limits enforced by Gears

| Parameter | Limit |
|-----------|-------|
| Worm thread starts | 1 to 8 |
| Worm module, threaded length, diameter quotient | greater than 0 |
| Pressure angle (worm and wheel) | greater than 0° and less than 45° |
| Worm root radius | greater than $0.1\,m$, so $q$ must be larger than about 2.7 |
| Wheel teeth | at least 10 |
| Wheel module, face width | greater than 0 |
| Lead angle (wheel helix angle) | from 0° to below 45° |

<seealso>
    <category ref="gears">
        <a href="Helical-Gears.md">Helical Gears</a>
        <a href="Hypoid-Gears.md">Hypoid Gears</a>
    </category>
</seealso>

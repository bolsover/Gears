# Design Limits

<link-summary>Numerical limits that Gears enforces when a gear or drive component is validated.</link-summary>

Each component checks its parameters before it builds geometry. When a limit is broken the component reports an error
that names the component and the limit. This topic collects the limits of every component in one place; the topic of
each component explains the reason for them.

These limits are the **minimum conditions for the geometry to be built**. They are not a guarantee that the design is
good. See the *Engineering requirements* section of each topic for the conditions of good practice.

## Involute gears and racks

| Component | Parameter | Limit |
|-----------|-----------|-------|
| Spur, helical, internal gear | Teeth | at least 1 |
| | Module, face width | greater than 0 |
| | Pressure angle | greater than 0° and less than 90° |
| Crossed helical pair | Helix angle | 0° to below 90° |
| | Profile shift | 0 |
| | Internal gears | not allowed |
| | Shaft angle | $\beta_1 \pm \beta_2$ within 0.01° |
| Gear pair, fixed centre distance | Centre distance | greater than $a \cos\alpha$ |
| Rack | Teeth | at least 1 |
| | Module, face width, body height | greater than 0 |
| | Pressure angle | greater than 0° and less than 60° |
| | Backlash | 0 to less than half the pitch |
| | Helix angle | 0° to 60° |

## Bevel and hypoid gears

| Component | Parameter | Limit |
|-----------|-----------|-------|
| Straight, spiral, zerol bevel | Teeth | at least 5 |
| | Pressure angle | greater than 0° and less than 45° |
| | Shaft angle | greater than 0° and less than 180° |
| | Face width | less than the cone distance (one third is usual) |
| | Bore | at most $0.6\,d_f$ |
| | Hub | larger than the bore, at most $0.85\,d_f$ |
| Spiral bevel | Mean spiral angle | 0° to 60° |
| | Transverse pressure angle | below 45° |
| | Cutter radius | not negative, large enough for the face width |
| Hypoid | Offset | not zero, less than $0.9999\,r_2$ |
| | Pinion spiral angle | 0° to below 85° |
| | Tooth proportions | standard only |

## Worm, cycloidal and planetary gears

| Component | Parameter | Limit |
|-----------|-----------|-------|
| Worm | Thread starts | 1 to 8 |
| | Pressure angle | greater than 0° and less than 45° |
| | Root radius | greater than $0.1\,m$ |
| Worm wheel | Teeth | at least 10 |
| | Lead angle | 0° to below 45° |
| Cycloidal gear | Teeth | at least 6 |
| | Root diameter | greater than 0 |
| | Centre hole | smaller than the root diameter |
| Planetary stage | Sun and planet teeth | at least 6 |
| | Spacing | $(z_s + z_r) / N$ a whole number |
| | Planet clearance | $2 a \sin(\pi / N)$ greater than the planet tip diameter |

## Belt and chain drives

| Component | Parameter | Limit |
|-----------|-----------|-------|
| HTD pulley | Grooves | at least 6; below the series minimum is flagged |
| | Bore | smaller than the root diameter |
| Timing belt | Series | both pulleys of one series |
| | Centre distance | greater than half the sum of the largest pulley diameters |
| Sprocket | Teeth | at least 6; 9 is recommended |
| | Bore | smaller than the diameter at the bottom of the roller seats |
| Roller chain | Series | both sprockets of one chain |
| | Centre distance | greater than half the sum of the tip diameters |
| | Links | at least 4 and even |
| | Wrap angle | 120° or more is recommended |

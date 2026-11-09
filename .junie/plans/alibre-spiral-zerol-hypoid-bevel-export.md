---
sessionId: session-261109-141408-b2dv
---

# Requirements

### Overview & Goals
Testing in Alibre shows that the curved bevel export (`AlibreGearExporter.ExportCurvedBevel`, multi-section loft cut, circular pattern) works, except that the loft cut stops short towards the gear axis, so the tooth spaces are not cut completely at the toe end.

Cause: the last loft section (scale `BevelBlankProfile.ToeScale`) is a flat sketch on a plane that lies exactly on the toe face of the blank. The toe face (blank line G to H, perpendicular to the pitch cone line) is revolved about the gear axis, so it is a cone, not a plane. A plane touches a cone along one line only, so away from the tooth-space centre line the cone surface lies on the material side of the plane. The loft ends before it reaches the cone, which leaves a thin sliver of uncut material at the sides of each tooth space on the toe face. The straight bevel export does not have this problem because its loft runs to the apex point. The heel end is not affected: the back face lies on the other side of its tangent plane, so the cut starts outside the material.

The goal is to extend the loft cut a little way past the toe towards the apex (the gear axis), so the toe face is cut through completely.

### Scope
**In scope**
- One extra loft section past the toe, towards the apex, in the section list used by `ExportCurvedBevel`.
- The extension length is computed from the gear (tooth width and toe radius), and limited to the cone distances the cutter trace reaches.
- Unit tests for the new section; update of the Writerside topic.

**Out of scope**
- The heel end of the loft, the straight bevel export, the pattern, the blank, and the plane and sketch code.
- The 3D view mesh (`BevelBlankProfile.BuildToothedMesh`), which clips its own sections.

### Functional Requirements
- The loft cut of a spiral, zerol or hypoid gear has one more section beyond the toe, at a smaller scale about the apex, with the twist that the tooth trace has at that cone distance.
- The extra section is large enough that the flat end plane lies beyond the toe cone face over the whole width of the tooth space.
- The default section list from `SpiralToothSpaceSections.Build` is unchanged unless the extension is asked for, so existing callers and tests are not affected; the exporter asks for it.
- If the trace does not reach any distance beyond the toe, the extension is shortened to what it reaches, or left out.

# Technical Design

### Current Implementation
- `Gears\Calculator\Alibre\SpiralToothSpaceSections.cs`, `Build(gear, plan, sectionCount)`: sections at `scales[k] = 1 + (toeScale - 1) * k / (count - 1)`, so the last section is exactly at `blank.ToeScale`, i.e. on the toe face plane. Twist of each is `blank.TwistAtScale(trace, scale)`, which calls `SpiralToothTrace.TwistAt` and throws `ArgumentOutOfRangeException` when `trace.Reaches(coneDistance)` is false. The plane origin is `plan.ApexNodeX + (plan.BackConeApexX - plan.ApexNodeX) * scale`.
- `Gears\Calculator\Alibre\AlibreGearExporter.cs`, `ExportCurvedBevel` (about lines 455-520): for each section it builds three design points, a plane (`CreateBy3Points`), a sketch and the mapped figures, then `AddSectionLoftCut` and `AddCircularFeaturePattern`. Nothing there depends on the section count or on the last section being the toe, so an extra section needs no change to this loop.
- `Gears\Calculator\BevelBlankProfile.cs`: the toe face is line G to H, perpendicular to the pitch cone line; `ToeScale = |GB| / |EB|`; `PointH` is the root end of the toe face.

### Key Decisions
- **Add one extension section past the toe, rather than moving the toe section.** The toe section stays where it is, so the twist spacing, the section count rule and the existing tests are unchanged. Moving the whole loft would also change the interval between sections.
- **Extension length from the geometry, with a margin.** A flat plane touching a cone at its centre line leaves a gap of about `s^2 / (2 r)` at a sideways distance `s`, where `r` is the toe pitch radius. Taking `s` as one circular pitch at the toe, `pi * r / teeth`, and doubling for margin gives a distance along the pitch cone of `L = pi^2 * r / teeth^2`, which is about 1 percent of `r` for 10 teeth and less for more. Because that is tiny for large gears, a floor of a quarter of the whole depth is not needed; use `L = max(pi^2 * r / teeth^2, 0.02 * ConeDistance * ToeScale)`, capped at 25 percent of the toe cone distance. (Exact numbers are a starting point; they are constants that can be tuned after the next Alibre check.)
- **Limited by what the cutter trace reaches.** The extended cone distance is checked with `SpiralToothTrace.Reaches`; if it is outside, it is reduced to the trace limit (with a small inset), and left out if nothing remains beyond the toe.
- **Default off in `Build`, on in the exporter.** A new optional parameter keeps the existing `Build` results and tests unchanged. `ExportCurvedBevel` passes `true`.
- **Scale stays above zero.** The smaller scale about the apex gives a smaller tooth space whose root lies on the root cone, so it cuts nothing below the root cone and nothing outside the blank beyond the toe.

### Proposed Changes
- `SpiralToothSpaceSections.Build(gear, plan, sectionCount, extendPastToe)`: after the regular sections, when `extendPastToe` is true, compute the extension scale and append one `ToothSpaceSection` with scale below `toeScale`, `ConeDistance = blank.ConeDistance * scale`, twist from `blank.TwistAtScale`, plane origin and scaled figures as for other sections. The half-pitch twist check also covers the step from the toe section to the extension section.
- A public helper `SpiralToothSpaceSections.ToeExtensionScale(...)` holds the length rule above so it can be tested on its own.
- `ToothSpaceSection` gets a read-only `IsExtension` flag (not needed by the exporter, but it makes tests and error messages clear).
- `AlibreGearExporter.ExportCurvedBevel`: call `SpiralToothSpaceSections.Build(gear, plan, CurvedBevelSectionCount, true)`. The section labels and the loft call already follow the list length. The stage text of the loft cut gains "including one section past the toe".
- `SpiralToothSpaceSections.MaxSections` and the `CurvedBevelSectionCount` doc comment: say that the extension section is in addition to the requested count.
- `Writerside\topics\alibre-curved-bevel-export.md`: describe the extension section and why (flat section against a conical toe face).

### Data Models / Contracts
```csharp
public static IReadOnlyList<ToothSpaceSection> Build(SpiralBevelGearComponent gear, BevelGearSketch plan,
    int sectionCount = 0, bool extendPastToe = false);

/// Scale about the apex of the section past the toe, or null when the trace reaches nothing beyond the toe.
public static double? ToeExtensionScale(BevelBlankProfile blank, SpiralToothTrace trace, int teeth);
```

### Risks
- **The extension may still be too short or too long.** Too short leaves a thinner sliver; too long makes the loft longer than needed, which is harmless to the part since beyond the toe there is no material. The constants are named and easy to tune after the next Alibre run.
- **Loft behaviour with a small extra interval.** A very short last interval next to a longer one can distort the loft. The extension is small and its twist change is small, and the exporter's existing fallbacks (minimised twist, empty collectors, natural twist) still apply.
- **Trace limit.** Gears with a short reach may allow only a tiny extension; then the sliver may remain and the documented guide-curve fallback applies.
- **Hypoid.** The same rule uses the hypoid blank's own `ConeDistance` and `ToeScale`, so no special case is needed.

# Testing

### Validation Approach
Build the solution and run `UnitTests`. The Alibre calls need a licensed session and cannot be run by the agent, so the geometry is tested in `SpiralToothSpaceSections`, and the effect inside Alibre is left for a manual check.

### Key Scenarios
- `Build(..., extendPastToe: true)` returns one more section than `Build(...)` with the same count, and the first sections are identical.
- The extra section has a scale below `ToeScale`, above zero, a cone distance equal to `ConeDistance * scale`, and the twist of the trace at that distance.
- The extension is at least the sideways gap of a flat plane against the toe cone (`pi^2 * r / teeth^2` along the cone) and at most 25 percent of the toe cone distance.
- Zerol, spiral (both hands) and hypoid gears all get an extension section; the twist step to it is below half a pitch.

### Edge Cases
- A trace that does not reach beyond the toe gives a shortened extension, or none (`ToeExtensionScale` returns null and `Build` adds no section).
- `extendPastToe: false` (the default) gives exactly the old list, so `TestSpiralToothSpaceSections` and the other existing tests are unchanged.
- A gear with a failing validation is still rejected before any Alibre call.

### Test Changes
Add cases to `UnitTests\TestSpiralToothSpaceSections.cs` for the points above. Existing tests stay as they are. After the build, run the full `UnitTests` suite, using a scratch output folder if Alibre locks `GearsAddOn.dll`.

# Delivery Steps

### ✓ Step 1: Add the toe extension section to the section generator, with tests
`SpiralToothSpaceSections.Build` can return one extra section past the toe, and the new behaviour is covered by unit tests.

- In `Gears\Calculator\Alibre\SpiralToothSpaceSections.cs`, add `ToeExtensionScale(blank, trace, teeth)` with the length rule (geometry term, small floor, 25 percent cap, limited by `trace.Reaches`).
- Add the optional `extendPastToe` parameter to both `Build` overloads (default false) and append the extension `ToothSpaceSection`; add `IsExtension` to `ToothSpaceSection`.
- Extend the half-pitch twist check to the toe-to-extension step.
- Add the test cases listed under Testing to `UnitTests\TestSpiralToothSpaceSections.cs`; build and run them.

### ✓ Step 2: Use the extension in the Alibre export and update the documentation
Exporting a spiral, zerol or hypoid gear cuts the loft through one extra section past the toe, so the toe end is cut through completely.

- In `AlibreGearExporter.ExportCurvedBevel`, pass `true` for `extendPastToe` and update the loft stage text.
- Update the doc comments of `CurvedBevelSectionCount` and `ExportCurvedBevel` (the extension section is in addition to the requested count).
- Add a short section to `Writerside\topics\alibre-curved-bevel-export.md` about the flat section against the conical toe face and the extension, and add "check that the toe face is cut through completely" to the manual check list.
- Build the solution and run the full `UnitTests` suite (scratch output folder if Alibre locks `GearsAddOn.dll`); report the executed count and that the Alibre calls still need the manual check.
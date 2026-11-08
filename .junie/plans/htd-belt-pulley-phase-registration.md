---
sessionId: session-261108-131602-1c7i
---

# Requirements

**Goal / outcome**: A belt tooth is centred in a pulley groove (cavity), not on a land (crest), on both pulleys, in the 3D scene, in animation and in the Alibre export. The reported "0.5 tooth out" is removed by a defined phase convention, not by deleting a `+0.5`.

**Scope**
- In: belt tooth placement (`BeltToothPath`), belt/pulley phase solving (both branches of `GearAssembly.SolveBelt`), passing the driver phase to the belt builders (scene, mesh, Alibre sketch/export), rotating the exported HTD pulley parts with their mount phase, regression tests, a short doc note.
- Out: pulley groove profile (`HtdPulleyComponent.HalfGroove` / `BuildOutline`; groove 0 on local +X, checked by `TestHtdPulley.cs:107-120`), `HtdProfile`, other mesh solvers, `AssemblyInfo.cs`.
- Out: export phase for non-HTD parts. Involute pairs and bevel gears are also exported at phase 0. Leave them unchanged and flag this to the owner.

**Done when**
- At a given driver phase a belt tooth centre on the driver wrap coincides angularly with a driver groove centre. The same holds for the driven pulley. The tolerance is 1e-6 rad when the path is a whole number of pitches, and the bound below otherwise.
- Alignment holds for any driver rotation, centre-line angle, equal and unequal tooth counts, and nonzero `PhaseOffsetRad`, whenever `SolveBelt` places the second shaft (branch 1 or 2). It holds in the scene and in the exported placements (pulley parts and belt sketch).
- **Known limitation**: if both shafts are already placed by other relations (`GearAssembly.cs:318-327`), the belt is registered on the driver only. The driven pulley is not rotated to match, as today. Alignment is not claimed for that case.
- The driven shaft keeps its speed ratio and direction, and its rotation stays continuous and unwrapped while the driver turns across grooves. Only a bounded, solved registration offset `C` is new.
- Non-HTD and bevel export placements are unchanged. `UnitTests` is green, with a non-zero executed-test count.

# Technical Design

**Findings (verified in code)**
- Pulley: groove k is centred at `k*2pi/Teeth` on local X. The groove step on the pitch circle is `pitch/rPitch`. The scene rotates pulleys with `GetLocalMotion(mount.PhaseRad)`, a `RotationZ` (`GearAssemblySceneBuilder.cs:37-39`, `GearComponent.cs:100`).
- Belt: `BeltToothPath.Build` (`Gears\Calculator\Assemblies\BeltToothPath.cs:124-186`) walks driven arc, upper run, driver arc, lower run. Tooth k is at `s = (k+0.5)*spacing`, with `spacing = total/BeltTeeth`. The seam is the driven pulley's lower tangent point, which is not a groove reference. The belt is built in the driver *shaft* frame and never moves with the driver phase. A tooth centre (u=0) is a groove centre, so any mismatch is a pure phase error.
- Hand check: 20/20 HTD5M at 120 mm, theta=0, phi=0 gives r=15.915 and L=340=68*p exactly. The tangent points at +-90 deg lie on grooves, so every tooth is exactly 0.5 pitch off, on the land. In general the offset is arbitrary. (Hand calculation, not run; Step 1 measures it first.)
- Solver: `SolveBelt` branch 1 (`GearAssembly.cs:312-333`) sets `driven.RotationRad = phiA/ratio - drivenOffset` (`:332`). Branch 2 (`:336-343`) sets `driver.RotationRad = phiB*ratio - driverOffset` (`:342`). Both ignore teeth. Both build the new shaft frame with the same X and Z axes as the placed one (`:330`, `:340`), so belt-path angles in the driver frame equal driven local angles. `BeltTeeth = round(L/p)` (`TimingBelt.cs:62`), `GearRatio = Driven/Driver` (`:68`).
- Scene: the belt frame (`GearAssemblySceneBuilder.cs:55-57`) ignores `driverMount.PhaseRad`, and the animator drives `InputShaft.RotationRad`. `Build` calls `assembly.Solve()` (`:21`), so the registration is recomputed every frame.
- Export: `GetPartPlacement` (`AlibreGearExporter.cs:1097-1111`; bevel branch first, translation only otherwise) is the placement for every exported gear and pulley (via `GetAssemblyParts`, `:902-928`). It has no mount phase. `GetBeltPlacement` (`:973-979`) has none either. `BeltSketchBuilder.Build(belt)` (`BeltSketch.cs:74`) is called from `Export(TimingBelt)` (`:989`) with no phase. The belt part is built in `ExportAssembly` at `:736-746` (`Export(beltPart.Key)`). Exported pulleys sit at phase 0 with groove 0 on the part +X (`PulleySketch.cs:87-114`: one groove cell centred on +X, radials at +-pi/Teeth, plus a pattern). If the belt sketch used the driver phase while the pulley stayed at phase 0, the export would be misaligned.

**Phase convention**
- Path is anticlockwise: driven arc (s from 0), upper run, driver arc, lower run. Define the driver phase `phiA = driverMount.PhaseRad`, `stepA = p/rA`, `stepB = p/rB`.
- Driver registration: a tooth centre sits on a driver groove centre at the middle of the driver wrap (arc angle `theta + upper + wrapA/2`, wrapA = `lower + 2pi - upper`). `j*` is the groove nearest that midpoint. `s_ref` is its path position, and teeth are at `s_ref + k*spacing`. **Normalise `s_ref` into `[0, spacing)`** by subtracting whole spacings and shifting the tooth index. Then `BeltToothPath` can still generate teeth in increasing s from 0, and its segment walk (`:178`) and land logic (`next > k && same segment`, `:231`) are unchanged. `s_ref` close to 0 or to `spacing` (tooth at the seam) must still give a closed, anticlockwise loop.
- Driven registration: take the belt tooth nearest the middle of the driven wrap, at path position `s'`. Its angle on the driven pulley is `theta + lower + (s' - segStart0)/rB`. The required driven phase is `phiB* = that angle mod stepB`.
- **`C` convention (decided)**: `C = phiB* - phiA/ratio`, then reduce **`C` itself**, not `phiB*`, into `[-stepB/2, stepB/2)` (for example `C -= stepB*floor(C/stepB + 0.5)`). Then `drivenShaft.RotationRad = phiA/ratio + C - drivenOffset`. The `phiA/ratio` term stays fully unwrapped, so the driven rotation does not collapse into one groove step and does not jump by a whole groove when the driver crosses grooves (a whole-groove jump is not a symmetry of any other gear on a compound driven shaft). Because rA/rB = 1/ratio, `phiA` cancels in `C`: `C = const + n*(spacing - p)/rB (mod stepB)`, so `C` is piecewise constant and moves only by a small snap (at most `p/(2*count*rB)`) when `j*` flips. Rare corner: if `C` sits near +-stepB/2 a snap can flip it by one whole groove. This is accepted: grooves are identical, so the pulley looks the same. Document it in the XML docs.
- Pure snapping (decided): `j*` and the nearest driven tooth are recomputed from scratch by a **pure, stateless helper** for every call. There is no sticky `j*` and no state. When `phiA` crosses a groove boundary, `j*` flips, the tooth set jumps by `p - spacing` (bounded below) and `C` jumps by at most `(spacing - p)/rB`. This is accepted and noted in a code comment and the XML docs.
- Shared helper: one static class in `Gears\Calculator\Assemblies` (for example `BeltRegistration`) holds `s_ref` and `C`. Inputs are the belt geometry and `phiA`. `BeltToothPath.Build`, `SolveBelt` and the tests use it. `HtdPulleyComponent.cs` already uses `Calculator.Assemblies`, and the model code (`GearAssembly`) may call the Calculator namespace on the same basis.
- Residual bound: `|spacing - p| <= p/(2*count)`. The drift for a tooth n teeth from the registration tooth is `n*(spacing - p)`, so it is below p/4 over a half wrap. It is exactly 0 when `PitchPathLength = N*p`. This is documented in the XML docs.

**Solver branches (deterministic procedure)**
- Branch 1 (driver placed): `C` is evaluated at the known `phiA`, and `drivenShaft.RotationRad = phiA/ratio + C - drivenOffset`. This is single-pass and explicit.
- Branch 2 (only driven placed, `phiA` unknown): `phiA` appears inside `C`, so the relation is implicit. Use a fixed two-pass procedure: `phiA0 = phiB*ratio`, evaluate `C(phiA0)`, then `phiA = (phiB - C(phiA0))*ratio`, and `driver.RotationRad = phiA - driverOffset`. Do not iterate further. Accept a single-snap residual: if `j*` flips between `phiA0` and `phiA` the alignment can be off by up to `(spacing - p)`. Add an alignment test for this branch with a tolerance of `p/(2*count)` on the belt side. It is strict when `L = N*p`.
- If both shafts are already placed (`:318-327`), keep the existing behaviour: the over-constraint check and return, with no rotation set. This is a **known limitation**: the driven pulley is not rotated to match the belt. Say so in the XML docs.

**Export decision**
- Chose (a) / not (b): `GetPartPlacement` adds `GetLocalMotion(mount.PhaseRad)` **for `HtdPulleyComponent` only**, composed after the axial translation. This matches the scene transform order (Placement, Translate(axial), GetLocalMotion(phase), `GearAssemblySceneBuilder.cs:37-39`). Chose to rotate the pulley with the belt rather than leave export at phase 0, because (b) would only align the driver and the driven pulley's `C` would never reach Alibre. The bevel path is untouched, and the non-HTD path is unchanged.
- `GetBeltPlacement` stays shaft placement + axial offset (it carries no rotation). The belt sketch gets the phase through `BeltSketchBuilder.Build(belt, driverPhaseRad)`, because the belt is not rotationally symmetric. `AlibreGearExporter.Export(TimingBelt)` needs the phase: add an optional parameter, `Export(belt, driverPhaseRad = 0)`. `ExportAssembly` passes `train.FindMount(belt.Driver, out _).PhaseRad` at the belt export call (`:736-746`). Keep the `GetBeltAssemblyParts` return type (`KeyValuePair<TimingBelt, Transform3D>`). The standalone belt export from the UI keeps phase 0 if the caller has none, and that is documented.

**Other decisions**
- Chose registration at the wrap midpoints with `spacing = total/count` / not exact-pitch spacing on arcs with slack on the runs - that makes teeth non-uniform and needs per-phase re-solving.
- Chose to leave `CentreDistance` untouched / not snap it to `N*pitch` - that changes shaft placement and the placement tests (`TestHtdBeltAndExport.cs:661-673`).
- Chose to move the belt teeth with the driver phase / not rotate the belt frame rigidly - the belt is not rotationally symmetric.
- Chose to put the constant `C` into the driven shaft rotation / not into `PhaseOffsetRad` - the offset is solved data, and the mount offset stays user-owned.
- Optional parameters (`driverPhaseRad = 0`) on `BeltToothPath.Build`, `TimingBeltMeshBuilder.Build` and `BeltSketchBuilder.Build` keep existing callers compiling (`BeltSketch.cs:79`, `TimingBeltMeshBuilder.cs:69`, `AlibreGearExporter.cs:989`, tests). With phase 0 the registration is the helper's phase-0 result, not the old `+0.5` layout.

**Nuances / corners**
- `BeltSketch.Loop` (the outer loop) is toothless and unaffected. `TimingBeltMeshBuilder.BuildLoop` is unaffected.
- The pulley `_cachedGeometry` (`HtdPulleyComponent.cs:30`) does not affect phase, because rotation is applied by the transform.
- Repo guideline: the regression test must fail before the fix. The strict fixture below does.

# Testing

- Test runner: `UnitTests\UnitTests.csproj` is a legacy (non-SDK) net481 project with NUnit 3.5 referenced from `packages\` and no `NUnit3TestAdapter` or `Microsoft.NET.Test.Sdk`. Use the `build` and `run_test` tools (or the Rider test runner), never `dotnet test`. Always check that the executed-test count is non-zero and includes the new tests.
- Alignment measure (test helper): for each pulley, the angular distance (as a fraction of a pitch) from each belt tooth centre on that wrap to the nearest groove centre `phi + j*step`. The tooth centre comes from the belt tooth `Bottom` spline of the loop pieces. Pulley phase comes from `shaft.RotationRad + PhaseOffsetRad`.
- **Strict fixture (fails before fix)**: 20/20 HTD5M at 120 mm, theta=0, `L = 340 = 68*p`. Pre-fix offset is exactly 0.5 pitch. After the fix the error is <= 1e-6 rad on both pulleys. Record the pre-fix values in `[DEBUG_LOG]` output.
- Wider scenarios: 20/40 and 30/30 at 120 mm, theta = 0/30/90/-45, nonzero `Shaft.RotationRad` and `PhaseOffsetRad`, driver rotation +-1.7 rad. Error <= p/(2*count) in pitch-length terms, i.e. a drift <= p/4 at the wrap ends, and exact at the wrap midpoints.
- **Continuity sweep**: on a non-`N*p` fixture, sweep the driver `RotationRad` through several groove steps (for example 0 to 3*stepA in small increments). The driven `RotationRad` stays unwrapped (it grows past stepB) and continuous: each increment differs from `Delta driver/ratio` by at most `p/(2*count*rB)` (plus a tiny epsilon), and never by about stepB. `C` stays within `[-stepB/2, stepB/2)`. Exception: the rare +-stepB/2 corner; choose a fixture that avoids it, or assert the whole-groove flip is the only allowed jump there.
- Seam corner: choose phases that put the registration tooth at `s ~ 0` and `s ~ spacing`. The loop stays closed (`BeltSketch.IsClosed`), anticlockwise, with `ToothCount = BeltTeeth`.
- Ratio tests (`TestHtdBeltAndExport.cs:675-690` and `:693-716`):
  - `TestBelt_Solve_TurnsTheDrivenPulley...` asserts `RotationRad == 0.6` exactly and the animator step is pi/2 rad. Reword it to assert the ratio on **differences**, `Delta driven = Delta driver/ratio`, plus alignment. Use a centre distance with `PitchPathLength == N*p` (for example 20/20 at 120 mm), where `C` is constant and the 1e-9 assertion is exact. Otherwise the tolerance is `p/(2*count*rB)`, since a pi/2 step crosses about 5 flips for a 20-groove driver.
  - `TestBelt_Solve_CanPlaceTheDriverFromThePlacedDrivenShaft` asserts `shaftA.RotationRad == 0.4` exactly, in branch 2. Reword it to check the placement plus the two-pass alignment (driver and driven groove alignment with the belt) and `Delta` consistency. It uses the strict fixture for exactness, or the p/(2*count) tolerance.
- Export: `GetPartPlacement(shaft, htdMount)` includes a rotation by `mount.PhaseRad` (the rotated X axis check). Exported pulley groove centres, from `PulleySketchBuilder` points transformed by the placement, match the `BeltSketchBuilder.Build(belt, phiA)` teeth at the same phase. Bevel (`TestAlibreBevelExport.cs:191-193`) and the translation-only tests (`TestAlibreAssemblyExport.cs:79,94`, `TestCycloidalGear.cs:532`, `TestHerringboneGear.cs:196-199,298-299`) stay green: translation unchanged, involute and cycloidal placements unchanged.
- Scene: the belt inner polyline aligns with the world groove centres. Two scenes whose driver rotation differs by Delta show belt teeth shifted by `rA*Delta` along the path.
- Regression suites to keep green: `TestHtdPulley.cs`, `TestHtdBeltAndExport.cs` (belt path, placement and contact radii at `:597-774`, Alibre sketch tests at `:244-360`).

# Assumptions & Open Questions

- **Convention assumed**: "aligned" means belt tooth centre = pulley groove centre (the belt tooth fills the groove cavity). If the owner wanted another reference (for example belt land centre on a groove), it is a constant `0.5*pitch` shift in the helper.
- **Registration point**: the driver wrap midpoint. A fixed tooth at the seam tangent is rejected, because it gives asymmetric drift and a seam dependent on `CentreLineAngleDeg`.
- **Residual mismatch**: a non-integer `PitchPathLength / pitch` leaves a drift below p/4 (zero for whole-pitch paths). Snapping the centre distance to a whole belt length would be more physical, but it changes placement and tests, so it was not chosen.
- **Both-placed case**: when both shafts are already placed, the driven pulley is not rotated to match the belt. This is kept as-is for minimal scope and documented.
- **Export phase scope**: only HTD pulley placement gains the mount phase. Other gear types stay at phase 0 in the export. This is flagged for the owner and not changed here.
- **Exported pulley part orientation** is assumed to have groove 0 on the part +X (as `PulleySketch.cs` builds it with step `2pi/Teeth`). The Step 2 pulley-export test confirms this from the sketch points.
- If the visible offset persists in the UI after the fix, check that the assembly is rebuilt after `Solve()` in `GearTrainPresenter` (`:634-654`, `:1057`). A grep found no belt cache, so this is only a fallback check.

# Delivery Steps

### ✓ Step 1: Registration helper, tooth placement and belt-to-pulley phase solve
Goal: Belt teeth sit on driver grooves at the driver wrap midpoint, and the driven phase is solved to match, in both `SolveBelt` branches, for any centre-line angle and tooth counts. The bounded offset `C` is centred modulo the driven groove step, with the driven rotation kept unwrapped.
Scope: new helper beside `Gears\Calculator\Assemblies\BeltToothPath.cs`, `BeltToothPath.Build` (optional driver phase), `Gears\Model\Assemblies\GearAssembly.cs` (`SolveBelt`), `UnitTests\TestHtdBeltAndExport.cs` (or a new `TestHtdBeltPhase.cs` in `UnitTests`).
Acceptance Criteria:
- [ ] The alignment-measure helper and the strict 20/20 HTD5M at 120 mm, theta=0 fixture are added first. Before the fix they show a 0.5-pitch offset; this is recorded in `[DEBUG_LOG]`.
- [ ] `BeltToothPath.Build(belt, driverPhaseRad = 0)` places teeth through the shared pure helper, with `s_ref` normalised into `[0, spacing)`. Loops stay closed and anticlockwise, including with a tooth at the seam (s ~ 0 and s ~ spacing).
- [ ] `SolveBelt` branch 1 uses `phiA/ratio + C - drivenOffset`, with `C` reduced into `[-stepB/2, stepB/2)`. Branch 2 uses the fixed two-pass procedure from Technical Design. The both-placed case is unchanged and documented as a known limitation.
- [ ] A continuity sweep test over several groove crossings passes: the driven rotation stays unwrapped and has no jump near stepB.
- [ ] Alignment tests pass for driver and driven: strict (1e-6 rad) on the `N*p` fixture, and within the documented bound for other tooth counts, angles 0/30/90/-45, shaft rotation and `PhaseOffsetRad`, and for branch 2.
- [ ] `TestBelt_Solve_TurnsTheDrivenPulley...` (`:675-690`) and `TestBelt_Solve_CanPlaceTheDriverFromThePlacedDrivenShaft` (`:693-716`) are reworded as in `# Testing`. Existing loop-closure, tooth-count and contact-radius tests still pass.
Verification: build with the `build` tool, then `run_test` with `path="fqn:<ns>.TestHtdBeltAndExport;<ns>.TestHtdPulley"` plus the new test class FQN if one was created (take `<ns>` from the test files) -> green, **and the reported executed-test count is non-zero and includes the new alignment and continuity tests**. Do not use `dotnet test` (legacy net481 project with NUnit 3.5 and no VSTest adapter: it can report zero tests and look green). If `run_test` is unavailable, use the Rider test runner with the same non-zero count requirement.

### * Step 2: Carry the phase to scene, mesh, belt sketch and pulley export
Goal: Scene, animation and Alibre export show the same aligned belt and pulleys. The belt slides with the driver, and exported HTD pulleys are rotated with their mount phase.
Scope: `Gears\Calculator\Assemblies\GearAssemblySceneBuilder.cs` (`:50-58`), `TimingBeltMeshBuilder.Build` (`:67`, optional phase), `Gears\Calculator\Alibre\BeltSketch.cs` (`BeltSketchBuilder.Build`, optional phase), `Gears\Calculator\Alibre\AlibreGearExporter.cs` (`GetPartPlacement` for `HtdPulleyComponent` only, `Export(TimingBelt)` phase, `ExportAssembly` belt export call), a `Writerside` belt topic if present (one paragraph on the convention, residual bound and the both-placed limitation), tests in `UnitTests\TestHtdBeltAndExport.cs`.
Acceptance Criteria:
- [ ] The scene builder passes `driverMount.PhaseRad` to the mesh builder. A test checks that the belt inner polyline, from `SceneBeltItem`, aligns with the world groove centres of both pulleys.
- [ ] Two scenes with driver rotations differing by Delta show belt teeth shifted by `rA*Delta` along the path.
- [ ] `GetPartPlacement` for an `HtdPulleyComponent` composes `GetLocalMotion(mount.PhaseRad)` after the axial translation. The Z translation is unchanged. Placements for involute, cycloidal, herringbone and bevel are identical to before (existing tests green).
- [ ] The belt sketch is built with the driver mount phase. A test shows the `BeltSketch` inner teeth match the scene inner outline at a nonzero phase (extend `TestBeltSketch_LoopsMatchTheBeltOutlinesOfTheScene`). A second test shows the exported pulley groove centres, from `PulleySketchBuilder` transformed by `GetPartPlacement`, coincide with the sketch teeth, for both pulleys.
- [ ] Callers that omit the phase still compile and give the phase-0 registration.
Verification: build with the `build` tool, then `run_test` with `path="UnitTests.csproj"` -> green, with an executed-test count at least the baseline count plus the new tests (zero or unchanged count means nothing ran). Do not use `dotnet test`.
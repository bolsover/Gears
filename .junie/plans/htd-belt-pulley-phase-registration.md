---
sessionId: session-261108-131602-1c7i
---

# Requirements

**Goal / outcome**: A meshed gear train exported to Alibre has its parts in the same angular pose as the 3D scene. A cycloidal wheel and pinion that mesh in the application also mesh in the Alibre assembly, with no 0.5 tooth offset.

**Scope**
- In: `AlibreGearExporter.GetPartPlacement` applies the mount phase for every mounted component (cycloidal, involute spur/helical/herringbone, internal, worm, worm wheel, rack, HTD pulley and bevel), as the scene does. Regression tests, and one `Writerside` note.
- Out: the gear sketches and profile generators (`CycloidalProfile`, `SingleToothSketchBuilder`, `PulleySketch`). The mesh solvers (`CycloidalMeshSolver` and the others). Belt and HTD logic, which was done in the previous task. `AssemblyInfo.cs`.

**Done when**
- For a solved cycloidal pair, the exported driven part, placed by `GetPartPlacement`, has its tooth outline at the same world position as the scene item (`SceneItem.Polylines`), within 1e-6 mm. The same holds for the driver at a nonzero driver rotation.
- The same parity holds for the other exportable families, and the rack slides by its phase as in the scene.
- At phase 0 every placement is unchanged, so the existing translation-only tests stay green. `UnitTests` is green with a non-zero executed-test count.

# Technical Design

**Root cause (verified in code)**
- The scene places every mount as `shaft.Placement ∘ Translate(axial) ∘ component.GetLocalMotion(mount.PhaseRad)` (`GearAssemblySceneBuilder.cs:37-39`). `GetLocalMotion` is `RotationZ(phase)` by default (`GearComponent.cs:100`) and a slide along X for a rack (`RackComponent.cs:96`).
- `CycloidalMeshSolver.Solve` (`:46-47`) gives the driven gear `pi + pi/z2 - phi*z1/z2`, with tooth 0 on local +X. `GearAssembly.SolveRelation` stores it as `drivenShaft.RotationRad = phase - PhaseOffsetRad` (`GearAssembly.cs:422`). `mount.PhaseRad` is therefore the full driven phase, including the half-tooth term. The involute solver uses the same `pi + pi/z2` (`ParallelAxisMeshSolver.cs:55,62`).
- `GetPartPlacement` (`AlibreGearExporter.cs:1101-1117`) is the placement used for every exported part (`GetAssemblyParts`, `:918`). It composes the mount phase for `HtdPulleyComponent` only (`:1115`). All other parts, cycloidal included, go to Alibre at phase 0. `CycloidalSketchBuilder.Build` draws the whole outline from `CycloidalProfile.Build` with tooth 0 on +X, the same geometry as the scene's local frame, so only the phase is missing.
- Why it shows as exactly 0.5 tooth: for an even driven tooth count, the `pi` part of the phase is a symmetry of the gear and only the `pi/z2` half tooth is left. For an odd count, `pi + pi/z` is a whole number of pitches, so no offset shows. The line-of-centres angle `theta` is part of the solved phase and is also dropped.

**Fix (decided)**
- In `GetPartPlacement`, replace the HTD-only line with one rule for every non-bevel component: `placement = placement.Compose(mount.Component.GetLocalMotion(mount.PhaseRad))`, after the axial translation. This is the same transform order as the scene. A rack slides by its phase through the same call. The rule includes the herringbone and "centred" parts, because the rotation is about the Z axis through the sketch plane origin.
- Bevel: the placement is `shaft ∘ GetBevelPartFrame(gear, axialOffset)`. The scene order is `shaft ∘ Translate(axial) ∘ Rz(phase) ∘ geometry`. The axial translation is along Z, so it commutes with `Rz`, and the bevel path becomes `shaft ∘ Rz(phase) ∘ GetBevelPartFrame(...)`. At phase 0 it is identical to today (`TestAlibreBevelExport.cs:191-193`). A scene-parity test must confirm this. If the bevel parity does not hold, keep the bevel path unchanged and flag it, rather than guess.
- Chose placement rotation / not baking the phase into each sketch: the sketches are built once per part and are phase-free by design, and HTD already uses this approach.
- The rule also turns the exported driver by the driver shaft's own rotation. That is intended: the export matches the pose shown in the application. Document it.
- No change to `GetBeltPlacement`, to shaft parts, or to the planetary export path unless a parity test shows a problem.

**Risks**
- Worm and worm wheel parts: the scene geometry has the axis on Z and a thread crossing +X at phase 0 (`WormComponent.cs:15-19`). The export sketch should match, but a parity check against the scene is needed. If a family's export frame differs from its scene frame, fix only that family's placement and record it in the notes.
- Existing tests that assert a rotation-free placement for a solved train: the grep found only translation-only assertions (`TestAlibreAssemblyExport.cs:79,94`, `TestCycloidalGear.cs:532`, `TestHerringboneGear.cs:196-199,298-299`) on unsolved shafts, which stay at phase 0.
- Planetary sets set shaft rotations and phases through their own solver (`PlanetaryGearSet.cs`). Parity should hold, because the scene uses the same mount phases. A test covers one planetary-style mount if it is cheap; otherwise it is noted.

# Testing

- Runner: `UnitTests` is a legacy net481 NUnit 3.5 project. Use the Rider test runner or the `build` and `run_test` tools, never `dotnet test`. Check that the executed count is non-zero and includes the new tests. New `.cs` files need a `<Compile Include>` entry in `UnitTests\UnitTests.csproj`.
- **Regression (fails before the fix)**: a cycloidal wheel and pinion with an even driven tooth count (for example a 30-tooth wheel driving a 12-tooth pinion, same module), solved with `GearAssembly.Solve()`. For each mount, transform `CycloidalProfile.Build(gear).Ring` by `GetPartPlacement` and compare with the polyline of the matching `SceneItem` from `GearAssemblySceneBuilder.Build`. The error must be below 1e-6 mm. Before the fix the driven part is off by `pi + pi/z2`, which for an even count is a 0.5 tooth turn. Print the offset with `[DEBUG_LOG]`.
- Variants: odd driven count (already aligned, still aligned), nonzero driver rotation, nonzero `CentreLineAngleDeg` (0, 30, -45), nonzero `PhaseOffsetRad`, wheel as driver and pinion as driver.
- Other families: involute spur and herringbone (the exported wedge outline from `SingleToothSketchBuilder`, converted from Alibre units to mm, lies on the scene polyline when placed); worm wheel and worm; rack (placement translation equals the phase slide along the rack X); bevel (probe-point parity as described above).
- HTD pulley stays aligned (existing `TestHtdBeltPhase.cs` export alignment test).
- Regression suites to keep green: `TestCycloidalGear.cs`, `TestAlibreAssemblyExport.cs`, `TestAlibreBevelExport.cs`, `TestAlibreRackWormExport.cs`, `TestHerringboneGear.cs`, `TestHtdBeltAndExport.cs`, `TestHtdBeltPhase.cs`.

# Assumptions & Open Questions

- **Convention**: the scene is the reference. Alibre parts are placed so that their pose equals the scene pose, including the driver's own rotation.
- **Bevel**: included by composing `Rz(phase)` before the bevel frame. If its parity test fails, bevel is left unchanged and reported.
- **Not changed**: `CycloidalMeshSolver`. The application meshing is correct, so the solver and the scene are the reference.
- **Standalone single-part export** (`Export(gear)` from the UI, with no assembly) has no mount and stays at phase 0.

# Delivery Steps

### ✓ Step 1: Apply the mount phase to every non-bevel part in the Alibre placement
Goal: A solved cycloidal pair, and involute, worm, worm wheel, rack and HTD parts, are exported in the same pose as the scene.
Scope: `Gears\Calculator\Alibre\AlibreGearExporter.cs` (`GetPartPlacement`, XML docs), new `UnitTests\TestAlibrePartPhase.cs` (plus the `<Compile>` entry in `UnitTests\UnitTests.csproj`).
Acceptance Criteria:
- [ ] The cycloidal scene-parity test (even driven count, wheel and pinion, both drivers) is written first and fails before the fix. The pre-fix offset is recorded with `[DEBUG_LOG]`.
- [ ] `GetPartPlacement` composes `mount.Component.GetLocalMotion(mount.PhaseRad)` after the axial translation for every non-bevel component. The HTD-only line is replaced.
- [ ] The cycloidal tests pass for odd and even driven counts, driver rotation, `CentreLineAngleDeg` 0/30/-45 and nonzero `PhaseOffsetRad`.
- [ ] Parity tests pass for involute spur and herringbone, worm and worm wheel, rack and HTD pulley, or any family that does not match its scene frame is reported.
- [ ] Existing placement tests still pass at phase 0.
Verification: build, then run `TestAlibrePartPhase`, `TestCycloidalGear`, `TestAlibreAssemblyExport`, `TestHerringboneGear` and `TestHtdBeltPhase` by file path in the Rider runner. The result is green with a non-zero count that includes the new tests.

### ✓ Step 2: Phase for bevel gears, documentation and full regression run
Goal: Bevel parts also follow the mount phase, the behaviour is documented, and the whole suite is green.
Scope: `AlibreGearExporter.GetPartPlacement` (bevel branch), `UnitTests\TestAlibrePartPhase.cs` (bevel probe-point parity), `Writerside\topics\gear-assemblies.md` (the export phase paragraph added in the earlier task), `UnitTests\TestAlibreBevelExport.cs` if an assertion needs a note.
Acceptance Criteria:
- [ ] The bevel placement is `shaft ∘ Rz(phase) ∘ GetBevelPartFrame(...)`. At phase 0 it equals the current placement (`TestAlibreBevelExport.cs:191-193` unchanged). A bevel parity test passes, or bevel is left unchanged with the reason recorded.
- [ ] The Writerside paragraph now says that every part is exported in the pose shown in the application, including the driver rotation, and that a standalone single-part export stays at phase 0.
- [ ] The full `UnitTests` project passes, with a non-zero executed count above the previous baseline of 679 plus the new tests.
Verification: build, run the bevel and new tests by path, then run `projectNames=["UnitTests"]` in the Rider runner. Report the executed count. Do not use `dotnet test`.
---
sessionId: session-261112-144910-1w79
---

# Requirements

### Overview & Goals
Reduce unnecessary complexity in the Gears solution while preserving 100% of behaviour. The code works, so every step is a **behaviour-preserving move or extraction** (Extract Class / Extract Method / file-per-class). No redesign, no new frameworks, no API, serialization, persistence, threading or security changes.

Priorities (in order): preserve functionality, preserve public interfaces, reduce cognitive complexity, maintainability, testability, consistency.

### Scope
**In scope (confirmed with the user):**
- Presenter and node-properties files in `GearsUI\Gui\Presenters`.
- `Gears\Calculator\Alibre\AlibreGearExporter.cs`, using a *facade plus helpers* decomposition.
- `GearsUI\Gui\Views\GearTrainForm.cs`.
- Core calculators and serializer: `GearMesh3DBuilder`, `GearParameterCalculator`, `AssemblySerializer`. Only Extract Method and file moves.

**Out of scope:**
- Changing any public signature, constant, or namespace of existing types.
- Changing JSON formats (`AssemblySerializer` DTOs stay private and unchanged), maths, Alibre call order, or the UI timer and threading.
- Test files, which are the safety net. They are left unchanged except where a new characterization test is added.
- bin/obj and `.vs` folders.

### Functional Requirements
- Every stage compiles and keeps all `UnitTests` green.
- Each stage is an isolated, PR-sized change.
- Final state: one public type per file where practical (filename = type name), smaller focused classes and methods, and shorter call chains.

# Architectural Assessment

### Architecture today
- `Gears` (core library, .NET Framework 4.8.1, LangVersion 10): `Model\` (domain, `Model\Assemblies\`), `Calculator\` (maths, meshes, `Calculator\Assemblies\` mesh solvers, `Calculator\Alibre\` Alibre export).
- `GearsUI` (WinForms): `Gui\Presenters\GearTrainPresenter` talks to `Gui\Views\IGearTrainView`, implemented by `GearTrainForm`. This is an MVP split that is already sound and is kept.
- `GearsAddOn` (Alibre add-on) and `UnitTests` (many `TestAlibre*`, `TestGearTrain*` suites exercise the public surface).
- **All csproj files are old-style with explicit `<Compile Include=...>`**, so every added or moved file must be registered in `Gears.csproj` / `GearsUI.csproj` in the same step.

### Complexity hotspots ranked by impact
| # | Hotspot | Issue | Proposed refactoring | Risk |
|---|---|---|---|---|
| 1 | `Calculator\Alibre\AlibreGearExporter.cs` (~1650 lines) | God class: 10+ `Export` overloads, assembly export, pure placement maths (`Get*Placement`, `ToAlibreMatrix`), selection filtering (`GetAssemblyParts`, `IsIncluded`), sketch drawing (`DrawFigures`, `DrawMappedFigures`, nested `SketchMap`), ~60 name constants. | Facade plus helpers: keep the class and every public member, delegate to new focused classes. | Medium |
| 2 | `GearsUI\Gui\Presenters\TrainNodeProperties.cs` (~1180 lines, 13 public classes) | Violates file-per-class. | Pure file split, one class per file, no code change. | Low |
| 3 | `GearsUI\Gui\Presenters\GearTrainPresenter.cs` (~1350 lines) | 2 enums + 1 class + presenter in one file. `AddMate` is a ~150-line switch building different mates. `CreateProperties` is a type-test chain. `Refresh` and the planetary code (`RebuildPlanetary`, `CreatePlanetaryReport`) are mixed with editing. | Move `TrainNodeKind`, `GearFamily`, `TrainTreeNode` to own files. Extract one private method per `MeshType` case from `AddMate`. Extract `NodePropertiesFactory`. Extract the planetary report into a helper. | Low to Medium |
| 4 | `GearsUI\Gui\Views\GearTrainForm.cs` (~1160 lines) | `BuildLayout` is ~220 lines. The Alibre export workflow (`ExportSelectedToAlibre` … `ExportItemToAlibre`, `DescribeItem`) and the parameter table filling are mixed with form wiring. | Extract `AlibreExportController` (no new UI, same dialogs and messages) and a table-filling helper. Split `BuildLayout` into private methods. | Medium |
| 5 | `Calculator\GearParameterCalculator.cs` (`CalculatePair` ~240 lines, `CalculateCrossedPair` ~100 lines, 2 data classes) | Long methods, data classes in the same file. | Move `CalculatedGearData` and `CalculatedGearPairData` to own files. Extract Method for self-contained calculation blocks. | Medium (numeric code) |
| 6 | `Calculator\GearMesh3DBuilder.cs` (`BuildInternalGearMesh` ~200 lines, `BuildExternalGearMesh` ~160 lines) | Long methods with similar loops. | Extract Method only, with no change to evaluation order or maths. `Mesh3D` stays nested because moving it would change its public type name. | Medium |
| 7 | `Model\Assemblies\AssemblySerializer.cs` (`ToDto` ~120 lines, `FromDto` ~125 lines) | Long type-switch methods. | Extract one read/write method per family, following the existing `WriteBevel`/`ReadBevel` pattern. DTOs and JSON stay untouched. | Medium |

Other large files (`AlibreGearGenerator`, `SolidMeshBuilder`, `GearAssembly`, `PropertyLayout`) are left alone under the "small safe simplification" rule. They can be revisited in a later roadmap.

# Technical Design

### Key Decisions
- **Facade plus helpers for the exporter (user-confirmed).** `AlibreGearExporter` keeps every public constant, static, and `Export*` overload with identical signatures and behaviour. Bodies move into internal helper classes in `Gears\Calculator\Alibre\`. `GearTrainForm`, `AlibreAssemblyPartExporter`, `AlibreSaveMode` and the tests need no edits.
- **Pure moves before logic changes.** File splits are done as separate steps from method extraction, so diffs stay reviewable.
- **Extract Method only in numeric code.** Keep evaluation order, no reordering of floating-point operations.
- **No DI container.** The presenter already receives its view through its constructor. New helpers are constructed by their owner, or are static or internal where the original was static.
- **Follow existing conventions:** block-scoped namespaces (`Bolsover.Gears.*`), XML docs on public API, `_camelCase` fields, explicit csproj `Compile Include` entries, and `internal` for new helpers (check for `InternalsVisibleTo` in `Properties\AssemblyInfo.cs` before relying on it from tests).

### Architecture Diagram
``` mermaid
graph LR
  Form[GearTrainForm] --> Presenter[GearTrainPresenter]
  Form --> AlibreCtl[AlibreExportController]
  Presenter --> PropFactory[NodePropertiesFactory]
  PropFactory --> Props[13 NodeProperties classes]
  AlibreCtl --> Facade[AlibreGearExporter facade]
  Facade --> Selector[AlibreAssemblyPartSelector]
  Facade --> Placement[AlibrePartPlacement]
  Facade --> Drawer[AlibreSketchDrawer]
  Facade --> Families[Per-family part exporters]
  Facade --> PartExp[AlibreAssemblyPartExporter]
```

### File Structure
**GearsUI\Gui\Presenters** (registered in `GearsUI.csproj`)
- New, split from `GearTrainPresenter.cs`: `TrainNodeKind.cs`, `GearFamily.cs`, `TrainTreeNode.cs`, `NodePropertiesFactory.cs`.
- New, split from `TrainNodeProperties.cs`: `GearNodeProperties.cs`, `ComponentNodePropertiesBase.cs`, `RackNodeProperties.cs`, `PulleyNodeProperties.cs`, `CycloidalNodeProperties.cs`, `BeltNodeProperties.cs`, `BevelNodeProperties.cs`, `SpiralBevelNodeProperties.cs`, `HypoidNodeProperties.cs`, `WormNodeProperties.cs`, `WormWheelNodeProperties.cs`, `PlanetaryNodeProperties.cs`, `ShaftNodeProperties.cs`. The old `TrainNodeProperties.cs` is removed once empty.
- Modified: `GearTrainPresenter.cs`.

**GearsUI\Gui\Views**
- New: `AlibreExportController.cs`, `ParameterTableFiller.cs`.
- Modified: `GearTrainForm.cs`.

**Gears\Calculator\Alibre**
- New: `AlibreAssemblyPartSelector.cs`, `AlibrePartPlacement.cs`, `AlibreSketchDrawer.cs` (contains the former nested `SketchMap`, made internal), `AlibreBevelPartExporter.cs` (`ExportCurvedBevel` and its plane, point and loft helpers), `AlibreRackWormPartExporter.cs` (rack and worm, `AddRackPattern`), `AlibreAssemblyBuilder.cs` (the body of `ExportAssembly` and its save and log helpers).
- Modified: `AlibreGearExporter.cs`, which becomes a facade that keeps the constants and public members.

**Gears\Calculator**
- New: `CalculatedGearData.cs`, `CalculatedGearPairData.cs`.
- Modified: `GearParameterCalculator.cs`, `GearMesh3DBuilder.cs`.

**Gears\Model\Assemblies**
- Modified: `AssemblySerializer.cs` (private methods only).

`Gears.csproj` and `GearsUI.csproj` are updated in every step that adds or removes files.

### Contracts preserved
- Public API of `AlibreGearExporter` stays exactly as it is, including `GetAssemblyParts`, `GetShaftAssemblyParts`, `GetBeltAssemblyParts`, `GetPartPlacement`, `GetShaftPlacement`, `GetBeltPlacement`, `GetBevelPartFrame`, `ToAlibreMatrix`, `UniqueName`, `IsExportable`, `IsHerringbone`, `MinimumAssemblyParts`, `DefaultAssemblyName`, `LastExportLog` and all name constants. Constants stay defined on the facade, and helpers reference them.
- Public types and namespaces in `GearsUI.Gui.Presenters` (`GearFamily`, `TrainNodeKind`, `TrainTreeNode`, `*NodeProperties`) keep the same names and namespace. Moving them to other files is source-compatible.
- `IGearTrainView` and `GearTrainPresenter` public methods keep their signatures.
- `AssemblySerializer` JSON property names and DTO shapes are untouched.

### Risks
- **Old-style csproj:** a forgotten `Compile Include` silently drops a type, or breaks the build. Mitigation: build after every file move and diff the include list against files on disk.
- **Facade delegation changing semantics:** for example `LastExportLog` (static or instance state), exception messages, or call order against `IADPartSession`. Mitigation: move method bodies verbatim and keep state on the facade, passed to helpers explicitly. The `TestAlibre*` suites exercise these paths.
- **WinForms code:** extraction must not change event wiring order or UI-thread access. `GearTrainForm.WireEvents` order and the animation timer are left as they are.
- **Floating-point drift:** only extract whole statement blocks with identical expressions; no algebraic simplification.
- **Visibility:** helpers are internal. If tests need them, check `InternalsVisibleTo` first and avoid widening visibility unnecessarily.

# Testing

### Validation Approach
- Before any change, run the full `UnitTests` project to record a baseline (counts of passed, failed and ignored tests).
- After every stage, build the solution and run the full test suite. The result must equal the baseline.
- Use the IDE build and test tools. Do not edit `bin\` or `obj\`.

### Key Scenarios
- `TestAlibre*`, `TestHtdBeltAndExport`, `TestHerringboneGear`, `TestCycloidalGear`, `TestCrossedHelicalMesh` and the `TestGear*` suites pass. These cover the exporter facade, placement maths and the assemblies.
- `TestGearTrain*`, `TestGearTrainEditorPhases3To7`, `TestMultiSelection`, `TestSpiralBevelEditor` and `TestPropertyLayout` pass. These cover the presenter, properties and tree.
- `TestGearParameterCalculator`, `TestGearMesh3DBuilder` and `TestGearAssembly` (including serialization round trips) pass. These cover the calculator, mesh and serializer.

### Edge Cases
- Every `MeshType` branch of `AddMate`, including the error branches (non-involute driver, hypoid on a bevel mesh, internal or herringbone crossed mesh). Add a small characterization test only if an existing one doesn't cover a branch.
- `AlibreGearExporter` argument-null checks and exception types, unchanged.
- Solution loads without missing-file errors: compare csproj `Compile Include` entries to the files on disk after each split.

### Verification Checklist (behaviour unchanged)
1. Baseline test counts recorded and matched after every stage.
2. Public-API diff of the `Gears` and `GearsUI` assemblies shows no removed or changed members.
3. `AssemblySerializer` round-trip tests pass, and a sample JSON from before and after is identical.
4. No changes under `bin\`, `obj\` or `Writerside\`, except docs that mention file layout if any.
5. No numeric expression, constant or call order changed in the calculator and mesh code. Reviewed by diff: moved or extracted lines only.

# Delivery Steps

### ✓ Step 1: Baseline and file-per-class splits for presenter, properties and calculator data classes
Types in `GearsUI` and `Gears\Calculator` live in one file each, with no code changes beyond moving them.

- Build and run the full `UnitTests` project to record the baseline.
- Move `TrainNodeKind`, `GearFamily` and `TrainTreeNode` out of `GearTrainPresenter.cs` into their own files.
- Split `TrainNodeProperties.cs` into 13 files (`GearNodeProperties`, `ComponentNodePropertiesBase`, `RackNodeProperties`, `PulleyNodeProperties`, `CycloidalNodeProperties`, `BeltNodeProperties`, `BevelNodeProperties`, `SpiralBevelNodeProperties`, `HypoidNodeProperties`, `WormNodeProperties`, `WormWheelNodeProperties`, `PlanetaryNodeProperties`, `ShaftNodeProperties`), keeping namespace and code identical.
- Move `CalculatedGearData` and `CalculatedGearPairData` out of `GearParameterCalculator.cs`.
- Update `GearsUI.csproj` and `Gears.csproj` `Compile Include` entries and remove the emptied `TrainNodeProperties.cs`.
- Build and run all tests, then compare with the baseline.

### ✓ Step 2: Simplify GearTrainPresenter: AddMate, CreateProperties and planetary code
The presenter's longest methods are short dispatchers calling small, named helpers, with identical results.

- In `GearTrainPresenter.AddMate`, extract one private method per `MeshType` case (`CrossedHelical`, `Rack`, `Bevel`, `Worm`, `Hypoid` and so on) that returns the mate and the shaft angle and offset. Keep the validation order and exception messages exactly as they are.
- Move the type-test chain of `CreateProperties` into a new `NodePropertiesFactory` (internal) while keeping the same precedence (hypoid before spiral before bevel).
- Extract `RebuildPlanetary` and `CreatePlanetaryReport` into a planetary helper class if they only depend on state that can be passed in. Otherwise only split them into smaller methods.
- Register new files in `GearsUI.csproj`, build, and run `TestGearTrain*`, `TestGearTrainEditorPhases3To7`, `TestSpiralBevelEditor`, `TestMultiSelection`, `TestPropertyLayout` and the full suite.

### ✓ Step 3: Extract static helpers from AlibreGearExporter behind the facade
`AlibreGearExporter` keeps every public static but delegates the pure logic to three focused classes.

- Create `AlibreAssemblyPartSelector` (`GetAssemblyParts`, `GetShaftAssemblyParts`, `GetBeltAssemblyParts`, `IsIncluded`, `CheckPartCount`, `IsExportable`, `IsHerringbone`, `UniqueName`).
- Create `AlibrePartPlacement` (`GetPartPlacement`, `GetShaftPlacement`, `GetBeltPlacement`, `GetBevelPartFrame`, `ToAlibreMatrix`).
- Create `AlibreSketchDrawer` (`DrawFigures`, `DrawMappedFigures`, nested `SketchMap` made internal).
- Leave one-line delegating members with the same public signatures and XML docs on `AlibreGearExporter`. All constants stay on the facade.
- Register files in `Gears.csproj`, build, and run `TestAlibre*`, `TestHtdBeltAndExport`, `TestHerringboneGear`, `TestCycloidalGear` and the full suite.

### ✓ Step 4: Extract per-family and assembly export logic from AlibreGearExporter
The instance export paths are split into focused part exporters, and the facade is a short routing class.

- Create `AlibreBevelPartExporter` (`Export(BevelGearComponent)`, `ExportCurvedBevel`, `AddSectionPoint`, `AddSectionLoftCut`, back-cone plane helpers, `AddValleyLoftCut`, `TryReadPlane`, `DeleteQuietly`).
- Create `AlibreRackWormPartExporter` (`Export(RackComponent)`, `Export(WormComponent)`, `Export(WormWheelComponent)`, `AddRackPattern`).
- Create `AlibreAssemblyBuilder` for the `ExportAssembly` body and its `SavePart`, `CheckSaveTarget`, `WriteLog` and `AddPart` helpers. Keep `LastExportLog` and the exception messages unchanged.
- Pass the root, session factory (`CreatePart`) and shared state explicitly to helpers. Keep the call order against `IADPartSession` identical.
- The facade keeps involute, herringbone, belt, shaft, pulley and cycloidal exports (or delegates those too if the diff stays mechanical). Run `TestAlibre*` and the full suite.

### ✓ Step 5: Slim down GearTrainForm
`GearTrainForm` only wires the UI, and the export workflow and table filling live in their own classes.

- Extract `AlibreExportController` from `ExportSelectedToAlibre`, `ExportAllToAlibre`, `ExportSeparatePartsToAlibre`, `ExportAssemblyToAlibre`, `ExportShaftToAlibre`, `ExportItemToAlibre` and `DescribeItem`. Messages, dialogs and `ShowMessage` calls stay identical, and the form passes in what it needs (presenter, exporter, owner window and message callback).
- Extract `ParameterTableFiller` from `FillImperialTable`, `FillParameterTable` and `GetLatexImage`.
- Split `BuildLayout` (about 220 lines) into private methods per panel. Keep control creation and wiring order, and do not touch `WireEvents` order or the animation timer.
- Register files in `GearsUI.csproj`, build, and run the suites that touch the form (`TestGearAddOn`, `TestGearTrainEditorPhases3To7`, and the full suite).

### ✓ Step 6: Extract Method in core calculators and serializer, and run the final verification
Long methods in `GearParameterCalculator`, `GearMesh3DBuilder` and `AssemblySerializer` are split, and the full verification checklist is completed.

- `GearParameterCalculator`: split `CalculatePair` and `CalculateCrossedPair` into private static methods for self-contained blocks, with identical expressions and order.
- `GearMesh3DBuilder`: extract helpers from `BuildInternalGearMesh` and `BuildExternalGearMesh` (profile preparation, slice loops, cap generation). `Mesh3D` stays nested.
- `AssemblySerializer`: extract per-family write and read methods from `ToDto` and `FromDto`, following the `WriteBevel`/`ReadBevel` pattern. DTOs and JSON names stay unchanged.
- Run the full test suite and compare with the baseline. Check the public API of both assemblies is unchanged, and the csproj includes match the files on disk.
- Write up the final checklist results.
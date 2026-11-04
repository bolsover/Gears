# Gears

Gears is a .NET solution for calculating, visualising and exporting gear geometry. It contains a core calculation
library, a stand-alone gear train editor, an add-on that opens the same editor inside **Alibre Design**, and a set of
unit tests.

## Features

- **Gear train editor**: build a train from shafts, gears and meshing relations, with a 2D view, a 3D view (HelixToolkit)
  and an animation of the input shaft.
- **Gear families**:
  - spur, helical and internal involute gears
  - rack and pinion (straight and helical racks)
  - straight bevel gears (Tredgold approximation, standard and Gleason tooth proportions)
  - worm and worm wheel
  - planetary sets (sun, planets, ring and carrier)
- **Design controls**: profile shift, clearance, backlash, fixed centre distance and *Fit profile shift*, with working
  pressure angle, working backlash and contact ratio reported for each mesh.
- **Parameter tables**: every calculated gear and mesh parameter is listed with its symbol, formula, value and unit
  (symbols follow *Elements of Metric Gear Technology*). Formulae are evaluated with NCalc and can be rendered as LaTeX.
- **Files and export**: save and load a train as JSON, export the scene as STL or OBJ, and send gear and tooth
  sketches to Alibre Design.
- **Units**: metric calculation, with imperial unit support.

See [Writerside\topics\gear-assemblies.md](Writerside/topics/gear-assemblies.md) for the equations, the
behaviour of each gear family and the known limits. Bevel and worm geometry is an approximation meant for
visualisation, DXF and STL hand-off, not for stress calculation.

## Solution layout

| Path | Description |
|---|---|
| `Gears\` | Core class library (`Gears.dll`, namespace `Bolsover.Gears`). `Model\` holds the domain types (gears, shafts, assemblies, variables, points); `Calculator\` holds the geometry and parameter calculation, mesh solvers, mesh builders, exporters and the Alibre sketch generators. |
| `GearsUI\` | Windows Forms application (`GearsUI.exe`). `Program.cs` starts `GearTrainForm`. `Gui\` contains the 2D scene control, the 3D viewport, the mesh adapter and the presenter and view classes. |
| `GearsAddOn\` | Managed Alibre Design add-on (`GearsAddOn.dll`). It adds a **Gears > Gear Train** menu that opens the editor, and includes `addon.json`, `GearsAddOn.adc` and `Install-GearsAddOn.ps1`. |
| `UnitTests\` | Automated tests for the calculators, assemblies, bevel, worm and rack export, Alibre export, unit conversion and the editor presenter. |
| `Tools\VariableClassGenerator\` | Small .NET 8 console tool that generates `IVariable` classes from a CSV file (columns `ClassName`, `Name`, `NCalc`, `Symbol`, `Formula`, `VariableRole`). |
| `Writerside\` | Documentation sources (JetBrains Writerside). |
| `Reference\` | Third-party Alibre add-on samples and reference material. |

## Requirements

- Windows (x64) with Visual Studio or JetBrains Rider.
- .NET Framework 4.8.1 developer pack (`Gears`, `GearsUI`, `GearsAddOn` and `UnitTests`; C# language version 10).
- .NET 8 SDK for `Tools\VariableClassGenerator`.
- **Alibre Design** (add-on build 29000 or later) and its API assemblies. The projects reference
  `AlibreAddOn.dll` and `AlibreX.dll` through the relative path `..\..\..\..\AlibreLib\`, so copy those two files to a folder
  called `AlibreLib` four levels above each project folder, or edit the `HintPath` entries in the `.csproj` files.
- NuGet packages are restored from `packages.config` (NCalc, ExtendedNumerics.BigDecimal, HelixToolkit,
  CommunityToolkit.Diagnostics, Microsoft.Extensions.*, System.Data.SQLite and others).

## Build

Open `Gears.sln` in Visual Studio or Rider and build the solution, or from a developer prompt:

```powershell
nuget restore Gears.sln
msbuild Gears.sln /p:Configuration=Debug
```

Build outputs go to each project's `bin\` and `obj\` folders; do not commit them.

## Run

- **Stand-alone editor**: set `GearsUI` as the startup project and run it.
- **Inside Alibre Design**: see below.

## Install the Alibre Design add-on

1. Build `GearsAddOn` (x64).
2. Close Alibre Design.
3. Open an **elevated** PowerShell in `GearsAddOn\bin\Debug` (or `Release`) and run:

   ```powershell
   .\Install-GearsAddOn.ps1             # copy to C:\ProgramData\Alibre AddOns\GearsAddOn and register
   .\Install-GearsAddOn.ps1 -InPlace    # register the build folder as it is (handy while developing)
   .\Install-GearsAddOn.ps1 -Uninstall  # remove the registration (and the copied files)
   ```

4. Start Alibre Design and choose **Gears > Gear Train**.

The script registers the add-on identifier `{05A0F2F2-61D4-48B3-A737-BD23152F6FFF}` under
`HKLM\SOFTWARE\Alibre Design Add-Ons`.

## Test

Run the `UnitTests` project from the IDE test runner, or from a developer prompt:

```powershell
vstest.console.exe UnitTests\bin\Debug\UnitTests.dll
```

The tests that talk to Alibre Design need the Alibre API assemblies described above.

## Documentation

The Writerside sources are in `Writerside\topics`. Update them when user-facing behaviour changes.

## Contributing

- Follow the standard .NET naming rules: PascalCase for types, methods and properties; `_camelCase` for private fields.
- Keep UI code separate from the geometry and model logic.
- Add or update a unit test with each bug fix or behaviour change.
- Keep changes minimal and leave `bin\` and `obj\` out of commits.

## License

Gears is released under the [MIT License](LICENSE). The Alibre Design API assemblies (`AlibreAddOn.dll`,
`AlibreX.dll`) and the NuGet packages are not covered by this license; each keeps its own terms.

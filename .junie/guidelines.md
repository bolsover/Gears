# Project Guidelines — Gears Solution

Last updated: 2025-08-19

## 1) Project Overview
Gears is a .NET solution for gear geometry calculation, rendering, and UI interaction.
It consists of a core library, UI applications, automated tests, and documentation.

High-level goals:
- Provide APIs and models for gears (geometry, features, points, profiles).
- Render and interact with gear tooth profiles in UI controls.
- Maintain unit tests to validate geometry and UI surface behaviors.

## 2) Repository Structure (high-level)
- Gears.sln — Main solution file.
- Gears/ — Core library and related components
  - Calculator/ — Geometry and math utilities
  - Model/ — Domain models (e.g., GearPoint, GearPoint3D, Feature, ToothProfile, etc.)
  - Builder/, Tests/, Data/, Properties/, bin/, obj/
- GearsUI/ — UI layer (legacy/classic WinForms/WPF elements), includes:
  - Gui/ — UI controls (e.g., ToothProfileControl)
  - Properties/, bin/, obj/
- UI/ — Additional UI project (net9.0-windows target)
- UnitTests/ — Automated tests for the solution
- Writerside/ — Documentation sources (JetBrains Writerside)
- WorkSheets/ — Miscellaneous worksheets/supporting materials
- .junie/ — Project automation guidelines for Junie (this file)

Note: bin/ and obj/ directories are build outputs; do not commit changes inside them.

## 3) Build & Test — How Junie should operate
Please prefer the provided tools over raw shell commands.

- Build the whole solution:
  - Use the build tool: functions.build
  - Avoid manual dotnet build unless necessary for debugging.

- Run tests:
  - To run all tests: functions.run_test with path="fullSolution"
  - To run a specific project: functions.run_test with path="UnitTests.csproj"
  - To run by namespace/type/test FQN: functions.run_test with path="fqn:Fully.Qualified.Name"
  - You can pass multiple targets separated by semicolons.

- Debug logging in tests:
  - Prefix with [DEBUG_LOG] in test output to help triage during runs.

## 4) Submission checklist for Junie
Before submitting changes:
- Build succeeds (no compile errors).
- All relevant tests pass. If adding or changing behavior, add/update tests accordingly.
- Only minimal, necessary changes are included to satisfy the issue.
- No edits in bin/ or obj/ directories; no extraneous files committed.

## 5) Coding Guidelines (C#)
- Follow standard .NET naming:
  - Types, methods, properties: PascalCase
  - Private fields: _camelCase
  - Local variables and parameters: camelCase
- Keep methods focused; prefer small, readable units.
- Use XML documentation comments on public APIs where it clarifies intent.
- Prefer immutability where practical for model types; validate inputs defensively.
- Handle edge cases explicitly (nulls, ranges, empty collections).
- Keep UI code separated from pure geometry/model logic.

## 6) Tests
- Place unit tests in UnitTests/ project; mirror namespaces when practical.
- When fixing a bug, add a regression test under UnitTests that fails before the fix and passes after.
- Use precise assertions and include error messages for clarity.

## 7) Documentation
- Writerside/ holds structured docs. Update or add topics when user-facing behavior changes.
- Keep high-level descriptions in sync with Model and Calculator changes (e.g., new Feature types).

## 8) Issue-specific Guidance for Junie
- Respect the "minimal change" principle.
- Explain your plan and progress in <UPDATE> sections as required by the workflow.
- Prefer specialized tools (search_project, get_file_structure, run_test, build) over generic shell commands.
- Use Windows paths with backslashes in any references.

## 9) Contacts & Ownership
- If behavior is unclear or the issue scope is ambiguous, ask for clarification with functions.ask_user and pause changes until scope is clarified.

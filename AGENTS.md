# Repository Guidelines

## Project Structure & Module Organization

- `Runtime/`: Runtime C# libraries (assembly definitions per area).
- `Editor/`: Editor-only tooling and UIElements/USS.
- `Tests/Runtime`, `Tests/Editor`: NUnit/UTF tests mirroring source folders (e.g., `Attributes`, `Extensions`).
- `Shaders/`, `Styles/`, `URP/`: Rendering assets with accompanying `.meta` files.
- `docs/` and top-level guides: Developer guides and references.
- `package.json`: Unity package metadata + helper scripts; `.editorconfig` defines formatting.

## Build, Test, and Development Commands

- Install hooks and tools: `npm run hooks:install` and `dotnet tool restore`.
- Format C#: `dotnet tool run CSharpier format` (pre-commit runs this automatically).
- Lint docs links: `npm run lint:docs` or `pwsh ./scripts/lint-doc-links.ps1 -VerboseOutput`.
- Run tests (Unity): add this package to a Unity 2021.3+ project, then use Test Runner (EditMode/PlayMode). CLI example:
  `Unity -batchmode -projectPath <Project> -runTests -testPlatform EditMode -testResults ./TestResults.xml -quit`.

## Coding Style & Naming Conventions

- Indentation: 4 spaces for `*.cs`; 2 spaces for JSON/YAML/`*.asmdef`.
- Line endings: CRLF; UTF-8 (no BOM) per `.editorconfig`.
- C#: explicit types over `var`; braces required; `using` inside namespace.
- Naming: PascalCase for types/public members; camelCase for fields/locals; interfaces prefixed `I` (e.g., `IResolver`); type params prefixed `T`; events start with `On...`.
- Do not use underscores in function names, especially test function names.
- Do not use regions, anywhere, ever.
- Avoid `var` wherever possible, use expressive types.
- Do not use nullable reference types.

## Reflection & API Access

- Avoid runtime reflection wherever possible in favor of explicit APIs and compiler-checked contracts.
- Prefer promoting private helpers to `internal` (with `[InternalsVisibleTo]`) rather than poking at them via reflection when tests or tooling need access.
- Use `nameof(...)` (or shared constants) instead of magic strings whenever referencing members; stringify identifiers only when Unity serialization mandates it and document why.
- Expose shared editor/runtime helpers via `internal` members and use `InternalsVisibleTo` for the assemblies that need access.
- If a Unity serialization hook requires string references, centralize them in a single source of truth and document why reflection is unavoidable.
- Editor and runtime test code should not rely on reflection to reach helpers we own; adjust visibility (typically `internal` + `InternalsVisibleTo`) so tests exercise real APIs, and reserve reflection for Unity/third-party surfaces or when reflection behavior is the subject under test.
- Reflection should be avoided at all costs in shipping/runtime code. If an unavoidable scenario arises (e.g., Unity serialization callbacks), document the reason and constrain it to the narrowest surface possible.

## Testing Guidelines

- Frameworks: NUnit + Unity Test Framework (`[Test]`, `[UnityTest]`).
- Structure tests to mirror `Runtime/` and `Editor/`; name files `*Tests.cs` (e.g., `Tests/Editor/MultiFileSelectorElementTests.cs`).
- Keep tests deterministic; prefer fast EditMode where possible. Long-running tests should use timeouts (see `Tests/Runtime/RuntimeTestTimeouts.cs`).
- Do not use regions.
- Try to use minimal comments and instead rely on expressive naming conventions and assertions.
- Do not use Description annotations for tests.
- Do not create `async Task` test methods - the Unity test runner does not support this. Make do with `IEnumerator` based UnityTestMethods.
- Do not use `Assert.ThrowsAsync`, it does not exist.
- When asserting that UnityEngine.Objects are null or not null, please check for null directly (thing != null, thing == null), to properly adhere to Unity Object existence checks.

## Commit & Pull Request Guidelines

- Commits: short, imperative summaries (e.g., “Fix JSON serialization for FastVector”), group related changes.
- PRs: clear description, link issues (`#123`), include before/after screenshots for editor UI, update relevant docs, and ensure tests + linters pass.
- Version bumps in `package.json` should be deliberate and typically done in a release PR.

## Security & Configuration Tips

- Do not commit Unity `Library/`, `obj/`, or secrets; keep `.meta` files for assets.
- Target Unity `2021.3`; verify `.asmdef` references when adding new namespaces.
- NPM publishing uses GitHub Secrets; never commit tokens.

## Agent-Specific Notes

- This file’s scope is the entire repo. Keep changes minimal, follow `.editorconfig`, respect folder boundaries (Runtime vs Editor), and update docs/tests alongside code changes.

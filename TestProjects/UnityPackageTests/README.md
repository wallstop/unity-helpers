# UnityPackageTests Project

This minimal Unity project exists solely to exercise the package’s EditMode and PlayMode tests via CI and the `scripts/run-unity-tests.*` helpers. The project keeps its manifest pointed at the repository root (`file:../..`) and marks the package as `testable` so that Unity automatically discovers every suite under `Tests/`.

**Do not use this project for gameplay or samples.** It intentionally stays bare-bones to ensure deterministic CI results:

- Unity version is locked through `ProjectSettings/ProjectVersion.txt`.
- Package dependencies live entirely inside `Packages/manifest.json`.
- Generated folders such as `Library/`, `Temp/`, `UserSettings/`, and cached `packages-lock.json` files are gitignored – Unity will create them on demand.

If you need to run the package tests locally without touching your main game, open this project in the Unity Hub (2022.3.51f1 LTS) and run the Test Runner.

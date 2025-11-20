# Unity Helpers Roadmap

These priorities focus on net-new capabilities on top of the systems that are already in the package today.

## 1. Comprehensive Inspector Tooling

**Currently shipping:** Attribute + property drawer suite covering enum toggles, dropdowns, show-if logic, and validation (`Runtime/Core/Attributes/*.cs`, `Editor/CustomDrawers/*.cs`), e.g., `WEnumToggleButtonsAttribute`, `WShowIfAttribute`, `WValueDropDownAttribute`, `NotNullAttribute`, and `ValidateAssignmentAttribute`.

**Next up:**

- Inline editors for nested ScriptableObjects/components (no popup inspectors) with preview and diff affordances.
- Tabbed/section navigation plus persistent layout bookmarks so large inspectors stay organized.
- Visual instrumentation (progress bars, warning badges, inline state telemetry) that reflects runtime data while editing.
- Additional attributes for disable-if, layer/sorting-layer selection, cross-field validators, and auto-generated help boxes.

## 2. Expanded Editor Tooling

**Currently shipping:** Animation Creator / Sprite Sheet Animation Creator (`Editor/Sprites/AnimationCreator.cs`) and Animation Event Editor (`Editor/AnimationEventEditor.cs`) plus the existing sprite, prefab, and persistence utilities documented in `Docs/EDITOR_TOOLS_GUIDE.md`.

**Next up:**

- Animation Creator enhancements: sprite sheet support, higher performance, dynamic framerate, previews, and more
- Sprite Sheet Cutter Tool
- Animation Event Editor refinements: timeline scrubbing, copy/paste across clips, presets, and validation overlays.
- Additional automation surfaces (import processor builder, prefab validation rulesets, texture/animation post processors) that can run headless or inside UI Toolkit dashboards.

## 3. Advanced Random & Statistical Testing

**Currently shipping:** Dozens of RNG implementations (PCG, Xoroshiro, SplitMix64, RomuDuo, FlurryBurst, NativePcg, etc.) behind `PRNG.Instance` and `RandomUtilities` (`Runtime/Core/Random/*.cs`).

**Next up:**

- CI-friendly statistical harness that can run PractRand/TestU01 suites and publish pass/fail artifacts automatically.
- Automated quality reports with histograms, percentile deltas, and change detection that gate pull requests.
- Higher-level sampling utilities (Poisson disk, stratified sampling, correlated noise, shuffled streams) plus deterministic scenario builders for QA.
- Job/Burst aware stream schedulers (seed pools, jump-ahead APIs, reservoir/permutation helpers) with accompanying property-based tests.

## 4. Enhanced Spatial Trees

**Currently shipping:** QuadTree2D, KdTree2D, RTree2D, SpatialHash2D plus experimental 3D variants (OctTree3D, KdTree3D, RTree3D, SpatialHash3D) under `Runtime/Core/DataStructure`.

**Next up:**

- Graduate the 3D trees out of experimental status with profiling data, docs, and parity with the polished 2D APIs.
- Mutable/incremental update variants so trees can accept localized inserts/removals instead of full rebuilds.
- Shape query parity with Unity Physics (ray/capsule/sphere casts, overlap tests) plus adapter structs to translate between PhysicsScene queries and spatial indices.
- Streaming/sectorized builders for large worlds so trees can be incrementally loaded per tile or job.

## 5. UI Toolkit Enhancements

**Currently shipping:** LayeredImage and MultiFileSelectorElement visual elements (`Runtime/Visuals/UIToolkit/*.cs`) with samples and persistence helpers.

**Next up:**

- UI Toolkit control pack with dockable panes, inspector tab bars, data tables, curve editors, and virtualized multi-column lists.
- Theme/palette system (USS + UXML snippets) plus sample scenes showing runtime/editor parity.
- Performance-focused patterns (batched bindings, incremental painters, list virtualization) codified as utilities + docs.
- Wizards and dashboards built on UI Toolkit to host the automation workflows outlined above.

## 6. Utility Expansion

**Currently shipping:** Broad utility set covering pooling (`Runtime/Utils/Buffers.cs`), singleton patterns, animation helpers, sprite utilities, compression helpers, and more under `Runtime/Utils`.

**Next up:**

- Cross-system bridges (effects ↔ serialization, pooling ↔ DI containers, random ↔ spatial query fuzzers) with ready-made samples.
- Additional math/combinatorics helpers (curve fitting, statistics, interpolation packs) and IO/localization conveniences.
- Opinionated service patterns (task/tween schedulers, async job orchestrators, gameplay timers) wired into diagnostics.

## 7. Performance Program

**Currently shipping:** Benchmarks and guidance for random generators and spatial trees (`Docs/RANDOM_PERFORMANCE.md`, `Docs/SPATIAL_TREE_2D_PERFORMANCE.md`) plus profiling notes in README.

**Next up:**

- Automated benchmark harness that can run inside CI, store baselines, and surface regressions per subsystem.
- Burst/Jobs rewrites of hot loops (spatial queries, pooling, math helpers) along with analyzer hints that steer consumers toward the fast paths.
- Allocation/GC audits with Roslyn analyzers + NUnit tests that lock in zero-allocation guarantees for critical APIs.

## 8. Attribute & Tag System Evolution

**Currently shipping:** Gameplay attributes/tags, effect stacks, and attribute metadata caches (`Runtime/Tags/*.cs`) with ScriptableObject-driven effect authoring.

**Next up:**

- TBD enhancements

# Improvement Plan (2025-11-26)

## Priority 2 — Maintainability & Developer Experience

### 5. Split `UnityExtensions` into focused modules *(In Progress)*

- **Problem**: `Runtime/Core/Extension/UnityExtensions.cs` is still a multi-thousand-line catch‑all (UI helpers, bounds/rect utilities, pooling helpers, etc.). Only the geometry block had been extracted, so most changes still collided in the root file and new helpers kept landing there by default.
- **2025-11-26 Progress**: Started carving the file into targeted partials by moving all bounds/rect/camera helpers into `UnityExtensions.Bounds.cs`. This includes center-point helpers, Rect↔Bounds conversions, RectTransform world rects, orthographic camera bounds, and the `BoundsInt`/`Bounds` aggregation utilities, reducing the root file by ~200 lines and isolating pooling dependencies. Followed up by extracting the UI helpers (`Slider.SetColors`, RectTransform offset setters) into `UnityExtensions.UI.cs`, and physics/collider helpers (`Rigidbody2D.Stop`, `Collider2D.IsCircleFullyContained`, `PolygonCollider2D.Invert`) into `UnityExtensions.Physics.cs`, keeping IMGUI and physics logic out of the geometry-heavy root. Remaining work: move the overlap/containment predicates and grid enumeration helpers into their own partial.
- **Next Step**: Continue extracting the remaining domains (UI + animation helpers, physics/rigidbody utilities, bounds/overlap predicates) so contributors can edit localized partials instead of the monolith.
- **Impact**: Smaller diffs, easier reviews, clearer ownership of complex helpers, and fewer merge conflicts when multiple teams touch `UnityExtensions`.

### 6. Decompose `AnimationEventEditor` loading & UI *(Completed – 2025-11-26)*

- **Problem**: `Editor/AnimationEventEditor.cs` remains a 1.4k-line monolith where slow reflection + UI rendering live together. It still blocks the main thread while scanning every `MonoBehaviour`, and there is no view-model/service split or automated coverage.
- **Next Steps**:
  1. Land the view-model tests + smoke tests for domain reload/open window perf.
  2. Trim the IMGUI layer once the tests pass.
- **2025-11-26 Progress**: Extracted a dedicated `AnimationEventEditorViewModel` (plus `AnimationEventItem`) and rewired the window to use it, then added `AnimationEventEditorViewModelTests` + `AnimationEventEditorSmokeTests` to cover loading/dirty-state logic and opening the window twice without regressions. Expanded that suite with data-driven regression tests that exercise null clip handling, sprite-curve ordering, duplication/move operations, baseline snapshots, and event array export to lock in the tricky edge cases. Also moved clip filtering, reset logic, same-time row reordering rules, time/function fields, sprite previews, clip selection UI, keyboard shortcuts, and the entire type/method/parameter IMGUI stack out of the window into helper classes so `AnimationEventEditor` now focuses on layout while the helpers own caching, validation, and field rendering. `docs/project/animation-event-editor.md` documents the helper architecture for future contributors, and the feature guide links to it.
- **Impact**: Responsive editor tooling, easier to extend/maintain, and guardrails that prevent regressions in animation-event workflows.

### 7. Consolidate `CommonTestBase` lifecycle logic *(Not Started)*

- **Problem**: Runtime and editor tests still depend on separate `CommonTestBase` implementations with diverging cleanup rules and dispatcher scopes. Bug fixes must be duplicated manually and some suites miss critical cleanup guarantees.
- **Next Steps** *(unchanged)*:
  1. Move the shared lifecycle logic into a single test-utility assembly consumed by both editor/runtime asmdefs, using `#if UNITY_EDITOR` for editor-only hooks (`EditorSceneManager`, asset cleanup, etc.).
  2. Update `scripts/lint-tests.ps1` and the Unity test workflow to enforce inheritance from the unified base, ensuring new tests automatically pick up the lifecycle guarantees.
  3. Document the standardized lifecycle (what `Track(...)` covers, when dispatchers/scenes are disposed) so downstream consumers know how to extend it safely.
- **2025-11-26 Progress**: Added the `WallstopStudios.UnityHelpers.Tests.Common` test-framework assembly and migrated both editor/runtime suites to a single `CommonTestBase` implementation (with shared async disposal, scene tracking, and dispatcher scope handling), eliminating the divergent copies under `Tests/Editor/Utils` and `Tests/Runtime/TestUtils`.
- **Impact**: Consistent cleanup semantics across all suites, less duplicated maintenance, and fewer test-only leaks/regressions making it into CI.

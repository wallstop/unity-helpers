# Improvement Plan

## Priority 0 — Correctness & Security

### 1. Harden `UnityMainThreadDispatcher` (Correctness, Ease-of-Use)

- **Problem**: `UnityMainThreadDispatcher.RunOnMainThread` is the recommended bridge from worker threads, yet `RuntimeSingleton<T>.Instance` creates or finds the component via Unity APIs (`FindAnyObjectByType`, `new GameObject(...)`) with no main-thread guard (`Runtime/Utils/RuntimeSingleton.cs:57-107`). Calling `UnityMainThreadDispatcher.Instance` on a background thread therefore hits undefined behavior, and the unbounded `ConcurrentQueue<Action>` in `Runtime/Core/Helper/UnityMainThreadDispatcher.cs:26-104` can grow without diagnostics or cancellation.
- **Actions**:
  1. Introduce a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]` (and `InitializeOnLoadMethod` for editor) that ensures the dispatcher component exists on the main thread before any user code can enqueue work.
  2. Add detection in `RuntimeSingleton<T>.Instance` to throw a descriptive exception when invoked off the main thread, or to marshal creation back via `SynchronizationContext`.
  3. Implement optional queue bounds/backpressure (e.g., warn or drop when pending work exceeds a threshold) and add cancellation-aware helpers (`RunAsync(Func<CancellationToken, Task>)`).
  4. Extend `UnityMainThreadDispatcherTests` with multi-threaded coverage that intentionally calls from worker threads and asserts safe initialization and bounded memory.
- **Impact**: Removes undefined behavior crashes, prevents runaway memory in editor tools that dispatch frequently, and makes the dispatcher trustworthy in both runtime and edit-time automation.
- **Checkpoint (2025-11-21)**:
  - Added `Runtime/Core/Helper/UnityMainThreadGuard.cs` and updated both `RuntimeSingleton<T>` and `ScriptableObjectSingleton<T>` so first-time creation on worker threads throws a descriptive error while pre-bootstrapping `UnityMainThreadDispatcher` (runtime + editor attributes) keeps the dispatcher available for background logging.
  - `UnityMainThreadDispatcher` now enforces a configurable queue limit (with warning + drop semantics), exposes `TryRunOnMainThread`, and provides cancellation-aware `RunAsync(Func<CancellationToken, Task>)` alongside more robust `Post/RunAsync` completions.
  - New tests in `Tests/Runtime/Helper/UnityMainThreadDispatcherTests.cs`, `Tests/Runtime/Utils/RuntimeSingletonTests.cs`, and `Tests/Editor/Utils/ScriptableObjectSingletonTests.cs` exercise multi-thread access, queue overflow handling, and cancellation to guard against regressions.
  - Introduced `[AutoLoadSingleton]` plus the editor-generated `SingletonAutoLoadManifest` so runtime + ScriptableObject singletons can opt into reflection-free auto-loading at startup, backed by `SingletonAutoLoader` tests (`Tests/Runtime/Helper/SingletonAutoLoaderTests.cs`).

### 2. Remove legacy `BinaryFormatter` surface area (Correctness, Maintainability)

- **Problem**: `Serializer` still exposes `SerializationType.SystemBinary` and pooled `BinaryFormatter` paths (`Runtime/Core/Serialization/Serializer.cs:279-782`). `BinaryFormatter` is obsolete, insecure, and no longer supported on modern .NET runtimes. Keeping it encourages unsafe usage and complicates platform upgrades.
- **Actions**:
  1. Gate all `SystemBinary` entry points behind `UNITY_EDITOR` or an explicit scripting define, throw `NotSupportedException` otherwise, and mark the enum value `[Obsolete(..., error: true)]`.
  2. Provide migration helpers (`Serializer.BinaryToJson<T>`, docs) and update `Docs/features/serialization` explaining the deprecation.
  3. Add analyzers/tests that fail when `SerializationType.SystemBinary` is consumed so regressions are caught.
- **Impact**: Eliminates a known security footgun, simplifies future .NET 8+ adoption, and guides users toward supported JSON/protobuf pipelines.

### 3. Fix concave hull neighbor selection allocations (Performance, Correctness)

- **Problem**: Both 2D and 3D concave hull builders repeatedly clone the full candidate set each iteration by clearing and `AddRange`-ing into a temporary list (`Runtime/Core/Extension/UnityExtensions.cs:2404-2691`). This is O(n²) copying and undermines the promise of “minimal allocations”, and the recursion fallback (`return BuildConcaveHull2(..., nearestNeighbors + 1)`) re-enters with freshly allocated state each time.
- **Actions**:
  1. Replace the `clockwisePoints.Clear(); clockwisePoints.AddRange(dataSet); ... RemoveRange(...)` loops with indexed views (e.g., maintain a shared `List<int>` of indices sorted by distance/right-hand-turn).
  2. Carry the working buffers through recursion instead of reallocating so nearest-neighbor retries reuse the same pools.
  3. Add profiling tests under `Tests/Runtime/Performance/SpatialTree3DPerformanceTests` (or a new concave hull suite) to assert zero-GC behavior for representative grids.
- **Impact**: Restores the advertised performance characteristics and prevents runaway GC spikes when designers feed large tilemaps into the concave hull utilities.

## Priority 1 — Performance & UX

### 4. De-duplicate payload storage in 3D spatial trees (Performance, Maintainability)

- **Problem**: `KdTree3D` and `OctTree3D` each keep three copies of every element: `ImmutableArray<T> elements`, an `Entry[]` that repeats the value plus position, and `_indices` for traversal (`Runtime/Core/DataStructure/KDTree3D.cs:110-205`, `Runtime/Core/DataStructure/OctTree3D.cs:140-236`). For large structs (e.g., effects metadata) this triples memory and cache misses.
- **Actions**:
  1. Store payloads once (either in `ImmutableArray<T>` or `Entry[]`) and have nodes reference indices only; expose enumerators that yield `in T` without extra copies.
  2. Provide `ISpatialTree3D<T>.GetEntries(Span<int> indices)` APIs so callers can map indices back to their own storage when they prefer not to copy.
  3. Update affected tests (`Tests/Runtime/DataStructures/SpatialTree3DBoundsConsistencyTests.cs`) to validate counts via indices rather than by comparing `List<T>` instances.
- **Impact**: Cuts tree memory in half, improves cache locality for queries, and aligns the 3D implementations with the more memory-efficient 2D trees.

### 5. Repair spatial semantics documentation links and coverage (Ease-of-Understanding)

- **Problem**: XML docs in `KdTree3D`/`OctTree3D` reference `Docs/SPATIAL_TREE_SEMANTICS.md` (all caps root) (`Runtime/Core/DataStructure/KDTree3D.cs:26-31`, `OctTree3D.cs:22-30`), but the actual guide lives at `Docs/features/spatial/spatial-tree-semantics.md`. Users clicking the link from IDE tooltips hit 404s, and there is no automated check keeping the semantics doc in sync with the growing 3D coverage.
- **Actions**:
  1. Update doc-comments to the correct relative path (`Docs/features/spatial/spatial-tree-semantics.md#...`) and add anchors for the sections referenced in code.
  2. Extend `scripts/lint-doc-links.ps1` to verify that every `Docs/*` reference inside source files resolves.
  3. Summarize the 3D parity guarantees (bounds-only vs point trees) in README and in `Docs/performance/spatial-tree-3d-performance.md` to reduce ambiguity.
- **Impact**: Makes the nuanced semantics discoverable directly from code, lowering onboarding time for gameplay engineers comparing trees.

### 6. Ensure tests and doc generators run in CI (Correctness, Maintainability)

- **Problem**: All workflows under `.github/workflows/` run only formatting, lint, npm publish, or tooling updates—none execute Unity/NUnit tests or the doc-sync scripts. `package.json` even sets `npm test` to a placeholder (`package.json:32-70`). As a result, regressions in 4k+ tests or mismatched performance docs go unnoticed until manual runs.
- **Actions**:
  1. Add a `unity-tests.yml` workflow that restores dotnet tools, runs `npm run lint:tests`, and invokes Unity Test Runner for EditMode + PlayMode using a lightweight project that references this package.
  2. Integrate `RandomPerformanceTests` / `SpatialTreePerformanceTests` in a nightly job that regenerates the markdown under `Docs/performance/*` and fails if git diff is non-empty.
  3. Teach `validate:content` (or a new script) to verify there are no orphaned `.tmp` or scratch files (see Priority 2) so CI stays reproducible.
- **Impact**: Provides the correctness signal the README promises, keeps benchmark docs trustworthy, and prevents accidental publish of untested changes.

### 7. Finish and integrate the Data Visualizer editor tool (Ease-of-Use, Maintainability)

- **Problem**: `temp_dv.cs` is an 8k+ line EditorWindow living at the repo root, outside any asmdef, referencing undefined types like `DataVisualizerSettings` (`temp_dv.cs:42-8337`). Unity never compiles this file inside the package folders, so none of this UI ships, yet it’s tracked with TODOs ("TODO: MIGRATE ALL STYLES TO USS + SPLIT STYLE SHEETS") and leaks internal UX guidelines.
- **Actions**:
  1. Move the feature under `Editor/DataVisualizer/` with its own asmdef and split the monolith into feature-specific partial classes (layout, state, persistence, inspectors).
  2. Add the missing ScriptableObjects/serializable state types (settings, user state) or delete the dead code if it’s no longer planned.
  3. Cover the window with UI Toolkit tests (UIToolkit Test Runner) so regressions are caught, and ensure styles live in `.uss` files per TODO references at `temp_dv.cs:3539, 4403, 7612`.
- **Impact**: Either ships a polished tool (aligning with README claims) or removes 300k+ bytes of confusing dead code, making the repo easier to navigate.

## Priority 2 — Maintainability & Developer Experience

### 8. Clean repository artifacts (`*.tmp`, stale scratch files)

- **Problem**: Files like `photonspin.c.tmp` (captured HTTP response headers) and `runtime_patch.tmp` live at the repo root, polluting npm releases and confusing contributors.
- **Actions**: Delete the artifacts, add the relevant patterns (`*.tmp`, `temp_*.cs`) to `.gitignore`, and extend `scripts/check-eol.ps1` or a new hygiene script to fail CI when such files appear.
- **Impact**: Keeps packages slim and avoids accidental disclosure of tool output or credentials.

### 9. Share a single `CommonTestBase` implementation (Maintainability)

- **Problem**: There are two nearly identical base classes for tests (`Tests/Runtime/TestUtils/CommonTestBase.cs` and `Tests/Editor/Utils/CommonTestBase.cs`). They already drift in behavior (scene tracking vs async cleanup), so fixes must be duplicated manually.
- **Actions**: Extract the shared logic into a test utility assembly referenced by both editor and runtime asmdefs, keep editor-only APIs behind `#if UNITY_EDITOR`, and update `scripts/lint-tests.ps1` to reference the shared location.
- **Impact**: Reduces duplicated maintenance, ensures lifecycle fixes (e.g., scene cleanup) apply to all tests, and simplifies documentation.

### 10. Modernize spatial query APIs for friendlier consumption (Ease-of-Use, Performance)

- **Problem**: Every spatial query requires the caller to provide a `List<T>` that the method immediately `Clear()`s (`Runtime/Core/DataStructure/KDTree3D.cs:581-715`, similar in other trees). This mutates caller state unexpectedly, prevents stackalloc/Span-based usage, and forces heap traffic for one-off queries.
- **Actions**:
  1. Add overloads that return pooled `PooledList<T>` handles or expose `IEnumerable<T>`/`IReadOnlyList<T>` results, leaving the existing mutable API for backwards compatibility.
  2. Provide `TryFill(Span<T> buffer, out int written)` variants for Burst/jobs scenarios.
  3. Update docs and samples to highlight the zero-allocation patterns so users don’t learn the hard way that their list contents get wiped.
- **Impact**: Improves ergonomics, enables high-performance jobs/Burst scenarios, and removes a common footgun where user-managed lists silently lose their contents.

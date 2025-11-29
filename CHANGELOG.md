# Changelog

All notable changes to Unity Helpers will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

See [the roadmap](docs/overview/roadmap.md) for details

## [2.2.0]

### Added

- **Auto-Load Singleton System**: New singleton pattern with configurable lifetimes and thread-safe execution
  - `UnityMainThreadGuard` for ensuring operations run on the main thread
  - `UnityMainThreadDispatcher` with configurable lifecycle management
  - Reworked auto-load singleton architecture for better scene persistence
- **Inspector Attributes & Drawers**: Comprehensive custom inspector tooling
  - `WGroup` attribute for visual grouping of inspector properties, including collapsible sections and palette-driven styling
  - `WButton` attribute with support for async/Task methods and custom styling
  - `EnumToggleButtons` for toggle-based enum selection in inspector
  - `WShowIf` conditional display attribute improvements
  - Enhanced dropdown attributes for better property selection
- **Serialization Data Structures**: Production-ready serializable collections
  - `SerializableDictionary<TKey, TValue>` with custom inspector drawer
  - `SerializableSortedDictionary<TKey, TValue>` with ordered iteration
  - `SerializableHashSet<T>` with custom set drawer and duplicate detection
  - `SerializableNullable<T>` for nullable value types in inspector
  - `SerializableType` for type references in inspector
  - Pagination support for large collections in editor
  - Inline nested editor support for complex types
- **Editor Tooling Enhancements**:
  - Enhanced `StringInListDrawer` for validated string input with suggestions
  - UI Toolkit-based editors for modern Unity editor integration
  - Configurable settings windows with improved layout and styling
  - Move up/down buttons for reordering collection elements
  - Add/remove buttons with improved visual styling
  - Add `Request Script Recompilation` which will automatically recompile any script changes
  - The “Request Script Compilation” utility now ships with a Unity Shortcut Manager binding (default **Ctrl/Cmd + Alt + R**) so you can trigger it without touching the menu. The shortcut appears under _Wallstop Studios / Request Script Compilation_ and can be remapped like any other Unity shortcut. The existing menu item remains at `Tools ▸ Wallstop Studios ▸ Unity Helpers`.
  - Coroutine wait buffer defaults can now be configured under **Project Settings ▸ Wallstop Studios ▸ Unity Helpers**. The generated `Resources/WallstopStudios/UnityHelpers/UnityHelpersBufferSettings.asset` applies the selected quantization, entry caps, and LRU mode automatically on domain reload or when the player starts (unless your code overrides the values at runtime).
  - Random and Performance runtime test suites now live in their own PlayMode assemblies, keeping namespaces tidy while still wiring markdown benchmark output to the existing docs.
- **Random Number Generation**: Extended PRNG capabilities
  - Additional random sampling methods with statistical improvements
- **Grid Concave Hull Reliability**:
  - Edge-split and grid KNN hull builders now insert missing axis-aligned corners after the initial pass, guaranteeing concave stair, horseshoe, and serpentine inputs retain their interior vertices even when only sparse samples exist.
  - New regression-focused tests (`UnityExtensionsGridConcaveHullTests`) cover staircase fallback, axis-corner preservation, and diagonal-only rejection to guard against future regressions.

### Fixed

- **Random Number Generation**: Critical edge case handling
  - Fixed poor handling of `NextFloat()` and `NextDouble()` potentially returning exactly `0.0` or `1.0` in extensions and helpers
  - Fixed sampling bias in `NextUlong()` for more uniform distribution
  - Ensured proper range handling for all random generation methods
- **IllusionFlow Random**: Serialization and performance issues
  - Fixed deserialization bugs in `IllusionFlow` components
  - Optimized to reduce GC churn during effect processing
- **Editor & Inspector**: Multiple rendering and caching bugs
  - Fixed stale label caching causing incorrect inspector display
  - Fixed test assembly identification for proper test organization
  - Fixed scene loading edge cases in editor workflows
- **Component System**: Runtime component query issues
  - Fixed `GetComponents` returning null arrays in some cases
  - Fixed jitter-related bugs in component updates
- **Extension Methods**: Mathematical edge cases
  - Fixed calculations with zero or negative areas (bounds, rectangles, circles)
  - Fixed color averaging bugs in color extension methods
- **Geometry & Spatial**: Convex hull computation
  - Fixed convex hull behavior for edge cases (collinear points, degenerate cases)
  - Improved hull computation accuracy and performance
- **GUID Generation**: Specification compliance
  - Fixed GUID v4 generation to properly set version and variant bits per RFC 4122
- **Editor Settings**: Project settings and drawer issues
  - Fixed obsolete API usage in editor code
  - Fixed project settings panel rendering issues
  - Fixed reflection-based property access for better performance
- **Scriptable Object Singletons**: Duplicate folders should no longer be created
  - Fixed a "should-never-happen" bug where, if a singleton was accessed for the first time off the main thread, it would never be able to be accessed for the lifetime of the process
  - Fixed a bug where auto-creation would happen concurrently with AssetDatabase importing, resulting in Unity crashing with no error message

### Improved

- **Performance Optimizations**:
  - Reduced reflection usage in custom property drawers (10-100x faster in some cases)
  - Optimized list navigation and caching for large collections
  - Faster indexing and lookup in serializable data structures
  - Improved drawer update performance for complex inspector hierarchies
  - Data structure conversion optimizations
- **Code Quality**:
  - Extensive test coverage additions (4,000+ total tests)
  - Deterministic random number generation tests
  - Better test organization and cleanup
  - Editor-specific test improvements
- **Documentation**:
  - Major documentation refactor for clarity
  - Added GUID generation documentation
  - Improved inline code documentation
  - Better attribute usage examples

### Changed

- **Breaking Changes**:
  - Removed `KVector2` (deprecated, use Unity's built-in Vector2)
  - Renamed `KGuid` -> `WGuid`, changed data layout
  - Forced `WallstopFastArrayPool` to force `unmanaged` types. This pool does not clear arrays and can leak references.
  - The legacy line-division concave hull overload `BuildConcaveHull(IEnumerable<FastVector3Int>, Grid, float scaleFactor, float concavity)` has been marked `[Obsolete]` and now throws `NotSupportedException`. Use `ConcaveHullStrategy.Knn` or `ConcaveHullStrategy.EdgeSplit` (and their dedicated helpers) instead; the docs now call out this retirement explicitly.
- **API Improvements**:
  - Simplified `TryAdd` methods for collections
  - Enforced `IComparable` constraint where appropriate for sorting
  - Better handling of null additions in collections
- **Editor Workflow**:
  - Auto-run CSharpier on code changes for consistent formatting
  - Updated editor tooling for better integration with Unity 2021.3+

### Technical Notes

- **Architecture**: Moved many editor drawers to dedicated namespace for better organization
- **Reflection**: Significant reduction in reflection calls for improved editor performance
- **Threading**: Enhanced thread safety with `UnityMainThreadGuard` and `UnityMainThreadDispatcher`
- **Memory**: Reduced GC allocations in hot paths (IllusionFlow, spatial queries, random generation)

---

## [2.0.0]

- Deprecate BinaryFormatter with `[Obsolete]`, keep functional for trusted/legacy scenarios.
- Make GameObject JSON converter output structured JSON with `name`, `type`, and `instanceId`.
- Fix stray `UnityEditor` imports in Runtime to ensure clean player builds.

---

## [1.x]

- See commit history for incremental features (random engines, spatial trees, serialization converters, editor tools).

# Changelog

All notable changes to Unity Helpers will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

See [the roadmap](./docs/overview/roadmap.md) for details

## [3.1.1]

### Fixed
- WInLineEditorOdinDrawer not compiles.

## [3.1.0]

### Added

- **Pool Access Frequency Tracking**: Intelligent purge decisions based on pool usage patterns
- **Memory Pressure Detection**: Proactive memory monitoring for intelligent pool purging
- **Cross-Pool Global Memory Budget**: Prevents aggregate memory bloat across all pools
- **Size-Aware Purge Policies**: Large objects (above LOH threshold) get stricter purge policies
  - `WallstopGenericPool<T>` automatically uses size-aware options during construction
- **SpriteSheetExtractor**: New editor tool for extracting individual sprites from sprite sheet textures.
  - This is an ALPHA feature, much functionality is currently broken.
- **Cache Data Structure**: New high-performance, configurable `Cache<TKey, TValue>` with fluent builder API
  - Multiple eviction policies: LRU, Segmented LRU (SLRU), LFU, FIFO, and Random
  - Time-based expiration with `ExpireAfterWrite` and `ExpireAfterAccess`
  - Weight-based sizing for entries of varying cost
  - Dynamic growth with configurable thrash detection
  - Loading cache support with `GetOrAdd` and custom loader functions
  - Thread-safe by default (single-threaded mode via `SINGLE_THREADED` define)
  - Eviction, get, and set callbacks for monitoring cache behavior
  - Statistics tracking with hit/miss counts
- **CachePresets**: Factory methods for creating pre-configured caches optimized for common gamedev scenarios
- **AnimationCreator Variable Framerate**: AnimationCreatorWindow now supports variable framerate animations using AnimationCurve
  - New `FramerateMode` enum (`Constant` or `Curve`) for choosing timing mode
  - Per-animation `framesPerSecondCurve` allows custom timing across animation progress
  - Curve presets: Flat, Ease In, Ease Out, and Sync with constant FPS
  - Frame timing preview shows per-frame durations before generation
- **AnimationCreator Live Preview**: Real-time animation preview panel
  - Play/pause/stop transport controls for preview playback
  - Frame scrubber for manual frame navigation
  - Respects variable framerate curves during preview
  - Shows current frame index and FPS in preview panel
- **AnimationData Cycle Offset**: New `cycleOffset` property (0-1) sets animation loop start point
- **Pool Auto-Purging**: `WallstopGenericPool<T>` now supports configurable auto-purging and eviction
  - New `PoolOptions<T>` class for configuring pool behavior at construction
  - `MaxPoolSize` limits pool capacity with automatic eviction of excess items
  - `IdleTimeoutSeconds` purges items that have been idle too long
  - `PurgeTrigger` flags control when purging occurs: `OnRent`, `OnReturn`, `Periodic`, or `Explicit`
  - `OnPurge` callback with `PurgeReason` (IdleTimeout, CapacityExceeded, Explicit) for monitoring
  - Intelligent purging mode tracks usage patterns to avoid purge-allocate cycles
  - `MinRetainCount` ensures a minimum number of items are always kept
- **Application Lifecycle Hooks for Pool Purging**: Automatic pool purging in response to system events
  - `Application.lowMemory` triggers emergency purge (ignores hysteresis, purges to `MinRetainCount`)
  - `Application.focusChanged` triggers purge when app backgrounds (mobile platforms)
  - New `PurgeReason` values: `MemoryPressure`, `AppBackgrounded`, `SceneUnloaded` (reserved)
  - Configurable via `PoolPurgeSettings.PurgeOnLowMemory` and `PoolPurgeSettings.PurgeOnAppBackground`
  - `GlobalPoolRegistry` tracks all pool instances for cross-pool operations
  - `PoolPurgeSettings.PurgeAllPools()` method for manual global purge
  - Lifecycle hooks automatically registered via `RuntimeInitializeOnLoadMethod`
- **RandomExtensions `NextOfExcept`**: New extension methods for selecting random elements with exclusions
  - `NextOfExcept(values)` - no exclusions (convenience overload)
  - `NextOfExcept(values, exception1...)` - exclude values
  - Zero-allocation using pooled collections internally

### Changed

- **BREAKING:** Pool purging now enabled by default with conservative settings
  - `GlobalEnabled` defaults to `true` (was `false`)
  - `DefaultBufferMultiplier` defaults to `2.0` (was `1.5`)
  - `DefaultHysteresisSeconds` defaults to `120` (was `60`)
  - `DefaultSpikeThresholdMultiplier` defaults to `2.5` (was `2.0`)
  - Use `PoolPurgeSettings.DisableGlobally()` to restore previous behavior
  - `UnityMainThreadDispatcher` auto-load behavior has changed from auto-loading to not auto-loading.
  - `UnityMainThreadDispatcher` hide flags have been changed to `None`.

- **DictionaryExtensions `ToDictionary`**: Now uses last-wins semantics for duplicate keys instead of throwing `ArgumentException`
  - Aligns with common dictionary initialization patterns
  - Applies to both `KeyValuePair<K,V>` and tuple `(K, V)` overloads

- **IEnumerableExtensions return types**: `OrderBy`, `Ordered`, and `Shuffled` methods now return `List<T>` instead of `IEnumerable<T>` for improved usability (indexable, known count)
  - **Note**: These methods now use eager evaluation (execute immediately) instead of deferred evaluation
  - Source code remains compatible—`List<T>` is assignable to `IEnumerable<T>`

### Improved

- **LRU cache eviction**: Bounded editor caches now use LRU (Least Recently Used) eviction instead of FIFO
  - Frequently-accessed cache entries are retained longer, improving cache hit rates
  - Both reads and writes update an item's "recency", preventing hot items from being evicted
  - Affects `EditorCacheHelper.AddToBoundedCache` and new `TryGetFromBoundedLRUCache` method
  - Applied to `InLineEditorShared`, `WShowIfPropertyDrawer`, and other bounded editor caches
- **Shuffled performance**: `IEnumerableExtensions.Shuffled` now uses O(n) Fisher-Yates shuffle instead of O(n log n) sort-based approach
- **LINQ elimination**: Removed LINQ usage across runtime code for reduced allocations and improved performance
  - Affects `Trie`, `Geometry`, `Serializer`, `ValidateAssignmentAttribute`, `WShowIfAttribute`, relational component attributes, and more
  - Uses pooled collections and explicit loops instead of LINQ methods
  - Zero-allocation patterns applied throughout
- **GlobalPoolRegistry.EnforceBudget() zero-allocation**: Replaced per-call `List<IPoolStatistics>` allocation with static reusable list protected by existing lock

### Fixed

- **Cache pre-allocation OutOfMemoryException**: Fixed production bug where `Cache<TKey, TValue>` would pre-allocate internal storage to `MaximumSize` instead of using a small initial capacity
  - Creating a cache with `MaximumSize = int.MaxValue` now works correctly instead of throwing `OutOfMemoryException`
  - New `InitialCapacity` option allows explicit control over starting allocation size (default 16)
  - Cache grows dynamically from `InitialCapacity` toward `MaximumSize` as items are added
  - `CacheBuilder<TKey, TValue>.InitialCapacity(int)` method for fluent configuration
  - `Cache<TKey, TValue>.MaximumSize` property added to expose configured maximum (distinct from `Capacity`)
  - Large `InitialCapacity` values are clamped to `MaxReasonableInitialCapacity` (65536) to prevent excessive allocations
- **Pool MinRetainCount not respected during gradual explicit purges**: Fixed `MinRetainCount` being ignored when using `MaxPurgesPerOperation` with explicit purges
  - Gradual purges now correctly stop when pool size reaches `MinRetainCount`
  - Added `_pool.Count > effectiveMinRetain` check to the purge loop condition in both thread-safe and non-thread-safe pool implementations
- **Pool idle timeout purges blocked by comfortable size**: Fixed idle timeout purges not occurring when pool size was at or below comfortable size
  - Idle timeout purges now proceed regardless of comfortable size, as they represent essential pool hygiene
  - Added `hasIdleTimeout` to loop entry condition to allow idle timeout evaluation independent of size
- **Pool hysteresis incorrectly blocking idle timeout purges**: Fixed hysteresis protection blocking all purge types including idle timeout
  - Idle timeout purges now proceed during hysteresis since they only remove items unused for extended periods
  - Capacity and explicit purges remain blocked during hysteresis to prevent thrashing
- **ScriptableObjectSingletonCreator race condition creating numbered duplicate folders**: Fixed race condition where parallel operations could cause Unity to create numbered duplicate folders like "Resources 1", "Resources 2", etc.
  - Added detection for Unity's numbered duplicate folder creation pattern
  - Automatically deletes duplicate folders and uses the intended folder path
  - Logs warning if duplicate folder deletion fails, alerting user to manual cleanup needed

## [3.0.5]

### Added

- **GitHub Pages Support**: All documentation is now available via a pretty [GitHub Pages](https://wallstop.github.io/unity-helpers/)
- **GitHub Wiki Support**: All documentation is now available via a less pretty [GitHub Wiki](https://github.com/wallstop/unity-helpers/wiki)
- **Comprehensive Odin Inspector Attribute Support**: All Unity Helpers inspector attributes now work seamlessly with Odin Inspector's `SerializedMonoBehaviour` and `SerializedScriptableObject` types
  - **`[WButton]`**: Full support including grouping, placement, history, async methods, and parameters
  - **`[WShowIf]`**: Conditional property display based on field values, methods, or comparisons
  - **`[WReadOnly]`**: Disables editing while preserving display in Odin inspectors
  - **`[WEnumToggleButtons]`**: Toggle button UI for enum selection with flags support
  - **`[WValueDropDown]`**: Dropdown selection from custom value lists
  - **`[WInLineEditor]`**: Inline editing of referenced ScriptableObjects and components
  - **`[WNotNull]`**: Null reference validation with HelpBox warnings/errors
  - **`[ValidateAssignment]`**: Field validation for null, empty strings, and empty collections
  - **`[StringInList]`**: String selection from predefined lists or method providers
  - **`[IntDropDown]`**: Integer selection from predefined value lists
  - No setup required — attributes work identically whether Odin Inspector is installed or not
  - Custom Odin drawers registered when `ODIN_INSPECTOR` symbol is defined
- **WButton Custom Editor Integration**: New `WButtonEditorHelper` class for integrating WButton functionality into custom editors
  - Only needed when creating custom `OdinEditor` subclasses for specific types
  - Provides simple API for any custom editor to draw WButton methods
  - Methods: `DrawButtonsAtTop()`, `DrawButtonsAtBottom()`, `ProcessInvocations()`, and convenience methods
  - Documented integration patterns for both Odin Inspector and standard Unity custom editors

### Fixed

- **Sprite Sheet Auto-Detection Preferring Non-Transparent Boundaries**: Fixed an issue where the "Auto Best" algorithm and other detection methods could select grid boundaries that pass through non-transparent pixels when transparent alternatives existed
  - Changed scoring system from linear to non-linear, heavily favoring fully transparent grid lines (10x higher score) over partially transparent ones
  - Adjusted boundary comparison to only prefer alternatives when transparency score differs by more than 5%, preventing minor variations from overriding better transparent boundaries
  - When scores are similar, the algorithm now prefers divisors closer to the originally detected cell size
  - This fix affects `ScoreDivisorByTransparency`, `ScoreCellSizeForDimension`, and `FindBestTransparencyAlignedDivisor` methods
- **Manual Recompile Silent Failure After Build**: Fixed an issue where the "Request Script Recompilation" menu item and shortcut would stop responding after building a project (particularly on Linux)
  - Added defensive null check in compilation pending evaluator to prevent silent `NullReferenceException`
  - The null evaluator scenario could occur when static field initialization failed or was corrupted during build operations without a domain reload

## [3.0.4]

### Fixed

- Documentation only (`WGroupEnd` examples)

## [3.0.3]

### Fixed

- Fix packaging issue related to rsp files

## [3.0.2]

### Fixed

- Fix packaging issue related to Styles/Elements/Progress.meta file

## [3.0.1]

### Fixed

- Updated `package.json` to be OpenUPM-compatible

## [3.0.0]

### Added

- **llms.txt**: Added `llms.txt` file following the [llmstxt.org](https://llmstxt.org/) specification for LLM-friendly documentation
  - Provides a structured overview of package features, APIs, and documentation links for AI assistants
  - Enables third-party LLMs to quickly understand and work with the Unity Helpers codebase
- **Auto-Load Singleton System**: New singleton pattern with configurable lifetimes and thread-safe execution
  - `UnityMainThreadGuard` for ensuring operations run on the main thread
  - `UnityMainThreadDispatcher` with configurable lifecycle management
  - `AutoLoadSingletonAttribute` for automatic singleton instantiation during Unity start-up phases
  - Reworked the autoload singleton architecture for better scene persistence
- **Asset Change Detection**: Monitor asset changes with `DetectAssetChangedAttribute`
  - Annotate methods to automatically execute when specific asset types are created or deleted
  - Support for inheritance with `IncludeAssignableTypes` option
  - Automatic registration and callback execution via asset processor
- **Inspector Attributes & Drawers**: Comprehensive custom inspector tooling
  - `WGroup` attribute for visual grouping of inspector properties, including collapsible sections and palette-driven styling
  - `WButton` attribute with support for async/Task methods and custom styling
  - `WEnumToggleButtons` attribute for toggle-based enum selection in inspector
  - `WShowIf` conditional display attribute improvements
  - Enhanced dropdown attributes for better property selection
- **Inspector Validation Attributes**: Enhanced inspector feedback for null/invalid field detection
  - `WNotNullAttribute` now displays a warning or error HelpBox in the inspector when the field is null
  - `WNotNullAttribute` new properties: `MessageType` (Warning/Error enum) and `CustomMessage` (string) for customizable feedback
  - `WNotNullAttribute` new constructor overloads for easy customization of message type and custom messages
  - New `WNotNullPropertyDrawer` for rendering validation feedback in the inspector
  - `ValidateAssignmentAttribute` now displays a warning or error HelpBox in the inspector when the field is invalid (null, empty string, or empty collection)
  - `ValidateAssignmentAttribute` new properties: `MessageType` (Warning/Error enum) and `CustomMessage` (string) for customizable feedback
  - `ValidateAssignmentAttribute` new constructor overloads for easy customization of message type and custom messages
  - New `ValidateAssignmentPropertyDrawer` for rendering validation feedback in the inspector
  - Both attributes maintain full backward compatibility—existing code works unchanged with default warning messages
  - `StringInListAttribute` now supports `[StringInList(nameof(Method))]` to call parameterless instance or static methods on the decorated object, and the drawer exposes the same experience in both IMGUI and UI Toolkit inspectors
  - `WButton` now supports `groupPriority` and `groupPlacement` parameters for fine-grained control over button group ordering and positioning
- **Serialization Data Structures**: Production-ready serializable collections
  - `SerializableDictionary<TKey, TValue>` with custom inspector drawer
  - `SerializableSortedDictionary<TKey, TValue>` with ordered iteration
  - `SerializableHashSet<T>` with custom set drawer and duplicate detection
  - `SerializableSortedSet<T>` for sorted sets with `IComparable<T>` elements
  - `SerializableNullable<T>` for nullable value types in inspector
  - `SerializableType` for type references in inspector
  - Pagination support for large collections in the Editor
  - Inline nested editor support for complex types
  - Undo/Redo support for all serializable collection modifications
  - Confirmation dialog when clearing collections to prevent accidental data loss
- **Editor Tooling Enhancements**:
  - Enhanced `StringInListDrawer` for validated string input with suggestions
  - UI Toolkit-based editors for modern Unity editor integration
  - Configurable settings windows with improved layout and styling
  - Move up/down buttons for reordering collection elements
  - Add/remove buttons with improved visual styling
  - Added **Request Script Recompilation** menu item (`Tools ▸ Wallstop Studios ▸ Unity Helpers`) to manually trigger script recompilation
  - The "Request Script Compilation" utility includes a Unity Shortcut Manager binding (default **Ctrl/Cmd + Alt + R**) for quick access. The shortcut appears under _Wallstop Studios / Request Script Compilation_ and can be remapped like any other Unity shortcut.
  - Coroutine wait buffer defaults can now be configured under **Project Settings ▸ Wallstop Studios ▸ Unity Helpers**. The generated `Resources/WallstopStudios/UnityHelpers/UnityHelpersBufferSettings.asset` applies the selected quantization, entry caps, and LRU mode automatically on domain reload or when the player starts (unless your code overrides the values at runtime).
  - Added **Unity Method Analyzer** (`Tools ▸ Wallstop Studios ▸ Unity Helpers ▸ Unity Method Analyzer`) for detecting inheritance issues and Unity lifecycle method errors across C# codebases
- **Random Number Generation**: Extended PRNG capabilities
  - Added `BlastCircuitRandom` and `WaveSplatRandom` generators with improved performance characteristics
  - New `RandomGeneratorMetadata` system for inspecting generator properties
  - Extended random sampling methods with improved statistical distribution
- **Array Pooling**: New `SystemArrayPool<T>` and unified `PooledArray<T>` return type
  - Added `SystemArrayPool<T>` wrapping `System.Buffers.ArrayPool<T>.Shared` for variable-sized allocations
  - Added `PooledArray<T>` struct as unified return type for all array pools with proper `Length` tracking
  - `WallstopArrayPool<T>` and `WallstopFastArrayPool<T>` now return `PooledArray<T>` instead of `PooledResource<T[]>`
  - Critical for `SystemArrayPool<T>`: returned arrays may be larger than requested; always use `pooled.Length`, not `array.Length`
- **Grid Concave Hull Reliability**:
  - Edge-split and grid KNN hull builders now insert missing axis-aligned corners after the initial pass, guaranteeing concave stair, horseshoe, and serpentine inputs retain their interior vertices even when only sparse samples exist.
  - Improved handling of staircase patterns, axis-corner preservation, and diagonal-only rejection for more robust hull generation.

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
  - Minor relational component performance improvements, specifically for children components
  - Reduced GC allocations across property drawers, editor tools, and various helpers
- **EnhancedImage Visual Component**:
  - Improved material instance management with proper cleanup OnDestroy
  - Better domain reload handling for HDR color and material state persistence
  - Enhanced editor inspector with automatic material fix suggestions
- **Animation Editor Tools**:
  - Fixed FPS field handling in Animation Viewer and Sprite Sheet Animation Creator
  - Improved frame reordering and preview responsiveness
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
  - `WallstopArrayPool<T>` and `WallstopFastArrayPool<T>` now return `PooledArray<T>` instead of `PooledResource<T[]>`. Update usages from `pooled.resource` to `pooled.Array` and consider using `pooled.Length` for iteration bounds.
  - The legacy line-division concave hull overload `BuildConcaveHull(IEnumerable<FastVector3Int>, Grid, float scaleFactor, float concavity)` has been marked `[Obsolete]` and now throws `NotSupportedException`. Use `ConcaveHullStrategy.Knn` or `ConcaveHullStrategy.EdgeSplit` (and their dedicated helpers) instead; the docs now call out this retirement explicitly.
  - `StringInList` inspectors now keep the property row single-line and open a dedicated popup that contains search, pagination, and keyboard navigation for large catalogs (applies to both IMGUI and UI Toolkit drawers, including `SerializableType`).
- **API Improvements**:
  - Simplified `TryAdd` methods for collections
  - Enforced `IComparable` constraint where appropriate for sorting
  - Better handling of null additions in collections
  - Updated editor tooling for better integration with Unity 2021.3+
  - Default IList.Sort to Grail sort for stability and improved performance
- **Documentation**:
  - Updated documentation to reflect new features and API changes
  - Re-organized documentation into a more logical structure
  - Consolidated documentation naming around kebab-case

---

## [2.0.0]

- Deprecate BinaryFormatter with `[Obsolete]`, keep functional for trusted/legacy scenarios.
- Make GameObject JSON converter output structured JSON with `name`, `type`, and `instanceId`.
- Fix stray `UnityEditor` imports in Runtime to ensure clean player builds.

---

## [1.x]

- See commit history for incremental features (random engines, spatial trees, serialization converters, editor tools).

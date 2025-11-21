# Changelog

## 2.1.4

### 🎨 Inspector Tooling (NEW)

Professional inspector attributes rivaling Odin Inspector - completely free:

- **`[WButton]`** - Add method buttons to inspector with full parameter support, async/Task execution, editor-time invocation, play mode control, and coroutine scheduling
- **`[WGroup]` / `[WGroupEnd]`** - Visual grouping with box styling, dynamic layout, indent control, and multi-column support
- **`[WFoldoutGroup]` / `[WFoldoutGroupEnd]`** - Collapsible sections with persistent state, nesting support, and custom styling
- **`[WShowIf]`** - Conditional field display based on other field values (enum, bool, numeric comparisons)
- **`[WEnumToggleButtons]`** - Display enums as toggle button grids instead of dropdowns
- **Settings System** - `Project Settings > Wallstop Studios > Unity Helpers` for global configuration via Project Settings
- Comprehensive documentation in `Docs/INSPECTOR_*.md` guides

### 💾 Serializable Collections (NEW)

Unity-native serializable generic collections with custom property drawers:

- **`SerializableDictionary<TKey, TValue>`** - Full dictionary with visual editor, duplicate key handling, search/filter, pagination, and collapsible entries
- **`SerializableHashSet<T>`** - Hash set with custom drawer, duplicate detection, and order preservation for inspector display
- **`SerializableSortedDictionary<TKey, TValue>`** - Sorted dictionary maintaining key order
- **`SerializableSortedSet<T>`** - Sorted set with custom drawer
- **SerializableSet "New Entry" foldouts** - Hash sets and sorted sets now include a "New Entry" foldout with configurable tweening (Project Settings ▸ Unity Helpers ▸ Set Foldouts) so you can stage complex values before adding them
- **`SerializableNullable<T>`** - Nullable value types in inspector
- **`SerializableType`** - Type references with assembly-qualified names, type filtering, and custom drawer
- All collections support nested types, complex value types, and proper Unity serialization

### 🎲 Random Number Generators

- Add **`BlastCircuitRandom`** - New high-performance PRNG algorithm
- Add **`WaveSplatRandom`** - New PRNG with excellent statistical properties
- Add `RandomGeneratorMetadata` for algorithm classification and performance characteristics
- Add `RandomSpeedBucket` enum for performance categorization
- Enhanced `AbstractRandom` with additional utility methods
- Auto-generated performance documentation in `Docs/RANDOM_PERFORMANCE.md`

### 🔧 Editor Improvements

- **`WGuid`** - Replaces `KGuid` with improved property drawer, auto-generation, and Guid v4 RFC compliance
- **`StringInListAttribute`** - Replaces `StringInList` helper with proper attribute-based dropdown
- **`WValueDropDownAttribute`** - Dynamic dropdown from method/property values
- **`IntDropdownAttribute`** - Enhanced with `DropdownValueProvider` support
- **Custom Property Drawers** - High-performance drawers for all serializable types with visual regression tests
- **Editor Utilities** - `GroupGUIIndentUtility`, `GroupGUIWidthUtility`, and WButton subsystem
- **Visual Regression Testing** - Framework for testing custom property drawers

### 📚 Documentation

- Add comprehensive inspector attribute guides:
  - `INSPECTOR_OVERVIEW.md` - Feature overview and quick start
  - `INSPECTOR_BUTTON.md` - WButton attribute deep dive
  - `INSPECTOR_GROUPING_ATTRIBUTES.md` - WGroup and WFoldoutGroup usage
  - `INSPECTOR_CONDITIONAL_DISPLAY.md` - WShowIf patterns
  - `INSPECTOR_SELECTION_ATTRIBUTES.md` - WEnumToggleButtons and dropdowns
  - `INSPECTOR_SETTINGS.md` - Settings system configuration
- Add `SERIALIZATION_TYPES.md` - Complete serializable types reference
- Add `ROADMAP.md` - Planned features and priorities
- Add `THIRD_PARTY_NOTICES.md` - License attributions
- Update `INDEX.md` with new features
- Enhanced `README.md` with feature highlights and badges

### 🧪 Testing

- 15,000+ lines of new tests for inspector attributes and serializable types
- Custom drawer visual regression test framework
- Comprehensive coverage for WButton, WGroup, WShowIf, and all serializable collections
- Editor test infrastructure improvements

### 🔨 Tooling & Infrastructure

- PowerShell scripts for pre-commit formatting (C#, Prettier, Markdown linting)
- Enhanced `.githooks/pre-commit` with staged file formatting
- Updated `.pre-commit-config.yaml` with latest tool versions
- `AGENTS.md` for LLM-assisted development guidelines

### ⚡ Performance

- Optimized reflection usage in property drawers (reduced runtime overhead)
- Cached metadata for WButton invocation
- Improved property drawer layout calculations
- Better serialization performance for collections

### 🛠️ Improvements

- Enhanced `ReflectionHelpers.Factory` with better type instantiation
- Add `Objects.cs` helper methods for common object operations
- Improved `WShowIfPropertyDrawer` with better condition evaluation
- Better `AnimationEventEditor` integration
- Enhanced `PrefabChecker` functionality

### 🔄 Refactoring

- Rename `KGuid` → `WGuid` (maintains meta file compatibility)
- Remove `KVector2` (use Unity's Vector2)
- Remove `KSerializableAttribute` (obsolete)
- Remove `StringInList` helper (replaced by `StringInListAttribute`)
- Remove `StringInListeDrawer` (typo, replaced by `StringInListDrawer`)
- Remove `KVector2Converter` (no longer needed)
- Consolidate serializable type drawers into dedicated namespace

### 🐛 Bug Fixes

- Fix Guid v4 generation to comply with RFC 4122
- Fix property drawer height calculations for nested types
- Fix serialization round-trip issues for complex types
- Correct spacing and alignment in custom property drawers
- Fix SerializableSet "New Entry" string inputs so typed values persist and add operations use the entered text instead of an empty string

### 🔒 Breaking Changes

- **Removed `KGuid`** - Use `WGuid` instead (auto-converts via meta file)
- **Removed `KVector2`** - Use Unity's `Vector2`
- **Removed `StringInList`** - Use `StringInListAttribute` instead
- **Removed `KSerializableAttribute`** - No longer needed
- **Renamed property drawer classes** - Internal, affects only custom editor code

### 📦 Migration Guide

**KGuid → WGuid:**

```csharp
// Before
[SerializeField] private KGuid _id;

// After
[SerializeField] private WGuid _id;  // Meta files preserved, no data loss
```

**StringInList → StringInListAttribute:**

No change, C# automatically omits `Attribute`.

**KVector2 Removal:**

```csharp
// Before
[SerializeField] private KVector2 _position;

// After
[SerializeField] private Vector2 _position;  // Use Unity's Vector2
```

---

## 2.0.0

- Deprecate BinaryFormatter with `[Obsolete]`, keep functional for trusted/legacy scenarios.
- Make GameObject JSON converter output structured JSON with `name`, `type`, and `instanceId`.
- Fix stray `UnityEditor` imports in Runtime to ensure clean player builds.

## 1.x

- See commit history for incremental features (random engines, spatial trees, serialization converters, editor tools).

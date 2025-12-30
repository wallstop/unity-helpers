# LLM Agent Instructions

This document provides guidelines for AI assistants working with this repository. Procedural skills are in the [skills/](./skills/) directory.

---

## Repository Overview

**Package**: `com.wallstop-studios.unity-helpers`
**Version**: 3.0.0
**Unity Version**: 2021.3+ (LTS)
**License**: MIT
**Repository**: <https://github.com/wallstop/unity-helpers>
**Root Namespace**: `WallstopStudios.UnityHelpers`

### Package Goals

Unity Helpers eliminates repetitive boilerplate code with production-ready utilities that are 10-100x faster than writing from scratch. It provides:

- **Professional Inspector Tooling** — Odin Inspector-level features for free (grouping, buttons, conditional display, toggle grids)
- **Zero-Boilerplate Component Wiring** — Auto-wire components using hierarchy-aware attributes
- **High-Performance Utilities** — 10-15x faster PRNGs, O(log n) spatial queries, zero-allocation pooling
- **Production-Ready Serialization** — Unity-aware JSON/Protobuf with schema evolution
- **Data-Driven Effects System** — Designer-friendly buffs/debuffs as ScriptableObjects
- **20+ Editor Tools** — Automate sprite, animation, texture, and prefab workflows

### Core Design Principles

1. **Zero boilerplate** — APIs handle tedious work; expressive, self-documenting code
2. **Performance-proven** — Measurable speed improvements with benchmarks
3. **Production-ready** — 8,000+ tests, IL2CPP/WebGL compatible, shipped in commercial games
4. **DRY architecture** — Extract common patterns into lightweight, reusable abstractions
5. **Self-documenting code** — Minimal comments; rely on descriptive names and obvious call patterns

---

## Project Structure

```text
Runtime/                   # Runtime C# libraries
  Core/
    Attributes/            # Inspector & component attributes
    DataStructure/         # Spatial trees, heaps, queues, tries, cyclic buffers
    Extension/             # Extension methods for Unity types, collections, strings, math
    Helper/                # Buffers, pooling, singletons, compression, logging
    Math/                  # Math utilities, ballistics, geometry
    Model/                 # Serializable types (Dictionary, HashSet, Nullable, Type, Guid)
    OneOf/                 # Discriminated unions
    Random/                # 15+ PRNG implementations with IRandom interface
    Serialization/         # JSON/Protobuf serialization with Unity type converters
    Threading/             # Thread pools, main thread dispatcher, guards
  Tags/                    # Effects/attribute system (AttributeEffect, TagHandler, Cosmetics)
  Visuals/                 # Visual components (EnhancedImage, LayeredImage)

Editor/                    # Editor-only tooling
  CustomDrawers/           # Property drawers for all custom attributes
    Odin/                  # Odin Inspector-specific drawers (9 files)
    Utils/                 # Shared drawer utilities (planned)
  CustomEditors/           # Custom inspectors (including Odin inspectors)
  Tools/                   # Editor windows (Animation Creator, Texture tools, etc.)

Tests/
  Runtime/                 # PlayMode tests mirroring Runtime/ structure
  Editor/                  # EditMode tests mirroring Editor/ structure
    CustomDrawers/Odin/    # Odin drawer tests (9 files)
    CustomEditors/         # Inspector tests (including Odin)
    TestTypes/             # Test helper types (planned extraction)
      Odin/                # Odin-specific test SO/MB types (planned)
      SharedEnums/         # Shared test enums (planned)
  Core/                    # Shared test utilities

Samples~/                  # Sample projects (imported via Package Manager)
```

---

## Skills Reference

Invoke these skills for specific tasks:

### Core Skills (Always Consider)

| Skill                                                              | When to Use                                             |
| ------------------------------------------------------------------ | ------------------------------------------------------- |
| [high-performance-csharp](./skills/high-performance-csharp.md)     | **ALL code** (features, bugs, editor)                   |
| [defensive-programming](./skills/defensive-programming.md)         | **ALL code** (features, bugs, editor)                   |
| [create-csharp-file](./skills/create-csharp-file.md)               | Creating any new `.cs` file                             |
| [create-unity-meta](./skills/create-unity-meta.md)                 | **MANDATORY** after creating ANY new file or folder     |
| [create-test](./skills/create-test.md)                             | Writing or modifying test files                         |
| [investigate-test-failures](./skills/investigate-test-failures.md) | **ANY test failure** — investigate before fixing        |
| [update-documentation](./skills/update-documentation.md)           | **MANDATORY** after ANY feature/bug fix/API change      |
| [validate-before-commit](./skills/validate-before-commit.md)       | **MANDATORY** before completing any task (run linters!) |
| [create-enum](./skills/create-enum.md)                             | Creating a new `enum` type                              |
| [create-scriptable-object](./skills/create-scriptable-object.md)   | Creating ScriptableObject data assets                   |
| [create-editor-tool](./skills/create-editor-tool.md)               | Creating Editor windows, drawers, inspectors            |
| [format-code](./skills/format-code.md)                             | **IMMEDIATELY** after ANY C# file change (not batched!) |
| [format-non-csharp](./skills/format-non-csharp.md)                 | **IMMEDIATELY** after ANY non-C# file change (Prettier) |
| [search-codebase](./skills/search-codebase.md)                     | Finding code, files, or patterns                        |
| [git-safe-operations](./skills/git-safe-operations.md)             | Scripts or hooks that interact with git index           |
| [avoid-reflection](./skills/avoid-reflection.md)                   | **ALL code** — never reflect on our own types           |
| [avoid-magic-strings](./skills/avoid-magic-strings.md)             | **ALL code** — use nameof() not strings                 |

### Performance Skills

| Skill                                                                | When to Use                                       |
| -------------------------------------------------------------------- | ------------------------------------------------- |
| [unity-performance-patterns](./skills/unity-performance-patterns.md) | Unity-specific optimizations (APIs, pooling)      |
| [gc-architecture-unity](./skills/gc-architecture-unity.md)           | Understanding Unity GC, incremental GC, manual GC |
| [memory-allocation-traps](./skills/memory-allocation-traps.md)       | Finding hidden allocation sources                 |
| [profile-debug-performance](./skills/profile-debug-performance.md)   | Profiling, debugging, measuring performance       |
| [performance-audit](./skills/performance-audit.md)                   | Reviewing performance-sensitive code              |
| [refactor-to-zero-alloc](./skills/refactor-to-zero-alloc.md)         | Converting allocating code to zero-allocation     |
| [mobile-xr-optimization](./skills/mobile-xr-optimization.md)         | Mobile, VR/AR, 90+ FPS targets                    |
| [use-array-pool](./skills/use-array-pool.md)                         | Working with temporary arrays                     |
| [use-pooling](./skills/use-pooling.md)                               | Working with temporary collections                |

### Feature Skills

| Skill                                                                      | When to Use                                      |
| -------------------------------------------------------------------------- | ------------------------------------------------ |
| [use-prng](./skills/use-prng.md)                                           | Implementing randomization                       |
| [use-spatial-structure](./skills/use-spatial-structure.md)                 | Spatial queries or proximity logic               |
| [use-data-structures](./skills/use-data-structures.md)                     | Heaps, queues, tries, buffers, bit sets          |
| [use-serialization](./skills/use-serialization.md)                         | Save files, network, persistence                 |
| [use-serializable-types](./skills/use-serializable-types.md)               | Dictionaries, HashSets, Nullable, Type, Guid     |
| [use-effects-system](./skills/use-effects-system.md)                       | Buffs, debuffs, stat modifications               |
| [use-singleton](./skills/use-singleton.md)                                 | Global managers, service locators, configuration |
| [use-relational-attributes](./skills/use-relational-attributes.md)         | Auto-wiring components via hierarchy             |
| [use-extension-methods](./skills/use-extension-methods.md)                 | Collection, string, color utilities              |
| [use-discriminated-union](./skills/use-discriminated-union.md)             | OneOf/Result types, type-safe unions             |
| [use-threading](./skills/use-threading.md)                                 | Main thread dispatch, thread safety              |
| [add-inspector-attribute](./skills/add-inspector-attribute.md)             | Improving editor UX with attributes              |
| [debug-il2cpp](./skills/debug-il2cpp.md)                                   | IL2CPP build issues or AOT errors                |
| [integrate-optional-dependency](./skills/integrate-optional-dependency.md) | Odin, VContainer, Zenject integration patterns   |
| [github-pages](./skills/github-pages.md)                                   | GitHub Pages, Jekyll, markdown link format       |
| [wiki-generation](./skills/wiki-generation.md)                             | GitHub Wiki deployment, sidebar links            |

---

## Build & Development Commands

### Setup

```bash
npm run hooks:install      # Install git hooks
dotnet tool restore        # Restore .NET tools (CSharpier, etc.)
```

### Formatting

```bash
dotnet tool run csharpier format .   # Format C#
```

### Linting

```bash
npm run lint:spelling                          # 🚨 Spell check (run IMMEDIATELY after ANY doc/comment change)
npm run lint:docs                              # Lint documentation links
npm run lint:markdown                          # Markdownlint rules
npm run lint:yaml                              # YAML style (trailing spaces, indentation)
pwsh -NoProfile -File scripts/lint-tests.ps1   # Lint test lifecycle (Track, DestroyImmediate)
```

### Testing

Tests require Unity 2021.3+. Ask user to run tests and provide output.

---

## Naming Conventions

| Element               | Convention  | Example                     |
| --------------------- | ----------- | --------------------------- |
| Types, public members | PascalCase  | `SerializableDictionary`    |
| Fields, locals        | camelCase   | `keyValue`, `itemCount`     |
| Interfaces            | `I` prefix  | `IResolver`, `ISpatialTree` |
| Type parameters       | `T` prefix  | `TKey`, `TValue`            |
| Events                | `On` prefix | `OnValueChanged`            |
| Constants (public)    | PascalCase  | `DefaultCapacity`           |

### File Naming

- C# files: 4 spaces indentation
- Config files (`.json`, `.yaml`, `.asmdef`): 2 spaces
- Line endings: CRLF for most files; **YAML files (`.yml`, `.yaml`) use LF**; **`.github/**` files use LF** (GitHub Actions requirement); **Markdown files (`.md`) use LF** (GitHub Pages/Jekyll compatibility); **Jekyll includes (`\_includes/\*.html`) use LF\*\*
- Encoding: UTF-8 (no BOM)

---

## Critical Rules Summary

See [create-csharp-file](./skills/create-csharp-file.md) for detailed rules. Key points:

1. `using` directives INSIDE namespace
2. NO underscores in method names (including tests)
3. Explicit types over `var`
4. NEVER use `#region`
5. NEVER use nullable reference types (`string?`)
6. One file per MonoBehaviour/ScriptableObject (production AND tests)
7. NEVER use `?.`, `??`, `??=` on UnityEngine.Object types
8. Minimal comments — only explain **why**, never **what**; rely on descriptive names
9. `#if` conditional blocks MUST be placed INSIDE the namespace; `#define` directives MUST be at the file top (C# language requirement)
10. **ALWAYS generate `.meta` files** after creating ANY file or folder (see below)
11. **ALWAYS update documentation** — Docs, XML docs, code samples, and CHANGELOG for every change (see [update-documentation](./skills/update-documentation.md))
12. **ALWAYS write exhaustive tests** — Normal, negative, edge cases, extreme scenarios, and "the impossible" (see [create-test](./skills/create-test.md))
13. **Enums MUST have explicit integer values** — EVERY enum member requires `= N`; first member MUST be `None`/`Unknown` with `= 0` and `[Obsolete]` (non-error) (see [create-enum](./skills/create-enum.md))
14. **NEVER use backtick-wrapped markdown file references** — Always use proper markdown links like `[readable-name](path/to/doc)` instead of wrapping filenames in backticks; **NEVER place inline code backticks on both sides of a markdown link on the same line**—the linter's regex can match across multiple backtick pairs and capture `.md` from link paths, triggering false positive errors; restructure sentences to avoid this pattern; run `npm run lint:docs` after ANY markdown change (see [validate-before-commit](./skills/validate-before-commit.md#markdown-inline-code--link-anti-pattern))
15. **NEVER use reflection on our own code** — Use `internal` + `[InternalsVisibleTo]` for test access; reflection is fragile and untraceable (see [avoid-reflection](./skills/avoid-reflection.md))
16. **NEVER use magic strings for code identifiers** — Use `nameof()` for members and `typeof()` for types; strings break silently on rename (see [avoid-magic-strings](./skills/avoid-magic-strings.md))
17. **Markdown code blocks REQUIRE language specifiers** — ALL fenced code blocks must have a language (`csharp`, `bash`, `text`, etc.); never use bare code fence blocks (see [update-documentation](./skills/update-documentation.md#markdown-linting-and-quality))
18. **NEVER use emphasis as headings** — Use proper `#` heading syntax, not **bold** or _italic_ text as section headers
19. **🚨 Run `npm run lint:docs` IMMEDIATELY after ANY markdown change** — Do NOT wait until task completion; run link linter IMMEDIATELY after each markdown file edit; catches missing `./` prefixes, broken links, and formatting issues; also run `npm run lint:markdown` and `npm run format:md:check`; fix all errors before proceeding to next file (see [validate-before-commit](./skills/validate-before-commit.md))
20. **Run actionlint after workflow changes** — `actionlint` MUST pass for ANY changes to `.github/workflows/*.yml` files; prevents runtime CI/CD failures from missing parameters (e.g., `config-name`), invalid triggers, and security issues (see [validate-before-commit](./skills/validate-before-commit.md#github-actions-workflow-linting-mandatory))
21. **🚨 Run yamllint after ANY YAML change** — `npm run lint:yaml` MUST pass for ALL `.yml`/`.yaml` files; catches trailing spaces, incorrect indentation, and style violations that Prettier does NOT fix; **WORKFLOW: after ANY YAML edit → `npx prettier --write <file>` → `npm run lint:yaml` → (for workflows) `actionlint`**; CI **WILL FAIL** on yamllint errors (see [validate-before-commit](./skills/validate-before-commit.md#yaml-file-formatting-and-linting-mandatory))
22. **🚨🚨🚨 Run linters IMMEDIATELY after EVERY change — VERIFY OUTPUT** — Do NOT wait until task completion; run appropriate linters after each file modification AND **verify the output shows success**: `npm run lint:spelling` for docs (add valid terms to `cspell.json`), `npm run lint:docs` + `npm run lint:markdown` for markdown, `npm run lint:yaml` for YAML, `actionlint` for workflows, `dotnet tool run csharpier format .` for C#; **if a linter exits with code 0 but reports "0 files matched" or similar, the pattern is likely wrong**; fix issues before moving to next file (see [validate-before-commit](./skills/validate-before-commit.md#mandatory-run-linters-immediately-after-every-change))
23. **Track ALL Unity objects in tests** — Use `Track()` wrapper for `Editor.CreateEditor()`, `new GameObject()`, `ScriptableObject.CreateInstance()`, etc.; NEVER use manual `DestroyImmediate` in finally blocks; run `pwsh -NoProfile -File scripts/lint-tests.ps1` **IMMEDIATELY** after ANY test changes (pre-push hook enforces this); use `// UNH-SUPPRESS` comment only when intentionally testing destroy behavior (see [create-test](./skills/create-test.md#unity-object-lifecycle-management-critical))
24. **🚨🚨🚨 Run Prettier IMMEDIATELY after EVERY non-C# file change — NO BATCHING** — `npx prettier --write <file>` MUST be executed **IMMEDIATELY after EACH individual file modification**, NOT batched at task completion; applies to ALL: `.md`, `.json`, `.yaml`, `.yml`, `.js`, and config files—**including files in `.llm/` directory**; **WORKFLOW: edit file → IMMEDIATELY run `npx prettier --write <file>` → verify → proceed to next file**; pre-push hooks **WILL REJECT** commits with Prettier issues; **NO EXCEPTIONS, NO DELAYS, NO BATCHING**—always verify with `npx prettier --check .` before committing (see [validate-before-commit](./skills/validate-before-commit.md#prettiermarkdown-formatting))
25. **Run CSharpier IMMEDIATELY after ANY C# file change** — `dotnet tool run csharpier format .` MUST be run after EVERY `.cs` file modification, even single-line edits; do NOT batch formatting until task completion; extra blank lines and spacing issues are common CI/CD failures that are easily preventable (see [format-code](./skills/format-code.md) and [validate-before-commit](./skills/validate-before-commit.md#c-changes-workflow))
26. **Verify GitHub Actions configuration files exist AND are on the default branch** — Before creating/modifying workflows, confirm all required config files exist (e.g., `release-drafter.yml` workflow requires `.github/release-drafter.yml` config); missing configs cause runtime failures NOT caught by `actionlint`; **CRITICAL**: Some actions (like `release-drafter`) require config files to exist on the **default branch (main)** at runtime—if adding both workflow AND config in the same PR, either: (1) disable/comment out triggers until config is merged to main, or (2) merge config file first in a separate PR (see [validate-before-commit](./skills/validate-before-commit.md#github-actions-configuration-file-requirements-mandatory))
27. **NEVER use `((var++))` in bash scripts with `set -e`** — The expression `((var++))` returns the pre-increment value; when `var` is 0, it returns 0 (falsy), causing `set -e` to exit the script; use `var=$((var + 1))` instead (assignment always succeeds); see [validate-before-commit](./skills/validate-before-commit.md#bash-arithmetic-safety-in-cicd-critical)
28. **🚨🚨🚨 Internal markdown links MUST use `./` or `../` prefix — NO EXCEPTIONS** — ALL internal links in markdown MUST use explicit relative paths with `./` or `../` prefix (e.g., `[Guide](./docs/guide)` or `[Other](../other-doc)`); **NEVER use bare paths** without the prefix (e.g., `[Bad](docs/guide)` or `[Bad](CHANGELOG)`); **NEVER use absolute GitHub Pages paths** starting with `/unity-helpers/` (e.g., `[Bad](/unity-helpers/docs/guide)`)—these are the GitHub Pages `baseurl` and break in CI where the site is not deployed; this applies to ALL markdown files including docs, skills, CHANGELOG, and README; without the prefix, `jekyll-relative-links` fails to convert links causing **broken pages or raw file downloads on GitHub Pages**; **the linter now automatically detects both missing `./`/`../` prefixes AND forbidden `/unity-helpers/` patterns**; **WORKFLOW: after ANY markdown change → IMMEDIATELY run `npm run lint:docs` to catch path issues → fix before proceeding**; CI WILL FAIL if links are incorrect (see [github-pages](./skills/github-pages.md))
29. **🚨🚨🚨 Run `npm run lint:spelling` IMMEDIATELY after ANY docs/comments change — NO EXCEPTIONS** — Spell check MUST be run **IMMEDIATELY** after modifying ANY markdown file, C# XML comments, or code comments—**NOT at task completion, NOT batched with other files**; spelling errors are the **#1 CI failure cause for documentation changes**; when cspell reports unknown words: (1) Fix actual typos, (2) Add valid technical terms to the appropriate dictionary in `cspell.json` (see cspell quick reference below); **WORKFLOW: edit file → IMMEDIATELY run `npm run lint:spelling` → fix issues or add to dictionary → run `npx prettier --write cspell.json` if modified → proceed to next file**; pre-push hooks **WILL REJECT** commits with spelling errors
30. **🚨 Example links in documentation MUST be escaped** — When showing example markdown link syntax (teaching correct/incorrect formats), ALWAYS wrap examples in: (1) fenced code blocks with `text` or `markdown` language specifier for multi-line examples, OR (2) inline backticks for brief single-line examples; **this is MANDATORY to prevent false positive CI failures**; the CI link-checking scripts URL-decode paths (handles `%20` for spaces), skip content inside fenced code blocks (` ``` ` or `~~~`), and skip inline code (backtick-wrapped content)—but only if properly escaped; unescaped example links WILL cause the linter to parse them as real links and report them as broken; see [update-documentation](./skills/update-documentation.md#escaping-example-links-in-documentation)
31. **🚨 Line ending configurations MUST be synchronized across ALL config files** — When modifying line endings in ANY config file, ensure ALL are updated together: `.gitattributes` (controls git checkout), `.prettierrc.json` (controls Prettier formatting), `.yamllint.yaml` (controls YAML linting), and `.editorconfig` (controls IDE behavior); mismatches cause CI failures because files are checked out with one ending but linters expect another; **current settings: YAML files use LF (`type: unix` in yamllint, `endOfLine: lf` override in Prettier), `.github/**`files use LF, most other text files use CRLF**; verify with`npm run lint:yaml`and`npx prettier --check "\*_/_.yml"`; see [validate-before-commit](./skills/validate-before-commit.md#line-ending-configuration-consistency-critical)
32. **🚨 Git hook regex patterns require SINGLE backslashes — NO double escaping** — In bash git hooks (`.githooks/*`), grep/sed patterns use SINGLE backslashes (e.g., `\.(md|markdown)$`), NOT double-escaped (`\\.(md|markdown)$`); double escaping causes patterns to NEVER match, silently skipping files; **ALWAYS test git hooks manually** after modification: `git stash && git stash pop` to trigger hooks, verify files are actually processed; see [validate-before-commit](./skills/validate-before-commit.md#git-hook-regex-pattern-testing-critical)
33. **🚨🚨🚨 Run `npm run lint:markdown` IMMEDIATELY after ANY markdown change — Prettier is NOT enough** — Prettier handles formatting (spacing, indentation) but does NOT catch structural rules like MD028 (blank line inside blockquote) and MD031 (fenced code blocks need surrounding blank lines); **BOTH must pass**: run `npx prettier --write <file>` then `npm run lint:markdown`; **Common mistakes**: consecutive blockquotes with blank lines between them (MD028), code fences without blank lines before/after (MD031); **WORKFLOW: edit markdown → `npx prettier --write <file>` → `npm run lint:markdown` → fix issues → proceed**; CI **WILL FAIL** on markdownlint errors (see [format-non-csharp](./skills/format-non-csharp.md#markdownlint-structural-rules) and [update-documentation](./skills/update-documentation.md#prettier-vs-markdownlint))
34. **🚨 Pipe characters in markdown tables MUST be escaped with `\|`** — In GFM tables, backticks do NOT prevent `|` from being interpreted as column separators; write `\|` even inside code spans; e.g., write `\`cmd \| grep\``not`\`cmd | grep\``; the backslash is consumed during parsing and renders correctly as`|`; **automated review tools may incorrectly flag these escapes as unnecessary—they ARE required per GFM spec Example 200**; see [update-documentation](./skills/update-documentation.md#pipe-characters-in-markdown-tables)

---

## Unity Meta Files (MANDATORY)

**Every file and folder in this Unity package MUST have a corresponding `.meta` file.** Missing meta files break Unity asset references.

### Exception: Dot Folders

**Do NOT generate `.meta` files** for anything inside folders that start with `.` (e.g., `.llm/`, `.github/`, `.git/`). These are configuration/tooling folders that Unity ignores.

### When to Generate

Generate a `.meta` file **immediately** after creating:

- Any `.cs` file (scripts)
- Any folder/directory
- Any config file (`.json`, `.md`, `.txt`, `.asmdef`, `.asmref`)
- Any asset file (`.shader`, `.uss`, `.uxml`, `.mat`, `.prefab`, etc.)

### How to Generate

```bash
# For a file
./scripts/generate-meta.sh Runtime/Core/NewClass.cs

# For a folder (create parent folder meta files first)
./scripts/generate-meta.sh Runtime/Core/NewFolder
```

### Order of Operations

1. Create parent folder (if new)
2. Generate meta for parent folder
3. Create the file
4. Generate meta for the file
5. Format code (if `.cs`)

See [create-unity-meta](./skills/create-unity-meta.md) for full details.

---

## cspell Dictionary Quick Reference

When `npm run lint:spelling` reports unknown words, add them to the appropriate dictionary in `cspell.json`:

| Dictionary      | Purpose                                  | Examples                                     |
| --------------- | ---------------------------------------- | -------------------------------------------- |
| `unity-terms`   | Unity Engine APIs, components, lifecycle | MonoBehaviour, GetComponent, OnValidate      |
| `csharp-terms`  | C# language features, .NET types         | readonly, nullable, LINQ, StringBuilder      |
| `package-terms` | This package's public API and type names | WallstopStudios, IRandom, SpatialHash        |
| `tech-terms`    | General programming/tooling terms        | async, config, JSON, middleware, refactoring |

**Adding words**: Edit `cspell.json` → find the dictionary's `words` array → add alphabetically.

---

## Software Architecture Principles

**MANDATORY**: Apply modern software engineering principles to ALL code (production, editor, tests):

### SOLID Principles

- **Single Responsibility** — Each class/method does one thing well
- **Open/Closed** — Extend via composition or inheritance, don't modify existing code
- **Liskov Substitution** — Subtypes must be substitutable for their base types
- **Interface Segregation** — Prefer small, focused interfaces over large ones
- **Dependency Inversion** — Depend on abstractions, not concrete implementations

### DRY & Abstraction

- **Never duplicate code** — Extract common patterns into reusable abstractions
- **Build lightweight abstractions** — Prefer value types (`readonly struct`) or static functions
- **Zero/minimal allocation** — Abstractions must not introduce heap allocations in hot paths
- **Favor composition** — Build complex behavior from simple, composable pieces
- **Extract repetitive patterns** — If you write similar code twice, abstract it

### Clean Architecture

- **Clear boundaries** — Runtime vs Editor vs Tests separation
- **Obvious dependencies** — Explicit interfaces and injection over hidden coupling
- **Design patterns** — Use appropriate patterns (Factory, Strategy, Observer, etc.) when they simplify code
- **Testability** — Design for easy unit testing; avoid hidden state

---

## High-Performance C# Requirements

**MANDATORY**: All code must follow [high-performance-csharp](./skills/high-performance-csharp.md) and [unity-performance-patterns](./skills/unity-performance-patterns.md). This applies to:

- **New features** — Design for zero allocation from the start
- **Bug fixes** — Must not regress performance; improve if possible
- **Editor tooling** — Inspectors run every frame; cache everything

### Why Zero-Allocation Matters

Unity uses the **Boehm-Demers-Weiser (BDW) garbage collector**:

- **Non-generational** — Scans entire heap on every collection
- **No compaction** — Memory fragments over time
- **Stop-the-world** — Game freezes during GC
- **Heap never shrinks** — Memory high-water mark persists until app restart

At 60 FPS with 1KB/frame allocation = **3.6 MB/minute** of garbage = frequent GC stutters.

See [gc-architecture-unity](./skills/gc-architecture-unity.md) for detailed GC architecture information.

### Quick Rules

| Forbidden                          | Use Instead                                     |
| ---------------------------------- | ----------------------------------------------- |
| LINQ (`.Where`, `.Select`, `.Any`) | `for` loops                                     |
| `new List<T>()` in methods         | `Buffers<T>.List.Get()`                         |
| Closures capturing variables       | Static lambdas or explicit loops                |
| `foreach` on `List<T>` (Mono)      | `for` loop with indexer (24 bytes/loop!)        |
| `params` method calls              | Chain 2-argument overloads                      |
| Delegate assignment in loops       | Assign once outside loop                        |
| Enum dictionary keys               | Custom `IEqualityComparer` or cast to int       |
| Struct without `IEquatable<T>`     | Implement `IEquatable<T>` to avoid boxing       |
| Reflection on our code             | `internal` + `[InternalsVisibleTo]`, interfaces |
| Reflection on external APIs        | `ReflectionHelpers` (last resort)               |
| `string +` in loops                | `Buffers.StringBuilder`                         |
| Duplicated code blocks             | Extract to shared abstraction                   |
| Batch formatting at end of task    | Format IMMEDIATELY after EACH file change       |
| Heavy class where struct suffices  | `readonly struct` with cached hash              |
| `GetComponent<T>()` in Update      | Cache in Awake/Start                            |
| `Camera.main` in Update            | Cache in Awake/Start                            |
| `Physics.RaycastAll`               | `Physics.RaycastNonAlloc` + buffer              |
| `gameObject.tag == "X"`            | `gameObject.CompareTag("X")`                    |
| `new WaitForSeconds()` in loop     | Cache as field                                  |
| `renderer.material` for changes    | `MaterialPropertyBlock`                         |
| `SendMessage`/`BroadcastMessage`   | Direct interface calls (1000x faster)           |

See [memory-allocation-traps](./skills/memory-allocation-traps.md) for comprehensive hidden allocation sources.

### Required Patterns

```csharp
// Collection pooling
using var lease = Buffers<T>.List.Get(out List<T> buffer);

// Array pooling (variable sizes)
using PooledArray<T> pooled = SystemArrayPool<T>.Get(count, out T[] array);

// Cached reflection (external APIs only)
ReflectionHelpers.TryGetField(type, "name", out FieldInfo field);

// Hot path inlining
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public int GetHashCode() => _cachedHash;

// IEquatable<T> for structs used in collections
public struct MyStruct : IEquatable<MyStruct>
{
    public bool Equals(MyStruct other) => /* field comparison */;
}

// Custom comparer for enum dictionary keys
public struct MyEnumComparer : IEqualityComparer<MyEnum>
{
    public bool Equals(MyEnum x, MyEnum y) => x == y;
    public int GetHashCode(MyEnum obj) => (int)obj;
}
```

---

## Reflection & Magic Strings

**MANDATORY**: Reflection and magic strings are FORBIDDEN for WallstopStudios code. This applies to ALL code (production, editor, tests).

### Reflection Rules

| Forbidden                            | Use Instead                                    |
| ------------------------------------ | ---------------------------------------------- |
| `typeof(OurType).GetMethod("name")`  | `internal` method + `[InternalsVisibleTo]`     |
| `typeof(OurType).GetField("name")`   | `internal` field + `[InternalsVisibleTo]`      |
| `Activator.CreateInstance(ourType)`  | Direct constructor or factory method           |
| Reflection to access private members | Make `internal` and grant test assembly access |

**Why reflection is forbidden on our code:**

- **Fragile**: Breaks silently on rename/refactor
- **Untraceable**: IDE "Find All References" misses reflection calls
- **Slow**: Orders of magnitude slower than direct access
- **Unnecessary**: We control the code—use `internal` visibility

### Magic String Rules

| Forbidden                                   | Use Instead                                   |
| ------------------------------------------- | --------------------------------------------- |
| `"MethodName"` for serialization callbacks  | `nameof(MethodName)`                          |
| `"fieldName"` for property paths            | `nameof(fieldName)`                           |
| `"TypeName"` for type references            | `typeof(TypeName).Name` or `nameof(TypeName)` |
| String literals referencing our identifiers | `nameof()` for compile-time safety            |

**Why magic strings are forbidden:**

- **Silent breakage**: Renaming breaks functionality with no compiler error
- **No refactoring support**: IDE rename operations miss string references
- **No IntelliSense**: Typos compile successfully but fail at runtime

### Test Access Pattern

```csharp
// In AssemblyInfo.cs (production assembly)
[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Tests.Editor")]
[assembly: InternalsVisibleTo("WallstopStudios.UnityHelpers.Tests.Runtime")]

// In production code - use internal instead of private
internal void MethodNeedingTestAccess() { }
internal int _fieldNeedingTestAccess;

// In test code - access directly, no reflection needed
_instance.MethodNeedingTestAccess();
Assert.AreEqual(expected, _instance._fieldNeedingTestAccess);
```

See [avoid-reflection](./skills/avoid-reflection.md) and [avoid-magic-strings](./skills/avoid-magic-strings.md) for complete guidelines.

---

## Defensive Programming Requirements

**MANDATORY**: All production code (Runtime AND Editor) must follow [defensive-programming](./skills/defensive-programming.md). This applies to:

- **New features** — Design for resilience from the start
- **Bug fixes** — Must not introduce new failure modes
- **Editor tooling** — Handle missing/destroyed objects, corrupt data, user interruption

### Core Principles

1. **Never throw exceptions** from public APIs (except true programmer errors)
2. **Handle all inputs gracefully** — null, empty, out-of-range, invalid type
3. **Maintain internal consistency** — State must be valid after any operation
4. **Fail silently with logging** — Log warnings for debugging, don't crash

### Quick Rules

| Forbidden                              | Use Instead                                    |
| -------------------------------------- | ---------------------------------------------- |
| `throw new ArgumentNullException()`    | Return `default`, empty, or `false`            |
| `throw new IndexOutOfRangeException()` | Bounds-check, return `default` or `false`      |
| `items[index]` without bounds check    | `TryGet(index, out value)` pattern             |
| `dictionary[key]` without check        | `dictionary.TryGetValue(key, out value)`       |
| `switch` without `default` case        | Always include `default` with fallback/logging |
| Unchecked Unity Object access          | `if (obj != null)` before all operations       |
| Bare event invocation                  | Wrap in try-catch, log exceptions              |

### Required Patterns

```csharp
// TryXxx pattern for failable operations
public bool TryGetValue(string key, out TValue value)
{
    value = default;
    if (string.IsNullOrEmpty(key))
    {
        return false;
    }
    return _dictionary.TryGetValue(key, out value);
}

// Safe indexing
public T Get(int index)
{
    if (index < 0 || index >= _items.Count)
    {
        return default;
    }
    return _items[index];
}

// Safe event invocation
if (OnValueChanged != null)
{
    try
    {
        OnValueChanged.Invoke(newValue);
    }
    catch (Exception ex)
    {
        Debug.LogError($"[{nameof(MyClass)}] Exception in handler: {ex}");
    }
}

// State repair after deserialization
public void OnAfterDeserialize()
{
    _items ??= new List<Item>();
    _items.RemoveAll(item => item == null);
    _index = Mathf.Clamp(_index, 0, Mathf.Max(0, _items.Count - 1));
}
```

---

## Agent-Specific Rules

### Zero-Flaky Test Policy (CRITICAL)

**This repository enforces a strict zero-flaky test policy.** Every test failure indicates a real bug—either in production code OR in the test itself. Both require comprehensive investigation and proper fixes.

#### Forbidden Actions

| ❌ NEVER Do This                           | ✅ ALWAYS Do This                              |
| ------------------------------------------ | ---------------------------------------------- |
| "Make the test pass" without understanding | Investigate root cause before ANY code changes |
| Ignore or dismiss intermittent failures    | Treat flaky tests as highest priority bugs     |
| Disable, skip, or `[Ignore]` failing tests | Fix the underlying issue                       |
| Add retry logic to hide flakiness          | Eliminate the source of non-determinism        |
| Assume "it works locally"                  | Reproduce and fix environment-specific issues  |

#### Investigation Process

1. **Read the full error** — Stack traces, assertion messages, expected vs actual
2. **Understand the test's intent** — What behavior is being verified?
3. **Classify the bug**:
   - **Production bug** — Test correctly identifies broken behavior → Fix production code
   - **Test bug** — Test itself is flawed → Fix the test
4. **Implement proper fix** — Address root cause, not symptoms
5. **Verify comprehensively** — Run full test suite, confirm no new flakiness

See [investigate-test-failures](./skills/investigate-test-failures.md) for detailed investigation procedures.

---

### Mandatory Testing for New Features (NON-NEGOTIABLE)

**All new production code MUST have EXHAUSTIVE test coverage.** Tests are NON-NEGOTIABLE for new code. No feature or bug fix is complete without comprehensive tests.

| Code Type               | Test Requirement                         |
| ----------------------- | ---------------------------------------- |
| Runtime classes/methods | Full test coverage in `Tests/Runtime/`   |
| Editor tools/windows    | Full test coverage in `Tests/Editor/`    |
| Property drawers        | Drawer behavior tests in `Tests/Editor/` |
| Inspector attributes    | Attribute behavior tests                 |
| Bug fixes               | Regression tests proving the fix works   |

#### Test Coverage Requirements (EXHAUSTIVE)

Every feature and bug fix MUST include tests for:

1. **Normal cases** — Typical usage scenarios, common inputs
2. **Negative cases** — Invalid inputs, error conditions, expected exceptions
3. **Edge cases** — Empty collections, single-element, boundary values, null, special characters
4. **Extreme scenarios** — Maximum sizes, minimum values, overflow conditions, resource limits
5. **"The Impossible"** — States that "should never happen" but could in production

#### Data-Driven Testing (PREFERRED)

**Prefer `[TestCase]` and `[TestCaseSource]` for comprehensive coverage.** Data-driven tests make it easy to add new scenarios and ensure exhaustive coverage.

```csharp
// Use dot-separated naming: Category.Scenario.Expected
yield return new TestCaseData(null, false).SetName("Input.Null.ReturnsFalse");
yield return new TestCaseData("", false).SetName("Input.Empty.ReturnsFalse");
yield return new TestCaseData("valid", true).SetName("Input.Valid.ReturnsTrue");
yield return new TestCaseData(new string('x', 10000), true).SetName("Input.VeryLong.ReturnsTrue");
yield return new TestCaseData("\0\n\r\t", false).SetName("Input.ControlChars.ReturnsFalse");
```

**Benefits of data-driven tests:**

- Easy to add new test cases without code duplication
- Clear visibility of all tested scenarios
- Ensures consistent test structure across cases
- Enables rapid expansion of coverage

See [create-test](./skills/create-test.md) for full testing guidelines.

### Scope & Behavior

- Keep changes minimal and focused
- Strictly follow `.editorconfig` formatting rules
- Respect folder boundaries (Runtime vs Editor)
- **ALWAYS update documentation** alongside code changes (see below)
- **ALWAYS generate `.meta` files** after creating ANY file or folder via `./scripts/generate-meta.sh <path>`
- **NEVER pipe output to `/dev/null`**

### Mandatory Documentation Updates (CRITICAL)

**ALL documentation MUST be updated after ANY feature addition or bug fix.** Incomplete documentation = incomplete work. Documentation is NOT optional.

**CHANGELOG is for USER-FACING changes ONLY.** Internal changes like CI/CD workflows, build scripts, dev tooling, and infrastructure do NOT belong in the CHANGELOG. Users don't care about how the package is built or tested—they care about what the package does for them.

| Change Type              | Required Updates                                              |
| ------------------------ | ------------------------------------------------------------- |
| New feature/class/method | Docs, XML docs, code samples, CHANGELOG `### Added`           |
| Bug fix                  | CHANGELOG `### Fixed`, docs if behavior changed               |
| API modification         | All affected docs, XML docs, samples, CHANGELOG               |
| Breaking change          | All docs, migration notes, CHANGELOG `### Changed` (Breaking) |
| Performance improvement  | CHANGELOG `### Improved`, performance docs if metrics changed |
| CI/CD, build scripts     | **NO CHANGELOG** — internal tooling, not user-facing          |
| Dev tooling, workflows   | **NO CHANGELOG** — internal infrastructure                    |

#### Documentation Scope (ALL Must Be Updated)

| Documentation Type | Location / Format                                       |
| ------------------ | ------------------------------------------------------- |
| Markdown docs      | `docs/` folder, [README](../README.md)                  |
| XML docs           | `///` comments on all public APIs                       |
| Code comments      | Inline comments explaining **why** (not what)           |
| Code samples       | In docs AND XML docs where applicable                   |
| CHANGELOG          | [CHANGELOG](../CHANGELOG.md) in Keep a Changelog format |

#### Documentation Quality Requirements

1. **All code samples MUST compile** — Test every example before committing; broken samples are bugs
2. **Use clear, direct language** — Avoid jargon; define technical terms when first used
3. **Indicate NEW behavior** — "Added in vX.Y.Z" for new features; note behavior changes
4. **Front-load key information** — Important details first, not buried in paragraphs
5. **Keep it concise** — Say what needs to be said, nothing more; no filler
6. **Be succinct and easy-to-understand** — A developer should grasp the concept in seconds
7. **No unexplained jargon** — If you use a technical term, explain it or link to explanation

#### CHANGELOG Format

Follow [Keep a Changelog](https://keepachangelog.com/) format:

```markdown
### Added

- **Feature Name**: Brief description of what was added
  - Sub-bullet for additional details

### Fixed

- Fixed null reference in SerializableDictionary drawer on Unity 2021

### Improved

- Improved QuadTree query performance by 40% for large datasets
```

See [update-documentation](./skills/update-documentation.md) for complete guidelines.

### Shell Tool Requirements

**MANDATORY**: Use high-performance tools instead of traditional Unix tools. See [search-codebase](./skills/search-codebase.md) for full documentation.

| Forbidden | Use Instead          | Reason                       |
| --------- | -------------------- | ---------------------------- |
| `grep`    | `rg` (ripgrep)       | 10-100x faster, better regex |
| `find`    | `fd`                 | 5x faster, friendlier syntax |
| `cat`     | `bat --paging=never` | Syntax highlighting          |
| `grep -r` | `rg`                 | Recursive by default         |

```bash
# ❌ NEVER
grep -r "pattern" .
find . -name "*.cs"
cat file.cs

# ✅ ALWAYS
rg "pattern"
fd "\.cs$"
bat --paging=never file.cs
```

### Portable Shell Scripting (CI/CD & Scripts)

**MANDATORY** for CI/CD workflows (`.github/workflows/*.yml`) and bash scripts (`scripts/*.sh`): Use POSIX-compliant tools instead of GNU-specific options. See [search-codebase](./skills/search-codebase.md#portable-shell-scripting-cicd--bash-scripts) and [validate-before-commit](./skills/validate-before-commit.md#portable-shell-scripting-in-workflows-critical) for full documentation.

| ❌ GNU-Specific (Don't Use)   | ✅ POSIX Alternative                | Why                           |
| ----------------------------- | ----------------------------------- | ----------------------------- |
| `grep -oP` (Perl regex)       | `grep -oE` (extended regex) + `sed` | `-P` unavailable on macOS/BSD |
| `sed -i` (in-place edit)      | `sed ... > tmp && mv tmp file`      | Syntax differs GNU vs BSD     |
| `readarray` / `mapfile`       | `while read` loop                   | Bash 4+ only                  |
| `grep -oP '\K'` (lookbehind)  | `grep -oE` + `sed 's/prefix//'`     | Perl-specific feature         |
| `/bin/sed`, `/usr/bin/awk`    | `sed`, `awk` (bare command)         | Paths differ across systems   |
| `cmd \| while read` + counter | Process substitution `< <(cmd)`     | Subshell variable loss        |

```bash
# ❌ NEVER in CI/CD or scripts (GNU-only, fails on macOS)
echo "$line" | grep -oP '\]\(\K[^)]+(?=\))'

# ✅ ALWAYS (POSIX-compliant, works everywhere)
echo "$line" | grep -oE '\]\([^)]+\)' | sed 's/^](//;s/)$//'
```

### Subshell Variable Pitfalls (CI/CD Scripts)

**CRITICAL**: Variables modified inside `cmd | while read` loops don't propagate to the parent shell. This causes silent bugs where counters are always 0.

```bash
# ❌ BUG - errors is always 0 (modified in subshell)
errors=0
find . -name "*.md" | while read -r file; do
  errors=$((errors + 1))  # Subshell's copy!
done
echo "$errors"  # Always prints 0!

# ✅ CORRECT - Process substitution keeps loop in parent shell
errors=0
while read -r file; do
  errors=$((errors + 1))
done < <(find . -name "*.md")
echo "$errors"  # Correct count!

# ✅ ALTERNATIVE - Temp file for counter (most reliable)
error_file=$(mktemp)
echo "0" > "$error_file"
find . -name "*.md" | while read -r file; do
  count=$(cat "$error_file")
  echo $((count + 1)) > "$error_file"
done
errors=$(cat "$error_file")
rm -f "$error_file"
```

See [validate-before-commit](./skills/validate-before-commit.md#subshell-variable-propagation-critical) for full documentation.

### Word Splitting Pitfalls (CI/CD Scripts)

**CRITICAL**: Using `for item in $variable` (unquoted) causes word splitting on spaces. This silently breaks iteration when items contain spaces or special characters.

```bash
# ❌ BUG - Word splitting breaks items with spaces
links=$(grep -oE '\]\([^)]+\)' "$file")
for link in $links; do  # Unquoted $links splits on spaces!
  check_link "$link"     # Links with spaces become fragments
done

# ✅ CORRECT - while read preserves entire lines
while IFS= read -r link; do
  [ -z "$link" ] && continue
  check_link "$link"
done < <(grep -oE '\]\([^)]+\)' "$file" 2>/dev/null || true)
```

See [validate-before-commit](./skills/validate-before-commit.md#word-splitting-and-special-characters-critical) for full documentation.

### Git Operations

**NEVER use `git add` or `git commit` commands.** User handles all staging/committing.

✅ Allowed: `git status`, `git log`, `git diff`
❌ Forbidden: `git add`, `git commit`, `git push`, `git reset`

#### Git Index Lock Safety (For Scripts)

When writing or modifying scripts that interact with git (pre-commit hooks, formatters, linters), **ALWAYS use the shared helper modules** to prevent `index.lock` contention errors. This is critical when users run interactive git tools like lazygit, GitKraken, or IDE integrations that may hold locks.

| Language   | Helper Module                     | Primary Function         |
| ---------- | --------------------------------- | ------------------------ |
| PowerShell | `scripts/git-staging-helpers.ps1` | `Invoke-GitAddWithRetry` |
| Bash       | `scripts/git-staging-helpers.sh`  | `git_add_with_retry`     |

**NEVER use raw `git add` in scripts** — always use the retry helpers.

See [git-safe-operations](./skills/git-safe-operations.md) for full documentation.

### Test Execution

Do not copy/clone this repository. Ask user to run tests and provide output.

### Paths

Never hard-code machine-specific absolute paths. Use relative paths or environment variables.

---

## Assembly Definitions

| Assembly                                     | Purpose               |
| -------------------------------------------- | --------------------- |
| `WallstopStudios.UnityHelpers`               | Runtime code          |
| `WallstopStudios.UnityHelpers.Editor`        | Editor code           |
| `WallstopStudios.UnityHelpers.Tests.Runtime` | Runtime tests         |
| `WallstopStudios.UnityHelpers.Tests.Editor`  | Editor tests          |
| `WallstopStudios.UnityHelpers.Tests.Core`    | Shared test utilities |

---

## Key Features Quick Reference

### Inspector Attributes

`[WGroup]`, `[WGroupEnd]`, `[WButton]`, `[WShowIf]`, `[WEnumToggleButtons]`, `[WInLineEditor]`, `[WValueDropDown]`, `[WSerializableCollectionFoldout]`, `[WReadOnly]`, `[WNotNull]`, `[ValidateAssignment]`, `[StringInList]`, `[IntDropDown]`, `[EnumDisplayName]`

### Relational Component Attributes

`[SiblingComponent]`, `[ParentComponent]`, `[ChildComponent]`
Options: `Optional`, `MaxDepth`, `OnlyAncestors`, `IncludeInactive`

### Serializable Data Structures

`SerializableDictionary<K,V>`, `SerializableSortedDictionary<K,V>`, `SerializableHashSet<T>`, `SerializableNullable<T>`, `SerializableType`, `WGuid`

### Spatial Data Structures

- **2D**: `QuadTree2D<T>`, `KdTree2D<T>`, `RTree2D<T>`, `SpatialHash2D<T>`
- **3D**: `OctTree3D<T>`, `KdTree3D<T>`, `RTree3D<T>`, `SpatialHash3D<T>`

### Other Data Structures

`CyclicBuffer<T>`, `Heap<T>`, `PriorityQueue<T>`, `Deque<T>`, `DisjointSet`, `Trie`, `SparseSet`, `BitSet`, `ImmutableBitSet`, `TimedCache<T>`

### Random Number Generators

`PRNG.Instance` (thread-local default), `IllusionFlow`, `PcgRandom`, `XorShiftRandom`, `XoroShiroRandom`, `SplitMix64`, `RomuDuo`, `WyRandom`, and more.

### Effects System

`AttributeEffect`, `AttributesComponent`, `TagHandler`, `EffectHandle`, `CosmeticEffectData`

### Serialization

- **JSON**: `Serializer.JsonSerialize()` / `JsonDeserialize()`
- **Protobuf**: `Serializer.ProtoSerialize()` / `ProtoDeserialize()`

### Pooling

- `Buffers<T>.List` / `Buffers<T>.HashSet` — Zero-allocation collection leases
- `WallstopArrayPool<T>` — Exact-size array pooling (fixed sizes only)
- `SystemArrayPool<T>` — Variable-size array pooling

### Singletons

`RuntimeSingleton<T>`, `ScriptableObjectSingleton<T>`, `[AutoLoadSingleton]`

### DI Integrations

- **VContainer**: `builder.RegisterRelationalComponents()`
- **Zenject**: `RelationalComponentsInstaller`
- **Reflex**: `RelationalComponentsInstaller`

### Odin Inspector Integration

All inspector attributes work seamlessly with Odin Inspector when installed (`ODIN_INSPECTOR` define symbol):

- **Automatic Activation**: Custom Odin drawers activate for `SerializedMonoBehaviour`/`SerializedScriptableObject`
- **Behavior Parity**: Identical behavior with or without Odin
- **Mixed Usage**: Can use Unity Helpers and Odin attributes on same class
- **No Dependency**: Package functions fully without Odin installed

**Odin-Enhanced Types**:

- `RuntimeSingleton<T>` — Inherits `SerializedMonoBehaviour` when Odin present
- `ScriptableObjectSingleton<T>` — Inherits `SerializedScriptableObject` when Odin present
- `AttributeEffectData` — Uses Odin's `[ShowIf]` when available, falls back to `[WShowIf]`

**File Locations**:

- Odin Drawers: `Editor/CustomDrawers/Odin/` (9 files)
- Odin Inspectors: `Editor/CustomEditors/` (3 files)
- Odin Tests: `Tests/Editor/CustomDrawers/Odin/` (9 files)

See [integrate-optional-dependency](./skills/integrate-optional-dependency.md) for implementation patterns.

### Editor Tools (Tools > Wallstop Studios > Unity Helpers)

- **Sprite Tools**: Cropper, Atlas Generator, Pivot Adjuster
- **Animation Tools**: Event Editor, Creator, Copier, Sheet Animation Creator
- **Texture Tools**: Blur, Resize, Settings Applier, Fit Texture Size
- **Validation**: Prefab Checker
- **Utilities**: ScriptableObject Singleton Creator

---

## Security & Configuration

- **Do not commit**: `Library/`, `obj/`, secrets, tokens
- **Do commit**: `.meta` files for all assets
- **Target Unity version**: 2021.3+
- **Verify `.asmdef` references** when adding new namespaces

---

## Commit & Pull Request Guidelines

### Commits

- Short, imperative summaries: "Fix JSON serialization for FastVector"
- Group related changes in single commits

### Pull Requests

- Clear description of changes
- Link related issues (`#123`)
- Include before/after screenshots for editor UI changes
- Update relevant documentation
- Ensure tests and linters pass

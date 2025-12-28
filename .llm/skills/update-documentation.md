# Skill: Update Documentation

**Trigger**: **MANDATORY** after ANY feature addition, bug fix, API change, or user-facing modification.

---

## When to Use

This skill applies **every time** you make changes to the codebase. Documentation is a first-class deliverable—incomplete documentation = incomplete work.

**CHANGELOG is for USER-FACING changes ONLY.** Internal changes like CI/CD workflows, build scripts, dev tooling, GitHub Actions, and infrastructure do NOT belong in the CHANGELOG. Users don't care about how the package is built or tested—they care about what the package does for them.

| Trigger                       | Required?    | Documentation Scope                               |
| ----------------------------- | ------------ | ------------------------------------------------- |
| **After ANY feature add**     | **REQUIRED** | Docs, XML docs, code samples, CHANGELOG           |
| **After ANY bug fix**         | **REQUIRED** | CHANGELOG, fix any docs describing wrong behavior |
| **After ANY API change**      | **REQUIRED** | All affected docs, XML docs, samples, CHANGELOG   |
| **After ANY behavior change** | **REQUIRED** | All affected docs, migration notes if needed      |
| **CI/CD, build scripts**      | N/A          | **NO CHANGELOG** — internal, not user-facing      |
| **Dev tooling, workflows**    | N/A          | **NO CHANGELOG** — internal infrastructure        |

---

## Documentation Types to Update

### 1. Markdown Documentation (`docs/` folder)

| Content Type              | Location                    | When to Update                      |
| ------------------------- | --------------------------- | ----------------------------------- |
| Feature documentation     | `docs/features/<category>/` | New features, API changes           |
| Usage guides              | `docs/guides/`              | New features, workflow changes      |
| API reference             | `docs/features/`            | API additions, modifications        |
| Performance documentation | `docs/performance/`         | Performance changes, new benchmarks |

### 2. XML Documentation Comments (inline `///`)

Required on ALL public types, methods, properties, and fields.

**Required elements**:

- `<summary>` — Brief description (one sentence)
- `<param>` — Every parameter documented
- `<returns>` — Return value documented
- `<exception>` — All thrown exceptions listed
- `<remarks>` — Additional details, caveats (when needed)
- `<example>` — Usage example (at least one per public API)

### 3. Code Samples

- **MUST be compilable** — Test before committing
- **MUST demonstrate typical usage** — Show common patterns
- **Include error handling** — Where appropriate
- **Self-contained** — No undeclared dependencies

### 4. CHANGELOG

- Follow Keep a Changelog format
- Categories: Added, Changed, Deprecated, Removed, Fixed, Security, Improved
- Version info for new features ("Added in vX.Y.Z")

### 5. Other Documentation

| Location                   | Purpose                       | When to Update               |
| -------------------------- | ----------------------------- | ---------------------------- |
| [llms.txt](../../llms.txt) | LLM-friendly package overview | New capabilities, major APIs |
| [README](../../README.md)  | Quickstart and overview       | Significant new features     |
| [Skills](./)               | Agent workflow procedures     | Workflow-affecting changes   |

---

## What Requires Documentation Updates

### Always Update Documentation For

| Change Type              | Documentation Required                                           |
| ------------------------ | ---------------------------------------------------------------- |
| New feature/class/method | Docs, XML docs, code samples, CHANGELOG                          |
| Bug fix                  | CHANGELOG, potentially docs if behavior changed                  |
| API modification         | All affected docs, XML docs, code samples, CHANGELOG             |
| Breaking change          | All affected docs, migration notes, CHANGELOG (Breaking Changes) |
| New attribute/decorator  | Inspector docs, usage examples, CHANGELOG                        |
| Editor tool              | Tools documentation, usage examples, CHANGELOG                   |
| Performance improvement  | CHANGELOG (Improved section), potentially performance docs       |

### NEVER Add to CHANGELOG

| Change Type                         | Why Excluded                                  |
| ----------------------------------- | --------------------------------------------- |
| CI/CD workflows (GitHub Actions)    | Internal infrastructure, not user-facing      |
| Build scripts, Makefiles            | Dev tooling, users don't interact with these  |
| Documentation deployment automation | Users access docs, don't care how they deploy |
| Test infrastructure changes         | Users don't run the package's test suite      |
| Code linting/formatting config      | Internal code quality standards               |
| Dev container, IDE config           | Development environment setup                 |

### Documentation Locations

| Content Type              | Location                    |
| ------------------------- | --------------------------- |
| Feature documentation     | `docs/features/<category>/` |
| Usage guides              | `docs/guides/`              |
| API reference (XML docs)  | Inline `///` comments       |
| Performance documentation | `docs/performance/`         |
| Changelog                 | Root changelog file         |
| LLM documentation         | Root llms.txt file          |
| Skill procedures          | `.llm/skills/`              |
| README quickstart         | Root README file            |

---

## Quality Requirements

### MANDATORY Standards

1. **Accuracy** — All code samples MUST compile and run correctly
2. **Clarity** — Clear, direct language; no unnecessary jargon
3. **Conciseness** — Say what needs to be said, nothing more
4. **Front-loaded** — Important information comes first
5. **Completeness** — Cover all parameters, return values, edge cases
6. **Examples** — Every public API needs at least one usage example
7. **Versioning** — New features include "Added in vX.Y.Z"
8. **Defined terms** — Technical terms defined when first used

### Code Sample Requirements

**All code samples MUST**:

- Compile without errors
- Be tested before committing
- Demonstrate typical usage patterns
- Be self-contained (no missing context)
- Include error handling where appropriate

```csharp
// ✅ GOOD: Correct, complete, compilable
using WallstopStudios.UnityHelpers.Core.Random;

IRandom random = PRNG.Instance;
int value = random.NextInt(0, 100);

// ❌ BAD: Incomplete, wrong namespace, won't compile
var random = new PRNG();  // Wrong: PRNG.Instance is correct
int value = random.Next();  // Wrong: Method is NextInt()
```

### Documentation Formatting

- Use **bold** for emphasis on important concepts
- Use `backticks` for code references (classes, methods, parameters)
- Use fenced code blocks with language hints (```csharp)
- Use tables for parameter/option lists
- Keep paragraphs short (2-4 sentences max)
- Use bullet points for lists of items

---

## Markdown Linting and Quality

**MANDATORY**: Run markdown linters after ANY changes to `.md` files.

### CRITICAL: Markdown Link Formatting

**NEVER use backtick-wrapped markdown file references.** Always use proper markdown links.

| ❌ WRONG                                       | ✅ CORRECT                                                |
| ---------------------------------------------- | --------------------------------------------------------- |
| See \`some-file\` for details                  | See [some-file](some-file) for details                    |
| Refer to \`skills/create-test\` for guidelines | Refer to [create-test](skills/create-test) for guidelines |
| Check \`context\` for rules                    | Check [context](context) for rules                        |

**Why this matters:**

- Backtick-wrapped file names are NOT clickable links
- The doc link linter (`./scripts/lint-doc-links.ps1`) will **fail** on backtick-wrapped markdown file references
- Proper links enable navigation and link validation
- CI will reject PRs with broken or improperly formatted links

### Required Commands After Markdown Changes

```bash
# MANDATORY: Run ALL of these after editing markdown files:
npm run lint:docs         # Check documentation links (MUST PASS)
npm run lint:markdown     # Check markdownlint rules
npm run format:md:check   # Check Prettier formatting

# Or run full content validation (includes all above):
npm run validate:content

# Alternative: Direct script invocation for verbose output:
pwsh ./scripts/lint-doc-links.ps1 -VerboseOutput
```

**STOP**: Do NOT mark documentation work complete until `npm run lint:docs` passes with zero errors.

### Code Block Language Specifiers

**ALL fenced code blocks MUST have a language specifier.** Blocks without specifiers will fail linting.

| Language   | Specifier    | Example Use Case                          |
| ---------- | ------------ | ----------------------------------------- |
| C#         | `csharp`     | All C# code examples                      |
| Bash       | `bash`       | Terminal commands, shell scripts          |
| PowerShell | `powershell` | Windows/PowerShell commands               |
| JSON       | `json`       | Configuration files, API responses        |
| YAML       | `yaml`       | Unity manifests, GitHub Actions           |
| XML        | `xml`        | XML documentation, config files           |
| Markdown   | `markdown`   | Markdown syntax examples                  |
| Plain text | `text`       | File structures, command output, diagrams |

````markdown
<!-- ✅ CORRECT: Language specifier present -->

```csharp
public void Example() { }
`` `

<!-- ❌ WRONG: Missing language specifier -->

`` `
public void Example() { }
`` `
```
````

### Heading Rules

**NEVER use emphasis (bold/italic) as a substitute for headings.** Use proper `#` heading syntax.

```markdown
<!-- ✅ CORRECT: Proper heading -->

## Button Configuration

The button supports...

<!-- ❌ WRONG: Bold text used as heading -->

**Button Configuration**

The button supports...
```

**Why this matters:**

- Proper headings create document structure for navigation
- Screen readers and accessibility tools rely on heading hierarchy
- Markdown linters enforce heading structure
- Table of contents generation requires proper headings

### Common Markdownlint Rules

| Rule  | Issue                        | Fix                                             |
| ----- | ---------------------------- | ----------------------------------------------- |
| MD032 | No blank line around lists   | Add blank line before and after lists           |
| MD031 | No blank line around fences  | Add blank line before and after code blocks     |
| MD022 | No blank line after headings | Add blank line after `#` headings               |
| MD040 | Fenced code without language | Add language specifier (`csharp`, `bash`, etc.) |
| MD025 | Multiple top-level headings  | Only one `#` heading per document               |
| MD009 | Trailing spaces              | Remove trailing whitespace (except line break)  |

### Markdown Quality Checklist

**Before committing ANY markdown changes:**

- [ ] **NO backtick-wrapped markdown file references** — use `[name](path/to/file)` links
- [ ] All fenced code blocks have language specifiers
- [ ] No emphasis (bold/italic) used as headings
- [ ] Blank lines before and after code blocks
- [ ] Blank lines before and after lists
- [ ] Blank lines after headings
- [ ] Proper heading hierarchy (no skipping levels)
- [ ] `npm run lint:docs` passes (doc link validation)
- [ ] `npm run lint:markdown` passes
- [ ] `npm run format:md:check` passes

### Auto-Fix Commands

```bash
# Auto-fix Prettier formatting issues
npm run format:md

# Markdownlint issues usually require manual fixes
# Review the error message and fix the specific issue
```

---

## CHANGELOG Format

The CHANGELOG follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

### User-Facing Changes ONLY

**CRITICAL**: The CHANGELOG documents changes that affect USERS of the package. Exclude:

| ❌ Exclude from CHANGELOG           | Why                                           |
| ----------------------------------- | --------------------------------------------- |
| CI/CD workflows (GitHub Actions)    | Internal build/test infrastructure            |
| Build scripts, dev tooling          | Users don't interact with these               |
| Documentation deployment automation | Users access docs, don't care how they deploy |
| Code linting/formatting changes     | Internal code quality, not user-facing        |
| Test infrastructure                 | Users don't run the package's test suite      |

| ✅ Include in CHANGELOG              | Why                                   |
| ------------------------------------ | ------------------------------------- |
| New runtime features/classes/methods | Users can use these in their projects |
| Bug fixes in runtime/editor code     | Affects user experience               |
| API changes, breaking changes        | Users need to update their code       |
| Performance improvements             | Users benefit from faster execution   |
| New inspector attributes/drawers     | Users see these in Unity Editor       |
| New editor tools/windows             | Users can access these tools          |

### Required Format

```markdown
## [Unreleased]

### Added

- **Feature Name**: Brief description of what was added
  - Sub-bullet for additional details

### Fixed

- Fixed [description of fix] in [component/area]

### Changed

- **Breaking**: [description] (if breaking change)
- [description of non-breaking change]

### Improved

- Improved [what] by [how/metric]

### Deprecated

- [feature] is deprecated, use [alternative] instead

### Removed

- Removed [feature/capability]

### Security

- Fixed [security issue description]
```

### Section Order

Sections should appear in this order (include only sections with content):

1. Added — New features (user-facing additions)
2. Fixed — Bug fixes
3. Improved — Enhancements to existing features
4. Changed — Changes to existing functionality (may include breaking changes)
5. Deprecated — Features marked for future removal
6. Removed — Features removed in this version
7. Security — Security-related fixes

### Entry Format

```markdown
### Added

- **Feature Name**: Brief description of what was added
  - Sub-bullet for additional details or sub-features
  - Another sub-bullet for related functionality
```

### Writing Good CHANGELOG Entries

| ✅ GOOD                                                               | ❌ BAD               |
| --------------------------------------------------------------------- | -------------------- |
| **WButton Odin Support**: WButton now works with Odin Inspector types | Added Odin support   |
| Fixed null reference in SerializableDictionary drawer on Unity 2021   | Fixed bug            |
| Improved QuadTree query performance by 40% for large datasets         | Made QuadTree faster |
| **Breaking**: Renamed KGuid to WGuid, changed data layout             | Renamed GUID class   |

### When to Add to Unreleased vs Version

- **Active development**: Add to `## [Unreleased]` section
- **Preparing release**: Move unreleased items to versioned section `## [X.Y.Z]`

---

## XML Documentation Standards

### Required Elements

```csharp
/// <summary>
/// Brief description of the type or member (one sentence).
/// </summary>
/// <remarks>
/// Additional details, usage notes, or important caveats.
/// Only include if there's genuinely more to say.
/// </remarks>
/// <typeparam name="T">Description of type parameter.</typeparam>
/// <param name="paramName">Description of parameter.</param>
/// <returns>Description of return value.</returns>
/// <exception cref="ArgumentNullException">When <paramref name="paramName"/> is null.</exception>
/// <example>
/// <code>
/// // Usage example
/// var result = MyMethod(input);
/// </code>
/// </example>
public T MyMethod<T>(string paramName) { }
```

### XML Doc Guidelines

1. **Summary is mandatory** for all public types and members
2. **Keep summary concise** — One sentence, ideally under 100 characters
3. **Document exceptions** — List all exceptions that can be thrown
4. **Use `<paramref>` and `<typeparamref>`** — For referencing parameters in descriptions
5. **Avoid redundancy** — Don't repeat the method name in the summary
6. **Use `<inheritdoc/>`** — When implementing interfaces or overriding, inherit docs

### Example: Well-Documented Method

```csharp
/// <summary>
/// Finds all items within the specified radius of a point.
/// </summary>
/// <param name="center">The center point of the search area.</param>
/// <param name="radius">The search radius. Must be positive.</param>
/// <param name="results">Buffer to store results. Cleared before use.</param>
/// <returns>The number of items found.</returns>
/// <exception cref="ArgumentOutOfRangeException">
/// When <paramref name="radius"/> is less than or equal to zero.
/// </exception>
/// <remarks>
/// Results are not guaranteed to be in any particular order.
/// For ordered results, use <see cref="QueryNearest"/>.
/// </remarks>
public int QueryRadius(Vector2 center, float radius, List<T> results)
```

---

## Documentation Checklist

**Before marking ANY work complete, verify these items.**

### Master Checklist (All Changes)

- [ ] All affected markdown docs updated
- [ ] XML docs on all public API members
- [ ] Code samples compile and run correctly
- [ ] CHANGELOG entry added
- [ ] Version info included for new features
- [ ] No broken links
- [ ] Technical terms defined when first used
- [ ] Language is clear and succinct

### For New Features

- [ ] Feature documentation in `docs/features/<category>/`
- [ ] XML documentation on all public types/members
- [ ] At least one working code sample in docs
- [ ] CHANGELOG entry in `### Added` section
- [ ] README updated if feature is significant
- [ ] llms.txt updated if feature adds new capabilities
- [ ] Related skills updated if feature affects workflows
- [ ] "Added in vX.Y.Z" version annotation included

### For Bug Fixes

- [ ] CHANGELOG entry in `### Fixed` section
- [ ] Documentation corrected if it described wrong behavior
- [ ] Code samples fixed if they demonstrated the bug

### For API Changes

- [ ] All documentation referencing old API updated
- [ ] Migration notes if breaking change
- [ ] CHANGELOG entry (in `### Changed` or `### Breaking Changes`)
- [ ] XML docs updated with new parameter names/types
- [ ] Code samples updated throughout
- [ ] **Breaking**: prefix used in CHANGELOG for breaking changes

### For Performance Improvements

- [ ] CHANGELOG entry in `### Improved` section
- [ ] Performance docs updated if metrics changed
- [ ] Benchmark results documented if available

---

## Common Documentation Mistakes

### ❌ Avoid These

| Mistake                         | Why It's Wrong                                |
| ------------------------------- | --------------------------------------------- |
| Copy-paste code without testing | Leads to broken examples users can't run      |
| "See code for details"          | Users shouldn't need to read source           |
| Outdated parameter names        | Causes confusion when code doesn't match docs |
| Missing edge case documentation | Users hit unexpected behavior                 |
| Version info missing            | Users don't know if feature exists            |
| Overly verbose explanations     | Readers lose focus; key info gets buried      |

### ✅ Do These Instead

| Best Practice             | Benefit                                  |
| ------------------------- | ---------------------------------------- |
| Test all code samples     | Users can copy-paste and run immediately |
| Self-contained examples   | No hunting for dependencies or context   |
| Document nullability      | Prevents null reference exceptions       |
| List all exceptions       | Users can handle errors appropriately    |
| Include "Added in vX.Y"   | Users know feature availability          |
| Front-load important info | Key details visible without scrolling    |

---

## Good vs Bad Examples

### XML Documentation

#### Bad Example: Vague, incomplete, no examples

```csharp
/// <summary>
/// Gets a random number.
/// </summary>
public int GetRandom(int max) { }
```

#### Good Example: Complete, clear, with example

```csharp
/// <summary>
/// Returns a random integer in the range [0, <paramref name="max"/>).
/// </summary>
/// <param name="max">The exclusive upper bound. Must be positive.</param>
/// <returns>A random integer from 0 (inclusive) to <paramref name="max"/> (exclusive).</returns>
/// <exception cref="ArgumentOutOfRangeException">
/// When <paramref name="max"/> is less than or equal to zero.
/// </exception>
/// <example>
/// <code>
/// int roll = random.GetRandom(6) + 1; // Returns 1-6
/// </code>
/// </example>
public int GetRandom(int max) { }
```

### CHANGELOG Entries

#### Bad Example: Vague, no context

```markdown
### Added

- Added new feature
- Bug fix

### Fixed

- Fixed issue
```

#### Good Example: Specific, actionable, descriptive

```markdown
### Added

- **WButton Odin Support**: WButton now works automatically with Odin Inspector's `SerializedMonoBehaviour` and `SerializedScriptableObject`
  - No setup required - just use `[WButton]` on methods in Odin types
  - Custom editors registered specifically for Odin types when `ODIN_INSPECTOR` symbol is defined

### Fixed

- Fixed null reference exception in `SerializableDictionary` drawer when dictionary contains null values on Unity 2021.3+
```

### Feature Documentation

#### Bad Example: Missing context, no example

```markdown
## WButton

Use WButton on methods.
```

#### Good Example: Complete with example and details

````markdown
## WButton Attribute

The `[WButton]` attribute exposes methods as clickable buttons in the Unity Inspector.

### Basic Usage

```csharp
using WallstopStudios.UnityHelpers.Attributes;

public class MyComponent : MonoBehaviour
{
    [WButton]
    public void DoSomething()
    {
        Debug.Log("Button clicked!");
    }
}
```
````

### Parameters

| Parameter        | Type               | Default | Description                                    |
| ---------------- | ------------------ | ------- | ---------------------------------------------- |
| `buttonName`     | `string`           | `null`  | Custom button label (uses method name if null) |
| `groupPriority`  | `int`              | `0`     | Order within button group (lower = first)      |
| `groupPlacement` | `WButtonPlacement` | `Top`   | Position buttons at top or bottom              |

### Notes

- Works with `async Task` methods (Added in v3.0.0)
- Supports Odin Inspector types automatically (Added in v3.0.4)

````text

---

## Indicating New Behavior

When documenting new features or changed behavior, clearly indicate the change:

### In CHANGELOG

```markdown
### Added

- **WButton Grouping**: New `groupPriority` and `groupPlacement` parameters for controlling button layout
  - `groupPriority` determines button order within a group (lower = earlier)
  - `groupPlacement` controls whether buttons appear at top or bottom of inspector
````

### In Feature Documentation

```markdown
## Button Grouping (Added in v3.0.0)

WButton now supports grouping buttons together with custom ordering...
```

### In XML Documentation

```csharp
/// <summary>
/// Gets or sets the group priority for button ordering.
/// </summary>
/// <remarks>
/// Added in v3.0.0. Lower values appear first within a group.
/// Default is 0.
/// </remarks>
public int GroupPriority { get; set; }
```

---

## Integration with Other Skills

This skill should be invoked alongside:

- [create-csharp-file](create-csharp-file.md) — New files need XML docs
- [create-test](create-test.md) — Test files serve as documentation
- [validate-before-commit](validate-before-commit.md) — Validates doc formatting
- [format-code](format-code.md) — Formats markdown and JSON docs

---

## Quick Reference Commands

```bash
# Validate documentation links
npm run lint:docs

# Format markdown
npm run format:md

# Check all documentation formatting
npm run validate:content
```

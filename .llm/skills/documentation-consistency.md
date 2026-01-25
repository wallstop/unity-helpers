# Skill: Documentation Consistency

<!-- trigger: docs consistency, cross-file consistency, performance claims, time estimates | When writing or reviewing documentation | Core -->

**Trigger**: When writing or reviewing documentation across multiple files (root README, docs readme, docs index, etc.)

---

## When to Use

This skill applies when creating or modifying documentation that appears in multiple locations or makes claims that must be consistent across the codebase.

---

## When NOT to Use

This skill is NOT needed for:

- **Single-file documentation changes** — No cross-file consistency to maintain
- **Internal code comments** — Not user-facing, no consistency requirement
- **Test files or examples** — Isolated documentation, doesn't need to match other files
- **CHANGELOG entries** — Already have their own format rules (see [update-documentation](./update-documentation.md))
- **XML documentation comments** — Per-member docs, not cross-file claims

---

## Key Principles

### 1. Performance Claims Must Be Consistent

Performance numbers MUST match across all documentation files. If a benchmark shows a range, use the same range everywhere.

| ❌ Inconsistent                                          | ✅ Consistent                                              |
| -------------------------------------------------------- | ---------------------------------------------------------- |
| README says "100x faster", docs/readme says "12x faster" | All files say "10-100x faster (varies by operation)"       |
| One file says "up to 15x", another says "10-15x"         | All files use the same phrasing: "10-15x faster"           |
| Vague claim without context                              | Specific claim with context: "~12x for method invocations" |

**Best Practice**: When performance varies by scenario, document the range AND explain what affects it:

```markdown
<!-- ✅ GOOD: Clear, consistent, explains variance -->

Cached delegates are 10-100x faster than raw `System.Reflection`
(method invocations ~12x; boxed scenarios up to 100x)

<!-- ❌ BAD: Inconsistent claims in different files -->

File A: "100x faster than System.Reflection"
File B: "up to 12x faster than System.Reflection"
```

### 2. Time Estimates Use Tilde (~) Prefix

All time estimates should use the tilde (~) prefix to indicate approximation.

| ❌ Without tilde | ✅ With tilde |
| ---------------- | ------------- |
| 2 minutes        | ~2 minutes    |
| 5 minutes        | ~5 minutes    |
| 10 minutes       | ~10 minutes   |
| 1 minute         | ~1 minute     |

**Example table**:

```markdown
| Task                    | Time to Value |
| ----------------------- | ------------- |
| Inspector Tooling setup | ~2 minutes    |
| Component wiring        | ~2 minutes    |
| Effects system          | ~5 minutes    |
```

### 3. Avoid Redundant Prefixes When Emoji Conveys Meaning

When using emoji to signal meaning (⏱️ for time, ✅ for success, etc.), do NOT add text prefixes that repeat the same information.

| ❌ Redundant                      | ✅ Clean            | Why                                          |
| --------------------------------- | ------------------- | -------------------------------------------- |
| ⏱️ **Estimated Example:** 2 min   | ⏱️ 2 min            | "Estimated Example:" is redundant with emoji |
| ⏱️ **Estimated:** ~5 minutes      | ⏱️ ~5 minutes       | ⏱️ already means "time/duration"             |
| ⏱️ **Time Estimate:** ~10 minutes | ⏱️ ~10 minutes      | Emoji is self-explanatory                    |
| ✅ **Success:** Tests passed      | ✅ Tests passed     | ✅ already signals success                   |
| ⚠️ **Warning:** Be careful        | ⚠️ Be careful       | ⚠️ already signals warning                   |
| 🎯 **Target:** High performance   | 🎯 High performance | Emoji already conveys "target/goal"          |

**Correct Patterns**:

```markdown
<!-- ✅ GOOD: Emoji alone signals meaning -->

⏱️ ~2 minutes
⏱️ ~5 minutes setup
🚀 10-15x faster than alternatives

<!-- ❌ BAD: Redundant text repeats what emoji shows -->

⏱️ **Estimated:** ~2 minutes
⏱️ **Time to complete:** ~5 minutes
🚀 **Performance:** 10-15x faster than alternatives
```

**Rule**: If an emoji clearly conveys the category (time, warning, success), the following text should be the VALUE, not a label describing the category.

**Exception — Distinct Metric Names**: When the label describes a _specific metric_ rather than the generic category, include it for clarity:

```markdown
<!-- ✅ OK: "Time Saved" is a specific metric, not just "time" -->

**⏱️ Time Saved:** 10-20 lines × hundreds of components = weeks

<!-- ✅ OK: "Time to Value" is a specific metric -->

**⏱️ Time to Value:** ~2 minutes

<!-- ❌ BAD: "Estimated" just restates "time" (the emoji's meaning) -->

⏱️ **Estimated:** ~2 minutes
```

The distinction: "Time Saved" and "Time to Value" are _metric names_ that add meaning beyond what ⏱️ conveys. "Estimated" or "Duration" merely restate the emoji's meaning.

### 4. Consistent Performance Claim Phrasing

Use consistent phrasing patterns for performance claims. Pick ONE pattern and use it everywhere.

| ❌ Inconsistent (same doc)                         | ✅ Consistent (same doc)                 |
| -------------------------------------------------- | ---------------------------------------- |
| "10-15x faster" here, "up to 15x" there            | "10-15x faster" everywhere               |
| "100x speedup" here, "up to 100x faster" there     | "10-100x faster" everywhere (with range) |
| "outperforms by 12x" here, "12x improvement" there | "~12x faster" everywhere                 |

**Standard Phrasing Patterns**:

```markdown
<!-- ✅ PREFERRED: Use these patterns consistently -->

Range format: 10-15x faster than [baseline]
Single value: ~12x faster than [baseline]
Comparison: X compared to [baseline] (NOT "vs" or "versus")

<!-- ❌ AVOID: These create inconsistency -->

"up to 15x faster" → Use "10-15x faster" (shows full range)
"as much as 100x speedup" → Use "10-100x faster" (consistent verb)
"12x improvement" → Use "~12x faster" (consistent structure)
"outperforms by X" → Use "X faster than" (clearer comparison)
```

**Why avoid "up to X"?** It implies the lower bound is unknown or insignificant. If you know the range, state it explicitly (e.g., "10-15x faster" instead of "up to 15x faster").

### 5. Avoid Run-On Sentences

Don't combine multiple distinct claims in a single sentence. Break them into clear, scannable points.

```markdown
<!-- ❌ BAD: Run-on sentence combining multiple claims -->

Random generation is 10-15x faster than Unity.Random (see benchmarks),
spatial queries use O(log n) algorithms for efficient large dataset handling,
and declarative inspector attributes can reduce custom editor code.

<!-- ✅ GOOD: Separated into clear points -->

Key performance highlights:

- 10-15x faster random generation than Unity.Random (see benchmarks)
- O(log n) spatial queries for efficient large dataset handling
- Declarative inspector attributes to reduce custom editor code
```

### 6. When to Use Bullet Lists vs Sentences

Use bullet lists for **multiple parallel items**. Use sentences for **single concepts or narrative flow**.

| Situation                          | Use           | Why                                    |
| ---------------------------------- | ------------- | -------------------------------------- |
| 3+ related features/capabilities   | Bullet list   | Easier to scan, each item stands alone |
| 2 items that are closely related   | Sentence OK   | "X and Y" is natural                   |
| Sequential steps or instructions   | Numbered list | Order matters                          |
| Single claim with explanation      | Sentence      | Context flows naturally                |
| Comparison of alternatives         | Table         | Side-by-side comparison                |
| Key-value pairs (feature: benefit) | Bullet list   | Clear association                      |

**Convert to bullets when:**

```markdown
<!-- ❌ BAD: Run-on list disguised as a sentence -->

This package provides PRNGs that are 10-15x faster, spatial trees with O(log n) queries,
zero-allocation pooling for collections and arrays, and thread-safe singletons.

<!-- ✅ GOOD: Proper bullet list -->

This package provides:

- PRNGs that are 10-15x faster than Unity.Random
- Spatial trees with O(log n) queries
- Zero-allocation pooling for collections and arrays
- Thread-safe singletons
```

**Keep as sentence when:**

```markdown
<!-- ✅ GOOD: Single claim with context, flows naturally -->

The spatial hash provides O(1) average-case lookups for grid-aligned queries.

<!-- ❌ UNNECESSARY: Over-formatted for simple content -->

Key features:

- O(1) average-case lookups for grid-aligned queries
```

### 7. Eliminate Redundancy

Don't repeat the same concept with different wording in the same section.

```markdown
<!-- ❌ BAD: Redundant statements -->

### Schema Evolution

Schema evolution support for backward-compatible serialization.
Forward and backward compatible serialization.

<!-- ✅ GOOD: Single clear statement -->

### Schema Evolution

Forward and backward compatible serialization — add new fields
without breaking existing saves.
```

### 8. Use Clear, Unambiguous Phrasing

Avoid phrasing that could be misread or misunderstood.

```markdown
<!-- ❌ BAD: Awkward, could be misread -->

Benchmarks show 10-15x faster random generation than Unity.Random

<!-- ✅ GOOD: Clear comparison structure -->

Benchmarks demonstrate 10-15x faster random generation compared to Unity.Random
```

### 9. Dependency Claims Must Be Accurate

Dependency descriptions must accurately reflect the package structure.

| Scenario                           | Correct Description                              |
| ---------------------------------- | ------------------------------------------------ |
| Dependency is bundled with package | "Zero external dependencies — [name] is bundled" |
| Dependency must be installed       | "Requires [name] for [feature]"                  |
| Optional dependency                | "Optional: [name] for [feature]"                 |

```markdown
<!-- ✅ GOOD: Accurate for bundled dependency -->

- ✅ **Zero external dependencies** — protobuf-net is bundled for binary serialization

<!-- ❌ BAD: Confusing/contradictory -->

- ✅ **Minimal external dependencies** - depends on protobuf-net for binary serialization
```

### 10. Anchor Link Format Rules

When creating anchor links (links to headings within a document), use consistent format.

**Standard Anchor Format** (GitHub/MkDocs compatible):

```markdown
<!-- ✅ CORRECT: Lowercase, hyphens for spaces, no special chars -->

[Link Text](#heading-name)
[Performance Section](#performance-claims)
[Time Estimates](#2-time-estimates-use-tilde--prefix)

<!-- ❌ WRONG: Various incorrect formats -->

[Link](#HeadingName) ← Wrong: Keep lowercase
[Link](#heading_name) ← Wrong: Use hyphens, not underscores
[Link](#Heading Name) ← Wrong: No spaces allowed
[Link](#heading-name-) ← Wrong: No trailing hyphens
```

**Anchor Generation Rules** (GitHub Flavored Markdown):

| Heading Text                | Correct Anchor                             |
| --------------------------- | ------------------------------------------ |
| `## Quick Start`            | `#quick-start`                             |
| `## 2. Time Estimates`      | `#2-time-estimates`                        |
| `## C# Code Examples`       | `#c-code-examples` (special chars removed) |
| `## What's New in v3.0`     | `#whats-new-in-v30`                        |
| `## PRNGs (Random Numbers)` | `#prngs-random-numbers`                    |
| `## ⚡ Top Time-Savers`     | `#-top-time-savers` (emoji → leading `-`)  |
| `### 2. 🔌 Auto-Wire`       | `#2--auto-wire` (period + emoji → `--`)    |

**Rules**:

1. Convert to lowercase
2. Replace spaces with hyphens (`-`)
3. Remove special characters (including emoji) except hyphens
4. Collapse multiple hyphens into one (but see emoji note below)
5. Remove trailing hyphens

**Emoji in Headings**: When a heading starts with emoji (e.g., `## ⚡ Top`), the emoji is removed but the resulting space becomes a leading hyphen. Markdownlint preserves this leading hyphen (e.g., `#-top`), even though some platforms may strip it. **Use markdownlint's format** (`#-top-time-savers`) for consistency with CI validation.

**Cross-Platform Compatibility**: If targeting both GitHub and MkDocs, test anchors on both platforms. MkDocs may handle some edge cases differently.

---

## Cross-File Consistency Checklist

When updating documentation, verify these items are consistent across ALL documentation files:

### Performance Claims

- [ ] Random generation speed (e.g., "10-15x faster than Unity.Random")
- [ ] Reflection speedup (e.g., "10-100x faster, varies by operation")
- [ ] Spatial query complexity (e.g., "O(log n)")
- [ ] Test count (e.g., "8,000+ tests")

### Feature Descriptions

- [ ] Dependency status (zero/bundled/external)
- [ ] Platform support claims (IL2CPP, WebGL, etc.)
- [ ] Compatibility statements (Unity versions)

### Formatting

- [ ] Time estimates use tilde (~) prefix
- [ ] No redundant prefixes when emoji conveys meaning
- [ ] Consistent performance claim phrasing (prefer "X-Y faster" ranges)
- [ ] Consistent use of bold/emphasis
- [ ] Consistent bullet point style
- [ ] 3+ parallel items use bullet lists, not run-on sentences
- [ ] Anchor links use lowercase-hyphenated format

---

## Files to Cross-Reference

When making documentation changes, check these files for consistency:

| File                                                                    | Purpose                     |
| ----------------------------------------------------------------------- | --------------------------- |
| [README](../../README.md)                                               | Root project readme         |
| [docs/readme](../../docs/readme.md)                                     | Detailed documentation      |
| [docs/index](../../docs/index.md)                                       | Documentation site homepage |
| [docs/overview/getting-started](../../docs/overview/getting-started.md) | Onboarding guide            |
| [llms.txt](../../llms.txt)                                              | LLM-friendly summary        |

---

## Common Mistakes

| Mistake                                | Impact                               | Fix                                         |
| -------------------------------------- | ------------------------------------ | ------------------------------------------- |
| Different performance numbers in files | Undermines credibility               | Use exact same numbers everywhere           |
| Missing tilde on time estimates        | Implies false precision              | Add ~ prefix to all estimates               |
| Run-on sentences with multiple claims  | Hard to scan, reduces comprehension  | Break into bullet points or short sentences |
| Redundant statements                   | Wastes reader time, looks unpolished | Consolidate to single clear statement       |
| Contradictory dependency claims        | Confuses users about requirements    | Clarify bundled vs external status          |
| Redundant prefix with emoji            | Cluttered, unprofessional            | Remove prefix when emoji conveys meaning    |
| "Up to X" instead of range             | Hides lower bound, inconsistent      | Use "X-Y faster" range format               |
| Wrong anchor format                    | Broken links, 404 errors             | Use lowercase-hyphenated format             |
| Inline list in sentence                | Hard to scan                         | Convert 3+ items to bullet list             |

---

## Related Skills

- [update-documentation](./update-documentation.md) — When and how to update docs
- [markdown-reference](./markdown-reference.md) — Markdown formatting rules
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation

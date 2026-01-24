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

### 3. Avoid Run-On Sentences

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

### 4. Eliminate Redundancy

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

### 5. Use Clear, Unambiguous Phrasing

Avoid phrasing that could be misread or misunderstood.

```markdown
<!-- ❌ BAD: Awkward, could be misread -->

Benchmarks show 10-15x faster random generation than Unity.Random

<!-- ✅ GOOD: Clear comparison structure -->

Benchmarks demonstrate 10-15x faster random generation compared to Unity.Random
```

### 6. Dependency Claims Must Be Accurate

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
- [ ] Consistent use of bold/emphasis
- [ ] Consistent bullet point style

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

---

## Related Skills

- [update-documentation](./update-documentation.md) — When and how to update docs
- [markdown-reference](./markdown-reference.md) — Markdown formatting rules
- [validate-before-commit](./validate-before-commit.md) — Pre-commit validation

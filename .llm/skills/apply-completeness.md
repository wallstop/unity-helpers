# Skill: Apply Completeness

<!-- trigger: completeness, boil-the-lake, thorough, do-everything, comprehensive | Always do the complete thing when cost is near-zero | Core -->

**Trigger**: When deciding whether to do extra work that makes a change more complete, robust, or thorough.

---

## When to Use

- When you notice adjacent improvements during any task
- When deciding "should I also fix X while I'm here?"
- When a change is 80% done and the remaining 20% is mechanical
- As a guiding principle for all implementation work

---

## When NOT to Use

- When the extra work is speculative ("we might need this someday")
- When the extra work changes scope significantly (different feature, different system)
- When the change is time-critical and the extras can be a follow-up
- When the extras require new dependencies or architectural decisions
- When you've already made 3+ fixes in a review session (use [self-regulate-changes](./self-regulate-changes.md) to track risk)

---

## Core Principle

> When the incremental cost of doing the complete thing approaches zero, always do the complete thing.

AI-assisted development compresses effort dramatically. Tasks that previously took hours now take seconds. When this is true, do not leave partial work — finish it.

---

## Effort Compression Table

| Task Type                              | Effort Without AI | Effort With AI | Do It?                   |
| -------------------------------------- | ----------------- | -------------- | ------------------------ |
| Add null checks to adjacent code       | 5 min             | 5 sec          | **Yes**                  |
| Update all XML docs for changed type   | 15 min            | 10 sec         | **Yes**                  |
| Add tests for edge cases you noticed   | 30 min            | 1 min          | **Yes**                  |
| Fix spelling in all files you touched  | 10 min            | 5 sec          | **Yes**                  |
| Refactor adjacent unrelated system     | 2 hours           | 30 min         | **No** — different scope |
| Add feature user didn't ask for        | 1 day             | 1 hour         | **No** — speculative     |
| Update CHANGELOG for your changes      | 5 min             | 10 sec         | **Yes**                  |
| Ensure meta files exist for new assets | 2 min             | 5 sec          | **Yes**                  |

---

## Decision Framework

```text
1. Is this work directly related to the current change?
   No → SKIP (different scope)
   Yes ↓
2. Is this mechanical/obvious, requiring no design judgment?
   No → ASK (needs human input)
   Yes ↓
3. Can this be completed in under 2 minutes of AI time?
   No → NOTE for follow-up
   Yes ↓
4. DO IT NOW
```

---

## Lake vs. Ocean

**Lake** = bounded, finite, completable:

- All public methods on a type have XML docs
- All new files have `.meta` files
- All test cases for a method are covered
- All references to a renamed symbol are updated

**Ocean** = unbounded, never done, scope creep:

- All types in the entire codebase have XML docs
- All possible edge cases for all methods
- All tech debt in adjacent code
- All performance optimizations possible

**Rule**: Boil the lake. Do not attempt to boil the ocean.

---

## Anti-Patterns

| Anti-Pattern                                       | Better Approach                                                        |
| -------------------------------------------------- | ---------------------------------------------------------------------- |
| "I'll come back to it"                             | Finish it now if < 2 min                                               |
| "That's out of scope" (for trivial fixes)          | Include it — the scope boundary isn't worth the overhead               |
| "I only changed line 5" (but lines 3-7 have a bug) | Fix the surrounding context you're already reading                     |
| "Tests pass so I'm done"                           | Check: is CHANGELOG updated? Are docs current? Are meta files present? |

---

## Related Skills

- [review-code-changes](./review-code-changes.md) - Catches when completeness was skipped
- [self-regulate-changes](./self-regulate-changes.md) - Balancing act: completeness during dev vs. stopping cascading fixes during review
- [validate-before-commit](./validate-before-commit.md) - Pre-commit quality gate
- [update-documentation](./update-documentation.md) - Documentation completeness

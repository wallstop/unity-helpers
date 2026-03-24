# Skill: Ask Structured Questions

<!-- trigger: ask-question, clarify, structured-question, user-input, disambiguation | Present questions with context, options, and recommendations | Core -->

**Trigger**: When you need human input to proceed — ambiguous requirements, design tradeoffs, or judgment calls.

---

## When to Use

- When a task has multiple valid approaches and you need direction
- When you discover something unexpected that changes the plan
- When a fix requires behavioral judgment (not mechanical)
- When scope is ambiguous and needs clarification

---

## When NOT to Use

- For mechanical decisions with clear best practices (just do it)
- When the answer is already in the codebase or documentation
- When the question can be answered by reading more context first

---

## Before Asking: Try to Answer Yourself

Before presenting a question:

1. **Search the codebase** for existing patterns or precedent
2. **Read the relevant skill files** for guidance
3. **Check [context](../context.md)** for stated conventions
4. **Look at adjacent code** for implicit conventions

If you find the answer, proceed without asking. Only ask when multiple valid approaches remain after research.

---

## Question Format

Every question to a human must follow this structure:

### 1. Re-Ground Context

One sentence reminding the human what you're working on and why this question arose:

> "While implementing the buffer pool resize logic, I found two valid approaches for handling concurrent access."

### 2. Plain English Summary

Explain the situation without jargon. Assume the reader hasn't been following every step:

> "The buffer pool needs to grow when demand exceeds capacity. The question is whether to block callers during resize or allow temporary over-allocation."

### 3. Options with Tradeoffs

Present 2-4 concrete options. Each option has:

| Component       | Description                                             |
| --------------- | ------------------------------------------------------- |
| **Label**       | Letter (A, B, C) and short name                         |
| **Description** | What this option does, in 1-2 sentences                 |
| **Tradeoff**    | What you gain and what you lose                         |
| **Effort**      | Relative effort (trivial, small, moderate, significant) |

Example:

> **A) Block during resize** — Callers wait while the pool grows. Simpler code, but brief latency spike. Effort: trivial.
>
> **B) Over-allocate temporarily** — Allow pool to exceed max during resize, shrink back later. No latency spike, but temporarily uses more memory. Effort: small.
>
> **C) Pre-allocate headroom** — Always keep 20% spare capacity. Prevents most resizes entirely. More memory usage baseline. Effort: moderate.

### 4. Recommendation

State which option you'd pick and why:

> "I recommend **B** because this pool is used in gameplay code where latency spikes are more disruptive than temporary over-allocation, and the shrink-back logic is straightforward."

---

## Question Anti-Patterns

| Anti-Pattern                                | Better Approach                                      |
| ------------------------------------------- | ---------------------------------------------------- |
| "What should I do?" (no options)            | Research first, then present specific options        |
| Wall of text with embedded question         | Lead with the question, then provide context         |
| "A or B?" with no tradeoffs                 | Always include what you gain/lose with each          |
| Asking multiple unrelated questions at once | One question per message; resolve sequentially       |
| Asking when you could search                | Search the codebase first                            |
| Technical jargon without explanation        | Plain English summary first, technical details after |

---

## Escalation vs. Question

Not all uncertainties need human input:

| Situation                                 | Action                                |
| ----------------------------------------- | ------------------------------------- |
| Two approaches, one clearly better        | Just do the better one                |
| Two approaches, genuinely tied            | Ask                                   |
| Missing information, findable in codebase | Search, don't ask                     |
| Missing information, not in codebase      | Ask                                   |
| Risk of data loss or breaking change      | Always ask                            |
| Reversible low-risk decision              | Make the call, note it in your output |

---

## Related Skills

- [review-plan](./review-plan.md) - Uses one-issue-at-a-time questioning
- [self-regulate-changes](./self-regulate-changes.md) - When to stop and ask vs. continue
- [review-code-changes](./review-code-changes.md) - ASK classification in Fix-First

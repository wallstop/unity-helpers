# Skill: GitHub Actions Shell Scripting

<!-- trigger: workflow shell, actions bash, gh api, runner temp, heredoc | Shell scripting best practices for GitHub Actions | Core -->

**Trigger**: When writing inline `run:` steps in GitHub Actions workflows.

---

## When to Use

- Writing `run:` steps in GitHub Actions workflows
- Using `gh api` or `curl` for GitHub API calls
- Passing multiline content between steps
- Parsing output from commands with shell tools
- Working with temporary files in CI environments

---

## When NOT to Use

- The logic should be a standalone, testable script
- A maintained action already covers the behavior
- Action outputs can replace parsing or polling

---

## Scope

This skill is an overview and checklist. Use the focused skills for implementation details:

- [github-actions-shell-foundations](./github-actions-shell-foundations.md) - Strict mode, temp files, heredocs, quoting, text processing, env vars, and annotations.
- [github-actions-shell-workflow-patterns](./github-actions-shell-workflow-patterns.md) - Action outputs, polling, GitHub API error handling, idempotency, job outputs, step summaries.

---

## Quick Checklist

- [ ] Every `run:` block starts with `set -euo pipefail`
- [ ] Use `$RUNNER_TEMP` instead of `/tmp` for temp files
- [ ] Multiline API bodies use `-F body=@file` not `-f body="..."`
- [ ] Heredocs use `<<-` with tabs or unindented content
- [ ] Check action outputs before writing polling code
- [ ] Polling loops (when necessary) have timeouts and exponential backoff
- [ ] AWK and sed use exact field matching (`$1 == "value"`)
- [ ] All variables are quoted (`"$VAR"` not `$VAR`)
- [ ] File and command existence checked before use
- [ ] API errors handled with clear messages, stderr captured for debugging
- [ ] Arithmetic uses `$((x + 1))` not `((x++))`
- [ ] Multiline `GITHUB_OUTPUT` uses random delimiters (`$(date +%s%N)_${RANDOM}`)
- [ ] Step-to-step content passed via env vars and `printf`, not heredocs in YAML
- [ ] Idempotent updates check before modifying (for re-runnable workflows)
- [ ] Job outputs defined for reusable workflows
- [ ] Step summaries added for visibility (`$GITHUB_STEP_SUMMARY`)
- [ ] Sensitive values masked with `::add-mask::`
- [ ] Run `actionlint` on workflow files before commit

---

## Related Skills

- [github-actions-shell-foundations](./github-actions-shell-foundations.md) - Inline shell safety and text handling patterns.
- [github-actions-shell-workflow-patterns](./github-actions-shell-workflow-patterns.md) - Workflow integration patterns and API handling.
- [github-actions-script-pattern](./github-actions-script-pattern.md) - Prefer standalone scripts for complex logic.

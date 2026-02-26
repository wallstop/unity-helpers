# Skill: Validate Before Commit

<!-- trigger: validate, commit, lint, check, verify | Before completing any task (run linters!) | Core -->

**Trigger**: **MANDATORY** before completing any task that modifies code or documentation.

---

## When to Use

Use this skill for pre-commit validation:

- Before completing any coding task
- Before asking user to review changes
- Before any discussion of "done" or "complete"
- After making ANY modifications to files

For detailed linter commands and configurations, see [linter-reference](./linter-reference.md).
For troubleshooting common errors, see [validation-troubleshooting](./validation-troubleshooting.md).

---

## Quick Reference

```bash
# Run all validations before completing any task
npm run validate:prepush
```

This single command runs ALL CI/CD checks locally, ensuring your changes will pass in GitHub Actions.

---

## The Golden Rules

### Rule 1: Format IMMEDIATELY After Every Change

**Do NOT batch formatting at the end of a task.** Format immediately after each file modification.

| File Type       | Formatter | Command                              |
| --------------- | --------- | ------------------------------------ |
| C# (`.cs`)      | CSharpier | `dotnet tool run csharpier format .` |
| Everything else | Prettier  | `npx prettier --write -- <file>`     |

### Rule 2: Run Linters IMMEDIATELY After Every Change

**Do NOT wait until task completion.** Run the appropriate linter after each file modification and fix issues before proceeding.

### Rule 3: Fix Before Moving On

1. Make a change to a file
2. Run the appropriate linter(s) for that file type
3. Fix any issues found
4. Only then move to the next file or task

---

## Common Mistakes

**Wrong** (batching until end):

1. Edit markdown file
2. Edit C# file
3. Edit YAML file
4. ... more edits ...
5. Run all formatters/linters at the end

**Correct** (format immediately after each):

1. Edit markdown -> `npx prettier --write -- <file>` -> `npm run lint:markdown`
2. Edit C# -> `dotnet tool run csharpier format .`
3. Edit YAML -> `npx prettier --write -- <file>` -> `npm run lint:yaml`
4. Edit test file -> `pwsh -NoProfile -File scripts/lint-tests.ps1` -> `dotnet tool run csharpier format .`

For detailed workflow patterns and more examples, see [formatting](./formatting.md).

---

## Workflow by File Type

### C# Changes

```bash
# After EVERY .cs file modification (even single-line edits):
dotnet tool run csharpier format .
npm run lint:csharp-naming
```

### Documentation Changes

```bash
# After EVERY .md file modification:
npx prettier --write -- <file>
npm run lint:spelling    # 🚨 #1 CI failure cause!
npm run lint:docs         # Validates links
npm run lint:markdown     # Structural rules
```

### YAML Changes

```bash
# After EVERY .yml/.yaml file modification:
npx prettier --write -- <file>
npm run lint:yaml

# For workflow files (.github/workflows/*.yml), also run:
actionlint
```

### Test File Changes

```bash
# 🚨 MANDATORY: After EVERY test file modification:
pwsh -NoProfile -File scripts/lint-tests.ps1

# Also run standard C# formatting:
dotnet tool run csharpier format .
npm run lint:csharp-naming
```

**CRITICAL**: The test linter is **MANDATORY** for any test file changes (files in `Tests/` directory). You **MUST** run it **IMMEDIATELY** after each test file modification — do NOT batch these checks at the end of your task.

**Why this matters**: Test lifecycle lint failures will cause the pre-push hook to fail. Catching and fixing these issues early (after each file change) prevents frustrating failures when you try to push your commits.

### Skill File Changes (`.llm/skills/*.md`)

```bash
# 🚨 MANDATORY: After EVERY skill file modification:
pwsh -NoProfile -File scripts/lint-skill-sizes.ps1

# Also run standard markdown formatting:
npx prettier --write -- <file>
npm run lint:markdown
```

**CRITICAL**: Skill files have a **500-line hard limit** enforced by the pre-commit hook. Files exceeding this limit **CANNOT be committed** and require human judgment to split.

| Lines   | Action Required                                          |
| ------- | -------------------------------------------------------- |
| <300    | No action needed                                         |
| 300-500 | Consider splitting preemptively to avoid future blockers |
| >500    | **MUST split before commit** — hook will reject the file |

**Why this matters**: Splitting large skill files requires human judgment (deciding topic boundaries, updating cross-references). Catching size issues early prevents blocking commits when you've completed all other work.

---

## What Gets Validated

The `npm run validate:prepush` command runs these checks:

1. **validate:content** — Documentation and formatting
   - `lint:docs` — Markdown links (no backtick `.md` refs)
   - `lint:markdown` — Markdownlint rules
   - `format:md:check` — Prettier markdown formatting
   - `format:json:check` — Prettier JSON/asmdef formatting
   - `format:yaml:check` — Prettier YAML formatting

2. **eol:check** — Line endings (CRLF, no BOM)

3. **validate:tests** — Test lifecycle lint (Track() usage)

4. **lint:csharp-naming** — C# naming conventions

---

## Critical Link Formatting Rules

For complete link formatting rules, escaping patterns, and linting rules, see [markdown-reference](./markdown-reference.md).

Key requirements:

- **ALL internal links MUST use `./` or `../` prefix** — no bare paths
- **NEVER use backtick-wrapped file references** — use proper markdown links
- **NEVER use absolute GitHub Pages paths** — no `/unity-helpers/...` paths

---

## Documentation Checklist

Before completing ANY task:

### Prettier Self-Check (MANDATORY)

- [ ] Did I run `npx prettier --write -- <file>` IMMEDIATELY after EVERY non-C# file?
- [ ] Did I verify each file with `npx prettier --check -- <file>`?
- [ ] Did I check config files too? (`.devcontainer/devcontainer.json`, `package.json`, etc.)
- [ ] Final check: `npx prettier --check -- .` passes?

### For New Features

- [ ] Feature documentation added/updated
- [ ] XML documentation on all public types/members
- [ ] At least one working code sample
- [ ] CHANGELOG entry in `### Added` section
- [ ] llms.txt updated if feature adds new capabilities

### For Bug Fixes

- [ ] CHANGELOG entry in `### Fixed` section
- [ ] Documentation corrected if it described wrong behavior

### For API Changes

- [ ] All documentation referencing old API updated
- [ ] CHANGELOG entry (in `### Changed`, marked Breaking if applicable)
- [ ] XML docs updated
- [ ] Code samples updated

---

## Pre-Existing Warnings

Some lint warnings may exist in the main branch. Focus on:

1. **New warnings** introduced by your changes
2. **Failing checks** (exit code 1)

If `validate:content` and `lint:csharp-naming` pass, your changes are ready.

---

## CLI Argument Safety

When passing file lists to CLI tools (prettier, markdownlint, yamllint, etc.), ALWAYS use a `--` end-of-options separator before the file arguments.

### Why

Without `--`, a staged filename like `--plugin=./evil.js` or `--config=malicious.yml` would be interpreted as a CLI option, not a filename. This is an option injection vulnerability.

### Pattern

```bash
# WRONG - filenames can be interpreted as options
npx --no-install prettier --write "${FILES[@]}"

# CORRECT - `--` prevents filenames from being treated as options
npx --no-install prettier --write -- "${FILES[@]}"
```

This applies to ALL tools that accept file arguments:

- `prettier --write -- "${FILES[@]}"`
- `markdownlint --fix --config X -- "${FILES[@]}"`
- `yamllint -c config.yaml -- "${FILES[@]}"`

In PowerShell scripts, add `'--'` to argument arrays before file paths:

```powershell
$cmdArgs = @('--yes', 'prettier', '--write', '--') + $filePaths
```

---

## Related Skills

- [linter-reference](./linter-reference.md) — Detailed linter commands and configurations
- [validation-troubleshooting](./validation-troubleshooting.md) — Common errors and fixes
- [update-documentation](./update-documentation.md) — Documentation requirements
- [formatting](./formatting.md) — CSharpier, Prettier, markdownlint workflow
- [markdown-reference](./markdown-reference.md) — Link formatting, structural rules
- [create-test](./create-test.md) — Test file requirements
- [test-data-driven](./test-data-driven.md) — Data-driven testing patterns
- [test-naming-conventions](./test-naming-conventions.md) — Naming rules and legacy test migration
- [manage-skills](./manage-skills.md) — Skill file maintenance and index regeneration

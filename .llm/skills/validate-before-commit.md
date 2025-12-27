# Skill: Validate Before Commit

**Trigger**: **MANDATORY** before completing any task that modifies code or documentation.

---

## Quick Reference

```bash
# Run all validations before completing any task
npm run validate:prepush
```

This single command runs ALL CI/CD checks locally, ensuring your changes will pass in GitHub Actions.

---

## When to Run

**ALWAYS run validation before:**

- Completing any coding task
- Asking user to review changes
- Before any discussion of "done" or "complete"
- After making ANY modifications to files

---

## What Gets Validated

The `npm run validate:prepush` command runs these checks in order:

1. **validate:content** — Documentation and formatting
   - `lint:docs` — Check markdown links
   - `lint:markdown` — Markdownlint rules
   - `format:md:check` — Prettier markdown formatting
   - `format:json:check` — Prettier JSON/asmdef formatting
   - `format:yaml:check` — Prettier YAML formatting

2. **eol:check** — Line endings
   - Ensures CRLF line endings
   - No BOM markers

3. **validate:tests** — Test lifecycle lint
   - Unity object tracking in tests
   - CommonTestBase inheritance

4. **lint:csharp-naming** — C# naming conventions
   - No underscores in method names
   - PascalCase for all methods

---

## Individual Check Commands

### C# Formatting (CSharpier)

```bash
# Check formatting (will fail if changes needed)
dotnet tool run csharpier check .

# Auto-fix formatting
dotnet tool run csharpier format .
```

### Markdown Formatting

```bash
# Check markdown formatting
npm run format:md:check

# Auto-fix markdown formatting
npm run format:md
```

### Markdownlint

```bash
# Check markdown lint rules
npm run lint:markdown
```

Common fixes:

- **MD032**: Add blank line before and after lists
- **MD009**: Remove trailing spaces
- **MD022**: Add blank line after headings
- **MD031**: Add blank line around fenced code blocks

### JSON/asmdef/asmref Formatting

```bash
# Check JSON formatting
npm run format:json:check

# Auto-fix JSON formatting
npm run format:json
```

### YAML Formatting

```bash
# Check YAML formatting
npm run format:yaml:check

# Auto-fix YAML formatting
npm run format:yaml
```

### C# Naming Conventions

```bash
# Check for underscore violations
npm run lint:csharp-naming
```

**Rules:**

- NO underscores in method names (including tests)
- Use PascalCase: `WhenInputIsNullReturnsDefault` NOT `When_Input_Is_Null_Returns_Default`
- Test data names: Use dots `TestCase.Scenario.Expected` NOT underscores

### Documentation Links

```bash
# Check for broken links in docs
npm run lint:docs
```

### End-of-Line Characters

```bash
# Check EOL characters
npm run eol:check

# Auto-fix EOL characters
npm run eol:fix
```

---

## CI/CD Check Mapping

| GitHub Action Workflow       | Local Command                       | Auto-Fix Command                     |
| ---------------------------- | ----------------------------------- | ------------------------------------ |
| CSharpier Auto Format        | `dotnet tool run csharpier check .` | `dotnet tool run csharpier format .` |
| Prettier Auto Fix (Markdown) | `npm run format:md:check`           | `npm run format:md`                  |
| Prettier Auto Fix (JSON)     | `npm run format:json:check`         | `npm run format:json`                |
| Prettier Auto Fix (YAML)     | `npm run format:yaml:check`         | `npm run format:yaml`                |
| Markdown & JSON Lint/Format  | `npm run validate:content`          | Run individual fix commands          |
| YAML Format + Lint           | `npm run format:yaml:check`         | `npm run format:yaml`                |
| C# Naming Convention Lint    | `npm run lint:csharp-naming`        | Rename methods manually              |
| Lint Docs Links              | `npm run lint:docs`                 | Fix broken links manually            |

---

## Common Failures and Solutions

### CSharpier Formatting Failed

```text
Error ./Editor/Utils/WButton/WButtonEditorHelper.cs - Was not formatted.
```

**Fix:**

```bash
dotnet tool run csharpier format .
```

### Markdownlint MD032 (Blanks Around Lists)

```text
docs/features/inspector/inspector-button.md:1154 MD032/blanks-around-lists
```

**Fix:** Add a blank line before and after the list:

```markdown
<!-- WRONG -->

Text before list:

- Item 1
- Item 2
  Text after list

<!-- CORRECT -->

Text before list:

- Item 1
- Item 2

Text after list
```

### C# Naming Lint (Underscores in Method Names)

```text
UNH004: Method name 'When_Input_Is_Null' contains underscore(s).
```

**Fix:** Rename method to PascalCase without underscores:

```csharp
// WRONG
public void When_Input_Is_Null_Returns_Default() { }

// CORRECT
public void WhenInputIsNullReturnsDefault() { }
```

### Prettier Markdown Check Failed

```text
Checking formatting...
[warn] docs/features/inspector/inspector-button.md
```

**Fix:**

```bash
npm run format:md
```

### EOL Check Failed

```text
LF issues: 3
Files with BOM: 1
```

**Fix:**

```bash
npm run eol:fix
```

---

## Full Validation Workflow

After completing any task:

```bash
# 1. Format all code and docs
dotnet tool run csharpier format .
npm run format:md
npm run format:json
npm run format:yaml

# 2. Run full validation
npm run validate:prepush

# 3. If validation passes, task is ready for review
# 4. If validation fails, fix issues and re-run
```

---

## Documentation Checklist

Before completing ANY task that adds features or fixes bugs, verify:

### For New Features

- [ ] Feature documentation added/updated in `docs/features/<category>/`
- [ ] XML documentation on all public types/members
- [ ] At least one working code sample in docs
- [ ] CHANGELOG entry in `### Added` section under `## [Unreleased]`
- [ ] llms.txt updated if feature adds new capabilities

### For Bug Fixes

- [ ] CHANGELOG entry in `### Fixed` section under `## [Unreleased]`
- [ ] Documentation corrected if it described wrong behavior

### For API Changes

- [ ] All documentation referencing old API updated
- [ ] CHANGELOG entry (in `### Changed` section, marked Breaking if applicable)
- [ ] XML docs updated with new parameter names/types
- [ ] Code samples updated throughout docs

See [update-documentation](update-documentation.md) for complete guidelines.

---

## Pre-Existing Warnings

Some lint warnings may exist in the main branch (e.g., test lifecycle warnings in `validate:tests`). Focus on:

1. **New warnings** introduced by your changes
2. **Failing checks** (exit code 1)

If `validate:content` and `lint:csharp-naming` pass, your changes are ready.

---

## Troubleshooting

### npm Command Not Found

```bash
npm install  # Install dependencies
```

### dotnet Tool Not Found

```bash
dotnet tool restore  # Restore .NET tools
```

### PowerShell Script Errors

Ensure you're running in a shell that supports PowerShell:

```bash
pwsh -NoProfile -File scripts/check-eol.ps1
```

# Skill: Validation Troubleshooting

<!-- trigger: error, ci, failure, troubleshoot, fix | Common validation errors, CI failures, fixes | Core -->

**Trigger**: When you encounter validation errors, CI failures, or linting issues.

---

## When to Use

Use this guide when you encounter:

- CI/CD pipeline failures
- Linter error messages you need to fix
- Spelling or formatting issues
- Link validation failures
- Test lifecycle violations

For the quick validation workflow, see [validate-before-commit](./validate-before-commit.md).
For detailed linter commands, see [linter-reference](./linter-reference.md).

---

## Most Common CI Failures

### 1. Spelling Errors (Most Frequent)

**Symptom**: `npm run lint:spelling` fails

**Fix Options**:

1. **Correct the spelling** if it's actually wrong
2. **Add to dictionary** if it's a valid technical term

```bash
# Add to cspell.json
{
  "words": [
    "PRNG",
    "Sirenix",
    "MonoBehaviour"
  ]
}
```

1. **Inline ignore** for single occurrences:

```markdown
<!-- cspell:ignore someword -->
```

### 2. Prettier Formatting Failures

**Symptom**: `format:md:check`, `format:json:check`, or `format:yaml:check` fails

**Fix**: Run Prettier to auto-fix:

```bash
npx prettier --write <file>

# Or fix all:
npx prettier --write .
```

### 3. Markdownlint Violations

**Symptom**: `npm run lint:markdown` fails

**Common fixes**:

| Error Code | Issue                    | Fix                                  |
| ---------- | ------------------------ | ------------------------------------ |
| MD007      | Wrong list indentation   | Use 2 spaces for nested lists        |
| MD009      | Trailing whitespace      | Remove trailing spaces               |
| MD012      | Multiple blank lines     | Reduce to single blank line          |
| MD022      | No blank around headings | Add blank line before/after headings |
| MD032      | No blank around lists    | Add blank line before/after lists    |

### 4. Backtick File Reference Errors

**Symptom**: `npm run lint:docs` fails with backtick reference warning

**Cause**: Using backtick-wrapped filenames instead of proper links

```markdown
<!-- ❌ WRONG -->

See `context.md` for guidelines.
Update `CHANGELOG.md` with changes.

<!-- ✅ CORRECT -->

See [context](./context.md) for guidelines.
Update [CHANGELOG](../CHANGELOG.md) with changes.
```

### 5. Link Without Relative Prefix

**Symptom**: `npm run lint:docs` fails with relative path warning

```markdown
<!-- ❌ WRONG -->

[create-test](create-test.md)
[context](skills/context.md)

<!-- ✅ CORRECT -->

[create-test](./create-test.md)
[context](./skills/context.md)
```

### 6. Broken Internal Links

**Symptom**: `npm run lint:docs` fails with "file not found"

**Causes**:

- Typo in filename
- File was moved or renamed
- Wrong path prefix

**Fix**: Verify the file exists and path is correct:

```bash
ls -la <path-to-file>
```

### 7. Missing Track() in Tests

**Symptom**: `npm run validate:tests` fails

**Cause**: Unity objects created without Track():

```csharp
// ❌ WRONG
GameObject obj = new GameObject("Test");
MyComponent comp = obj.AddComponent<MyComponent>();

// ✅ CORRECT
GameObject obj = Track(new GameObject("Test"));
MyComponent comp = Track(obj.AddComponent<MyComponent>());
```

### 8. C# Naming Convention Violations

**Symptom**: `npm run lint:csharp-naming` fails

**Common issues**:

```csharp
// ❌ WRONG: lowercase method
public void processData() { }

// ✅ CORRECT
public void ProcessData() { }

// ❌ WRONG: missing underscore on private field
private int count;

// ✅ CORRECT
private int _count;
```

### 9. Line Ending Issues

**Symptom**: `npm run eol:check` fails

**Fix**:

```bash
npm run eol:fix
```

---

## Debugging Failed CI Runs

### Step 1: Check Which Check Failed

Look at the GitHub Actions output. Each job has a name indicating what failed:

- `validate:content` — Documentation/formatting issue
- `lint:csharp-naming` — C# naming convention
- `eol:check` — Line endings
- `validate:tests` — Test lifecycle issue

### Step 2: Reproduce Locally

```bash
# Run the exact same check locally
npm run validate:prepush
```

### Step 3: Fix and Verify

1. Fix the issue
2. Run the specific failing command
3. Run full validation: `npm run validate:prepush`

---

## Special Cases

### Pre-Existing Warnings

Some warnings may exist in the main branch. To determine if a warning is pre-existing:

```bash
# Check if warning exists on main
git stash
git checkout main
npm run <failing-command>
git checkout -
git stash pop
```

If the warning exists on main, it's pre-existing and doesn't block your PR.

### Conflicts Between Linters

Occasionally linters may conflict. Resolution priority:

1. **Prettier** for formatting (it wins on spacing, line breaks)
2. **Markdownlint** for structure (it wins on heading levels, list structure)
3. **CSpell** for spelling (always fix or add to dictionary)

### Files That Should Be Ignored

If a file shouldn't be linted, check if it should be in:

- `.prettierignore` — Skip Prettier formatting
- `.markdownlintignore` — Skip markdown linting
- `cspell.json` ignorePaths — Skip spell checking

---

## Error Message Reference

### Prettier Errors

| Message                       | Meaning             | Fix                  |
| ----------------------------- | ------------------- | -------------------- |
| "Code style issues found"     | Formatting mismatch | Run `--write`        |
| "Unexpected identifier"       | Invalid syntax      | Check file syntax    |
| "No parser could be inferred" | Unknown file type   | Check file extension |

### Markdownlint Errors

| Rule  | Description                   | Quick Fix                           |
| ----- | ----------------------------- | ----------------------------------- |
| MD001 | Heading levels increment by 1 | Don't skip heading levels           |
| MD007 | Unordered list indentation    | Use 2 spaces                        |
| MD009 | Trailing spaces               | Delete trailing whitespace          |
| MD010 | Hard tabs                     | Convert to spaces                   |
| MD011 | Reversed link syntax          | Use `[text](url)` not `(url)[text]` |
| MD012 | Multiple blank lines          | Reduce to single blank              |
| MD022 | Headings blank lines          | Add blank before/after heading      |
| MD023 | Headings start at line start  | Remove leading spaces               |
| MD031 | Fenced code blank lines       | Add blank before/after fence        |
| MD032 | Lists blank lines             | Add blank before/after list         |
| MD034 | Bare URL                      | Use `<url>` or `[text](url)`        |
| MD037 | Spaces inside emphasis        | Remove `** text **`                 |
| MD038 | Spaces inside code            | Remove `` ` code ` ``               |
| MD039 | Spaces inside links           | Remove `[ text ](url)`              |
| MD040 | Fenced code no language       | Add language after ` ``` `          |
| MD047 | No newline at end of file     | Add trailing newline                |

### CSpell Errors

| Message           | Meaning              | Fix                               |
| ----------------- | -------------------- | --------------------------------- |
| "Unknown word"    | Not in dictionary    | Fix spelling or add to dictionary |
| "Multiple errors" | Several misspellings | Fix each one individually         |

### C# Naming Errors

| Pattern             | Expected             | Example Fix                   |
| ------------------- | -------------------- | ----------------------------- |
| `method_name`       | `MethodName`         | PascalCase                    |
| `processData`       | `ProcessData`        | Capitalize first letter       |
| `private int count` | `private int _count` | Add underscore prefix         |
| `public int _value` | `public int Value`   | Remove underscore, capitalize |

---

## Quick Recovery Commands

```bash
# Fix ALL Prettier formatting
npx prettier --write .

# Fix line endings
npm run eol:fix

# Format C#
dotnet tool run csharpier format .

# Full validation
npm run validate:prepush
```

---

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Quick validation workflow
- [linter-reference](./linter-reference.md) — Detailed linter commands
- [formatting](./formatting.md) — CSharpier, Prettier, markdownlint workflow
- [markdown-reference](./markdown-reference.md) — Link formatting, structural rules

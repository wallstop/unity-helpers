# Skill: Linter Reference

<!-- trigger: lint, linter, csharpier, prettier, cspell | Detailed linter commands, configurations | Core -->

**Trigger**: When you need detailed linter commands, configurations, or to understand what each linter checks.

---

## When to Use

Use this reference when you need:

- Complete linter command documentation
- Configuration file locations and settings
- Understanding what specific rules check
- Details about npm script internals

For the quick validation workflow, see [validate-before-commit](./validate-before-commit.md).
For fixing common errors, see [validation-troubleshooting](./validation-troubleshooting.md).

> **Note:** This is a comprehensive reference for linter details. For the quick pre-commit workflow,
> see [validate-before-commit](./validate-before-commit.md).

---

## All Linter Commands

### Master Validation Command

```bash
# Run ALL checks at once (recommended before commit/push)
npm run validate:prepush
```

### Individual Commands

| Command                      | Description                                 |
| ---------------------------- | ------------------------------------------- |
| `npm run lint:spelling`      | Spell check all documentation (CSpell)      |
| `npm run lint:docs`          | Check markdown links and backtick refs      |
| `npm run lint:markdown`      | Markdownlint structural rules               |
| `npm run lint:yaml`          | YAML syntax validation                      |
| `npm run lint:csharp-naming` | C# naming conventions (method casing, etc.) |
| `npm run format:md:check`    | Check markdown formatting (Prettier)        |
| `npm run format:json:check`  | Check JSON/ASMDEF formatting (Prettier)     |
| `npm run format:yaml:check`  | Check YAML formatting (Prettier)            |
| `npm run eol:check`          | Line endings (CRLF) and BOM check           |
| `npm run validate:tests`     | Test lifecycle lint (Track() usage)         |

---

## CSpell Spell Checker

### Command

```bash
npm run lint:spelling
```

### Configuration

Located at `cspell.json` in the project root.

### Adding Words to Dictionary

Add technical terms, package names, or domain-specific words to `cspell.json`:

```json
{
  "words": ["PRNG", "Odin", "Sirenix", "MonoBehaviour", "stackalloc"]
}
```

### Inline Ignores

For single occurrences, use CSpell ignore comments:

```markdown
<!-- cspell:ignore someword -->
```

Or in code:

```csharp
// cspell:ignore someword
```

---

## Markdownlint

### Command

```bash
npm run lint:markdown
```

### Configuration

Located at `.markdownlint.json` in the project root.

### Disabled Rules

These rules are disabled in this project:

| Rule    | Description                  | Why Disabled           |
| ------- | ---------------------------- | ---------------------- |
| `MD013` | Line length                  | Prettier handles this  |
| `MD041` | First line should be heading | Some files have badges |
| `MD033` | Inline HTML                  | Needed for formatting  |
| `MD024` | Duplicate headings           | Allowed in this repo   |

### Common Rule Violations

| Rule    | Issue                                   | Fix                             |
| ------- | --------------------------------------- | ------------------------------- |
| `MD007` | Wrong list indentation                  | Use 2 spaces for nested lists   |
| `MD009` | Trailing spaces                         | Remove trailing whitespace      |
| `MD012` | Multiple consecutive blank lines        | Reduce to single blank line     |
| `MD022` | Headings should be surrounded by blanks | Add blank lines around headings |
| `MD032` | Lists should be surrounded by blanks    | Add blank lines around lists    |

---

## Link Linter (lint:docs)

### Command

```bash
npm run lint:docs
```

### What It Checks

1. **Broken internal links** — Links to non-existent files
2. **Missing anchors** — Links to non-existent headings
3. **Backtick file references** — Using backtick-wrapped filenames instead of proper links

### Link Format Requirements

```markdown
<!-- ✅ CORRECT: Use relative paths with ./ or ../ -->

[create-test](./create-test.md)
[context](../context.md)

<!-- ❌ WRONG: No relative prefix -->

[create-test](create-test.md)
```

---

## Prettier

### Commands

```bash
# Check formatting
npx prettier --check .
npx prettier --check <file>

# Fix formatting
npx prettier --write .
npx prettier --write <file>
```

### Configuration

Located at `.prettierrc.json`:

```json
{
  "proseWrap": "always",
  "printWidth": 100,
  "tabWidth": 2,
  "trailingComma": "es5"
}
```

### File Coverage

Prettier handles these file types:

- Markdown (`.md`)
- JSON (`.json`, `.asmdef`)
- YAML (`.yml`, `.yaml`)
- JavaScript (`.js`)

### Ignored Files

See `.prettierignore` for files excluded from formatting.

---

## CSharpier

### Command

```bash
dotnet tool run csharpier format .
```

### Configuration

Located at `.csharpierrc.json`:

```json
{
  "printWidth": 100,
  "useTabs": false,
  "tabWidth": 4
}
```

### Important Notes

- CSharpier only formats C# files
- Always run after ANY `.cs` file modification
- Cannot fix naming convention violations (use manual fixes)

---

## C# Naming Linter

### Command

```bash
npm run lint:csharp-naming
```

### What It Checks

| Pattern           | Requirement              | Example         |
| ----------------- | ------------------------ | --------------- |
| Public methods    | PascalCase               | `GetValue()`    |
| Private methods   | PascalCase               | `ProcessData()` |
| Public properties | PascalCase               | `Count`         |
| Private fields    | `_camelCase` with prefix | `_myField`      |
| Constants         | PascalCase or UPPER_CASE | `MaxValue`      |
| Parameters        | camelCase                | `itemCount`     |
| Type parameters   | `T` or `T` + PascalCase  | `T`, `TValue`   |

### Common Violations

```csharp
// ❌ WRONG: lowercase method name
public void processData() { }

// ✅ CORRECT: PascalCase method name
public void ProcessData() { }

// ❌ WRONG: missing underscore prefix
private int count;

// ✅ CORRECT: underscore prefix
private int _count;
```

---

## YAML Linter

### Command

```bash
npm run lint:yaml
```

### What It Checks

- YAML syntax validity
- Proper indentation
- Duplicate keys

### For GitHub Workflow Files

```bash
# Additional check for workflow files
actionlint
```

---

## Test Lifecycle Linter

### Command

```bash
npm run validate:tests
pwsh -NoProfile -File scripts/lint-tests.ps1
```

### What It Checks

All Unity object creation in tests must use `Track()`:

```csharp
// ✅ CORRECT: Track() ensures cleanup
GameObject obj = Track(new GameObject("Test"));
MyComponent comp = Track(obj.AddComponent<MyComponent>());

// ❌ WRONG: Untracked objects may leak
GameObject obj = new GameObject("Test");
```

---

## Line Ending Linter

### Command

```bash
npm run eol:check
```

### Requirements

- All files must use CRLF line endings
- No UTF-8 BOM (Byte Order Mark)

### Fixing Line Endings

```bash
npm run eol:fix
```

---

## NPM Script Breakdown

### validate:prepush

Runs these in sequence:

1. `validate:content` (docs + formatting)
2. `eol:check`
3. `validate:tests`
4. `lint:csharp-naming`

### validate:content

Runs these in sequence:

1. `lint:docs`
2. `lint:markdown`
3. `format:md:check`
4. `format:json:check`
5. `format:yaml:check`

---

## Configuration File Locations

| Tool         | Config File          | Purpose                |
| ------------ | -------------------- | ---------------------- |
| CSpell       | `cspell.json`        | Spell check dictionary |
| Markdownlint | `.markdownlint.json` | Markdown rules         |
| Prettier     | `.prettierrc.json`   | Formatting options     |
| Prettier     | `.prettierignore`    | Ignored files          |
| CSharpier    | `.csharpierrc.json`  | C# formatting options  |
| ESLint       | `.eslintrc.json`     | JavaScript linting     |
| EditorConfig | `.editorconfig`      | Editor settings        |

---

## Related Skills

- [validate-before-commit](./validate-before-commit.md) — Quick validation workflow
- [validation-troubleshooting](./validation-troubleshooting.md) — Common errors and fixes
- [formatting](./formatting.md) — CSharpier, Prettier, markdownlint workflow
- [markdown-reference](./markdown-reference.md) — Link formatting, structural rules

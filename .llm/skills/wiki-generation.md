# Wiki Generation

This skill covers best practices for GitHub Wiki generation and maintenance in this repository.

---

## Overview

The repository uses automated GitHub Actions workflows to generate and deploy wiki content from the `docs/` folder. Understanding the link format differences is critical to avoid broken wiki pages.

---

## Key Concepts

### Link Syntax: Markdown vs MediaWiki

GitHub Wiki supports two link syntaxes, but **only one works correctly when pages are Markdown**:

| Syntax Type | Format                   | Use When              |
| ----------- | ------------------------ | --------------------- |
| Markdown    | `[Display Text](Page)`   | ✅ Pages are Markdown |
| MediaWiki   | `[[Page\|Display Text]]` | ❌ Never for Markdown |

**CRITICAL**: Always use standard **Markdown link syntax** `[Display](Page)` for wiki sidebar and page links. The MediaWiki wikilink syntax `[[Page|Display]]` does NOT render correctly when wiki pages are Markdown files.

### Why This Matters

GitHub Wiki renders links based on the page format:

- **Markdown pages** (`.md` files): Use `[text](link)` syntax
- **MediaWiki pages**: Use `[[link|text]]` syntax

Our wiki pages are all Markdown (`.md`), so all links must use Markdown syntax.

---

## Wiki Page Naming Convention

Documentation files are transformed into wiki page names:

```text
Source: docs/overview/getting-started.md
Wiki:   Overview-Getting-Started.md
```

**Rules:**

1. Replace `/` with `-`
2. Remove `.md` extension
3. Capitalize first letter of each segment

**Examples:**

<!-- markdownlint-disable MD013 -->

```text
Source File                              Wiki Page Name
docs/overview/index.md                   Overview-Index.md
docs/features/inspector/buttons.md       Features-Inspector-Buttons.md
docs/guides/odin-migration-guide.md      Guides-Odin-Migration-Guide.md
```

<!-- markdownlint-enable MD013 -->

---

## Sidebar Generation

The wiki sidebar file is generated automatically by the `deploy-wiki.yml`
workflow.

### Correct Format

```markdown
## 📚 Documentation

### Getting Started

- [Home](Home)
- [Getting Started](Overview-Getting-Started)
- [Glossary](Overview-Glossary)

### Features

- [Inspector Overview](Features-Inspector-Overview)
```

### Incorrect Format (DO NOT USE)

```markdown
## 📚 Documentation

### Getting Started

- [[Home]]
- [[Overview-Getting-Started|Getting Started]]
```

The MediaWiki format above will render incorrectly—showing the page name as
display text and corrupting the link target.

---

## Validation

The `deploy-wiki.yml` workflow includes validation to catch broken sidebar links:

<!-- markdownlint-disable MD013 -->

```bash
# Extracts [Display](Page) links and verifies each page exists
# Character class includes: letters, numbers, hyphens, underscores, periods
missing_pages=$(grep -oE '\]\([A-Za-z0-9_.-]+\)' _Sidebar.md | \
  sed 's/\](//;s/)//' | sort -u | while read -r page; do
  [ ! -f "$page.md" ] && echo "$page"
done || true)
```

<!-- markdownlint-enable MD013 -->

---

## Page Name Characters

**Valid Characters**: Page names should contain only:

- Letters (A-Z, a-z)
- Numbers (0-9)
- Hyphens (-)
- Underscores (\_)

Avoid spaces and other special characters.

---

## Troubleshooting

### Symptom: Sidebar links show page name instead of display text

**Cause**: Using MediaWiki wikilink syntax `[[Page|Display]]` instead of
Markdown syntax `[Display](Page)`

**Fix**: Convert all sidebar links to Markdown format:

```bash
# Before (broken)
echo "- [[Overview-Getting-Started|Getting Started]]"

# After (correct)
echo "- [Getting Started](Overview-Getting-Started)"
```

### Symptom: Links point to wrong pages

**Cause**: Wiki page naming doesn't match the generated filename

**Fix**: Verify page naming follows the conversion rules:

1. File path segments become hyphen-separated
2. Each segment is capitalized
3. `.md` extension is stripped in links

### Symptom: Sidebar validation passes but links are broken

**Cause**: Validation regex doesn't match the link format being used

**Fix**: Ensure validation regex matches the actual link format:

- For `[Display](Page)`: Use `\]\([A-Za-z0-9_.-]+\)`
- For `[[Page|Display]]`: Use `\[\[[A-Za-z0-9_.-]+` (deprecated)

---

## Testing

Run wiki generation tests locally:

```bash
npm run test:wiki-generation
```

Tests cover:

- `get_display_name` function correctness
- Markdown link format generation
- Regression prevention (no MediaWiki syntax)
- Validation regex patterns
- Workflow file syntax verification

**CRITICAL**: The `get_display_name` function in the test file MUST be an exact
copy of the implementation in `deploy-wiki.yml`. If you modify the function in
either location, update both to stay in sync.

Tests run automatically in CI via `validate-wiki-links.yml`.

---

## Related Files

<!-- markdownlint-disable MD013 -->

| File                                        | Purpose                          |
| ------------------------------------------- | -------------------------------- |
| `.github/workflows/deploy-wiki.yml`         | Main wiki deployment workflow    |
| `.github/workflows/validate-wiki-links.yml` | PR link validation + wiki tests  |
| `scripts/tests/test-wiki-generation.sh`     | Wiki generation regression tests |
| `docs/`                                     | Source documentation files       |

<!-- markdownlint-enable MD013 -->

---

## Related Skills

- [update-documentation](./update-documentation.md) — Documentation standards
- [validate-before-commit](./validate-before-commit.md) — Pre-commit checks
- [github-pages](./github-pages.md) — GitHub Pages deployment (separate from wiki)

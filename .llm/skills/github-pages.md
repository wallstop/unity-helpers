# Skill: GitHub Pages Best Practices

**Trigger**: When creating, modifying, or troubleshooting GitHub Pages documentation with Jekyll.

---

## When to Use

This skill applies when:

- Creating new documentation pages for GitHub Pages
- Modifying Jekyll configuration (`_config.yml`)
- Adding or updating CSS themes
- Fixing broken links in documentation
- Troubleshooting GitHub Pages rendering issues

---

## Jekyll Configuration Best Practices

### Required Plugins

The following plugins MUST be configured in `_config.yml`:

| Plugin                  | Purpose                     | Critical?   |
| ----------------------- | --------------------------- | ----------- |
| `jekyll-relative-links` | Convert `.md` links to HTML | **YES**     |
| `jekyll-remote-theme`   | Use GitHub Pages themes     | Recommended |
| `jekyll-seo-tag`        | SEO metadata                | Recommended |
| `jekyll-sitemap`        | Auto-generate sitemap       | Recommended |
| `jekyll-include-cache`  | Performance optimization    | Optional    |

### Permalink Configuration

```yaml
# _config.yml
collections:
  docs:
    output: true
    permalink: /:collection/:path/

defaults:
  - scope:
      path: ""
      type: "pages"
    values:
      layout: "default"
```

### Relative Links Configuration

```yaml
# CRITICAL: Enable relative links plugin
relative_links:
  enabled: true
  collections: true
```

---

## Markdown Link Format (CRITICAL)

**This is the most common source of broken links on GitHub Pages.**

### The Rule

⚠️ **MANDATORY**: ALL internal links to markdown files MUST use explicit relative format with `./` or `../` prefix.

**There are NO exceptions to this rule.** Links without prefixes WILL break on GitHub Pages and CI WILL fail.

### Quick Validation

```bash
# Run this BEFORE committing any documentation changes:
npm run lint:docs

# This catches missing ./ prefixes and broken links
# CI will fail if this command fails locally
```

**Wrong vs Correct Examples:**

```text
❌ WRONG: ](docs/file)       →  ✅ CORRECT: ](./docs/file)       (Missing ./ prefix)
❌ WRONG: ](CHANGELOG)       →  ✅ CORRECT: ](./CHANGELOG)       (Missing ./ prefix)
❌ WRONG: ](feature)         →  ✅ CORRECT: ](./feature)         (Missing ./ prefix)
❌ WRONG: ](overview)        →  ✅ CORRECT: ](./overview)        (Missing ./ AND extension)
❌ WRONG: ](/docs/file)      →  ✅ CORRECT: ](./docs/file)       (Absolute path, needs relative)
✅ OK:    ](../parent)       →  ✅ CORRECT: ](../parent)         (../ prefix is correct)
```

> **Note**: All examples above should include the `.md` extension in actual usage.

### Why This Matters

The `jekyll-relative-links` plugin converts markdown links to HTML links during the Jekyll build process. However, the plugin has a critical limitation: **it ONLY recognizes links that use explicit relative path prefixes (`./` or `../`)**.

**Technical Detail**: The plugin's path matching regex specifically looks for relative path indicators. Bare paths are NOT recognized as relative links and are passed through unchanged.

**Without the prefix** (bare paths):

- Plugin ignores the link entirely — no conversion occurs
- Link renders as raw markdown file download (browser downloads the source)
- 404 errors on GitHub Pages (the file doesn't exist at the expected URL)
- Links may appear to work locally but break in production

**With the prefix** (explicit relative paths):

- Plugin recognizes and processes the link
- Link correctly converts to HTML extension
- Works consistently both locally and on GitHub Pages

**Validation**: Run `npm run lint:docs` to catch missing `./` prefixes before committing. This command checks all documentation links and will fail if any links are missing the required relative path prefix.

### Escaping Example Links

When documenting link format (showing correct/incorrect examples), the CI workflow and local linter automatically skip content that is properly escaped.

#### What the CI Handles Automatically

The CI workflow (`lint-doc-links.yml`) now properly handles:

- **Fenced code blocks** (` ``` ` or `~~~`) — all content inside is skipped
- **Inline backticks** — content inside single backticks is skipped
- **URL-encoded paths** — `%20` and other encodings are properly decoded before validation

This means you can safely show example link syntax in documentation without triggering false positives.

#### Best Practices for Example Links

**For multi-line examples**: Use fenced code blocks with the `text` language specifier:

```text
❌ WRONG: ]\(file.md)        →  Missing ./ prefix
✅ CORRECT: ]\(./file.md)    →  Proper relative path
```

**For brief inline examples**: Use single backticks with escaped brackets:

```text
Use the format ]\(./file.md) for internal links (shown escaped).
```

**Legacy escape pattern**: The escaped bracket pattern prevents the linter from parsing it as a link:

```text
<!-- Still works but less preferred -->
]\(file.md)  →  Escaped, linter skips this
```

#### Local vs CI Validation

> **Note**: Local `npm run lint:docs` runs the PowerShell linter (`scripts/lint-doc-links.ps1`) which has similar but not identical logic to the CI bash scripts. Both handle code blocks and backticks, but minor edge cases may differ. Always verify CI passes after local validation.

**Why escaping matters**: Unescaped example links trigger false positive lint errors because:

1. "Wrong" examples intentionally lack `./` prefix
2. Example paths don't point to real files
3. The linter cannot distinguish teaching examples from real links

See [update-documentation](./update-documentation.md#escaping-example-links-in-documentation) for detailed escaping methods.

### Examples

```markdown
<!-- ✅ CORRECT: All internal .md links use explicit relative paths -->

See the [Getting Started Guide](./docs/guides/getting-started.md) for setup instructions.

For API details, check the [Features Overview](./docs/features/overview.md).

Return to the [main README](./README.md) or view the [Changelog](../CHANGELOG.md).

From a nested doc: [Parent Section](../overview/index.md)
```

```markdown
<!-- ❌ WRONG: Missing ./ prefix - these will break on GitHub Pages -->

See the [Getting Started Guide](docs/guides/getting-started.md) for setup instructions.

Check the [Changelog](CHANGELOG.md).
```

---

## CSS Theming for Unified Appearance

### CSS Variables in `:root`

**Always define default CSS variables in `:root`** to ensure themes work before JavaScript loads:

```css
:root {
  /* Define ALL theme variables with defaults */
  --bg-primary: #1e1e1e;
  --bg-secondary: #252526;
  --text-primary: #d4d4d4;
  --text-secondary: #b0b0b0;
  --border-color: #3c3c3c;
  --link-color: #569cd6;
  /* ... all other variables */
}
```

### Override Remote Theme Elements Comprehensively

When using a remote theme (like `pages-themes/minimal`), override ALL structural elements to prevent "box" appearance:

```css
/* Make all nested elements transparent */
.wrapper,
.wrapper > *,
.inner,
header,
header *,
section,
section *,
footer,
footer * {
  background-color: transparent !important;
  box-shadow: none !important;
}

/* Re-apply backgrounds ONLY to root containers */
body {
  background-color: var(--bg-primary) !important;
}

header {
  background-color: var(--bg-primary) !important;
  border-bottom: 1px solid var(--border-color) !important;
}

section {
  background-color: var(--bg-primary) !important;
}

footer {
  background-color: var(--bg-primary) !important;
  border-top: 1px solid var(--border-color) !important;
}
```

### Theming Best Practices

| Principle                    | Implementation                                                        |
| ---------------------------- | --------------------------------------------------------------------- |
| Transparent nested elements  | Apply `background: transparent` to all children                       |
| Root-only backgrounds        | Apply background colors only to `body`, `header`, `section`, `footer` |
| Consistent color family      | Use same `--bg-primary` for all containers                            |
| No box shadows on containers | Remove `box-shadow` from theme elements                               |
| Use `!important` sparingly   | Required to override remote theme styles                              |

### Theme Switching Support

```css
/* Dark theme (default) */
[data-theme="dark"] {
  --bg-primary: var(--dark-bg-primary);
  --text-primary: var(--dark-text-primary);
  /* ... */
}

/* Light theme */
[data-theme="light"] {
  --bg-primary: var(--light-bg-primary);
  --text-primary: var(--light-text-primary);
  /* ... */
}
```

---

## Testing Links Locally

### Local Jekyll Server

```bash
# Install dependencies (first time only)
bundle install

# Run local server with live reload
bundle exec jekyll serve --livereload

# Server runs at http://localhost:4000/unity-helpers/
```

### Common Issues and Fixes

| Issue                         | Cause                                | Fix                                     |
| ----------------------------- | ------------------------------------ | --------------------------------------- |
| Links show as `.md` downloads | Missing `./` prefix                  | Add `./` or `../` to all internal links |
| 404 on nested pages           | Wrong relative path depth            | Count `../` correctly from current file |
| Styles not loading            | Wrong `baseurl` in `_config.yml`     | Ensure `baseurl` matches repo name      |
| Theme not applying            | Missing `jekyll-remote-theme` plugin | Add to `_config.yml` plugins list       |
| Relative links not converting | Plugin not enabled                   | Add `relative_links: enabled: true`     |

### Testing Checklist

- [ ] All `.md` links have `./` or `../` prefix
- [ ] Click every link on every page
- [ ] Test from root AND nested directories
- [ ] Verify no raw `.md` file downloads
- [ ] Check browser console for 404 errors

---

## CI/CD Validation

### Required Validation Commands

**MANDATORY**: Run these before committing documentation changes:

```bash
# Check all documentation links (MUST PASS)
npm run lint:docs

# Check markdown formatting
npm run lint:markdown

# Check Prettier formatting
npm run format:md:check

# Or run full content validation (includes all above):
npm run validate:content
```

### Link Checking Script

The `lint-doc-links` script validates:

- All internal `.md` links resolve to existing files
- Links use proper `./` or `../` prefixes
- No broken anchor links (`#section-name`)
- No backtick-wrapped file references

```bash
# Verbose output for debugging
pwsh ./scripts/lint-doc-links.ps1 -VerboseOutput
```

### GitHub Actions Workflow

The `.github/workflows/lint-doc-links.yml` workflow runs automatically on:

- Pull requests affecting documentation
- Pushes to main branch

**PRs with broken documentation links will be blocked.**

---

## Common CI Failures

When CI fails on documentation PRs, here are the most common causes and fixes:

| Failure Type                     | Error Message (Pattern)                   | Fix                                                           |
| -------------------------------- | ----------------------------------------- | ------------------------------------------------------------- |
| Missing `./` prefix on links     | `Link missing relative prefix`            | Add `./` to all internal links → run `npm run lint:docs`      |
| Broken internal link             | `Link target does not exist`              | Fix the path or create missing file → run `npm run lint:docs` |
| Unescaped example links          | `Link target does not exist` (for demos)  | Wrap example links in code blocks → run `npm run lint:docs`   |
| Unknown words in spelling check  | `Unknown word: someWord`                  | Add word to `cspell.json` words array                         |
| Trailing spaces in YAML          | `Delete trailing whitespace`              | Run `npx prettier --write <file>`                             |
| Markdown formatting issues       | Various markdownlint errors               | Run `npm run lint:markdown`                                   |
| Backtick-wrapped file references | `File reference should not use backticks` | Use markdown links instead of backticks for file references   |

### Debugging Failed CI

```bash
# Step 1: Reproduce locally (run ALL of these)
npm run lint:docs          # Check link formats and targets
npm run lint:spelling      # Check for unknown words
npm run lint:markdown      # Check markdown formatting
npm run format:md:check    # Check Prettier formatting

# Step 2: Auto-fix what can be fixed
npx prettier --write "**/*.md"

# Step 3: Manual fixes for remaining issues
# - Add ./  to internal links
# - Add unknown words to cspell.json
# - Fix broken link targets
```

---

## Quick Reference

### Link Format Cheat Sheet

| From Location            | To Location              | Link Format (pattern)   |
| ------------------------ | ------------------------ | ----------------------- |
| Root `index` file        | `docs/guide` file        | `]​(./docs/guide)`      |
| `docs/guide` file        | Root `CHANGELOG` file    | `]​(../CHANGELOG)`      |
| `docs/features/api` file | `docs/guides/start` file | `]​(../guides/start)`   |
| `docs/guides/start` file | `docs/guides/next` file  | `]​(./next)` (same dir) |

### Configuration Checklist

- [ ] `jekyll-relative-links` plugin enabled
- [ ] `relative_links.enabled: true` in `_config.yml`
- [ ] `baseurl` matches repository name
- [ ] All internal links use `./` or `../` prefix
- [ ] CSS variables defined in `:root`
- [ ] Remote theme elements overridden transparently

---

## Related Skills

- [update-documentation](./update-documentation.md) — Documentation standards and CHANGELOG format
- [create-csharp-file](./create-csharp-file.md) — C# file creation guidelines

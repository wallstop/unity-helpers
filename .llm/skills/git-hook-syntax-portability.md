# Skill: Git Hook Syntax & Portability

<!-- trigger: hook regex, case pattern, end-of-options, CRLF newline, grep portability, stderr suppression | Hook regex, CLI safety, CRLF handling, portable grep patterns | Core -->

**Trigger**: When writing regex patterns, case statements, or CLI invocations in hook scripts, or fixing portability issues.

---

## When to Use

- Writing regex patterns in bash hooks (single vs double backslash)
- Using `case` statements to match file paths
- Passing file lists to CLI tools safely
- Fixing or appending newlines while preserving CRLF/LF style
- Ensuring grep patterns work on both GNU and BSD systems

---

## When NOT to Use

- For index safety or hook templates → see [git-hook-safety](./git-hook-safety.md)
- For validation philosophy or debugging → see [git-hook-lifecycle-debugging](./git-hook-lifecycle-debugging.md)

---

## Git Hook Regex Patterns (CRITICAL)

> **CRITICAL**: Regex patterns in bash git hooks require SINGLE backslashes.
> Double escaping causes patterns to match NOTHING silently!

```bash
# CORRECT - Single backslash
git diff --cached --name-only | grep -E '\.(md|markdown)$'

# WRONG - Double-escaped (matches nothing!)
git diff --cached --name-only | grep -E '\\.(md|markdown)$'
```

**ALWAYS test patterns manually after modification:**

```bash
# Test that pattern matches expected files
echo "test.md" | grep -E '\.(md|markdown)$'  # Should output: test.md
```

See [validate-before-commit](./validate-before-commit.md#git-hook-regex-pattern-testing-critical)
for full testing checklist.

---

## Bash Case Patterns vs Filename Globbing (CRITICAL)

In bash `case` statements, `*` matches **any character including `/`**. This is DIFFERENT from filename globbing (e.g., `ls *.md`) where `*` does NOT match `/`.

```bash
# case: .llm/skills/*.md matches ALL depths
case "$file" in .llm/skills/*.md) echo "match" ;; esac
# Matches: .llm/skills/foo.md, .llm/skills/sub/dir/foo.md

# filename glob: .llm/skills/*.md matches ONE level only
ls .llm/skills/*.md       # Only matches .llm/skills/foo.md
ls .llm/skills/**/*.md    # ** needed for recursive glob
```

**Rules**:

- DO NOT use `**/*.md` notation in `case` comments -- `**` is glob-specific and irrelevant
- DO NOT add redundant depth alternatives like `.llm/skills/*.md|.llm/skills/*/*.md`
- The single pattern `.llm/skills/*.md` already matches all subdirectory depths in `case`
- Regression test: [test-hook-patterns.sh](../../scripts/tests/test-hook-patterns.sh)

---

## CLI Argument Safety: End-of-Options Separator

When passing staged file lists to CLI tools in hooks, ALWAYS place `--` between options and file arguments:

```bash
# WRONG - filenames can be interpreted as options
npx --no-install prettier --write "${FILES[@]}"

# CORRECT - `--` prevents option injection from filenames
npx --no-install prettier --write -- "${FILES[@]}"
markdownlint --fix --config .markdownlint.json -- "${FILES[@]}"
yamllint -c .yamllint.yaml -- "${FILES[@]}"
```

**Why**: A staged filename like `--plugin=./evil.js` would be parsed as a CLI flag without `--`. This is an option injection vulnerability.

> **CAVEAT** — PowerShell's `-File` CLI does NOT honor POSIX `--` and will fail with "parameter name '' is ambiguous." When calling `pwsh -File <script>` or `powershell -File <script>` from bash, pass explicit named parameters instead (e.g. `-Paths "${ARR[@]}"`). See [bash-pwsh-invocation](./bash-pwsh-invocation.md) for the full rule and enforcement lint.

### Never Transport File Lists Through `echo ... | xargs`

`echo "$FILES" | xargs tool ...` is unsafe for path lists. `xargs` re-splits on whitespace, so filenames with spaces are mangled, and newline-delimited shell variables are not a reliable file transport format.

```bash
# WRONG - whitespace in filenames is split into multiple arguments
echo "$CHANGED_MD" | xargs markdownlint --config .markdownlint.json --

# CORRECT (bash) - keep paths in arrays and expand them directly
markdownlint --config .markdownlint.json -- "${CHANGED_MD[@]}"

# CORRECT (generic) - if xargs is required, feed it NUL-delimited input
printf '%s\0' "${CHANGED_MD[@]}" | xargs -0 markdownlint --config .markdownlint.json --
```

**Rule**: In bash hooks, prefer arrays plus `"${ARRAY[@]}"` over newline-delimited variables. Only use `xargs` with `-0` and NUL-delimited input.

### File-Reading Commands Also Need `--`

Commands that read file contents (`tail`, `head`, `cat`, `od`, etc.) are also vulnerable to option injection and must use `--`:

```bash
# WRONG - filename starting with '-' is interpreted as option
tail -n 1 "$file"
head -c 10 "$file"
cat "$file"
od -c "$file"

# CORRECT - `--` ensures filename is never parsed as an option
tail -n 1 -- "$file"
head -c 10 -- "$file"
cat -- "$file"
od -c -- "$file"
```

**Why**: A file named `-n` or `--help` would cause unexpected behavior or errors without `--`.

---

## CRLF-Aware Newline Handling

When fixing missing final newlines in hook scripts, you MUST detect the file's existing line ending style before appending. Blindly appending `\n` (LF) to a CRLF file creates mixed line endings that fail `eol:check`.

### Detecting Line Endings Before Appending

```bash
# WRONG - Always appends LF, breaks CRLF files
if [ "$(tail -c 1 -- "$file" | od -An -tx1 | tr -d ' ')" != "0a" ]; then
    printf '\n' >> "$file"
fi

# CORRECT - Detect existing line endings and match them
if [ "$(tail -c 1 -- "$file" | od -An -tx1 | tr -d ' ')" != "0a" ]; then
    # Check if file uses CRLF by looking for carriage return characters
    if grep -q $'\r' "$file" 2>/dev/null; then
        printf '\r\n' >> "$file"  # CRLF file - append CRLF
    else
        printf '\n' >> "$file"    # LF file - append LF
    fi
fi
```

### Why This Matters

1. Windows-style files use CRLF (`\r\n`) line endings
2. Unix-style files use LF (`\n`) line endings
3. Appending LF to a CRLF file creates a "mixed" file with both styles
4. The `eol:check` validation fails on mixed line endings
5. This is a common cause of CI failures after "fixing" final newlines

### PowerShell Equivalent

```powershell
# WRONG - -NoNewline then manual newline can create mixed endings
$content = Get-Content -Raw $file
# ... process content ...
Set-Content -NoNewline -Path $file -Value $content
Add-Content -Path $file -Value ""  # Adds OS-default line ending

# CORRECT - Detect and preserve existing line endings
$content = Get-Content -Raw $file
$useCRLF = $content -match "`r`n"
# ... process content ...
$newline = if ($useCRLF) { "`r`n" } else { "`n" }
[System.IO.File]::WriteAllText($file, $content + $newline)
```

---

## Portable Grep & Stderr Hygiene (CRITICAL)

Enforced by `test-shell-portability.sh` (`npm run test:shell-portability`).

**Grep portability rules:**

- `\|` alternation is a GNU BRE extension — always use `grep -E` with `|` instead
- `\s` is Perl-compatible, not POSIX — always use `[[:space:]]`

```bash
# WRONG - GNU-only patterns
grep -q 'error\|warning' file.txt
grep -E '^\s*#' file.txt

# CORRECT - POSIX-portable
grep -qE 'error|warning' file.txt
grep -E '^[[:space:]]*#' file.txt
```

**Stderr suppression (`2>/dev/null`) rules:**

- **Allowed**: tool detection (`command -v`), process cleanup (`kill`), grep no-match, git exploratory calls, version checks
- **Forbidden on lint/validation tools**: cspell, prettier, markdownlint, lint-cspell-config.js — their stderr contains actionable diagnostic output

### Git Path Parsing Must Not Assume Single-Field Paths

Structured git output often places the path after several whitespace-delimited fields. Do not parse paths with fixed-field extraction like `awk '{print $4}'`, because valid git paths can contain spaces.

```bash
# WRONG - truncates paths containing spaces
file_path=$(echo "$line" | awk '{print $4}')

# CORRECT - strip the first three metadata fields, keep the remainder verbatim
mode="${line%% *}"
file_path="${line#* }"
file_path="${file_path#* }"
file_path="${file_path#* }"
```

**Rule**: When parsing `git ls-files -s` or similar output, treat the path as the remainder after the metadata fields, not as a fixed single field.

### Repo-Root Anchoring for `git ls-files` and Similar Commands

If a script derives `REPO_ROOT` / `$repoRoot` from its own location, every git command that enumerates repo-relative paths must be anchored to that root as well. Otherwise, invoking the script from a subdirectory silently changes the meaning of the returned paths.

```bash
# WRONG - output becomes relative to the caller's cwd
git ls-files -z -- '*.md'

# CORRECT - anchor git's working directory at the repository root
git -C "$REPO_ROOT" ls-files -z -- '*.md'

# ALSO CORRECT - cd once at script startup, then run relative git commands
cd "$REPO_ROOT"
git ls-files -z -- '*.md'
```

```powershell
# WRONG - relative paths depend on the caller's cwd
$gitFiles = git ls-files --cached --others --exclude-standard

# CORRECT - anchor git and any filesystem fallback at the repository root
$gitFiles = & git -C $repoRoot ls-files --cached --others --exclude-standard
$fallback = Get-ChildItem -Path $repoRoot -Recurse -File
```

**Rule**: If the script later resolves those paths with `Join-Path $repoRoot ...` or `"$REPO_ROOT/$path"`, you MUST either `cd` to the repo root first or pass `-C $repoRoot` on the git command. Do not mix repo-root-derived filesystem paths with caller-cwd-derived git output.

---

## `git check-ignore` Requires Repo-Relative POSIX Paths (CRITICAL)

> **CRITICAL**: `git check-ignore` silently **misclassifies** absolute paths — especially on Windows — by returning "not ignored" (exit 1) for files that ARE gitignored.

This matters because safety gates (e.g., `agent-preflight.ps1`'s auto-delete guard) refuse to remove files that appear "not gitignored." A silent misclassification turns a cleanable stray artifact into a permanent manual-review task.

**The rule**: always normalize to **repo-relative POSIX** (forward-slash) before invoking `git check-ignore`. The same rule applies to any git plumbing command documented as repo-relative (`git ls-files -- <path>`, `git diff --relative`, etc.).

```powershell
# WRONG - absolute Windows-style path; git check-ignore may misclassify.
& git -C $repoRoot check-ignore -q -- "C:\repo\scripts\pre-commit.log"

# CORRECT - normalize first via scripts/git-path-helpers.ps1.
. (Join-Path $PSScriptRoot 'git-path-helpers.ps1')
$relative = ConvertTo-GitRelativePosixPath -Path $absPath -RepoRoot $repoRoot
if ($null -eq $relative) {
    # Path is outside the repo root — refuse the auto-delete gate.
    $unignoredFiles.Add($absPath) | Out-Null
    continue
}
& git -C $repoRoot check-ignore -q -- "$relative"
$checkExit = $LASTEXITCODE
# Exit codes: 0 = ignored, 1 = not ignored, 128 = error.
```

Reference helper: `scripts/git-path-helpers.ps1` — exposes `ConvertTo-GitRelativePosixPath -Path <abs-or-rel> -RepoRoot <abs>`:

- Absolute path inside repo → repo-relative POSIX (`scripts/foo.ps1`).
- Absolute path outside repo → `$null` (caller must refuse the gate).
- Relative path with `\` separators → forward-slash POSIX.
- Repo root itself → `.`.
- Case-insensitive prefix match on Windows (NTFS).

Regression tests: `scripts/tests/test-git-path-helpers.ps1` covers the helper in isolation; `scripts/tests/test-agent-preflight.ps1`'s `PathNormalize_*` cases exercise the end-to-end call path with a gitignored-stray fixture.

---

## Related Skills

- [git-hook-patterns](./git-hook-patterns.md) - Hub: all hook pattern categories
- [git-hook-safety](./git-hook-safety.md) - Index safety, permissions, templates
- [git-hook-lifecycle-debugging](./git-hook-lifecycle-debugging.md) - Validation, config, errors, debugging
- [validate-before-commit](./validate-before-commit.md) - Pre-commit validation commands
- [formatting](./formatting.md) - CSharpier, Prettier, markdownlint workflow

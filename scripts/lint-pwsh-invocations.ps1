#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Detect anti-patterns in bash -> PowerShell (.ps1) script invocations.

.DESCRIPTION
    PowerShell's `-File` CLI mode does NOT honor the POSIX `--` argument
    separator. Passing `--` as an argument surfaces as:

      "Parameter cannot be processed because the parameter name ''
       is ambiguous."

    This lint scans the repo for invocation anti-patterns so we catch the
    mistake at commit / CI time rather than during a rare hook branch.

    Error codes emitted:
      PWS001 - `pwsh|powershell -File <script> --` (the core bug)
      PWS002 - In-process `& <script>.ps1 --` inside scripts/tests/*.ps1
               (tests MUST exercise the same invocation path production uses;
               the in-process call operator masks CLI-binding bugs)

    Scanned paths:
      - *.sh
      - .githooks/*
      - .github/workflows/*.yml
      - scripts/tests/*.ps1
      - package.json

    Multi-line invocation detection:
      Bash / YAML `run: |` blocks may split a `pwsh ... -File ... -- ...`
      invocation across physical lines using `\` continuations. We first scan
      each physical line, then compute a "logically joined" view — any line
      ending in a trailing `\` (ignoring trailing whitespace) is joined with
      the next line — and scan that view too. Violations found only on the
      joined view report the physical line number where the invocation STARTS.

    Excluded:
      - Lines inside PowerShell comment-based help blocks (open/close markers).
      - Lines beginning with a '#' comment character in .ps1, .sh, .yml, .yaml
        files. (Caveat: `#` inside quoted YAML strings is treated as a
        comment start; we accept this minor edge case because scanning for
        invocations inside a quoted YAML string is not a pattern we care
        about.)
      - This script itself (scripts/lint-pwsh-invocations.ps1) and the
        corresponding test script, which use the anti-pattern as fixture text.

.PARAMETER VerboseOutput
    Show per-file diagnostics including files that were scanned with no
    violations.

.EXAMPLE
    ./scripts/lint-pwsh-invocations.ps1
    Lint the whole repo.

.EXAMPLE
    ./scripts/lint-pwsh-invocations.ps1 -VerboseOutput
    Lint with verbose per-file output.
#>
param(
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
    if ($VerboseOutput) { Write-Host "[lint-pwsh-invocations] $msg" -ForegroundColor Cyan }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$selfRel = 'scripts/lint-pwsh-invocations.ps1'
$selfTestRel = 'scripts/tests/test-lint-pwsh-invocations.ps1'

# PWS001: `pwsh|powershell ... -File <script> ... --` (end of token, or followed by whitespace).
# The intermediate-args groups before AND after `-File` accept ANY
# whitespace-separated tokens (flags, positionals, or quoted values). This is
# pragmatic — we want the regex to tolerate real-world invocations like
# `pwsh -NoProfile -File foo.ps1 positional -- arg` and `pwsh -File "path with spaces.ps1" -- arg`.
# The script-path token accepts either a double-quoted string (possibly
# containing spaces), a single-quoted string, or a bare token.
$pws001Pattern = '(?:^|[\s;&|"''`(])(pwsh|powershell)\b(?:\s+\S+)*?\s+-File\s+(?:"[^"]+"|''[^'']+''|\S+)(?:\s+\S+)*?\s+--(?=\s|$|")'
# PWS001-variant: array-indirection pwsh invocation. Catches the common
# bash pattern where the pwsh command line is stored in an array and
# expanded with "${PWSH_CMD[@]}" (or any array whose name ends with
# _CMD / _PWSH / CMD — and by convention all-caps identifiers). Example:
#   PWSH_CMD=(pwsh -NoProfile -File)
#   "${PWSH_CMD[@]}" foo.ps1 -- arg     # BUG — still hits PowerShell -File mode
# We match: a `"${NAME[@]}"` expansion followed later by a `.ps1` token and
# eventually a standalone `--`. The name class is permissive (all-caps with
# optional _ digits) to catch the common conventions while keeping false
# positives low.
$pws001ArrayPattern = '"\$\{[A-Z][A-Z0-9_]*\[@\]\}"\s+(?:\S+\s+)*?\S+\.ps1(?:\s+\S+)*?\s+--(?=\s|$|")'
# PWS002: in-process `& <something> -- ...` inside test scripts. The `<something>`
# is either a literal *.ps1 path (quoted or unquoted) or a variable whose name
# ends with "Path" / "Script" or is obviously a script reference. We match the
# narrower common forms deliberately — `&` also appears in many legitimate
# contexts (Start-Job &, logical AND, etc.) so we over-index on call-style
# invocations that take `--` as the first argument.
$pws002Pattern = '&\s+(?:\$[A-Za-z_][A-Za-z0-9_]*(?:Path|Script|Ps1|Cmd|Tool)?|["''][^"'']*\.ps1["'']|[^\s"'']+\.ps1)\s+--(?=\s|$|")'

function Get-RepoRelativePath {
    param([string]$FullPath)
    $normalized = $FullPath.Replace('\', '/')
    $root = $repoRoot.Replace('\', '/')
    if ($normalized.StartsWith($root + '/')) {
        return $normalized.Substring($root.Length + 1)
    }
    return $normalized
}

function Get-TargetFiles {
    $results = [System.Collections.Generic.List[string]]::new()

    # *.sh (recursive, but skip node_modules / site / .git)
    Get-ChildItem -Path $repoRoot -Recurse -File -Filter '*.sh' -ErrorAction SilentlyContinue |
        Where-Object {
            $rel = Get-RepoRelativePath $_.FullName
            $rel -notmatch '^(node_modules|site|\.git)/'
        } |
        ForEach-Object { $results.Add($_.FullName) | Out-Null }

    # .githooks/* (non-recursive files)
    $hooksDir = Join-Path $repoRoot '.githooks'
    if (Test-Path $hooksDir) {
        Get-ChildItem -Path $hooksDir -File -ErrorAction SilentlyContinue |
            ForEach-Object { $results.Add($_.FullName) | Out-Null }
    }

    # .github/workflows/*.yml
    $wfDir = Join-Path (Join-Path $repoRoot '.github') 'workflows'
    if (Test-Path $wfDir) {
        Get-ChildItem -Path $wfDir -File -Filter '*.yml' -ErrorAction SilentlyContinue |
            ForEach-Object { $results.Add($_.FullName) | Out-Null }
        Get-ChildItem -Path $wfDir -File -Filter '*.yaml' -ErrorAction SilentlyContinue |
            ForEach-Object { $results.Add($_.FullName) | Out-Null }
    }

    # scripts/tests/*.ps1
    $testsDir = Join-Path (Join-Path $repoRoot 'scripts') 'tests'
    if (Test-Path $testsDir) {
        Get-ChildItem -Path $testsDir -File -Filter '*.ps1' -ErrorAction SilentlyContinue |
            ForEach-Object { $results.Add($_.FullName) | Out-Null }
    }

    # package.json
    $pkgJson = Join-Path $repoRoot 'package.json'
    if (Test-Path $pkgJson) { $results.Add($pkgJson) | Out-Null }

    return $results | Sort-Object -Unique
}

# Returns $true if the given line (inside a .ps1 file) is part of a comment-based
# help block. We track `<# ... #>` state across the whole file and also skip
# lines that contain a `.EXAMPLE`, `.SYNOPSIS`, `.DESCRIPTION`, etc. directive
# marker or the line immediately after one (heuristic, since CBH content lives
# inside the `<# ... #>` wrapper anyway — this is a second-level safety net for
# inline documentation).
function Get-CommentBlockMap {
    param([string[]]$Lines)

    $map = New-Object bool[] $Lines.Length
    $inBlock = $false
    for ($i = 0; $i -lt $Lines.Length; $i++) {
        $line = $Lines[$i]
        if ($inBlock) {
            $map[$i] = $true
            if ($line -match '#>') {
                $inBlock = $false
            }
            continue
        }
        if ($line -match '<#') {
            $map[$i] = $true
            if (-not ($line -match '#>')) {
                $inBlock = $true
            }
            continue
        }
        # Full-line comment beginning with `#`.
        if ($line -match '^\s*#') {
            $map[$i] = $true
        }
    }
    return , $map
}

$targets = @(Get-TargetFiles)
Write-Info "Scanning $($targets.Count) file(s)"

$violations = [System.Collections.Generic.List[object]]::new()

foreach ($file in $targets) {
    $rel = Get-RepoRelativePath $file

    # Exclusions: this script itself, and its own test (test fixtures live in
    # tempdirs — the test script text intentionally contains the bad pattern as
    # a STRING to build fixtures, but never as an actual invocation).
    if ($rel -eq $selfRel) { continue }
    if ($rel -eq $selfTestRel) { continue }

    $lines = @()
    try {
        # Coerce to array so empty files (returns $null) don't crash under
        # StrictMode when we access .Length, and single-line-no-trailing-newline
        # files (returns a scalar String) don't cause the loop to iterate over
        # characters instead of lines (silently missing violations).
        $lines = @(Get-Content -LiteralPath $file -ErrorAction Stop)
    } catch {
        Write-Info "Skipping unreadable file: $rel"
        continue
    }

    $isPs1 = $file.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase)
    $isSh = $file.EndsWith('.sh', [System.StringComparison]::OrdinalIgnoreCase)
    $isYaml = $file.EndsWith('.yml', [System.StringComparison]::OrdinalIgnoreCase) `
        -or $file.EndsWith('.yaml', [System.StringComparison]::OrdinalIgnoreCase)
    $commentMap = $null
    if ($isPs1) {
        $commentMap = Get-CommentBlockMap -Lines $lines
    }

    # Build a "logically joined" view that merges physical lines ending in `\`
    # with their successor(s). This catches bash/YAML-run multi-line pwsh
    # invocations that would otherwise slip past the per-line regex.
    #
    # joinedLines[j]      = concatenated content (with continuations collapsed
    #                       into a single space, per bash semantics)
    # joinedStartLines[j] = physical (1-based) line number where segment j
    #                       began — used for reporting.
    # joinedHasContinuation[j] = whether this joined entry was actually built
    #                       from 2+ physical lines (so we avoid double-reporting
    #                       violations that already matched on the raw line).
    #
    # Comment-line handling: bash does NOT continue a '#' comment across a
    # trailing `\` — the comment ends at the physical EOL. So when building the
    # join, we must NOT:
    #   (a) start a join group from a comment line (its `\` is a no-op), and
    #   (b) absorb a subsequent comment line as the continuation of a prior
    #       non-comment line (a comment line's contents are not part of the
    #       logical command — though bash would treat that as a syntax error,
    #       we simply terminate the join at the comment boundary).
    # Only .sh / .yml / .yaml files honor this skip; .ps1 and package.json
    # don't have bash-style `#` semantics so we leave them alone.
    $honorHashComments = $isSh -or $isYaml
    $joinedLines = [System.Collections.Generic.List[string]]::new()
    $joinedStartLines = [System.Collections.Generic.List[int]]::new()
    $joinedHasContinuation = [System.Collections.Generic.List[bool]]::new()
    $k = 0
    while ($k -lt $lines.Length) {
        $startLine = $k + 1
        $merged = $lines[$k]
        $hadContinuation = $false
        # If the start line is itself a comment and this file honors `#`
        # comments, do not join anything — record the physical line as-is so
        # the index advances correctly.
        $startIsComment = $honorHashComments -and ($merged -match '^\s*#')
        if (-not $startIsComment) {
            # `\` at end of line (possibly followed by trailing whitespace).
            while ($merged -match '\\\s*$' -and ($k + 1) -lt $lines.Length) {
                # If the NEXT line is a comment and we honor `#`, stop the
                # join at the comment boundary (bash would also stop there).
                if ($honorHashComments -and ($lines[$k + 1] -match '^\s*#')) {
                    break
                }
                $hadContinuation = $true
                # Strip the trailing backslash (and any trailing whitespace
                # before it) and replace with a single space before joining
                # the next physical line's content. This matches how
                # bash/YAML effectively sees it.
                $merged = ($merged -replace '\\\s*$', '') + ' ' + $lines[$k + 1]
                $k++
            }
        }
        $joinedLines.Add($merged) | Out-Null
        $joinedStartLines.Add($startLine) | Out-Null
        $joinedHasContinuation.Add($hadContinuation) | Out-Null
        $k++
    }

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $lineNum = $i + 1

        # Skip comment/help lines in .ps1 files.
        if ($isPs1 -and $commentMap[$i]) {
            continue
        }
        # Skip full-line comments in shell and YAML files. Note: a `#` inside
        # a quoted YAML string is also treated as a comment start here — see
        # the DESCRIPTION block for the accepted edge case.
        if (($isSh -or $isYaml) -and ($line -match '^\s*#')) {
            continue
        }

        if ($line -match $pws001Pattern) {
            $violations.Add(@{
                Path = $rel
                Line = $lineNum
                Code = 'PWS001'
                Message = "pwsh/powershell -File invocation passes '--' as a separator; PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                Content = $line.Trim()
            }) | Out-Null
            continue
        }

        if ($line -match $pws001ArrayPattern) {
            $violations.Add(@{
                Path = $rel
                Line = $lineNum
                Code = 'PWS001'
                Message = "pwsh/powershell invocation via bash array indirection (""`${NAME[@]}"") passes '--' as a separator; if the array expands to a `pwsh -File` command, PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                Content = $line.Trim()
            }) | Out-Null
            continue
        }

        if ($line -match $pws002Pattern) {
            $isTest = $rel -like 'scripts/tests/*.ps1'
            if ($isTest) {
                $violations.Add(@{
                    Path = $rel
                    Line = $lineNum
                    Code = 'PWS002'
                    Message = "Test invokes .ps1 via in-process '&' with '--'; tests must exercise the same invocation path production uses ('pwsh -NoProfile -File ... -Paths ...'), otherwise CLI-binding bugs are masked."
                    Content = $line.Trim()
                }) | Out-Null
            }
        }
    }

    # Second pass: logically-joined lines. Only consider entries actually built
    # from a continuation (otherwise we'd double-report plain single-line hits).
    for ($j = 0; $j -lt $joinedLines.Count; $j++) {
        if (-not $joinedHasContinuation[$j]) { continue }
        $joined = $joinedLines[$j]
        $startLine = $joinedStartLines[$j]
        $startIdx = $startLine - 1

        # Skip if the *physical* start line is a known comment.
        if ($isPs1 -and $commentMap[$startIdx]) { continue }
        if (($isSh -or $isYaml) -and ($lines[$startIdx] -match '^\s*#')) { continue }

        if ($joined -match $pws001Pattern) {
            $violations.Add(@{
                Path = $rel
                Line = $startLine
                Code = 'PWS001'
                Message = "pwsh/powershell -File invocation (multi-line with '\' continuation) passes '--' as a separator; PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                Content = $joined.Trim()
            }) | Out-Null
            continue
        }

        if ($joined -match $pws001ArrayPattern) {
            $violations.Add(@{
                Path = $rel
                Line = $startLine
                Code = 'PWS001'
                Message = "pwsh/powershell invocation via bash array indirection (""`${NAME[@]}"") (multi-line with '\' continuation) passes '--' as a separator; if the array expands to a `pwsh -File` command, PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                Content = $joined.Trim()
            }) | Out-Null
            continue
        }

        if ($joined -match $pws002Pattern) {
            $isTest = $rel -like 'scripts/tests/*.ps1'
            if ($isTest) {
                $violations.Add(@{
                    Path = $rel
                    Line = $startLine
                    Code = 'PWS002'
                    Message = "Test invokes .ps1 via in-process '&' with '--' (multi-line with '\' continuation); tests must exercise the same invocation path production uses ('pwsh -NoProfile -File ... -Paths ...'), otherwise CLI-binding bugs are masked."
                    Content = $joined.Trim()
                }) | Out-Null
            }
        }
    }

    # Third pass (YAML-only): detect `run: >` folded block scalars that carry
    # a multi-line pwsh invocation WITHOUT `\` continuations. YAML folds the
    # scalar body into a single space-separated string before bash sees it, so
    # the entire block runs as one command — the `--` reaches pwsh.
    #
    # We intentionally do NOT fold `run: |` (literal block scalar). Under `|`,
    # each line is preserved as a separate command line; bash runs them
    # individually, and a bare `pwsh \n -NoProfile \n -File ... \n -- arg`
    # without `\` continuations would already be a bash syntax error. The
    # `run: |`-with-backslashes case is fully covered by the continuation pass
    # above, so folding `|` here would only produce spurious duplicate reports.
    #
    # Algorithm: for each physical line matching
    # `^(\s*)(?:-\s+)?run:\s*>[-+]?`, read subsequent lines that are MORE
    # indented than the `run:` key itself. Join them with single spaces and
    # apply PWS001. Report at the `run:` line number.
    if ($isYaml) {
        for ($i = 0; $i -lt $lines.Length; $i++) {
            $line = $lines[$i]
            # Skip comments.
            if ($line -match '^\s*#') { continue }
            if ($line -notmatch '^(?<indent>\s*)(?:-\s+)?run\s*:\s*>[-+]?\s*(#.*)?$') {
                continue
            }
            $keyIndent = $Matches['indent'].Length
            $bodyLines = [System.Collections.Generic.List[string]]::new()
            $j = $i + 1
            while ($j -lt $lines.Length) {
                $next = $lines[$j]
                # Blank lines are part of the scalar — preserve them as a space
                # in the join.
                if ($next -match '^\s*$') {
                    $bodyLines.Add('') | Out-Null
                    $j++
                    continue
                }
                # Detect indent: how many leading spaces before first non-ws?
                $nextIndent = ($next -replace '^(\s*).*$', '$1').Length
                if ($nextIndent -le $keyIndent) { break }
                # Skip block-internal comment lines (bash/YAML both ignore).
                if ($next -match '^\s*#') {
                    $j++
                    continue
                }
                $bodyLines.Add($next.TrimStart()) | Out-Null
                $j++
            }
            if ($bodyLines.Count -eq 0) { continue }
            # Join with single spaces — a close-enough approximation of YAML
            # folding semantics for regex-needle matching. We don't care about
            # paragraph boundaries or literal-block newline preservation since
            # we're just searching for the `-File <script> --` pattern.
            $blockJoined = ($bodyLines -join ' ') -replace '\s+', ' '
            $blockStartLine = $i + 1
            if ($blockJoined -match $pws001Pattern) {
                $violations.Add(@{
                    Path = $rel
                    Line = $blockStartLine
                    Code = 'PWS001'
                    Message = "pwsh/powershell -File invocation inside YAML block scalar passes '--' as a separator; PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                    Content = $blockJoined.Trim()
                }) | Out-Null
                continue
            }
            if ($blockJoined -match $pws001ArrayPattern) {
                $violations.Add(@{
                    Path = $rel
                    Line = $blockStartLine
                    Code = 'PWS001'
                    Message = "pwsh/powershell invocation via bash array indirection (""`${NAME[@]}"") inside YAML block scalar passes '--' as a separator; if the array expands to a `pwsh -File` command, PowerShell -File does not honor POSIX '--' and will fail with 'parameter name '' is ambiguous'. Use explicit named params like -Paths instead."
                    Content = $blockJoined.Trim()
                }) | Out-Null
            }
        }
    }
}

if ($violations.Count -gt 0) {
    foreach ($v in $violations) {
        Write-Host ("{0}:{1}: {2} {3}" -f $v.Path, $v.Line, $v.Code, $v.Message) -ForegroundColor Red
        if ($VerboseOutput) {
            Write-Host ("    > {0}" -f $v.Content) -ForegroundColor DarkGray
        }
    }
    Write-Host ""
    Write-Host ("[lint-pwsh-invocations] {0} violation(s) found." -f $violations.Count) -ForegroundColor Red
    exit 1
}

if ($VerboseOutput) {
    Write-Host "[lint-pwsh-invocations] OK: No pwsh/powershell invocation anti-patterns detected." -ForegroundColor Green
}
exit 0

#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validate .github/dependabot.yml against the Dependabot v2 schema.

.DESCRIPTION
    Performs structural checks on one or more Dependabot configuration files to
    catch common mistakes such as invalid top-level keys, missing schedule
    blocks, and misplaced keys that belong inside groups.

    Error codes emitted:
      DEP001 - 'version: 2' is missing or does not appear before 'updates:'
      DEP002 - 'multi-ecosystem-groups:' top-level key present (invalid)
      DEP003 - 'multi-ecosystem-group:' key inside an updates entry (invalid)
      DEP004 - 'patterns:' key at updates-entry level instead of inside groups
      DEP005 - An updates entry is missing a 'schedule:' block
      DEP006 - A 'groups:' entry is missing 'patterns:' inside it

.PARAMETER Paths
    One or more paths to validate. All provided paths are checked and errors
    from all files are reported. Defaults to '.github/dependabot.yml'.

.PARAMETER VerboseOutput
    Show verbose output including which files are being checked.

.EXAMPLE
    ./scripts/lint-dependabot.ps1
    Validate the default .github/dependabot.yml.

.EXAMPLE
    ./scripts/lint-dependabot.ps1 -VerboseOutput
    Validate with verbose output.

.EXAMPLE
    ./scripts/lint-dependabot.ps1 -- .github/dependabot.yml other/dependabot.yml
    Validate multiple explicit paths.
#>
param(
    [switch]$VerboseOutput,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Paths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) {
    if ($VerboseOutput) { Write-Host "[lint-dependabot] $msg" -ForegroundColor Cyan }
}

# ── Per-file validation function ─────────────────────────────────────────────
# Returns a (possibly empty) list of error strings for the given file.
function Get-DependabotErrors {
    param([string]$FilePath)

    $lines = Get-Content $FilePath
    $fileErrors = [System.Collections.Generic.List[string]]::new()

    # ── DEP001: version: 2 must appear before the updates: section ───────────
    # Scan only lines preceding the first top-level structural key other than
    # 'version:'. Finding 'version: 2' after 'updates:' is also wrong.
    $hasVersion2 = $false
    foreach ($line in $lines) {
        # Stop scanning once we hit the updates: block or any other top-level key
        # that is not 'version:' or a comment/blank line.
        # Use '^[a-z][a-z-]*\s*:' to match only YAML keys (not arbitrary lines).
        if (($line -match '^updates\s*:') -or (($line -match '^[a-z][a-z-]*\s*:') -and ($line -notmatch '^version\s*:'))) {
            break
        }
        if ($line -match '^\s*version\s*:\s*2\s*$') {
            $hasVersion2 = $true
            break
        }
    }
    if (-not $hasVersion2) {
        $fileErrors.Add('DEP001: "version: 2" is missing or does not appear before the "updates:" section')
    }

    # ── DEP002: multi-ecosystem-groups: top-level key (invalid) ──────────────
    foreach ($line in $lines) {
        if ($line -match '^multi-ecosystem-groups\s*:') {
            $fileErrors.Add('DEP002: "multi-ecosystem-groups:" is not a valid Dependabot v2 top-level key')
            break
        }
    }

    # ── Stateful per-entry analysis (DEP003, DEP004, DEP005, DEP006) ─────────
    # We iterate line-by-line tracking:
    #   $inUpdates        - true once we pass 'updates:'
    #   $inEntry          - true inside an '  - package-ecosystem:' block
    #   $entryHasSchedule - true if the current entry has '    schedule:'
    #   $inGroupsBlock    - true while inside a '    groups:' block (4-space)
    #   $inGroupsItem     - true while inside a named group under groups (6-space)
    #   $groupsItemHasPatterns - true if current named group has 'patterns:'

    $inUpdates = $false
    $inEntry = $false
    $entryHasSchedule = $false
    $inGroupsBlock = $false
    $inGroupsItem = $false
    $groupsItemHasPatterns = $false
    $currentEcosystem = ''
    $entryLineNumber = 0
    $lineNum = 0

    foreach ($line in $lines) {
        $lineNum++

        # Detect start of 'updates:' section (top-level, no indent)
        if ($line -match '^updates\s*:') {
            $inUpdates = $true
            continue
        }

        if (-not $inUpdates) { continue }

        # Detect start of a new entry: '  - package-ecosystem:' (2-space + dash)
        if ($line -match '^  - package-ecosystem\s*:') {
            # Close previous entry
            if ($inEntry) {
                if ($inGroupsItem -and -not $groupsItemHasPatterns) {
                    $fileErrors.Add("DEP006: A 'groups:' entry (near line $lineNum) is missing 'patterns:' inside it")
                }
                if (-not $entryHasSchedule) {
                    $fileErrors.Add("DEP005: updates entry '$currentEcosystem' (near line $entryLineNumber) is missing a 'schedule:' block")
                }
            }
            $inEntry = $true
            $entryHasSchedule = $false
            $inGroupsBlock = $false
            $inGroupsItem = $false
            $groupsItemHasPatterns = $false
            $entryLineNumber = $lineNum
            if ($line -match 'package-ecosystem\s*:\s*["\x27]?(\S+?)["\x27]?\s*$') {
                $currentEcosystem = $Matches[1]
            } else {
                $currentEcosystem = 'unknown'
            }
            continue
        }

        if (-not $inEntry) { continue }

        # ── DEP003: multi-ecosystem-group: inside an entry ───────────────────
        if ($line -match '^    multi-ecosystem-group\s*:') {
            $fileErrors.Add("DEP003: ""multi-ecosystem-group:"" is not a valid Dependabot v2 key (found in entry '$currentEcosystem', line $lineNum)")
        }

        # ── Detect schedule: at entry level (4-space indent) ─────────────────
        if ($line -match '^    schedule\s*:') {
            $entryHasSchedule = $true
        }

        # ── Detect groups: block start (4-space indent) ───────────────────────
        if ($line -match '^    groups\s*:') {
            $inGroupsBlock = $true
            $inGroupsItem = $false
            $groupsItemHasPatterns = $false
            continue
        }

        if ($inGroupsBlock) {
            # Skip blank lines and comments inside the groups block — they are not group items.
            if ($line -match '^\s*$' -or $line -match '^\s*#') {
                continue
            }

            # A named group item: exactly 6-space indent + a YAML key
            # (e.g. '      all-dependencies:').  Use a strict key-character class to
            # avoid treating comment lines as group items.
            if ($line -match '^\s{6}[A-Za-z0-9_.~-]+\s*:') {
                if ($inGroupsItem -and -not $groupsItemHasPatterns) {
                    $fileErrors.Add("DEP006: A 'groups:' entry (near line $lineNum) is missing 'patterns:' inside it")
                }
                $inGroupsItem = $true
                $groupsItemHasPatterns = $false
                continue
            }

            # patterns: inside a named group (8-space indent)
            if ($line -match '^        patterns\s*:') {
                $groupsItemHasPatterns = $true
                continue
            }

            # If we hit a 4-space key that is not groups:, we've left the groups block
            if ($line -match '^    [^\s]' -and $line -notmatch '^    -') {
                if ($inGroupsItem -and -not $groupsItemHasPatterns) {
                    $fileErrors.Add("DEP006: A 'groups:' entry (near line $lineNum) is missing 'patterns:' inside it")
                    $inGroupsItem = $false
                }
                $inGroupsBlock = $false
            }
        }

        # ── DEP004: patterns: at entry level (not inside groups) ─────────────
        # 4-space indent patterns: or 2-space indent patterns:
        if (-not $inGroupsBlock) {
            if ($line -match '^    patterns\s*:' -or $line -match '^  patterns\s*:') {
                $fileErrors.Add("DEP004: ""patterns:"" found at entry level in '$currentEcosystem' (line $lineNum); it must be inside a 'groups:' block")
            }
        }
    }

    # Close the last entry
    if ($inEntry) {
        if ($inGroupsItem -and -not $groupsItemHasPatterns) {
            $fileErrors.Add("DEP006: A 'groups:' entry (near line $lineNum) is missing 'patterns:' inside it")
        }
        if (-not $entryHasSchedule) {
            $fileErrors.Add("DEP005: updates entry '$currentEcosystem' (near line $entryLineNumber) is missing a 'schedule:' block")
        }
    }

    # Return the list as a single object (not enumerated) so the caller receives
    # the list itself — including an empty list with Count=0 — rather than $null.
    # Without the unary comma PowerShell enumerates IEnumerable objects in the
    # pipeline, and an empty list becomes $null in the caller, which throws under
    # Set-StrictMode -Version Latest when .Count is accessed.
    return , $fileErrors
}

# ── Build the list of files to validate ──────────────────────────────────────
$filesToCheck = @()
if ($Paths -and $Paths.Count -gt 0) {
    foreach ($p in $Paths) {
        if ($p -eq '--') { continue }
        try {
            $resolved = Resolve-Path $p -ErrorAction Stop
            $filesToCheck += $resolved.Path
        } catch {
            Write-Info "Skipping path '$p' because it was not found."
        }
    }
}

if ($filesToCheck.Count -eq 0) {
    # Default location relative to repo root (one level up from scripts/)
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $defaultPath = Join-Path $repoRoot '.github' 'dependabot.yml'
    if (Test-Path $defaultPath) {
        $filesToCheck += $defaultPath
    } else {
        Write-Warning "dependabot.yml not found at: $defaultPath — skipping schema check."
        exit 0
    }
}

# ── Validate every file ───────────────────────────────────────────────────────
$totalErrors = 0
$showPrefix = $filesToCheck.Count -gt 1

foreach ($fileToCheck in $filesToCheck) {
    Write-Info "Checking: $fileToCheck"

    $fileErrors = Get-DependabotErrors -FilePath $fileToCheck

    if ($fileErrors.Count -eq 0) {
        if ($VerboseOutput) {
            Write-Host "  PASS  $fileToCheck" -ForegroundColor Green
        }
        continue
    }

    $totalErrors += $fileErrors.Count
    foreach ($err in $fileErrors) {
        if ($showPrefix) {
            Write-Host "${fileToCheck}: $err" -ForegroundColor Red
        } else {
            Write-Host $err -ForegroundColor Red
        }
    }
}

if ($totalErrors -eq 0 -and $VerboseOutput) {
    Write-Host "All dependabot config files passed schema checks." -ForegroundColor Green
}

if ($totalErrors -gt 0) {
    exit 1
} else {
    exit 0
}

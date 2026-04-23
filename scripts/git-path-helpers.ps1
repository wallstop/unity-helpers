# =============================================================================
# Git Path Helpers (PowerShell, dot-sourceable)
# =============================================================================
# Shared helpers for normalizing paths before handing them to git plumbing
# commands that are sensitive to path format.
#
# The immediate motivator is `git check-ignore`, which reliably accepts
# repo-relative POSIX paths but can silently MISCLASSIFY input that is an
# absolute path (especially on Windows where absolute paths contain `\`).
# A misclassification where an ignored file looks "not ignored" is
# particularly hazardous because safety gates (e.g. agent-preflight's
# auto-delete guard) then refuse to remove a legitimate stray file.
#
# Callers should always normalize before calling:
#     git check-ignore
#     git ls-files -- <path>
#     git diff --relative
# and any other git command that documents repo-relative semantics.
#
# Usage:
#   . (Join-Path $PSScriptRoot 'git-path-helpers.ps1')
#   $rel = ConvertTo-GitRelativePosixPath -Path $abs -RepoRoot $repoRoot
#   if ($null -eq $rel) { # path is outside the repo — caller decides what to do }
# =============================================================================

Set-StrictMode -Version Latest

<#
.SYNOPSIS
    Normalize a filesystem path to a repo-relative POSIX form suitable for
    `git check-ignore` and related git plumbing commands.

.DESCRIPTION
    - If $Path is absolute, resolve it to a full path and verify that it
      lives under $RepoRoot. On Windows the match is OrdinalIgnoreCase
      because NTFS is case-insensitive by default; on Linux/macOS the
      match is Ordinal (case-sensitive) because ext4/APFS are case-
      sensitive by default. Platform detection uses
      [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform,
      which is available on Windows PowerShell 5.1 (.NET Framework 4.7.1+).
      If the path is outside the repo root, returns $null.
    - If $Path is relative, leave it unchanged aside from normalizing
      backslashes to forward slashes and stripping a leading `./`.
    - If $Path IS the repo root, returns `.` (the conventional
      representation for "the repo itself").
    - All path separators in the output are `/` regardless of platform.
    - Trailing `/` on the input is tolerated.

    Rationale: `git check-ignore` does NOT reliably accept Windows
    backslash paths, and mixing absolute + relative input can silently
    misclassify files. Normalizing once at the call site prevents the
    entire class of bug.

.PARAMETER Path
    The filesystem path to normalize. May be absolute or relative. Required.

.PARAMETER RepoRoot
    The repository root as an absolute path. Used both for resolving
    absolute $Path inputs (to strip the prefix) and for validating that
    the path lives inside the repo. Required.

.OUTPUTS
    [string] — repo-relative POSIX path, or `.` when the input IS the repo
        root itself.
    $null    — when the input is an absolute path outside the repo root.

.EXAMPLE
    ConvertTo-GitRelativePosixPath -Path '/home/user/repo/foo/bar.txt' -RepoRoot '/home/user/repo'
    # Returns: foo/bar.txt

.EXAMPLE
    ConvertTo-GitRelativePosixPath -Path 'C:\repo\Docs\readme.md' -RepoRoot 'C:\repo'
    # Returns: Docs/readme.md

.EXAMPLE
    ConvertTo-GitRelativePosixPath -Path 'Docs\readme.md' -RepoRoot 'C:\repo'
    # Returns: Docs/readme.md

.EXAMPLE
    ConvertTo-GitRelativePosixPath -Path '/tmp/outside' -RepoRoot '/home/user/repo'
    # Returns: $null
#>
function ConvertTo-GitRelativePosixPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
        return $null
    }

    # Normalize repo root once. GetFullPath collapses `.` and `..` segments
    # and produces a platform-native path we can string-compare against.
    $normalizedRepoRoot = [System.IO.Path]::GetFullPath($RepoRoot)
    $normalizedRepoRoot = $normalizedRepoRoot.Replace('\', '/')
    # Strip any trailing slash so the prefix comparison below is consistent.
    $normalizedRepoRoot = $normalizedRepoRoot.TrimEnd('/')

    if ([System.IO.Path]::IsPathRooted($Path)) {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
        $fullPath = $fullPath.Replace('\', '/')
        $fullPath = $fullPath.TrimEnd('/')

        if ($fullPath -eq $normalizedRepoRoot) {
            return '.'
        }

        $repoPrefix = "$normalizedRepoRoot/"
        # Platform-aware prefix comparison: Windows NTFS is case-insensitive
        # by default (C:\Repo and c:\repo resolve to the same file), whereas
        # ext4/APFS on Linux/macOS are case-sensitive. Using OrdinalIgnoreCase
        # unconditionally would quietly fold `/home/user/Repo/foo` into a
        # match for RepoRoot `/home/user/repo`, which on a case-sensitive FS
        # is a DIFFERENT repo and therefore outside the intended root.
        $comparison = if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
                [System.Runtime.InteropServices.OSPlatform]::Windows)) {
            [System.StringComparison]::OrdinalIgnoreCase
        } else {
            [System.StringComparison]::Ordinal
        }
        if (-not $fullPath.StartsWith($repoPrefix, $comparison)) {
            return $null
        }

        return $fullPath.Substring($repoPrefix.Length)
    }

    $normalizedPath = $Path.Replace('\', '/')
    if ($normalizedPath.StartsWith('./')) {
        $normalizedPath = $normalizedPath.Substring(2)
    }
    $normalizedPath = $normalizedPath.TrimEnd('/')

    if ([string]::IsNullOrWhiteSpace($normalizedPath)) {
        return '.'
    }

    return $normalizedPath
}

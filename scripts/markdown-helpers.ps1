# Shared helpers for markdown-aware parsing across lint and test scripts.
#
# Provides code-block-aware functions that correctly handle fenced code blocks
# when analyzing markdown structure (headings, etc.).
#
# Usage:
#   . $PSScriptRoot/markdown-helpers.ps1
#   $h1Lines = Get-MarkdownH1Lines -Lines (Get-Content -Path 'file.md')

function Get-MarkdownH1Lines {
    param([string[]]$Lines)
    $result = @()
    $inFence = $false
    $fenceChar = ''
    $fenceLen = 0
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        if (-not $inFence) {
            if ($line -match '^\s*(`{3,}|~{3,})') {
                $fenceChar = $Matches[1][0]
                $fenceLen = $Matches[1].Length
                $inFence = $true
                continue
            }
        }
        else {
            if ($line -match "^\s*($fenceChar{$fenceLen,})\s*$") {
                $inFence = $false
                continue
            }
            continue
        }
        if ($line -match '^# ') {
            $result += [PSCustomObject]@{ LineNumber = $i + 1; Text = $line }
        }
    }
    return $result
}

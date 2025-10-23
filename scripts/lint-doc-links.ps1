Param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath '.').Path

function Write-Violation {
    param(
        [string]$File,
        [int]$LineNumber,
        [string]$Message,
        [string]$Line
    )
    $prefix = "${File}:${LineNumber}:"
    if ($VerboseOutput) {
        Write-Host "$prefix $Message`n  $Line" -ForegroundColor Red
    } else {
        Write-Host "$prefix $Message" -ForegroundColor Red
    }
}

$schemePattern = [regex]'^[a-zA-Z][a-zA-Z0-9+\.-]*:'

function Get-MarkdownLinkTarget {
    param([string]$RawTarget)

    if ([string]::IsNullOrWhiteSpace($RawTarget)) {
        return $null
    }

    $trimmed = $RawTarget.Trim()

    if ($trimmed.StartsWith('<') -and $trimmed.Contains('>')) {
        $closingIndex = $trimmed.IndexOf('>')
        if ($closingIndex -gt 1) {
            $trimmed = $trimmed.Substring(1, $closingIndex - 1)
        }
    }

    $spaceIndex = $trimmed.IndexOfAny(@(' ', "`t"))
    if ($spaceIndex -ge 0) {
        $trimmed = $trimmed.Substring(0, $spaceIndex)
    }

    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        return $null
    }

    return $trimmed
}

function Is-LocalImageTarget {
    param([string]$Target)

    if ([string]::IsNullOrWhiteSpace($Target)) {
        return $false
    }

    $trimmed = $Target.Trim()
    if ($schemePattern.IsMatch($trimmed)) {
        return $false
    }

    if ($trimmed.StartsWith('#') -or $trimmed.StartsWith('//')) {
        return $false
    }

    return $true
}

function Resolve-ImagePath {
    param(
        [string]$SourceFile,
        [string]$Target,
        [string]$RepoRoot
    )

    if ([string]::IsNullOrWhiteSpace($Target)) {
        return $null
    }

    $normalized = $Target.Trim() -replace '\\', '/'
    $cutIndex = $normalized.IndexOfAny(@('#', '?'))
    if ($cutIndex -ge 0) {
        $normalized = $normalized.Substring(0, $cutIndex)
    }

    while ($normalized.StartsWith('./')) {
        $normalized = $normalized.Substring(2)
    }

    $normalized = $normalized.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    $separator = [System.IO.Path]::DirectorySeparatorChar
    $normalized = $normalized.Replace('/', $separator).Replace('\', $separator)

    $sourceDirectory = Split-Path -Path $SourceFile -Parent
    $candidate = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($sourceDirectory, $normalized))

    if (Test-Path -LiteralPath $candidate) {
        return $candidate
    }

    if ($normalized.Length -gt 0 -and $normalized[0] -ne '.') {
        $rootRelative = $normalized.TrimStart($separator)
        $candidateFromRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::Combine($RepoRoot, $rootRelative))
        if (Test-Path -LiteralPath $candidateFromRoot) {
            return $candidateFromRoot
        }
    }

    return $null
}

$mdPattern = [regex]'[A-Za-z0-9._/\-]+\.md(?:#[A-Za-z0-9_\-]+)?'
$linkPattern = [regex]'\[[^\]]+\]\([^)]+\)'
$anglePattern = [regex]'<[^>]+>'
$filenameTextLinkPattern = [regex]'\[(?<text>[^\]]+?\.md(?:#[^\]]+)?)\]\((?<target>[^)]+?\.md(?:#[^)]+)?)\)'
$inlineCodeMdPattern = [regex]'`[^`\n]*?\.md[^`\n]*?`'
$imagePattern = [regex]'!\[[^\]]*\]\((?<target>[^)]+)\)'
$imageReferencePattern = [regex]'!\[[^\]]*\]\[(?<label>[^\]]+)\]'
$definitionPattern = [regex]'^\s*\[(?<label>[^\]]+)\]:\s*(?<rest>.+)$'

$violationCount = 0

Get-ChildItem -Path . -Recurse -Include *.md -File |
    Where-Object { $_.FullName -notmatch '(?i)[\\/](node_modules)[\\/]' } |
    ForEach-Object {
    $file = $_.FullName
    $lines = Get-Content -LiteralPath $file
    $lineCount = $lines.Length
    $linkDefinitions = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([System.StringComparer]::OrdinalIgnoreCase)
    $inFence = $false

    for ($index = 0; $index -lt $lineCount; $index++) {
        $line = $lines[$index]
        if ($line -match '^\s*```' -or $line -match '^\s*~~~') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) { continue }

        $definitionMatch = $definitionPattern.Match($line)
        if ($definitionMatch.Success) {
            $label = $definitionMatch.Groups['label'].Value.Trim()
            $rest = $definitionMatch.Groups['rest'].Value
            $target = Get-MarkdownLinkTarget $rest
            if ($label -and $target) {
                $linkDefinitions[$label] = $target
            }
        }
    }

    $inFence = $false
    for ($index = 0; $index -lt $lineCount; $index++) {
        $line = $lines[$index]
        $lineNo = $index + 1

        if ($line -match '^\s*```' -or $line -match '^\s*~~~') {
            $inFence = -not $inFence
            continue
        }
        if ($inFence) { continue }

        if ($inlineCodeMdPattern.IsMatch($line)) {
            $violationCount++
            Write-Violation -File $file -LineNumber $lineNo -Message "Inline code mentions .md; use a human-readable link instead" -Line $line
        }

        $stripped = $linkPattern.Replace($line, '')
        $stripped = $anglePattern.Replace($stripped, '')

        if ($mdPattern.IsMatch($stripped)) {
            $violationCount++
            Write-Violation -File $file -LineNumber $lineNo -Message "Bare .md mention; convert to [Readable Text](file.md)" -Line $line
        }

        foreach ($m in $filenameTextLinkPattern.Matches($line)) {
            $text = $m.Groups['text'].Value
            $target = $m.Groups['target'].Value
            if ($text -match '\.md') {
                $violationCount++
                Write-Violation -File $file -LineNumber $lineNo -Message "Link text is a filename; use human-readable text for $target" -Line $line
            }
        }

        foreach ($match in $imagePattern.Matches($line)) {
            $rawTarget = $match.Groups['target'].Value
            $resolvedTarget = Get-MarkdownLinkTarget $rawTarget
            if ($resolvedTarget -and (Is-LocalImageTarget $resolvedTarget)) {
                $imagePath = Resolve-ImagePath -SourceFile $file -Target $resolvedTarget -RepoRoot $repoRoot
                if (-not $imagePath) {
                    $violationCount++
                    Write-Violation -File $file -LineNumber $lineNo -Message "Image target '$resolvedTarget' does not resolve to a file" -Line $line
                }
            }
        }

        foreach ($match in $imageReferencePattern.Matches($line)) {
            $label = $match.Groups['label'].Value.Trim()
            if ($linkDefinitions.ContainsKey($label)) {
                $resolvedTarget = $linkDefinitions[$label]
                if ($resolvedTarget -and (Is-LocalImageTarget $resolvedTarget)) {
                    $imagePath = Resolve-ImagePath -SourceFile $file -Target $resolvedTarget -RepoRoot $repoRoot
                    if (-not $imagePath) {
                        $violationCount++
                        Write-Violation -File $file -LineNumber $lineNo -Message "Image reference '$label' points to '$resolvedTarget' which does not resolve" -Line $line
                    }
                }
            }
        }
    }
}

if ($violationCount -gt 0) {
    Write-Host "Markdown link lint failed: $violationCount issue(s) found." -ForegroundColor Red
    exit 1
} else {
    Write-Host "Markdown link lint passed: no issues found." -ForegroundColor Green
}


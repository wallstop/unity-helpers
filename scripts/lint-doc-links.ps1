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

function Is-LocalTarget {
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

function Resolve-LocalPath {
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

    # Strip GitHub Pages baseurl prefix if present
    # This handles absolute paths like /unity-helpers/docs/images/...
    if ($normalized.StartsWith('/unity-helpers/')) {
        $normalized = $normalized.Substring('/unity-helpers'.Length)
    }

    while ($normalized.StartsWith('./')) {
        $normalized = $normalized.Substring(2)
    }

    $normalized = $normalized.Trim()
    if ([string]::IsNullOrWhiteSpace($normalized)) {
        return $null
    }

    try {
        $normalized = [System.Uri]::UnescapeDataString($normalized)
    } catch {
        # ignore decoding errors and keep original
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
# LIMITATION: [^)] patterns cannot handle URLs with parentheses like file(1).md
# This is valid markdown but rare. Files with parens in names should be renamed.
$linkPattern = [regex]'\[[^\]]+\]\([^)]+\)'
$standardLinkPattern = [regex]'(?<!\!)\[(?<text>[^\]]+)\]\((?<target>[^)]+)\)'
$anglePattern = [regex]'<[^>]+>'
$filenameTextLinkPattern = [regex]'\[(?<text>[^\]]+?\.md(?:#[^\]]+)?)\]\((?<target>[^)]+?\.md(?:#[^)]+)?)\)'
# Match inline code that mentions a .md file path (not just a bare extension like `.md` or `.json`)
# Excludes: `*.md`, `.md`, `*.json`, `.cs` etc. - these are file type references, not file paths
$inlineCodeMdPattern = [regex]'`[^`\n]*[A-Za-z0-9_\-]+\.md[^`\n]*`'
# Pattern to detect bare file extension references that should NOT be flagged
$fileExtensionPattern = [regex]'^`(\*)?\.[\w]+`$'
$imagePattern = [regex]'!\[[^\]]*\]\((?<target>[^)]+)\)'
$imageReferencePattern = [regex]'!\[[^\]]*\]\[(?<label>[^\]]+)\]'
$definitionPattern = [regex]'^\s*\[(?<label>[^\]]+)\]:\s*(?<rest>.+)$'
# Pattern to detect internal links missing ./ or ../ prefix (starts with letter, not a scheme)
# This is CRITICAL for GitHub Pages - jekyll-relative-links requires explicit relative paths
# BEHAVIOR: This intentionally matches broadly (any link starting with a letter) and then
# filters out external schemes (http://, ftp://, etc.) in the loop below. This approach
# ensures we catch all internal links that need the relative prefix, while correctly
# skipping external links regardless of their scheme.
$missingRelativePrefixPattern = [regex]'\]\((?<target>[a-zA-Z][^)]*)\)'

# Pattern to detect absolute GitHub Pages paths that won't work in CI
# Links like /unity-helpers/ or /unity-helpers/docs/... break when validated locally
# because /unity-helpers is the GitHub Pages site baseurl, not a real directory
$absoluteGitHubPagesPrefixPattern = [regex]'\]\((?<target>/unity-helpers(?:/[^)]*)?)\)'

$violationCount = 0
$codeDocsPattern = [regex]'(?i)docs[\\/][A-Za-z0-9._/\\-]+\.md(?:#[A-Za-z0-9_\-]+)?'
$codeFileExtensions = @('.cs', '.csproj', '.props', '.targets', '.ps1', '.psm1', '.psd1', '.py', '.ts', '.tsx', '.js', '.jsx', '.json', '.yml', '.yaml', '.sh', '.cmd')

# Use git ls-files for efficiency and to respect .gitignore
$gitFiles = git ls-files --cached --others --exclude-standard 2>$null
if (-not $gitFiles) {
    Write-Host "Warning: Not in a git repository or git not available, falling back to filesystem scan" -ForegroundColor Yellow
    $gitFiles = Get-ChildItem -Path . -Recurse -File | ForEach-Object { $_.FullName.Substring($repoRoot.Length + 1) }
}

$mdFiles = $gitFiles | Where-Object { $_ -match '\.md$' }

$mdFiles | ForEach-Object {
    $relativePath = $_
    $file = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $file)) { return }
    $lines = Get-Content -LiteralPath $file
    $lineCount = $lines.Length
    $linkDefinitions = New-Object 'System.Collections.Generic.Dictionary[string,object]' ([System.StringComparer]::OrdinalIgnoreCase)
    $inFence = $false

    for ($index = 0; $index -lt $lineCount; $index++) {
        $line = $lines[$index]
        $lineNo = $index + 1
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
                $linkDefinitions[$label] = [pscustomobject]@{
                    Target = $target
                    LineNumber = $lineNo
                }
            }
        }
    }

    foreach ($entry in $linkDefinitions.GetEnumerator()) {
        $definitionTarget = $entry.Value.Target
        $definitionLine = $entry.Value.LineNumber
        if (-not $definitionTarget) { continue }
        if (-not (Is-LocalTarget $definitionTarget)) { continue }
        if ($definitionTarget -notmatch '(?i)\.md($|[?#])') { continue }

        $resolvedDefinition = Resolve-LocalPath -SourceFile $file -Target $definitionTarget -RepoRoot $repoRoot
        if (-not $resolvedDefinition) {
            $violationCount++
            Write-Violation -File $file -LineNumber $definitionLine -Message "Reference link target '$definitionTarget' does not resolve to an existing markdown file" -Line ''
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

        # Check for inline code that mentions .md files (but not bare extension references)
        foreach ($match in $inlineCodeMdPattern.Matches($line)) {
            $codeContent = $match.Value
            # Skip if it's a file extension or glob pattern reference like:
            # `.md`, `*.md`, `**/*.md`, `.json`, `*.xml`, etc.
            # Pattern: starts with `, optional glob chars (**/), optional *, then .extension, ends with `
            if ($codeContent -match '^`(\*\*/|\./)?(\*)?\.[\w]+`$') {
                continue
            }
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

        # Check for absolute GitHub Pages paths (e.g., /unity-helpers/ or /unity-helpers/docs/...)
        # These break in CI because /unity-helpers is the GitHub Pages baseurl, not a real directory
        # IMPORTANT: First strip inline code (backticks) to avoid false positives from example links
        $lineWithoutInlineCode = $line -replace '``[^`]*``', '' -replace '`[^`]*`', ''
        foreach ($match in $absoluteGitHubPagesPrefixPattern.Matches($lineWithoutInlineCode)) {
            $target = $match.Groups['target'].Value
            $violationCount++
            # Provide helpful fix suggestion
            $suggestedFix = $target -replace '^/unity-helpers/?', './'
            if ($suggestedFix -eq './') { $suggestedFix = './README.md' }
            Write-Violation -File $file -LineNumber $lineNo -Message "Absolute GitHub Pages path '$target' will break in CI; use relative path instead (e.g., '$suggestedFix')" -Line $line
        }

        # Check for internal links missing ./ or ../ relative prefix
        # This is CRITICAL for GitHub Pages - jekyll-relative-links requires explicit relative paths
        # Skip: external links (http/https/mailto), anchors (#), images (!), and links already with ./
        # IMPORTANT: First strip inline code (backticks) to avoid false positives from example links
        # LIMITATIONS of inline code stripping:
        # - Does not handle escaped backticks (\`) - these are rare in practice
        # - Triple+ backticks on same line may have unexpected interactions
        # - Nested backticks are not standard markdown
        # The -replace pattern removes double-backtick spans first (``text``), then single spans
        foreach ($match in $missingRelativePrefixPattern.Matches($lineWithoutInlineCode)) {
            $target = $match.Groups['target'].Value
            # Skip external links (http:// https:// mailto: etc.)
            if ($target -match '^[a-zA-Z][a-zA-Z0-9+\.-]*:') { continue }
            # Skip if target is empty or starts with ./ or ../
            if ($target -match '^\.\.?/') { continue }
            # Skip anchor-only links
            if ($target.StartsWith('#')) { continue }
            # This is a bare path without relative prefix - flag it
            $violationCount++
            Write-Violation -File $file -LineNumber $lineNo -Message "Internal link '$target' missing relative prefix (./ or ../); jekyll-relative-links requires explicit relative paths" -Line $line
        }

        foreach ($match in $standardLinkPattern.Matches($line)) {
            $rawTarget = $match.Groups['target'].Value
            $resolvedTarget = Get-MarkdownLinkTarget $rawTarget
            if (-not $resolvedTarget) { continue }
            if (-not (Is-LocalTarget $resolvedTarget)) { continue }
            if ($resolvedTarget -notmatch '(?i)\.md($|[?#])') { continue }

            $linkPath = Resolve-LocalPath -SourceFile $file -Target $resolvedTarget -RepoRoot $repoRoot
            if (-not $linkPath) {
                $violationCount++
                Write-Violation -File $file -LineNumber $lineNo -Message "Markdown link target '$resolvedTarget' does not resolve to an existing markdown file" -Line $line
            }
        }

        foreach ($match in $imagePattern.Matches($line)) {
            $rawTarget = $match.Groups['target'].Value
            $resolvedTarget = Get-MarkdownLinkTarget $rawTarget
            if ($resolvedTarget -and (Is-LocalTarget $resolvedTarget)) {
                $imagePath = Resolve-LocalPath -SourceFile $file -Target $resolvedTarget -RepoRoot $repoRoot
                if (-not $imagePath) {
                    $violationCount++
                    Write-Violation -File $file -LineNumber $lineNo -Message "Image target '$resolvedTarget' does not resolve to a file" -Line $line
                }
            }
        }

        foreach ($match in $imageReferencePattern.Matches($line)) {
            $label = $match.Groups['label'].Value.Trim()
            if ($linkDefinitions.ContainsKey($label)) {
                $definition = $linkDefinitions[$label]
                $resolvedTarget = $definition.Target
                if ($resolvedTarget -and (Is-LocalTarget $resolvedTarget)) {
                    $imagePath = Resolve-LocalPath -SourceFile $file -Target $resolvedTarget -RepoRoot $repoRoot
                    if (-not $imagePath) {
                        $violationCount++
                        Write-Violation -File $file -LineNumber $lineNo -Message "Image reference '$label' points to '$resolvedTarget' which does not resolve" -Line $line
                    }
                }
            }
        }
    }
}

# Process code files for docs references
$codeFiles = $gitFiles | Where-Object { 
    $ext = [System.IO.Path]::GetExtension($_)
    $codeFileExtensions -contains $ext
}

$codeFiles | ForEach-Object {
    $relativePath = $_
    $file = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $file)) { return }
    $lines = Get-Content -LiteralPath $file
    for ($index = 0; $index -lt $lines.Length; $index++) {
        $line = $lines[$index]
        $lineNo = $index + 1
        foreach ($match in $codeDocsPattern.Matches($line)) {
            $rawTarget = $match.Value
            $normalizedTarget = $rawTarget -replace '\\', '/'
            $resolvedPath = Resolve-LocalPath -SourceFile $file -Target $normalizedTarget -RepoRoot $repoRoot
            if (-not $resolvedPath) {
                $violationCount++
                Write-Violation -File $file -LineNumber $lineNo -Message "Source reference '$normalizedTarget' does not resolve to an existing markdown file" -Line $line
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


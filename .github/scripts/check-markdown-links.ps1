Param(
    [string]$Root = "."
)

$ErrorActionPreference = 'Stop'

function Normalize-Name {
    param([string]$s)
    if ([string]::IsNullOrWhiteSpace($s)) { return "" }
    # Remove extension (like .md), collapse non-alphanumerics, lowercase
    $noExt = $s -replace '\.[^\.]+$',''
    $normalized = ($noExt -replace '[^A-Za-z0-9]', '')
    return $normalized.ToLowerInvariant()
}

$issueCount = 0

# Exclude typical directories that shouldn't be scanned
$excludeDirs = @('.git', 'node_modules', '.vs')

$mdFiles = Get-ChildItem -Path $Root -Recurse -File -Filter *.md |
    Where-Object { $excludeDirs -notcontains $_.Directory.Name }

# Regex for inline markdown links (exclude images), capture optional title
$pattern = '(?<!\!)\[(?<text>[^\]]+)\]\((?<target>[^)\s]+)(?:\s+"[^"]*")?\)'

foreach ($file in $mdFiles) {
    $lines = Get-Content -LiteralPath $file.FullName -Encoding UTF8
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $matches = [System.Text.RegularExpressions.Regex]::Matches($line, $pattern)
        foreach ($m in $matches) {
            $text = $m.Groups['text'].Value.Trim()
            $targetRaw = $m.Groups['target'].Value.Trim()

            # Skip anchors, external links, and mailto
            if ($targetRaw -match '^(#|https?://|mailto:|tel:|data:)') { continue }

            # Remove query/anchor for file checks
            $targetCore = $targetRaw -replace '[?#].*$',''

            # Decode URL-encoded chars
            try { $targetCore = [uri]::UnescapeDataString($targetCore) } catch { }

            # Only care about links to markdown files
            if (-not ($targetCore -match '\.md$')) { continue }

            $fileName = [System.IO.Path]::GetFileName($targetCore)
            $baseName = [System.IO.Path]::GetFileNameWithoutExtension($targetCore)

            # Fail when the visible link text is the raw file name
            $isExactFileName = $text.Equals($fileName, [System.StringComparison]::OrdinalIgnoreCase)

            # Also fail when the visible text looks like a path or ends with .md
            # contains path separators and no whitespace (heuristic for raw paths)
            $looksLikePath = ($text -match '[\\/]' -and -not ($text -match '\\s'))
            $looksLikeMarkdownFileName = $text.Trim().ToLowerInvariant().EndsWith('.md')

            if ($isExactFileName -or $looksLikePath -or $looksLikeMarkdownFileName) {
                $issueCount++
                $lineNo = $i + 1
                $msg = "Link text '$text' should be human-readable, not a raw file name or path"
                # GitHub Actions annotation
                Write-Output "::error file=$($file.FullName),line=$lineNo::$msg (target: $targetRaw)"
            }
        }
    }
}

if ($issueCount -gt 0) {
    Write-Host "Found $issueCount documentation link(s) with non-human-readable text." -ForegroundColor Red
    Write-Host "Use a descriptive phrase instead of the raw file name."
    exit 1
}
else {
    Write-Host "All markdown links have human-readable text."
}

Param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'

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

$mdPattern = [regex]'[A-Za-z0-9._/\-]+\.md(?:#[A-Za-z0-9_\-]+)?'
$linkPattern = [regex]'\[[^\]]+\]\([^)]+\)'
$anglePattern = [regex]'<[^>]+>'
$filenameTextLinkPattern = [regex]'\[(?<text>[^\]]+?\.md(?:#[^\]]+)?)\]\((?<target>[^)]+?\.md(?:#[^)]+)?)\)'
$inlineCodeMdPattern = [regex]'`[^`\n]*?\.md[^`\n]*?`'

$violationCount = 0

Get-ChildItem -Path . -Recurse -Include *.md -File | ForEach-Object {
    $file = $_.FullName
    $inFence = $false
    $lineNo = 0
    Get-Content -LiteralPath $file | ForEach-Object {
        $line = $_
        $lineNo++

        # Track fenced code blocks (``` or ~~~)
        if ($line -match '^\s*```' -or $line -match '^\s*~~~') {
            $inFence = -not $inFence
        }
        if ($inFence) { return }

        # 1) Flag inline code mentions of .md
        if ($inlineCodeMdPattern.IsMatch($line)) {
            $violationCount++
            Write-Violation -File $file -LineNumber $lineNo -Message "Inline code mentions .md; use a human-readable link instead" -Line $line
        }

        # Strip markdown links and angle-bracket autolinks before scanning for bare mentions
        $stripped = $linkPattern.Replace($line, '')
        $stripped = $anglePattern.Replace($stripped, '')

        # 2) Bare .md mention outside links
        if ($mdPattern.IsMatch($stripped)) {
            $violationCount++
            Write-Violation -File $file -LineNumber $lineNo -Message "Bare .md mention; convert to [Readable Text](file.md)" -Line $line
        }

        # 3) Links whose visible text is still a filename
        foreach ($m in $filenameTextLinkPattern.Matches($line)) {
            $text = $m.Groups['text'].Value
            $target = $m.Groups['target'].Value
            # Ignore if the text already contains spaces (unlikely for filename.md); otherwise flag
            if ($text -match '\.md') {
                $violationCount++
                Write-Violation -File $file -LineNumber $lineNo -Message "Link text is a filename; use human-readable text for $target" -Line $line
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

<#
.SYNOPSIS
    Language-aware comment masking for text-based linters.

.DESCRIPTION
    Provides utilities that replace comment characters with spaces while
    preserving line count and column offsets. Callers can scan the masked
    output with regular expressions without tripping on documentation,
    docstrings, or language comments, while still reporting violations
    against the original source text.

.NOTES
    Public API:
        Get-LanguageFromExtension -Path <string>
        Get-CommentMaskedLines    -Lines <string[]> -Language <string>
        Get-CommentRanges         -Text  <string>   -Language <string>
#>

Set-StrictMode -Version Latest

$script:LanguageByExtension = @{
    '.ps1'      = 'powershell'
    '.psm1'     = 'powershell'
    '.psd1'     = 'powershell'
    '.cs'       = 'csharp'
    '.csx'      = 'csharp'
    '.js'       = 'javascript'
    '.jsx'      = 'javascript'
    '.mjs'      = 'javascript'
    '.cjs'      = 'javascript'
    '.ts'       = 'typescript'
    '.tsx'      = 'typescript'
    '.py'       = 'python'
    '.sh'       = 'shell'
    '.bash'     = 'shell'
    '.yml'      = 'yaml'
    '.yaml'     = 'yaml'
    '.cmd'      = 'cmd'
    '.bat'      = 'cmd'
    '.json'     = 'json'
    '.md'       = 'markdown'
    '.markdown' = 'markdown'
    '.csproj'   = 'xml'
    '.props'    = 'xml'
    '.targets'  = 'xml'
}

function Get-LanguageFromExtension {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([string]::IsNullOrWhiteSpace($Path)) { return $null }

    $ext = [System.IO.Path]::GetExtension($Path)
    if ([string]::IsNullOrEmpty($ext)) { return $null }

    $key = $ext.ToLowerInvariant()
    if ($script:LanguageByExtension.ContainsKey($key)) {
        return $script:LanguageByExtension[$key]
    }
    return $null
}

function New-CommentRange {
    param(
        [int]$Start,
        [int]$End,
        [string]$Kind
    )
    return [pscustomobject]@{
        Start = $Start
        End   = $End
        Kind  = $Kind
    }
}

function Get-PowerShellCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        $c = $Text[$i]

        if ($c -eq '<' -and ($i + 1) -lt $len -and $Text[$i + 1] -eq '#') {
            $start = $i
            $i += 2
            $found = $false
            while ($i -lt $len - 1) {
                if ($Text[$i] -eq '#' -and $Text[$i + 1] -eq '>') {
                    $i += 2
                    $found = $true
                    break
                }
                $i++
            }
            if (-not $found) {
                Write-Warning "PowerShell block comment starting at offset $start was not terminated before EOF."
                $i = $len
            }
            $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
            continue
        }

        if ($c -eq '#') {
            $start = $i
            while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
            $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'LineComment'))
            continue
        }

        if ($c -eq '@' -and ($i + 1) -lt $len -and ($Text[$i + 1] -eq '"' -or $Text[$i + 1] -eq "'")) {
            $quote = $Text[$i + 1]
            $j = $i + 2
            while ($j -lt $len -and $Text[$j] -ne "`n") { $j++ }
            if ($j -lt $len) {
                $i = $j + 1
                $foundClose = $false
                while ($i -lt $len) {
                    if ($Text[$i] -eq "`n") {
                        # Per PowerShell spec, the here-string terminator `"@` (or `'@`)
                        # MUST appear at column 0 of its line — no leading whitespace —
                        # AND nothing else may follow it on the same line except an
                        # optional CR/LF or end-of-text. Stripping leading whitespace
                        # here previously made the closer match indented `"@` and
                        # closer lines with trailing junk, both of which are syntax
                        # errors in real PowerShell.
                        $k = $i + 1
                        if (($k + 1) -lt $len -and $Text[$k] -eq $quote -and $Text[$k + 1] -eq '@') {
                            $after = $k + 2
                            if ($after -ge $len -or $Text[$after] -eq "`n" -or $Text[$after] -eq "`r") {
                                $i = $after
                                $foundClose = $true
                                break
                            }
                        }
                        if ($k -eq $len - 2 -and $Text[$k] -eq $quote -and $Text[$k + 1] -eq '@') {
                            $i = $k + 2
                            $foundClose = $true
                            break
                        }
                    }
                    $i++
                }
                if (-not $foundClose) {
                    Write-Warning "PowerShell here-string starting with @$quote was not terminated before EOF."
                    $i = $len
                }
                continue
            }
        }

        if ($c -eq '"') {
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '`' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq '"') { $i++; break }
                $i++
            }
            continue
        }

        if ($c -eq "'") {
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq "'") {
                    if (($i + 1) -lt $len -and $Text[$i + 1] -eq "'") { $i += 2; continue }
                    $i++; break
                }
                $i++
            }
            continue
        }

        $i++
    }

    return ,$ranges
}

function Get-CSharpCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        $c = $Text[$i]

        if ($c -eq '/' -and ($i + 1) -lt $len) {
            $next = $Text[$i + 1]
            if ($next -eq '/') {
                $start = $i
                $kind = 'LineComment'
                if (($i + 2) -lt $len -and $Text[$i + 2] -eq '/') { $kind = 'XmlDoc' }
                while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind $kind))
                continue
            }
            if ($next -eq '*') {
                $start = $i
                $i += 2
                $found = $false
                while ($i -lt $len - 1) {
                    if ($Text[$i] -eq '*' -and $Text[$i + 1] -eq '/') {
                        $i += 2
                        $found = $true
                        break
                    }
                    $i++
                }
                if (-not $found) {
                    Write-Warning "C# block comment starting at offset $start was not terminated before EOF."
                    $i = $len
                }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
                continue
            }
        }

        if ($c -eq '"' -and ($i + 2) -lt $len -and $Text[$i + 1] -eq '"' -and $Text[$i + 2] -eq '"') {
            $quoteCount = 0
            $j = $i
            while ($j -lt $len -and $Text[$j] -eq '"') { $quoteCount++; $j++ }
            $i = $j
            $found = $false
            while ($i -lt $len) {
                if ($Text[$i] -eq '"') {
                    $endCount = 0
                    $k = $i
                    while ($k -lt $len -and $Text[$k] -eq '"') { $endCount++; $k++ }
                    if ($endCount -ge $quoteCount) {
                        $i = $k
                        $found = $true
                        break
                    }
                    $i = $k
                    continue
                }
                $i++
            }
            if (-not $found) {
                Write-Warning "C# raw string literal was not terminated before EOF."
                $i = $len
            }
            continue
        }

        if ($c -eq '@' -and ($i + 1) -lt $len -and $Text[$i + 1] -eq '"') {
            $i += 2
            while ($i -lt $len) {
                if ($Text[$i] -eq '"') {
                    if (($i + 1) -lt $len -and $Text[$i + 1] -eq '"') { $i += 2; continue }
                    $i++; break
                }
                $i++
            }
            continue
        }

        if ($c -eq '$' -and ($i + 1) -lt $len -and $Text[$i + 1] -eq '"') {
            $i += 2
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq '"') { $i++; break }
                $i++
            }
            continue
        }

        if ($c -eq '"') {
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq '"') { $i++; break }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            continue
        }

        if ($c -eq "'") {
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq "'") { $i++; break }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            continue
        }

        $i++
    }

    return ,$ranges
}

function Get-JavaScriptCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    # Tokens after which a `/` should be parsed as a regex literal opener
    # rather than a division operator. This is a JS lexer disambiguation rule:
    # regex is allowed at expression start (after operators, control keywords,
    # newlines) and disallowed after values (identifiers, numbers, `)`, `]`).
    $regexAllowKeywords = @(
        'return','typeof','in','instanceof','void','delete','new','throw','await','yield','of','do','else','case'
    )

    $len = $Text.Length
    $i = 0
    $regexAllowed = $true

    while ($i -lt $len) {
        $c = $Text[$i]

        if ($c -eq '/' -and ($i + 1) -lt $len) {
            $next = $Text[$i + 1]
            if ($next -eq '/') {
                $start = $i
                while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'LineComment'))
                $regexAllowed = $true
                continue
            }
            if ($next -eq '*') {
                $start = $i
                $i += 2
                $found = $false
                while ($i -lt $len - 1) {
                    if ($Text[$i] -eq '*' -and $Text[$i + 1] -eq '/') {
                        $i += 2
                        $found = $true
                        break
                    }
                    $i++
                }
                if (-not $found) {
                    Write-Warning "JavaScript/TypeScript block comment starting at offset $start was not terminated before EOF."
                    $i = $len
                }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
                continue
            }
            if ($regexAllowed) {
                # Regex literal: skip body, honoring `[...]` character classes (where `/` is literal)
                # and `\/` escapes. After the closing `/`, consume ASCII flag chars.
                $i++
                $inClass = $false
                while ($i -lt $len) {
                    $ch = $Text[$i]
                    if ($ch -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                    if ($ch -eq '[') { $inClass = $true; $i++; continue }
                    if ($ch -eq ']' -and $inClass) { $inClass = $false; $i++; continue }
                    if ($ch -eq '/' -and -not $inClass) { $i++; break }
                    if ($ch -eq "`n") { break }
                    $i++
                }
                while ($i -lt $len -and $Text[$i] -match '[a-zA-Z]') { $i++ }
                $regexAllowed = $false
                continue
            }
            # Plain division operator
            $i++
            $regexAllowed = $true
            continue
        }

        if ($c -eq '"' -or $c -eq "'") {
            $quote = $c
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq $quote) { $i++; break }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            $regexAllowed = $false
            continue
        }

        if ($c -eq '`') {
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq '`') { $i++; break }
                $i++
            }
            $regexAllowed = $false
            continue
        }

        if ($c -match '[A-Za-z_$]') {
            $start = $i
            while ($i -lt $len -and $Text[$i] -match '[A-Za-z0-9_$]') { $i++ }
            $word = $Text.Substring($start, $i - $start)
            if ($regexAllowKeywords -contains $word) {
                $regexAllowed = $true
            } else {
                $regexAllowed = $false
            }
            continue
        }

        if ($c -match '[0-9]') {
            while ($i -lt $len -and $Text[$i] -match '[0-9eE.xXbBoOaAfFnNlLuU]') { $i++ }
            $regexAllowed = $false
            continue
        }

        if ($c -eq ')' -or $c -eq ']') {
            $regexAllowed = $false
            $i++
            continue
        }

        # `}` closes a block/object — in expression context (e.g. `${x}/y/g`)
        # the next `/` is division on the value the brace produced, not a
        # regex opener. Treat it as a value terminator (conservative).
        if ($c -eq '}') {
            $regexAllowed = $false
            $i++
            continue
        }

        # `++` and `--` are postfix operators that yield a value, so the next
        # `/` is division, not a regex. Without this branch, the generic
        # punctuation fallback below would set $regexAllowed = $true and a
        # subsequent `/` could be misread as a regex literal opener — which
        # the lexer then walks until the NEXT `/`, swallowing real division
        # and any code between (e.g. `a++/b; /* live */`). Consume both
        # characters atomically to keep the state machine honest.
        if (($c -eq '+' -or $c -eq '-') -and ($i + 1) -lt $len -and $Text[$i + 1] -eq $c) {
            $regexAllowed = $false
            $i += 2
            continue
        }

        if ($c -eq "`n" -or $c -eq ' ' -or $c -eq "`t" -or $c -eq "`r") {
            $i++
            continue
        }

        # Any other punctuation/operator allows a regex to follow.
        $regexAllowed = $true
        $i++
    }

    return ,$ranges
}

function Get-PythonCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        $c = $Text[$i]

        $prefixLen = 0
        $isRaw = $false
        if ($c -match '[rRuUbBfF]') {
            $p = 0
            while (($i + $p) -lt $len -and $Text[$i + $p] -match '[rRuUbBfF]' -and $p -lt 2) { $p++ }
            if (($i + $p) -lt $len -and ($Text[$i + $p] -eq '"' -or $Text[$i + $p] -eq "'")) {
                $prefixLen = $p
                for ($q = 0; $q -lt $p; $q++) {
                    if ($Text[$i + $q] -eq 'r' -or $Text[$i + $q] -eq 'R') { $isRaw = $true; break }
                }
            }
        }

        $quoteIndex = $i + $prefixLen
        if ($quoteIndex -lt $len -and ($Text[$quoteIndex] -eq '"' -or $Text[$quoteIndex] -eq "'")) {
            $quote = $Text[$quoteIndex]
            $isTriple = ($quoteIndex + 2) -lt $len -and $Text[$quoteIndex + 1] -eq $quote -and $Text[$quoteIndex + 2] -eq $quote

            if ($isTriple) {
                $start = $i
                $i = $quoteIndex + 3
                $found = $false
                while ($i -lt $len - 2) {
                    if (-not $isRaw -and $Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                    if ($Text[$i] -eq $quote -and $Text[$i + 1] -eq $quote -and $Text[$i + 2] -eq $quote) {
                        $i += 3
                        $found = $true
                        break
                    }
                    $i++
                }
                if (-not $found) {
                    Write-Warning "Python triple-quoted string starting at offset $start was not terminated before EOF."
                    $i = $len
                }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
                continue
            }

            $i = $quoteIndex + 1
            while ($i -lt $len) {
                if (-not $isRaw -and $Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq $quote) { $i++; break }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            continue
        }

        if ($c -eq '#') {
            $start = $i
            while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
            $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'LineComment'))
            continue
        }

        $i++
    }

    return ,$ranges
}

function Get-ShellCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    # Heredocs (`<<EOF`, `<<-'TAG'`, `<<"TAG"`) treat their body as live data,
    # not comments. We split into lines, detect heredoc openers per line, and
    # skip comment scanning until the terminator (optionally indented if `<<-`).
    # Recognize all four bash heredoc opener forms:
    #   <<TAG     — body is parameter-expanded (we still treat as live data)
    #   <<'TAG'   — quoted, no expansion
    #   <<"TAG"   — quoted, no expansion
    #   <<\TAG    — backslash form, no expansion (equivalent to single-quoted)
    # Bash additionally allows an optional trailing `# comment` on the
    # redirection line itself, e.g. `cat <<EOF # note`. The terminator tail
    # `\s*(?:#[^\r\n]*)?\s*$` accepts that form so the heredoc body stays
    # unmasked. If we anchored with just `\s*$`, the opener regex would miss
    # such lines, the heredoc state would not be entered, and body content
    # like `# inside heredoc` would be reinterpreted as a shell comment.
    $heredocPattern = [regex]'<<\s*(?<dash>-?)\s*(?:''(?<tag>[A-Za-z_][A-Za-z0-9_]*)''|"(?<tag>[A-Za-z_][A-Za-z0-9_]*)"|\\(?<tag>[A-Za-z_][A-Za-z0-9_]*)|(?<tag>[A-Za-z_][A-Za-z0-9_]*))\s*(?:#[^\r\n]*)?\s*$'

    $lines = $Text -split "(?<=\n)"  # keep trailing newlines so offsets match
    $offset = 0
    $inHeredoc = $false
    $heredocTag = $null
    $heredocAllowIndent = $false

    foreach ($rawLine in $lines) {
        $lineStart = $offset
        $offset += $rawLine.Length
        $line = $rawLine.TrimEnd("`n").TrimEnd("`r")

        if ($inHeredoc) {
            # POSIX heredoc terminator rules:
            #   <<TAG  : terminator MUST appear on its own line at column 0
            #            with NO leading whitespace and nothing trailing.
            #   <<-TAG : terminator may have LEADING TAB characters stripped
            #            (spaces are NOT stripped — `    EOF` does NOT close
            #            a `<<-EOF` heredoc).
            # We previously also accepted `$line.Trim() -eq $heredocTag`,
            # which silently stripped spaces in BOTH forms — wrong for both.
            # The fix: branch strictly on $heredocAllowIndent.
            $isTerminator = if ($heredocAllowIndent) {
                $line.TrimStart("`t") -eq $heredocTag
            } else {
                $line -eq $heredocTag
            }
            if ($isTerminator) {
                $inHeredoc = $false
                $heredocTag = $null
            }
            continue
        }

        $m = $heredocPattern.Match($line)
        if ($m.Success) {
            $heredocTag = $m.Groups['tag'].Value
            $heredocAllowIndent = ($m.Groups['dash'].Value -eq '-')
            $inHeredoc = $true
            # Mask an optional trailing `# comment` on the opener line. Bash
            # treats `cat <<EOF # note` as a redirection followed by a real
            # shell comment — the comment text is NOT part of the heredoc
            # body. Locate the `#` within the tail (`\s*(?:#[^\r\n]*)?\s*$`)
            # by searching from the end of the tag capture; anything from
            # that `#` to EOL is recorded as a LineComment range.
            $tagEnd = $m.Groups['tag'].Index + $m.Groups['tag'].Length
            # Skip past a closing `'` or `"` if the tag was quoted. The
            # regex only captures the inner identifier, so the closing
            # quote (if present) sits at $tagEnd.
            if ($tagEnd -lt $line.Length -and ($line[$tagEnd] -eq "'" -or $line[$tagEnd] -eq '"')) {
                $tagEnd++
            }
            $hashIndex = $line.IndexOf('#', $tagEnd)
            if ($hashIndex -ge 0) {
                $absStart = $lineStart + $hashIndex
                $absEnd = $lineStart + $line.Length
                $ranges.Add((New-CommentRange -Start $absStart -End $absEnd -Kind 'LineComment'))
            }
            continue
        }

        # Walk this line to detect comments using the original logic.
        $atStart = $true
        $i = 0
        $lineLen = $line.Length
        while ($i -lt $lineLen) {
            $c = $line[$i]

            if ($c -eq '"') {
                $atStart = $false
                $i++
                while ($i -lt $lineLen) {
                    if ($line[$i] -eq '\' -and ($i + 1) -lt $lineLen) { $i += 2; continue }
                    if ($line[$i] -eq '"') { $i++; break }
                    $i++
                }
                continue
            }

            if ($c -eq "'") {
                $atStart = $false
                $i++
                while ($i -lt $lineLen) {
                    if ($line[$i] -eq "'") { $i++; break }
                    $i++
                }
                continue
            }

            if ($c -eq '#') {
                $prevIsBoundary = $atStart
                if (-not $prevIsBoundary -and $i -gt 0) {
                    $prev = $line[$i - 1]
                    if ($prev -eq ' ' -or $prev -eq "`t") { $prevIsBoundary = $true }
                }
                if ($prevIsBoundary) {
                    $absStart = $lineStart + $i
                    $absEnd = $lineStart + $lineLen
                    $ranges.Add((New-CommentRange -Start $absStart -End $absEnd -Kind 'LineComment'))
                    break
                }
            }

            if ($c -ne ' ' -and $c -ne "`t") { $atStart = $false }
            $i++
        }
    }

    return ,$ranges
}

function Get-YamlCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    $atStart = $true
    while ($i -lt $len) {
        $c = $Text[$i]

        if ($c -eq "`n") { $atStart = $true; $i++; continue }

        if ($c -eq '"') {
            $atStart = $false
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq '\' -and ($i + 1) -lt $len) { $i += 2; continue }
                if ($Text[$i] -eq '"') { $i++; break }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            continue
        }

        if ($c -eq "'") {
            $atStart = $false
            $i++
            while ($i -lt $len) {
                if ($Text[$i] -eq "'") {
                    if (($i + 1) -lt $len -and $Text[$i + 1] -eq "'") { $i += 2; continue }
                    $i++; break
                }
                if ($Text[$i] -eq "`n") { break }
                $i++
            }
            continue
        }

        if ($c -eq '#') {
            $prevIsBoundary = $atStart
            if (-not $prevIsBoundary -and $i -gt 0) {
                $prev = $Text[$i - 1]
                if ($prev -eq ' ' -or $prev -eq "`t") { $prevIsBoundary = $true }
            }
            if ($prevIsBoundary) {
                $start = $i
                while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
                $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'LineComment'))
                continue
            }
        }

        if ($c -ne ' ' -and $c -ne "`t") { $atStart = $false }
        $i++
    }

    return ,$ranges
}

function Get-CmdCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        $lineStart = $i
        while ($i -lt $len -and $Text[$i] -ne "`n") { $i++ }
        $lineEnd = $i

        $scan = $lineStart
        while ($scan -lt $lineEnd -and ($Text[$scan] -eq ' ' -or $Text[$scan] -eq "`t")) { $scan++ }

        $isComment = $false
        if ($scan -lt $lineEnd) {
            if (($scan + 1) -lt $lineEnd -and $Text[$scan] -eq ':' -and $Text[$scan + 1] -eq ':') {
                $isComment = $true
            } elseif (($scan + 2) -lt $lineEnd -and
                      ($Text[$scan] -eq 'r' -or $Text[$scan] -eq 'R') -and
                      ($Text[$scan + 1] -eq 'e' -or $Text[$scan + 1] -eq 'E') -and
                      ($Text[$scan + 2] -eq 'm' -or $Text[$scan + 2] -eq 'M')) {
                $after = $scan + 3
                if ($after -ge $lineEnd -or $Text[$after] -eq ' ' -or $Text[$after] -eq "`t") {
                    $isComment = $true
                }
            }
        }

        if ($isComment) {
            $ranges.Add((New-CommentRange -Start $scan -End $lineEnd -Kind 'LineComment'))
        }

        if ($i -lt $len -and $Text[$i] -eq "`n") { $i++ }
    }

    return ,$ranges
}

function Get-MarkdownCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        if ($i + 3 -lt $len -and $Text[$i] -eq '<' -and $Text[$i + 1] -eq '!' -and $Text[$i + 2] -eq '-' -and $Text[$i + 3] -eq '-') {
            $start = $i
            $i += 4
            $found = $false
            while ($i -lt $len - 2) {
                if ($Text[$i] -eq '-' -and $Text[$i + 1] -eq '-' -and $Text[$i + 2] -eq '>') {
                    $i += 3
                    $found = $true
                    break
                }
                $i++
            }
            if (-not $found) {
                Write-Warning "Markdown HTML comment starting at offset $start was not terminated before EOF."
                $i = $len
            }
            $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
            continue
        }
        $i++
    }

    return ,$ranges
}

function Get-XmlCommentRanges {
    param([string]$Text)

    $ranges = New-Object System.Collections.Generic.List[object]
    if ([string]::IsNullOrEmpty($Text)) { return ,$ranges }

    $len = $Text.Length
    $i = 0
    while ($i -lt $len) {
        if ($i + 3 -lt $len -and $Text[$i] -eq '<' -and $Text[$i + 1] -eq '!' -and $Text[$i + 2] -eq '-' -and $Text[$i + 3] -eq '-') {
            $start = $i
            $i += 4
            $found = $false
            while ($i -lt $len - 2) {
                if ($Text[$i] -eq '-' -and $Text[$i + 1] -eq '-' -and $Text[$i + 2] -eq '>') {
                    $i += 3
                    $found = $true
                    break
                }
                $i++
            }
            if (-not $found) {
                Write-Warning "XML comment starting at offset $start was not terminated before EOF."
                $i = $len
            }
            $ranges.Add((New-CommentRange -Start $start -End $i -Kind 'BlockComment'))
            continue
        }

        if ($Text[$i] -eq '"' -or $Text[$i] -eq "'") {
            $quote = $Text[$i]
            $i++
            while ($i -lt $len -and $Text[$i] -ne $quote) { $i++ }
            if ($i -lt $len) { $i++ }
            continue
        }

        $i++
    }

    return ,$ranges
}

function Get-JsonCommentRanges {
    param([string]$Text)
    $ranges = New-Object System.Collections.Generic.List[object]
    return ,$ranges
}

function Get-CommentRanges {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Text,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Language
    )

    if ([string]::IsNullOrEmpty($Text) -or [string]::IsNullOrEmpty($Language)) {
        $empty = New-Object System.Collections.Generic.List[object]
        return ,$empty
    }

    switch ($Language) {
        'powershell' { return Get-PowerShellCommentRanges -Text $Text }
        'csharp'     { return Get-CSharpCommentRanges     -Text $Text }
        'javascript' { return Get-JavaScriptCommentRanges -Text $Text }
        'typescript' { return Get-JavaScriptCommentRanges -Text $Text }
        'python'     { return Get-PythonCommentRanges     -Text $Text }
        'shell'      { return Get-ShellCommentRanges      -Text $Text }
        'yaml'       { return Get-YamlCommentRanges       -Text $Text }
        'cmd'        { return Get-CmdCommentRanges        -Text $Text }
        'json'       { return Get-JsonCommentRanges       -Text $Text }
        'markdown'   { return Get-MarkdownCommentRanges   -Text $Text }
        'xml'        { return Get-XmlCommentRanges        -Text $Text }
        default      {
            $empty = New-Object System.Collections.Generic.List[object]
            return ,$empty
        }
    }
}

function Get-CommentMaskedLines {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [AllowEmptyCollection()]
        [string[]]$Lines,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [AllowEmptyString()]
        [string]$Language
    )

    if ($null -eq $Lines -or $Lines.Count -eq 0) {
        return @()
    }

    if ([string]::IsNullOrEmpty($Language)) {
        return ,$Lines
    }

    $supported = @('powershell', 'csharp', 'javascript', 'typescript', 'python', 'shell', 'yaml', 'cmd', 'json', 'markdown', 'xml')
    if ($supported -notcontains $Language) {
        return ,$Lines
    }

    $joined = [string]::Join("`n", $Lines)
    $ranges = Get-CommentRanges -Text $joined -Language $Language

    $chars = $joined.ToCharArray()
    foreach ($range in $ranges) {
        for ($k = $range.Start; $k -lt $range.End -and $k -lt $chars.Length; $k++) {
            # Preserve newlines to keep line count stable; mask every other char with a single space
            # so column offsets of surviving (non-comment) text are unchanged.
            if ($chars[$k] -ne "`n" -and $chars[$k] -ne "`r") {
                $chars[$k] = ' '
            }
        }
    }

    $maskedString = -join $chars
    $maskedLines = $maskedString -split "`n"

    if ($maskedLines.Count -ne $Lines.Count) {
        if ($maskedLines.Count -eq ($Lines.Count + 1) -and $maskedLines[-1] -eq '') {
            $maskedLines = $maskedLines[0..($maskedLines.Count - 2)]
        }
    }

    return ,$maskedLines
}

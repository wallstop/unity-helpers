#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Test runner for comment-stripping.ps1.

.DESCRIPTION
    Verifies that Get-CommentMaskedLines correctly masks comments across
    the supported languages while preserving line count and column
    offsets, and that Get-LanguageFromExtension maps extensions correctly.

.PARAMETER VerboseOutput
    Show verbose per-test diagnostics.

.EXAMPLE
    pwsh -NoProfile -File scripts/tests/test-comment-stripping.ps1
    pwsh -NoProfile -File scripts/tests/test-comment-stripping.ps1 -VerboseOutput
#>
param(
    [switch]$VerboseOutput
)

Set-StrictMode -Version Latest

# Dot-source the helper FIRST so it loads under its own production-time error
# semantics. We then opt the test harness itself into `Stop`-on-error so any
# unexpected throw from a fixture aborts the suite immediately rather than
# being silently swallowed.
. (Join-Path $PSScriptRoot '..' 'comment-stripping.ps1')

$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = ''
    )
    if ($Passed) {
        Write-Host "  [PASS] $TestName" -ForegroundColor Green
        $script:TestsPassed++
    } else {
        Write-Host "  [FAIL] $TestName" -ForegroundColor Red
        if ($Message) {
            Write-Host "         $Message" -ForegroundColor Yellow
        }
        $script:TestsFailed++
        $script:FailedTests += $TestName
    }
}

function Assert-Masking {
    param(
        [string]$Name,
        [string]$Language,
        [string[]]$InputLines,
        [string[]]$ExpectedLines
    )

    try {
        $masked = Get-CommentMaskedLines -Lines $InputLines -Language $Language
    } catch {
        Write-TestResult $Name $false "Threw exception: $($_.Exception.Message)"
        return
    }

    if ($null -eq $masked) {
        Write-TestResult $Name $false 'Returned $null'
        return
    }

    $maskedArr = @($masked)
    if ($maskedArr.Count -ne $InputLines.Count) {
        Write-TestResult $Name $false ("Line count mismatch: masked={0}, input={1}" -f $maskedArr.Count, $InputLines.Count)
        return
    }

    for ($i = 0; $i -lt $InputLines.Count; $i++) {
        if ($maskedArr[$i].Length -ne $InputLines[$i].Length) {
            Write-TestResult $Name $false ("Column count mismatch at line {0}: masked={1} (len {2}), input=`{3}` (len {4})" -f $i, $maskedArr[$i], $maskedArr[$i].Length, $InputLines[$i], $InputLines[$i].Length)
            return
        }
    }

    if ($maskedArr.Count -ne $ExpectedLines.Count) {
        Write-TestResult $Name $false ("Expected {0} lines, got {1}" -f $ExpectedLines.Count, $maskedArr.Count)
        return
    }

    for ($i = 0; $i -lt $ExpectedLines.Count; $i++) {
        if ($maskedArr[$i] -ne $ExpectedLines[$i]) {
            Write-TestResult $Name $false ("Line {0} mismatch.`n    expected: |{1}|`n    actual:   |{2}|" -f $i, $ExpectedLines[$i], $maskedArr[$i])
            return
        }
    }

    Write-TestResult $Name $true
}

Write-Host "Testing comment-stripping.ps1..." -ForegroundColor White

Write-Host "`n  Section: Get-LanguageFromExtension" -ForegroundColor White

$extCases = @(
    @{ Ext = 'foo.ps1';      Expected = 'powershell' }
    @{ Ext = 'a.psm1';       Expected = 'powershell' }
    @{ Ext = 'a.psd1';       Expected = 'powershell' }
    @{ Ext = 'a.cs';         Expected = 'csharp' }
    @{ Ext = 'a.csx';        Expected = 'csharp' }
    @{ Ext = 'a.js';         Expected = 'javascript' }
    @{ Ext = 'a.jsx';        Expected = 'javascript' }
    @{ Ext = 'a.mjs';        Expected = 'javascript' }
    @{ Ext = 'a.cjs';        Expected = 'javascript' }
    @{ Ext = 'a.ts';         Expected = 'typescript' }
    @{ Ext = 'a.tsx';        Expected = 'typescript' }
    @{ Ext = 'a.py';         Expected = 'python' }
    @{ Ext = 'a.sh';         Expected = 'shell' }
    @{ Ext = 'a.bash';       Expected = 'shell' }
    @{ Ext = 'a.yml';        Expected = 'yaml' }
    @{ Ext = 'a.yaml';       Expected = 'yaml' }
    @{ Ext = 'a.cmd';        Expected = 'cmd' }
    @{ Ext = 'a.bat';        Expected = 'cmd' }
    @{ Ext = 'a.json';       Expected = 'json' }
    @{ Ext = 'a.md';         Expected = 'markdown' }
    @{ Ext = 'a.markdown';   Expected = 'markdown' }
    @{ Ext = 'a.csproj';     Expected = 'xml' }
    @{ Ext = 'a.props';      Expected = 'xml' }
    @{ Ext = 'a.targets';    Expected = 'xml' }
    @{ Ext = 'a.xyz';        Expected = $null }
    @{ Ext = 'NoExtension';  Expected = $null }
)

foreach ($case in $extCases) {
    $actual = Get-LanguageFromExtension -Path $case.Ext
    $pass = ($null -eq $case.Expected -and $null -eq $actual) -or ($actual -eq $case.Expected)
    Write-TestResult ("ExtensionMap_{0}" -f $case.Ext) $pass ("expected={0}, actual={1}" -f $case.Expected, $actual)
}

Write-Host "`n  Section: PowerShell" -ForegroundColor White

Assert-Masking -Name 'Ps_BlockDocstringMasked' -Language 'powershell' `
    -InputLines @('<# .EXAMPLE docs/foo.md #>') `
    -ExpectedLines @('                          ')

Assert-Masking -Name 'Ps_MultilineBlockDocstring' -Language 'powershell' `
    -InputLines @('<#', '.DESC', ' docs/x.md', '#>') `
    -ExpectedLines @('  ', '     ', '          ', '  ')

Assert-Masking -Name 'Ps_DoubleQuotedStringPreserved' -Language 'powershell' `
    -InputLines @('$x = "docs/foo.md"') `
    -ExpectedLines @('$x = "docs/foo.md"')

Assert-Masking -Name 'Ps_SingleQuotedStringPreserved' -Language 'powershell' `
    -InputLines @("`$y = 'docs/foo.md'") `
    -ExpectedLines @("`$y = 'docs/foo.md'")

Assert-Masking -Name 'Ps_TrailingLineCommentMasked' -Language 'powershell' `
    -InputLines @("`$z = 'a' # docs/foo.md") `
    -ExpectedLines @("`$z = 'a'              ")

Assert-Masking -Name 'Ps_HereStringDoublePreserved' -Language 'powershell' `
    -InputLines @('$h = @"', ' docs/foo.md ', '"@') `
    -ExpectedLines @('$h = @"', ' docs/foo.md ', '"@')

Assert-Masking -Name 'Ps_HereStringSinglePreserved' -Language 'powershell' `
    -InputLines @("`$h = @'", ' docs/foo.md ', "'@") `
    -ExpectedLines @("`$h = @'", ' docs/foo.md ', "'@")

# Round-3 MAJOR 5 regression: PowerShell here-string terminator MUST be at
# column 0 with nothing else after it. An indented `"@` does NOT close the
# here-string in real PowerShell, so the body — including a trailing line
# comment — must remain unmasked (still inside the here-string).
Assert-Masking -Name 'Ps_HereStringIndentedCloseDoesNotTerminate' -Language 'powershell' `
    -InputLines @('$h = @"', 'body', '    "@', '# docs/foo.md') `
    -ExpectedLines @('$h = @"', 'body', '    "@', '# docs/foo.md')

# Round-3 MAJOR 5 regression: trailing junk on the closer line means the
# closer doesn't terminate. The here-string remains open through following
# lines, so a later `# comment` line is body content, not a comment.
Assert-Masking -Name 'Ps_HereStringTrailingJunkDoesNotTerminate' -Language 'powershell' `
    -InputLines @('$h = @"', 'body', '"@ junk', '# docs/foo.md') `
    -ExpectedLines @('$h = @"', 'body', '"@ junk', '# docs/foo.md')

Assert-Masking -Name 'Ps_HashInsideDoubleQuoteIsLiteral' -Language 'powershell' `
    -InputLines @('$x = "text # not a comment"') `
    -ExpectedLines @('$x = "text # not a comment"')

Write-Host "`n  Section: C#" -ForegroundColor White

Assert-Masking -Name 'Cs_XmlDocMasked' -Language 'csharp' `
    -InputLines @('/// <summary>docs/foo.md</summary>') `
    -ExpectedLines @('                                  ')

Assert-Masking -Name 'Cs_BlockCommentMasked' -Language 'csharp' `
    -InputLines @('/* docs/foo.md */') `
    -ExpectedLines @('                 ')

Assert-Masking -Name 'Cs_InlineBlockThenString' -Language 'csharp' `
    -InputLines @('/* intro */ var p = "docs/x.md";') `
    -ExpectedLines @('            var p = "docs/x.md";')

Assert-Masking -Name 'Cs_VerbatimStringWithEscapedQuote' -Language 'csharp' `
    -InputLines @('var p = @"docs/""foo"".md";') `
    -ExpectedLines @('var p = @"docs/""foo"".md";')

Assert-Masking -Name 'Cs_RawStringPreserved' -Language 'csharp' `
    -InputLines @('var p = """docs/foo.md""";') `
    -ExpectedLines @('var p = """docs/foo.md""";')

Assert-Masking -Name 'Cs_EscapeSequenceEndOfString' -Language 'csharp' `
    -InputLines @('var p = "\\"; var q = 1;') `
    -ExpectedLines @('var p = "\\"; var q = 1;')

Assert-Masking -Name 'Cs_LineCommentMasked' -Language 'csharp' `
    -InputLines @('int x = 1; // docs/foo.md') `
    -ExpectedLines @('int x = 1;               ')

Write-Host "`n  Section: JavaScript / TypeScript" -ForegroundColor White

Assert-Masking -Name 'Js_LineCommentMasked' -Language 'javascript' `
    -InputLines @('// docs/foo.md') `
    -ExpectedLines @('              ')

Assert-Masking -Name 'Js_BlockThenTemplateLiteral' -Language 'javascript' `
    -InputLines @('/* docs/a.md */ const p = `docs/${x}.md`;') `
    -ExpectedLines @('                const p = `docs/${x}.md`;')

Assert-Masking -Name 'Ts_LineCommentMasked' -Language 'typescript' `
    -InputLines @('const a = 1; // docs/foo.md') `
    -ExpectedLines @('const a = 1;               ')

Assert-Masking -Name 'Js_RegexLiteralWithSlashesInClassPreservesNonComment' -Language 'javascript' `
    -InputLines @('const r = /[a//b]/g; const x = "docs/live.md";') `
    -ExpectedLines @('const r = /[a//b]/g; const x = "docs/live.md";')

Assert-Masking -Name 'Js_RegexCharClassWithStarSlash' -Language 'javascript' `
    -InputLines @('const r = /[/*]/.test(s);') `
    -ExpectedLines @('const r = /[/*]/.test(s);')

Assert-Masking -Name 'Js_DivisionNotMaskedAsComment' -Language 'javascript' `
    -InputLines @('var z = a / b / c;') `
    -ExpectedLines @('var z = a / b / c;')

Assert-Masking -Name 'Js_RegexAfterReturnRecognized' -Language 'javascript' `
    -InputLines @('return /foo/.test(x);') `
    -ExpectedLines @('return /foo/.test(x);')

Assert-Masking -Name 'Js_LineCommentAfterDivisionStillMasked' -Language 'javascript' `
    -InputLines @('var z = a / b; // docs/foo.md') `
    -ExpectedLines @('var z = a / b;               ')

# Round-4 regex disambiguation: after the postfix `++` operator, the next `/`
# is division — NOT a regex opener. Without the explicit `++`/`--` branch in
# Get-JavaScriptCommentRanges, the generic punctuation fallback sets
# $regexAllowed = $true. The lexer then opens a regex at `/b`, walks to the
# next `/` (which is the FIRST `/` of `// docs/mask.md`), so `// docs/mask.md`
# is no longer recognized as a line-comment opener and stays UNMASKED.
# Load-bearing fixture: a SINGLE `/` after `++` (so the lexer's regex scan
# crosses into the trailing comment).
Assert-Masking -Name 'Js_PostfixIncrementFollowedByDivisionIsNotRegex' -Language 'javascript' `
    -InputLines @('var z = a++/b; // docs/mask.md') `
    -ExpectedLines @('var z = a++/b;                ')

# Mirror case for postfix `--`.
Assert-Masking -Name 'Js_PostfixDecrementFollowedByDivisionIsNotRegex' -Language 'javascript' `
    -InputLines @('var z = a--/b; // docs/mask.md') `
    -ExpectedLines @('var z = a--/b;                ')

# `}` value-context terminator: a `}` directly followed by `/` (e.g. an
# object-literal in expression position used as the dividend of a division)
# should treat the `/` as division. Without the `}` branch, the generic
# punctuation fallback sets $regexAllowed = $true and the lexer opens a
# regex at `/b`, walking to the first `/` of `// docs/mask.md` and
# swallowing the line-comment opener.
Assert-Masking -Name 'Js_CloseBraceFollowedByDivisionIsNotRegex' -Language 'javascript' `
    -InputLines @('var z = {a:1}/b; // docs/mask.md') `
    -ExpectedLines @('var z = {a:1}/b;                ')

Write-Host "`n  Section: Python" -ForegroundColor White

Assert-Masking -Name 'Py_TripleQuoteDocstringMasked' -Language 'python' `
    -InputLines @('""" docs/foo.md """') `
    -ExpectedLines @('                   ')

Assert-Masking -Name 'Py_MultilineTripleMasked' -Language 'python' `
    -InputLines @('"""', 'docs/foo.md', '"""') `
    -ExpectedLines @('   ', '           ', '   ')

Assert-Masking -Name 'Py_DoubleQuotedStringPreserved' -Language 'python' `
    -InputLines @('x = "docs/foo.md"') `
    -ExpectedLines @('x = "docs/foo.md"')

Assert-Masking -Name 'Py_HashInsideStringPreserved' -Language 'python' `
    -InputLines @('x = "# not a comment"') `
    -ExpectedLines @('x = "# not a comment"')

Assert-Masking -Name 'Py_HashCommentMasked' -Language 'python' `
    -InputLines @('x = "a"  # docs/foo.md') `
    -ExpectedLines @('x = "a"               ')

Assert-Masking -Name 'Py_RawStringPrefix' -Language 'python' `
    -InputLines @('x = r"docs/foo.md"') `
    -ExpectedLines @('x = r"docs/foo.md"')

Assert-Masking -Name 'Py_RawStringWithBackslashN' -Language 'python' `
    -InputLines @('x = r"\n"') `
    -ExpectedLines @('x = r"\n"')

Assert-Masking -Name 'Py_RawStringDocsPreserved' -Language 'python' `
    -InputLines @('x = r"docs/live.md"') `
    -ExpectedLines @('x = r"docs/live.md"')

Assert-Masking -Name 'Py_RawBytesStringDocsPreserved' -Language 'python' `
    -InputLines @('x = rb"docs/live.md"') `
    -ExpectedLines @('x = rb"docs/live.md"')

Write-Host "`n  Section: Shell" -ForegroundColor White

Assert-Masking -Name 'Sh_LineCommentMasked' -Language 'shell' `
    -InputLines @('# docs/foo.md') `
    -ExpectedLines @('             ')

Assert-Masking -Name 'Sh_HashInsideDoubleQuoteNotComment' -Language 'shell' `
    -InputLines @('echo "# not a comment"') `
    -ExpectedLines @('echo "# not a comment"')

Assert-Masking -Name 'Sh_HashInsideSingleQuoteNotComment' -Language 'shell' `
    -InputLines @("echo '# not a comment'") `
    -ExpectedLines @("echo '# not a comment'")

Assert-Masking -Name 'Sh_TrailingHashMasked' -Language 'shell' `
    -InputLines @('echo ok # docs/foo.md') `
    -ExpectedLines @('echo ok              ')

Assert-Masking -Name 'Sh_HeredocBodyNotMasked' -Language 'shell' `
    -InputLines @('cat <<EOF', '# live docs/foo.md', 'EOF') `
    -ExpectedLines @('cat <<EOF', '# live docs/foo.md', 'EOF')

Assert-Masking -Name 'Sh_HeredocQuotedTagBodyNotMasked' -Language 'shell' `
    -InputLines @("cat <<'EOF'", '# live docs/foo.md', 'EOF') `
    -ExpectedLines @("cat <<'EOF'", '# live docs/foo.md', 'EOF')

Assert-Masking -Name 'Sh_HeredocIndentedTerminator' -Language 'shell' `
    -InputLines @('cat <<-EOF', "`t# live docs/foo.md", "`tEOF") `
    -ExpectedLines @('cat <<-EOF', "`t# live docs/foo.md", "`tEOF")

Assert-Masking -Name 'Sh_CommentAfterHeredocStillMasked' -Language 'shell' `
    -InputLines @('cat <<EOF', 'body', 'EOF', '# docs/foo.md') `
    -ExpectedLines @('cat <<EOF', 'body', 'EOF', '             ')

# Round-3 BLOCKER 3 regression: bash recognizes `<<\TAG` as a quoted-style
# heredoc (no expansion), but the round-2 opener regex omitted that form.
# Body must NOT be masked.
Assert-Masking -Name 'Sh_HeredocBackslashTagBodyNotMasked' -Language 'shell' `
    -InputLines @('cat <<\EOF', '# heredoc body', 'EOF') `
    -ExpectedLines @('cat <<\EOF', '# heredoc body', 'EOF')

# Round-3 MAJOR 6 regression: <<- only strips TABS, not spaces. Load-bearing
# fixture: a body line containing `# inside-heredoc` sits BETWEEN a
# space-indented `EOF` (which must NOT close the heredoc — POSIX <<- only
# strips tabs) and the unindented terminator. Under the bug (`TrimStart()` with
# no args), the space-indented EOF would close the heredoc and the next line
# (`# inside-heredoc`) would be reinterpreted as a Bash comment and masked.
# Under the fix (`TrimStart("`t")`), the heredoc stays open through the
# space-indented line and the `# inside-heredoc` line survives unmasked
# because it is heredoc body content, not shell source.
Assert-Masking -Name 'Sh_HeredocDashIgnoresSpaceIndentedTerminator' -Language 'shell' `
    -InputLines @('cat <<-EOF', '    EOF', '# inside-heredoc', 'EOF', '# real comment') `
    -ExpectedLines @('cat <<-EOF', '    EOF', '# inside-heredoc', 'EOF', '              ')

# Round-6 MINOR regression: bash accepts an optional trailing `# comment` on
# a heredoc opener line (e.g. `cat <<EOF # trailing`). The opener regex must
# recognize the line so the heredoc body is entered; the trailing comment on
# the opener itself is a real shell comment and must be masked. Under the
# bug (regex anchored with only `\s*$`), the opener would NOT match, the
# heredoc state would never be entered, and the body line `# inside heredoc`
# would be reinterpreted as a shell comment and masked — violating bash
# semantics.
Assert-Masking -Name 'Sh_HeredocTrailingCommentOnOpenerStillRecognized' -Language 'shell' `
    -InputLines @('cat <<EOF # trailing comment', '# inside heredoc', 'EOF', '# after') `
    -ExpectedLines @('cat <<EOF                   ', '# inside heredoc', 'EOF', '       ')

Write-Host "`n  Section: YAML" -ForegroundColor White

Assert-Masking -Name 'Yaml_TrailingHashMasked' -Language 'yaml' `
    -InputLines @('key: value # docs/foo.md') `
    -ExpectedLines @('key: value              ')

Assert-Masking -Name 'Yaml_FullLineComment' -Language 'yaml' `
    -InputLines @('# docs/foo.md') `
    -ExpectedLines @('             ')

Assert-Masking -Name 'Yaml_HashInsideQuoteNotComment' -Language 'yaml' `
    -InputLines @('key: "see #1"') `
    -ExpectedLines @('key: "see #1"')

Write-Host "`n  Section: CMD" -ForegroundColor White

Assert-Masking -Name 'Cmd_RemLineMasked' -Language 'cmd' `
    -InputLines @('REM docs/foo.md') `
    -ExpectedLines @('               ')

Assert-Masking -Name 'Cmd_DoubleColonLineMasked' -Language 'cmd' `
    -InputLines @(':: docs/foo.md') `
    -ExpectedLines @('              ')

Assert-Masking -Name 'Cmd_RemLowerCase' -Language 'cmd' `
    -InputLines @('rem docs/foo.md') `
    -ExpectedLines @('               ')

Assert-Masking -Name 'Cmd_NonCommentLinePreserved' -Language 'cmd' `
    -InputLines @('echo docs/foo.md') `
    -ExpectedLines @('echo docs/foo.md')

Write-Host "`n  Section: JSON (no masking)" -ForegroundColor White

Assert-Masking -Name 'Json_NoMasking_LineLooksLikeComment' -Language 'json' `
    -InputLines @('{"a": "# not a comment", "b": "// also not"}') `
    -ExpectedLines @('{"a": "# not a comment", "b": "// also not"}')

Write-Host "`n  Section: Markdown" -ForegroundColor White

Assert-Masking -Name 'Md_HtmlCommentMasked' -Language 'markdown' `
    -InputLines @('<!-- docs/foo.md -->') `
    -ExpectedLines @('                    ')

Assert-Masking -Name 'Md_MultilineHtmlCommentMasked' -Language 'markdown' `
    -InputLines @('<!--', 'docs/foo.md', '-->') `
    -ExpectedLines @('    ', '           ', '   ')

Write-Host "`n  Section: Unknown / null language" -ForegroundColor White

$unknownResult = Get-CommentMaskedLines -Lines @('# docs/foo.md') -Language 'unknown'
Write-TestResult 'Unknown_LanguageReturnsInputUnchanged' ((@($unknownResult).Count -eq 1) -and ($unknownResult[0] -eq '# docs/foo.md'))

$nullResult = Get-CommentMaskedLines -Lines @('abc') -Language $null
Write-TestResult 'Null_LanguageReturnsInputUnchanged' ((@($nullResult).Count -eq 1) -and ($nullResult[0] -eq 'abc'))

$emptyIn = Get-CommentMaskedLines -Lines @() -Language 'powershell'
Write-TestResult 'Empty_InputReturnsEmpty' ((@($emptyIn)).Count -eq 0)

Write-Host "`n  Section: Edge cases (CRLF, full-comment files)" -ForegroundColor White

# CRLF: each line in $InputLines does not contain `n itself, but masking joins
# with `n internally — verify the CR characters at column ends are preserved.
Assert-Masking -Name 'Crlf_PowerShellCommentMasked' -Language 'powershell' `
    -InputLines @("# docs/foo.md`r", "Write-Host ok`r") `
    -ExpectedLines @("             `r", "Write-Host ok`r")

# Round-3 MINOR 12: a UTF-8 BOM (U+FEFF) at the start of a file must not
# confuse the masker. The BOM is not a comment opener, and the block
# comment that follows must still be masked correctly.
$bom = [string][char]0xFEFF
Assert-Masking -Name 'Bom_PreservedAndBlockCommentMasked' -Language 'powershell' `
    -InputLines @("$bom<# block #>") `
    -ExpectedLines @("$bom           ")

Assert-Masking -Name 'AllComments_FilePreservesShape' -Language 'powershell' `
    -InputLines @('# c1', '# c2', '# c3') `
    -ExpectedLines @('    ', '    ', '    ')

Assert-Masking -Name 'AllComments_PythonTriple' -Language 'python' `
    -InputLines @('"""', 'docs/foo.md', '"""') `
    -ExpectedLines @('   ', '           ', '   ')

# Unterminated PowerShell here-string at EOF: no '@' boundary — must warn.
$heredocWarnings = $null
$null = Get-CommentMaskedLines -Lines @('$h = @"', 'open without close') -Language 'powershell' -WarningVariable heredocWarnings -WarningAction SilentlyContinue
$heredocWarnList = @($heredocWarnings)
$hasHeredocWarn = ($heredocWarnList | Where-Object { $_ -match 'here-string' } | Measure-Object).Count -gt 0
Write-TestResult 'Ps_UnterminatedHereStringWarns' $hasHeredocWarn ("warnings: {0}" -f (($heredocWarnList | ForEach-Object { $_.ToString() }) -join '|'))

Write-Host "`n  Section: Unterminated comment warning" -ForegroundColor White

$warnings = $null
$null = Get-CommentMaskedLines -Lines @('<# unterminated docs/foo.md') -Language 'powershell' -WarningVariable warnings -WarningAction SilentlyContinue
$warningList = @($warnings)
$hasWarn = ($warningList | Where-Object { $_ -match 'not terminated' } | Measure-Object).Count -gt 0
Write-TestResult 'Ps_UnterminatedBlockWarns' $hasWarn ("warnings: {0}" -f (($warningList | ForEach-Object { $_.ToString() }) -join '|'))

Write-Host ""
Write-Host ("Tests passed: {0}" -f $script:TestsPassed) -ForegroundColor Green
Write-Host ("Tests failed: {0}" -f $script:TestsFailed) -ForegroundColor $(if ($script:TestsFailed -gt 0) { 'Red' } else { 'Green' })
if ($script:FailedTests.Count -gt 0) {
    Write-Host 'Failed tests:' -ForegroundColor Red
    foreach ($t in $script:FailedTests) {
        Write-Host "  - $t" -ForegroundColor Red
    }
}

exit $script:TestsFailed

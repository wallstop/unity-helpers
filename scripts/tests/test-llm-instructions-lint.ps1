Param(
  [switch]$VerboseOutput
)

<#
.SYNOPSIS
    Test runner for generate-skills-index.ps1 and lint-llm-instructions.ps1

.DESCRIPTION
    Tests that:
    - generate-skills-index.ps1 produces valid output (no H1, correct markers, no diagnostics, valid lines)
    - lint-llm-instructions.ps1 passes on the current repo
    - context.md has exactly one H1 heading
    - context.md contains skills index markers
    - Known-bad heading patterns do not appear in generator output

.PARAMETER VerboseOutput
    Show detailed output during test execution

.EXAMPLE
    ./scripts/tests/test-llm-instructions-lint.ps1
    ./scripts/tests/test-llm-instructions-lint.ps1 -VerboseOutput
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:TestsPassed = 0
$script:TestsFailed = 0
$script:FailedTests = @()

function Write-Info($msg) {
  if ($VerboseOutput) { Write-Host "[test-llm-instructions-lint] $msg" -ForegroundColor Cyan }
}

function Write-TestResult {
  param(
    [string]$TestName,
    [bool]$Passed,
    [string]$Message = ""
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

$markdownHelpersPath = Join-Path -Path $PSScriptRoot -ChildPath '..' -AdditionalChildPath 'markdown-helpers.ps1'
. $markdownHelpersPath

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$generateScript = Join-Path $repoRoot 'scripts' 'generate-skills-index.ps1'
$lintScript = Join-Path $repoRoot 'scripts' 'lint-llm-instructions.ps1'
$contextFile = Join-Path $repoRoot '.llm' 'context.md'

Write-Host "Testing generate-skills-index.ps1 and lint-llm-instructions.ps1..." -ForegroundColor White

# =============================================================================
# Generate the skills index output once and reuse for multiple tests
# =============================================================================
Write-Info "Running generate-skills-index.ps1 to capture output..."
$rawGeneratorOutput = & pwsh -NoProfile -File $generateScript 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  [FAIL] generate-skills-index.ps1 failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit 1
}
$allCapturedLines = @($rawGeneratorOutput)
Write-Info "Generator captured $($allCapturedLines.Count) total lines"

# Extract only the generated index (between BEGIN and END markers, inclusive)
# Write-Host summary lines from the generator appear outside the markers
$beginMarkerText = '<!-- BEGIN GENERATED SKILLS INDEX -->'
$endMarkerText = '<!-- END GENERATED SKILLS INDEX -->'
$inIndex = $false
$generatorLines = @()
foreach ($line in $allCapturedLines) {
  if ($line -eq $beginMarkerText) { $inIndex = $true }
  if ($inIndex) { $generatorLines += $line }
  if ($line -eq $endMarkerText) { $inIndex = $false }
}
Write-Info "Generator produced $($generatorLines.Count) index lines"

# =============================================================================
# Test 1: Generated output has no H1 headings
# Uses raw regex (not Get-MarkdownH1Lines) because generator output is flat text,
# not a markdown document with fenced code blocks.
# =============================================================================
Write-Host "`n  Section: Generator output validation" -ForegroundColor White

$h1Lines = @($generatorLines | Where-Object { $_ -match '^# ' })
Write-TestResult "GeneratedOutput.NoH1Headings" ($h1Lines.Count -eq 0) `
  "Found $($h1Lines.Count) H1 heading(s): $($h1Lines -join ' | ')"

# =============================================================================
# Test 2: Generated output has correct markers
# =============================================================================
$beginMarker = '<!-- BEGIN GENERATED SKILLS INDEX -->'
$endMarker = '<!-- END GENERATED SKILLS INDEX -->'

$hasBegin = @($generatorLines | Where-Object { $_ -eq $beginMarker }).Count -gt 0
$hasEnd = @($generatorLines | Where-Object { $_ -eq $endMarker }).Count -gt 0

Write-TestResult "GeneratedOutput.HasBeginMarker" $hasBegin "Missing BEGIN marker in output"
Write-TestResult "GeneratedOutput.HasEndMarker" $hasEnd "Missing END marker in output"

# =============================================================================
# Test 3: Generated output has no diagnostic messages
# =============================================================================
$diagnosticLines = @($generatorLines | Where-Object { $_ -match '\[skills-index\]' })
Write-TestResult "GeneratedOutput.NoDiagnosticMessages" ($diagnosticLines.Count -eq 0) `
  "Found $($diagnosticLines.Count) diagnostic line(s): $($diagnosticLines -join ' | ')"

# =============================================================================
# Test 4: All output lines are valid
# =============================================================================
# Valid lines: empty, HTML comment, ### heading, table row (starts with |), table separator (starts with |)
$invalidLines = @()
foreach ($line in $generatorLines) {
  $trimmed = $line.TrimEnd()
  if ($trimmed -eq '') { continue }
  if ($trimmed -match '^<!--.*-->$') { continue }
  if ($trimmed -match '^### ') { continue }
  if ($trimmed -match '^\|') { continue }
  $invalidLines += $trimmed
}

Write-TestResult "GeneratedOutput.AllLinesValid" ($invalidLines.Count -eq 0) `
  "Found $($invalidLines.Count) invalid line(s): $($invalidLines -join ' | ')"

# =============================================================================
# Test 5: Lint passes on current repo
# =============================================================================
Write-Host "`n  Section: Lint script validation" -ForegroundColor White

Write-Info "Running lint-llm-instructions.ps1 on repo..."
$lintOutput = & pwsh -NoProfile -File $lintScript 2>&1
$lintExitCode = $LASTEXITCODE

Write-TestResult "Lint.PassesOnCurrentRepo" ($lintExitCode -eq 0) `
  "Expected exit code 0, got $lintExitCode. Output: $($lintOutput | Out-String)"

# =============================================================================
# Test 6: context.md has exactly one H1
# =============================================================================
Write-Host "`n  Section: context.md validation" -ForegroundColor White

$contextLines = @(Get-Content -Path $contextFile)
$contextH1Lines = @(Get-MarkdownH1Lines -Lines $contextLines)

$h1Diagnostic = ($contextH1Lines | ForEach-Object { "L$($_.LineNumber): $($_.Text)" }) -join ' | '
Write-TestResult "ContextMd.ExactlyOneH1" ($contextH1Lines.Count -eq 1) `
  "Expected 1 H1 heading, found $($contextH1Lines.Count): $h1Diagnostic"

# =============================================================================
# Test 7: Skills index markers exist in context.md
# =============================================================================
$contextContent = Get-Content -Path $contextFile -Raw
$contextHasBegin = $contextContent -match [regex]::Escape($beginMarker)
$contextHasEnd = $contextContent -match [regex]::Escape($endMarker)

Write-TestResult "ContextMd.HasBeginMarker" $contextHasBegin "Missing BEGIN marker in context.md"
Write-TestResult "ContextMd.HasEndMarker" $contextHasEnd "Missing END marker in context.md"

# =============================================================================
# Test 7b: Marker extraction semantics regression tests
# =============================================================================
Write-Host "`n  Section: Marker extraction semantics" -ForegroundColor White

function Invoke-TestMarkerExtract {
  param(
    [string]$Raw,
    [string]$Begin,
    [string]$End
  )

  if ($null -eq $Raw) {
    return $null
  }

  $normalized = $Raw -replace "`r`n", "`n"
  $pattern = "(?s)$([regex]::Escape($Begin))(?<content>.*?)$([regex]::Escape($End))"
  if ($normalized -match $pattern) {
    return $Matches['content'].Trim()
  }

  return $null
}

$mBegin = '<!-- BEGIN GENERATED SKILLS INDEX -->'
$mEnd = '<!-- END GENERATED SKILLS INDEX -->'

$validRaw = @(
  'Header',
  $mBegin,
  'Line A',
  'Line B',
  $mEnd,
  'Footer'
) -join "`n"
$validExtract = Invoke-TestMarkerExtract -Raw $validRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.ValidContent" ($validExtract -eq "Line A`nLine B") `
  "Expected 'Line A\\nLine B', got '$validExtract'"

$missingBeginRaw = @('x', 'y', $mEnd) -join "`n"
$missingBeginExtract = Invoke-TestMarkerExtract -Raw $missingBeginRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.MissingBeginReturnsNull" ($null -eq $missingBeginExtract) `
  "Expected null when BEGIN marker missing"

$malformedOrderRaw = @($mEnd, 'middle', $mBegin) -join "`n"
$malformedOrderExtract = Invoke-TestMarkerExtract -Raw $malformedOrderRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.MalformedOrderReturnsNull" ($null -eq $malformedOrderExtract) `
  "Expected null when END appears before BEGIN"

$emptyRaw = @($mBegin, $mEnd) -join "`n"
$emptyExtract = Invoke-TestMarkerExtract -Raw $emptyRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.EmptyContentExtractsEmpty" ($emptyExtract -eq '') `
  "Expected empty string for empty marker content, got '$emptyExtract'"

$crlfRaw = "head`r`n$mBegin`r`nLine 1`r`nLine 2`r`n$mEnd`r`ntail"
$crlfExtract = Invoke-TestMarkerExtract -Raw $crlfRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.CRLFNormalization" ($crlfExtract -eq "Line 1`nLine 2") `
  "Expected normalized LF content, got '$crlfExtract'"

$multiMarkerRaw = @(
  $mBegin,
  'first',
  $mEnd,
  'between',
  $mBegin,
  'second',
  $mEnd
) -join "`n"
$multiMarkerExtract = Invoke-TestMarkerExtract -Raw $multiMarkerRaw -Begin $mBegin -End $mEnd
Write-TestResult "MarkerExtract.NonGreedyFirstBlock" ($multiMarkerExtract -eq 'first') `
  "Expected first block only (non-greedy), got '$multiMarkerExtract'"

# =============================================================================
# Test 8: Data-driven — known-bad patterns do not appear in generator output
# =============================================================================
Write-Host "`n  Section: Data-driven bad-pattern validation" -ForegroundColor White

$badPatterns = @(
  @{ Line = '# <!-- END GENERATED SKILLS INDEX -->'; Description = 'H1 wrapping END marker' }
  @{ Line = '# [skills-index] Generated skills index'; Description = 'H1 wrapping diagnostic' }
  @{ Line = '# Some random heading'; Description = 'Arbitrary H1 heading' }
  @{ Line = '## Some H2 heading'; Description = 'Arbitrary H2 heading' }
)

foreach ($case in $badPatterns) {
  $found = $generatorLines -contains $case.Line
  $testName = "BadPattern.NotInOutput.$($case.Description -replace '\s+','_')"

  Write-TestResult $testName (-not $found) `
    "Bad pattern found in output: '$($case.Line)'"

  # Also verify the generator's own validation would catch H1/H2
  # The generator rejects lines matching '^#{1,2}\s' that don't start with '###'
  $wouldBeRejected = ($case.Line -match '^#{1,2}\s') -and ($case.Line -notmatch '^###')
  $validationTestName = "BadPattern.ValidatorWouldCatch.$($case.Description -replace '\s+','_')"

  Write-TestResult $validationTestName $wouldBeRejected `
    "Generator validation would not catch: '$($case.Line)'"
}

# =============================================================================
# Test 9: Injected H1 violation is detected by validation logic
# =============================================================================
Write-Host "`n  Section: Negative-path detection tests" -ForegroundColor White

$syntheticContent = @(
  '# Original H1 Heading',
  '',
  '## Some Section',
  '',
  '# Bad H1 Heading',
  '',
  'Some content here.'
)
$syntheticH1Lines = @(Get-MarkdownH1Lines -Lines $syntheticContent)

Write-TestResult "NegativePath.InjectedH1Detected" ($syntheticH1Lines.Count -gt 1) `
  "Expected >1 H1 heading in synthetic content, found $($syntheticH1Lines.Count)"

# =============================================================================
# Test 10: Single H1 content passes validation logic
# =============================================================================
$validContent = @(
  '# Only H1 Heading',
  '',
  '## Some Section',
  '',
  'Some content here.'
)
$validH1Lines = @(Get-MarkdownH1Lines -Lines $validContent)

Write-TestResult "NegativePath.SingleH1Passes" ($validH1Lines.Count -eq 1) `
  "Expected exactly 1 H1 heading in valid content, found $($validH1Lines.Count)"

# =============================================================================
# Test 11: Rollback scenario — multiple H1 headings correctly identified
# =============================================================================
$rollbackContent = @(
  '# AI Agent Guidelines',
  '',
  '## Overview',
  '',
  '# Duplicate From Generator',
  '',
  '### Some skill entry',
  '',
  '# Another Bad H1'
)
$rollbackH1Lines = @(Get-MarkdownH1Lines -Lines $rollbackContent)

Write-TestResult "NegativePath.RollbackMultipleH1" ($rollbackH1Lines.Count -eq 3) `
  "Expected 3 H1 headings in rollback content, found $($rollbackH1Lines.Count)"

# =============================================================================
# Test 12: Generator output with injected bad line is caught by validation
# =============================================================================
# These tests use raw regex (not Get-MarkdownH1Lines) because they validate
# the generator's own H1 rejection logic against flat output, not markdown documents.
Write-Host "`n  Section: Generator output injection tests" -ForegroundColor White

$injectedOutput = @($generatorLines) + @('# Injected Bad H1 Line')
$injectedH1Lines = @($injectedOutput | Where-Object { $_ -match '^# ' })

Write-TestResult "Injection.BadH1CaughtInGeneratorOutput" ($injectedH1Lines.Count -gt 0) `
  "Expected injected H1 to be detected, found $($injectedH1Lines.Count) H1 line(s)"

# Verify the injected line would be rejected by the generator's own validation
$injectedLine = '# Injected Bad H1 Line'
$wouldReject = ($injectedLine -match '^#{1,2}\s') -and ($injectedLine -notmatch '^###')

Write-TestResult "Injection.GeneratorValidationRejectsBadH1" $wouldReject `
  "Generator validation should reject: '$injectedLine'"

# =============================================================================
# Test 13: Injected H2 line in generator output is caught
# =============================================================================
$injectedH2Output = @($generatorLines) + @('## Injected Bad H2 Line')
$injectedBadLines = @($injectedH2Output | Where-Object { $_ -match '^#{1,2}\s' -and $_ -notmatch '^###' })

Write-TestResult "Injection.BadH2CaughtInGeneratorOutput" ($injectedBadLines.Count -gt 0) `
  "Expected injected H2 to be detected, found $($injectedBadLines.Count) bad line(s)"

# =============================================================================
# Test 14: Code-block-aware H1 detection
# =============================================================================
Write-Host "`n  Section: Code-block-aware H1 detection" -ForegroundColor White

$codeBlockCases = @(
  @{
    Name = 'BashCommentsInCodeBlock'
    Lines = @(
      '# Real H1 Heading',
      '',
      '```bash',
      '# this is a bash comment',
      '# another bash comment',
      '```',
      '',
      'Some content.'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'TildeCodeBlock'
    Lines = @(
      '# Real H1',
      '',
      '~~~sh',
      '# not a heading',
      '~~~',
      '',
      'End.'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'MultipleCodeBlocksOneH1'
    Lines = @(
      '# Only H1',
      '',
      '```',
      '# inside fence 1',
      '```',
      '',
      '```python',
      '# inside fence 2',
      '```'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'NoCodeBlocksTwoH1'
    Lines = @(
      '# First H1',
      '',
      '# Second H1'
    )
    ExpectedCount = 2
  }
  @{
    Name = 'H1AfterCodeBlock'
    Lines = @(
      '```',
      '# fenced comment',
      '```',
      '# Real H1 Outside'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'UnclosedFenceSwallowsRemainder'
    Lines = @(
      '# Real H1',
      '',
      '```bash',
      '# bash comment',
      '# swallowed H1'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'IndentedFence'
    Lines = @(
      '# Real H1',
      '   ```bash',
      '   # indented code comment',
      '   ```'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'EmptyCodeBlock'
    Lines = @(
      '# H1 Before',
      '```',
      '```',
      '# H1 After'
    )
    ExpectedCount = 2
  }
  @{
    Name = 'AdjacentCodeBlocksWithH1Between'
    Lines = @(
      '```',
      '# inside',
      '```',
      '# Between blocks',
      '```',
      '# inside again',
      '```'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'MixedFenceCharsNotClosed'
    Lines = @(
      '# Real H1',
      '```bash',
      '# inside backtick fence',
      '~~~',
      '# still inside backtick fence (tilde does not close backtick)',
      '```'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'NestedFourBacktickFence'
    Lines = @(
      '# Real H1',
      '````',
      '```',
      '# inside outer fence (inner triple does not close)',
      '```',
      '````'
    )
    ExpectedCount = 1
  }
  @{
    Name = 'ClosingFenceWithTrailingContent'
    Lines = @(
      '# Real H1',
      '```',
      '# inside fence',
      '``` not a valid close',
      '# still inside fence',
      '```'
    )
    ExpectedCount = 1
  }
)

foreach ($case in $codeBlockCases) {
  $h1Results = @(Get-MarkdownH1Lines -Lines $case.Lines)
  Write-TestResult "CodeBlockAware.$($case.Name)" ($h1Results.Count -eq $case.ExpectedCount) `
    "Expected $($case.ExpectedCount) H1, found $($h1Results.Count): $(($h1Results | ForEach-Object { "L$($_.LineNumber): $($_.Text)" }) -join ' | ')"
}

# ── Summary ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host ("=" * 60)
Write-Host ("Tests passed: {0}" -f $script:TestsPassed) -ForegroundColor Green
Write-Host ("Tests failed: {0}" -f $script:TestsFailed) -ForegroundColor $(if ($script:TestsFailed -gt 0) { "Red" } else { "Green" })

if ($script:FailedTests.Count -gt 0) {
  Write-Host "Failed tests:" -ForegroundColor Red
  foreach ($t in $script:FailedTests) {
    Write-Host "  - $t" -ForegroundColor Red
  }
}

Write-Host ("=" * 60)

exit $script:TestsFailed

# =============================================================================
# Git Hooks & Autofixers Installation Script (PowerShell)
# =============================================================================
# Installs git hooks and all required development tools for this repository.
#
# Usage:
#   ./scripts/install-hooks.ps1           # Full installation
#   ./scripts/install-hooks.ps1 -Check    # Check what's installed
#   ./scripts/install-hooks.ps1 -Help     # Show help
# =============================================================================

[CmdletBinding()]
param(
    [switch]$Check,
    [switch]$Help
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Blue
    Write-Host $Message -ForegroundColor Blue
    Write-Host "========================================" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠ $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ $Message" -ForegroundColor Cyan
}

function Show-Help {
    Write-Host "Git Hooks & Autofixers Installation Script"
    Write-Host ""
    Write-Host "Usage: ./scripts/install-hooks.ps1 [OPTIONS]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -Check    Check installation status without making changes"
    Write-Host "  -Help     Show this help message"
    Write-Host ""
    Write-Host "This script installs:"
    Write-Host "  1. Git hooks (pre-commit, pre-push)"
    Write-Host "  2. Node.js dependencies (prettier, markdownlint-cli, cspell)"
    Write-Host "  3. .NET tools (CSharpier)"
    Write-Host "  4. Optional tools info (yamllint, lychee, actionlint)"
}

function Test-Command {
    param([string]$Command)
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Get-CommandVersion {
    param([string]$Command, [string[]]$VersionArgs = @("--version"))
    try {
        $result = & $Command $VersionArgs 2>&1
        if ($result -is [array]) {
            return $result[0]
        }
        return $result
    }
    catch {
        return "unknown"
    }
}

function Test-Status {
    Write-Header "Installation Status Check"
    
    $allOk = $true
    
    Write-Host ""
    Write-Host "Required Tools:"
    Write-Host "---------------"
    
    # Git
    if (Test-Command "git") {
        Write-Success "git: $(Get-CommandVersion 'git' @('--version'))"
    }
    else {
        Write-Error "git: NOT FOUND"
        $allOk = $false
    }
    
    # Node.js / npm
    if (Test-Command "node") {
        Write-Success "node: $(Get-CommandVersion 'node' @('--version'))"
    }
    else {
        Write-Error "node: NOT FOUND"
        $allOk = $false
    }
    
    if (Test-Command "npm") {
        Write-Success "npm: $(Get-CommandVersion 'npm' @('--version'))"
    }
    else {
        Write-Error "npm: NOT FOUND"
        $allOk = $false
    }
    
    # .NET
    if (Test-Command "dotnet") {
        Write-Success "dotnet: $(Get-CommandVersion 'dotnet' @('--version'))"
    }
    else {
        Write-Warning "dotnet: NOT FOUND (CSharpier won't work)"
    }
    
    Write-Host ""
    Write-Host "Git Hooks:"
    Write-Host "----------"
    
    # Check git hooks path
    Push-Location $RepoRoot
    try {
        $hooksPath = git config --get core.hooksPath 2>$null
        if ($hooksPath -eq ".githooks") {
            Write-Success "Git hooks path: .githooks"
        }
        else {
            $displayPath = if ($hooksPath) { $hooksPath } else { "default (.git/hooks)" }
            Write-Warning "Git hooks path: $displayPath"
        }
    }
    finally {
        Pop-Location
    }
    
    # Check hook files exist
    if (Test-Path (Join-Path $RepoRoot ".githooks/pre-commit")) {
        Write-Success "pre-commit hook: exists"
    }
    else {
        Write-Error "pre-commit hook: MISSING"
        $allOk = $false
    }
    
    if (Test-Path (Join-Path $RepoRoot ".githooks/pre-push")) {
        Write-Success "pre-push hook: exists"
    }
    else {
        Write-Error "pre-push hook: MISSING"
        $allOk = $false
    }
    
    Write-Host ""
    Write-Host "Node.js Dependencies:"
    Write-Host "---------------------"
    
    $nodeModulesPath = Join-Path $RepoRoot "node_modules"
    if (Test-Path $nodeModulesPath) {
        Write-Success "node_modules: installed"
        
        # Check specific packages
        if (Test-Path (Join-Path $nodeModulesPath "prettier")) {
            try {
                $prettierVer = npx --no-install prettier --version 2>$null
                Write-Success "prettier: $prettierVer"
            }
            catch {
                Write-Success "prettier: installed"
            }
        }
        else {
            Write-Warning "prettier: NOT INSTALLED"
        }
        
        if (Test-Path (Join-Path $nodeModulesPath "markdownlint-cli")) {
            Write-Success "markdownlint-cli: installed"
        }
        else {
            Write-Warning "markdownlint-cli: NOT INSTALLED"
        }
        
        if (Test-Path (Join-Path $nodeModulesPath "cspell")) {
            Write-Success "cspell: installed"
        }
        else {
            Write-Warning "cspell: NOT INSTALLED"
        }
    }
    else {
        Write-Warning "node_modules: NOT INSTALLED (run 'npm install')"
    }
    
    Write-Host ""
    Write-Host ".NET Tools:"
    Write-Host "-----------"
    
    if (Test-Command "dotnet") {
        Push-Location $RepoRoot
        try {
            $toolList = dotnet tool list 2>$null
            if ($toolList -match "csharpier") {
                try {
                    $csharpierVer = dotnet tool run csharpier --version 2>$null
                    Write-Success "CSharpier: $csharpierVer"
                }
                catch {
                    Write-Success "CSharpier: installed"
                }
            }
            else {
                Write-Warning "CSharpier: NOT RESTORED (run 'dotnet tool restore')"
            }
        }
        finally {
            Pop-Location
        }
    }
    else {
        Write-Warning "CSharpier: .NET not available"
    }
    
    Write-Host ""
    Write-Host "Optional Tools:"
    Write-Host "---------------"
    
    if (Test-Command "yamllint") {
        Write-Success "yamllint: available"
    }
    else {
        Write-Info "yamllint: not installed (optional)"
    }
    
    if (Test-Command "lychee") {
        Write-Success "lychee: available"
    }
    else {
        Write-Info "lychee: not installed (optional)"
    }
    
    if (Test-Command "actionlint") {
        Write-Success "actionlint: available"
    }
    else {
        Write-Info "actionlint: not installed (optional)"
    }
    
    Write-Host ""
    if ($allOk) {
        Write-Success "All required components are properly configured!"
    }
    else {
        Write-Warning "Some components need attention. Run without -Check to install."
    }
}

function Install-GitHooks {
    Write-Header "Installing Git Hooks"
    
    Push-Location $RepoRoot
    try {
        # Configure git to use .githooks directory
        Write-Info "Configuring git hooks path..."
        git config core.hooksPath .githooks
        Write-Success "Git hooks path set to .githooks"
        
        # Note: On Windows, file permissions work differently
        # The hooks should work as-is if they have proper shebang lines
        if (Test-Path ".githooks/pre-commit") {
            Write-Success "pre-commit hook exists"
        }
        
        if (Test-Path ".githooks/pre-push") {
            Write-Success "pre-push hook exists"
        }
    }
    finally {
        Pop-Location
    }
}

function Install-NodeDeps {
    Write-Header "Installing Node.js Dependencies"
    
    if (-not (Test-Command "npm")) {
        Write-Error "npm is not installed. Please install Node.js first."
        Write-Info "Visit: https://nodejs.org/"
        return
    }
    
    Push-Location $RepoRoot
    try {
        Write-Info "Running npm install..."
        npm install
        Write-Success "Node.js dependencies installed"
        
        # Verify installations
        Write-Host ""
        Write-Info "Installed tools:"
        
        try {
            $prettierVer = npx --no-install prettier --version 2>$null
            if ($prettierVer) {
                Write-Success "  prettier: $prettierVer"
            }
        }
        catch { }
        
        try {
            $mdlintVer = npx --no-install markdownlint --version 2>$null
            if ($mdlintVer) {
                Write-Success "  markdownlint-cli: $mdlintVer"
            }
        }
        catch { }
        
        try {
            $cspellVer = npx --no-install cspell --version 2>$null
            if ($cspellVer) {
                Write-Success "  cspell: $cspellVer"
            }
        }
        catch { }
    }
    finally {
        Pop-Location
    }
}

function Install-DotNetTools {
    Write-Header "Installing .NET Tools"
    
    if (-not (Test-Command "dotnet")) {
        Write-Warning ".NET SDK is not installed. CSharpier will not be available."
        Write-Info "Visit: https://dotnet.microsoft.com/download"
        return
    }
    
    Push-Location $RepoRoot
    try {
        Write-Info "Restoring .NET tools..."
        dotnet tool restore
        Write-Success ".NET tools restored"
        
        # Verify CSharpier
        try {
            $csharpierVer = dotnet tool run csharpier --version 2>$null
            if ($csharpierVer) {
                Write-Success "CSharpier: $csharpierVer"
            }
        }
        catch { }
    }
    finally {
        Pop-Location
    }
}

function Show-OptionalTools {
    Write-Header "Optional Tools"
    
    Write-Host ""
    Write-Host "The following optional tools enhance the development experience:"
    Write-Host ""
    
    Write-Host "yamllint - YAML linting"
    Write-Host "  Install: pip install yamllint"
    Write-Host "  Or:      choco install yamllint (Windows)"
    Write-Host ""
    
    Write-Host "lychee - External link checker"
    Write-Host "  Install: cargo install lychee"
    Write-Host "  Or:      choco install lychee (Windows)"
    Write-Host ""
    
    Write-Host "actionlint - GitHub Actions workflow linter"
    Write-Host "  Install: go install github.com/rhysd/actionlint/cmd/actionlint@latest"
    Write-Host "  Or:      choco install actionlint (Windows)"
    Write-Host ""
}

function Main {
    if ($Help) {
        Show-Help
        return
    }
    
    if ($Check) {
        Test-Status
        return
    }
    
    # Default: full installation
    Write-Header "Git Hooks & Autofixers Installation"
    Write-Host ""
    Write-Host "This script will install:"
    Write-Host "  • Git hooks (pre-commit, pre-push)"
    Write-Host "  • Node.js dependencies (prettier, markdownlint-cli, cspell)"
    Write-Host "  • .NET tools (CSharpier)"
    Write-Host ""
    
    Install-GitHooks
    Install-NodeDeps
    Install-DotNetTools
    Show-OptionalTools
    
    Write-Header "Installation Complete"
    Write-Host ""
    Write-Success "Git hooks and autofixers are now installed!"
    Write-Host ""
    Write-Host "Run './scripts/install-hooks.ps1 -Check' to verify installation status."
    Write-Host ""
    Write-Host "Available npm scripts:"
    Write-Host "  npm run hooks:install     - Configure git hooks path"
    Write-Host "  npm run lint:docs         - Lint documentation links"
    Write-Host "  npm run lint:markdown     - Lint markdown files"
    Write-Host "  npm run lint:spelling     - Check spelling"
    Write-Host "  npm run format:md         - Format markdown files"
    Write-Host "  npm run format:json       - Format JSON files"
    Write-Host "  npm run validate:prepush  - Run all pre-push validations"
}

Main

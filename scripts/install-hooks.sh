#!/usr/bin/env bash
set -e

# =============================================================================
# Git Hooks & Autofixers Installation Script (Bash)
# =============================================================================
# Installs git hooks and all required development tools for this repository.
#
# Usage:
#   ./scripts/install-hooks.sh           # Full installation
#   ./scripts/install-hooks.sh --check   # Check what's installed
#   ./scripts/install-hooks.sh --help    # Show help
# =============================================================================

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

print_header() {
    echo ""
    echo -e "${BLUE}========================================${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}========================================${NC}"
}

print_success() {
    echo -e "${GREEN}✓${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}⚠${NC} $1"
}

print_error() {
    echo -e "${RED}✗${NC} $1"
}

print_info() {
    echo -e "${BLUE}ℹ${NC} $1"
}

show_help() {
    echo "Git Hooks & Autofixers Installation Script"
    echo ""
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  --check    Check installation status without making changes"
    echo "  --help     Show this help message"
    echo ""
    echo "This script installs:"
    echo "  1. Git hooks (pre-commit, pre-push)"
    echo "  2. Node.js dependencies (prettier, markdownlint-cli, cspell)"
    echo "  3. .NET tools (CSharpier)"
    echo "  4. Optional tools info (yamllint, lychee, actionlint)"
}

check_command() {
    if command -v "$1" >/dev/null 2>&1; then
        return 0
    else
        return 1
    fi
}

check_status() {
    print_header "Installation Status Check"
    
    local all_ok=true
    
    echo ""
    echo "Required Tools:"
    echo "---------------"
    
    # Git
    if check_command git; then
        print_success "git: $(git --version)"
    else
        print_error "git: NOT FOUND"
        all_ok=false
    fi
    
    # Node.js / npm
    if check_command node; then
        print_success "node: $(node --version)"
    else
        print_error "node: NOT FOUND"
        all_ok=false
    fi
    
    if check_command npm; then
        print_success "npm: $(npm --version)"
    else
        print_error "npm: NOT FOUND"
        all_ok=false
    fi
    
    # .NET
    if check_command dotnet; then
        print_success "dotnet: $(dotnet --version)"
    else
        print_warning "dotnet: NOT FOUND (CSharpier won't work)"
    fi
    
    # PowerShell
    if check_command pwsh; then
        print_success "pwsh: $(pwsh --version 2>/dev/null | head -1)"
    elif check_command powershell; then
        print_success "powershell: available"
    else
        print_warning "PowerShell: NOT FOUND (some scripts won't work)"
    fi
    
    echo ""
    echo "Git Hooks:"
    echo "----------"
    
    # Check git hooks path
    local hooks_path
    hooks_path=$(git -C "$REPO_ROOT" config --get core.hooksPath 2>/dev/null || echo "")
    if [[ "$hooks_path" == ".githooks" ]]; then
        print_success "Git hooks path: .githooks"
    else
        print_warning "Git hooks path: ${hooks_path:-default (.git/hooks)}"
    fi
    
    # Check hook files exist
    if [[ -f "$REPO_ROOT/.githooks/pre-commit" ]]; then
        print_success "pre-commit hook: exists"
    else
        print_error "pre-commit hook: MISSING"
        all_ok=false
    fi
    
    if [[ -f "$REPO_ROOT/.githooks/pre-push" ]]; then
        print_success "pre-push hook: exists"
    else
        print_error "pre-push hook: MISSING"
        all_ok=false
    fi
    
    echo ""
    echo "Node.js Dependencies:"
    echo "---------------------"
    
    if [[ -d "$REPO_ROOT/node_modules" ]]; then
        print_success "node_modules: installed"
        
        # Check specific packages
        if [[ -d "$REPO_ROOT/node_modules/prettier" ]]; then
            local prettier_ver
            prettier_ver=$(npx --no-install prettier --version 2>/dev/null || echo "unknown")
            print_success "prettier: $prettier_ver"
        else
            print_warning "prettier: NOT INSTALLED"
        fi
        
        if [[ -d "$REPO_ROOT/node_modules/markdownlint-cli" ]]; then
            print_success "markdownlint-cli: installed"
        else
            print_warning "markdownlint-cli: NOT INSTALLED"
        fi
        
        if [[ -d "$REPO_ROOT/node_modules/cspell" ]]; then
            print_success "cspell: installed"
        else
            print_warning "cspell: NOT INSTALLED"
        fi
    else
        print_warning "node_modules: NOT INSTALLED (run 'npm install')"
    fi
    
    echo ""
    echo ".NET Tools:"
    echo "-----------"
    
    if check_command dotnet; then
        if dotnet tool list 2>/dev/null | grep -q csharpier; then
            local csharpier_ver
            csharpier_ver=$(dotnet tool run csharpier --version 2>/dev/null || echo "unknown")
            print_success "CSharpier: $csharpier_ver"
        else
            print_warning "CSharpier: NOT RESTORED (run 'dotnet tool restore')"
        fi
    else
        print_warning "CSharpier: .NET not available"
    fi
    
    echo ""
    echo "Optional Tools:"
    echo "---------------"
    
    if check_command yamllint; then
        print_success "yamllint: available"
    else
        print_info "yamllint: not installed (optional)"
    fi
    
    if check_command lychee; then
        print_success "lychee: available"
    else
        print_info "lychee: not installed (optional)"
    fi
    
    if check_command actionlint; then
        print_success "actionlint: available"
    else
        print_info "actionlint: not installed (optional)"
    fi
    
    echo ""
    if $all_ok; then
        print_success "All required components are properly configured!"
    else
        print_warning "Some components need attention. Run without --check to install."
    fi
}

install_hooks() {
    print_header "Installing Git Hooks"
    
    cd "$REPO_ROOT"
    
    # Configure git to use .githooks directory
    print_info "Configuring git hooks path..."
    git config core.hooksPath .githooks
    print_success "Git hooks path set to .githooks"
    
    # Ensure hooks are executable
    if [[ -f ".githooks/pre-commit" ]]; then
        chmod +x .githooks/pre-commit
        print_success "pre-commit hook is executable"
    fi
    
    if [[ -f ".githooks/pre-push" ]]; then
        chmod +x .githooks/pre-push
        print_success "pre-push hook is executable"
    fi
}

install_node_deps() {
    print_header "Installing Node.js Dependencies"
    
    cd "$REPO_ROOT"
    
    if ! check_command npm; then
        print_error "npm is not installed. Please install Node.js first."
        print_info "Visit: https://nodejs.org/"
        return 1
    fi
    
    print_info "Running npm install..."
    npm install
    print_success "Node.js dependencies installed"
    
    # Verify installations
    if check_command npx; then
        echo ""
        print_info "Installed tools:"
        if npx --no-install prettier --version >/dev/null 2>&1; then
            print_success "  prettier: $(npx --no-install prettier --version)"
        fi
        if npx --no-install markdownlint --version >/dev/null 2>&1; then
            print_success "  markdownlint-cli: $(npx --no-install markdownlint --version)"
        fi
        if npx --no-install cspell --version >/dev/null 2>&1; then
            print_success "  cspell: $(npx --no-install cspell --version)"
        fi
    fi
}

install_dotnet_tools() {
    print_header "Installing .NET Tools"
    
    cd "$REPO_ROOT"
    
    if ! check_command dotnet; then
        print_warning ".NET SDK is not installed. CSharpier will not be available."
        print_info "Visit: https://dotnet.microsoft.com/download"
        return 0
    fi
    
    print_info "Restoring .NET tools..."
    dotnet tool restore
    print_success ".NET tools restored"
    
    # Verify CSharpier
    if dotnet tool list 2>/dev/null | grep -q csharpier; then
        print_success "CSharpier: $(dotnet tool run csharpier --version 2>/dev/null || echo 'installed')"
    fi
}

show_optional_tools() {
    print_header "Optional Tools"
    
    echo ""
    echo "The following optional tools enhance the development experience:"
    echo ""
    
    echo "yamllint - YAML linting"
    echo "  Install: pip install yamllint"
    echo "  Or:      brew install yamllint (macOS)"
    echo ""
    
    echo "lychee - External link checker"
    echo "  Install: cargo install lychee"
    echo "  Or:      brew install lychee (macOS)"
    echo ""
    
    echo "actionlint - GitHub Actions workflow linter"
    echo "  Install: go install github.com/rhysd/actionlint/cmd/actionlint@latest"
    echo "  Or:      brew install actionlint (macOS)"
    echo ""
    
    echo "PowerShell (pwsh) - Required for some lint scripts"
    echo "  Install: https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell"
    echo ""
}

main() {
    # Parse arguments
    case "${1:-}" in
        --check)
            check_status
            exit 0
            ;;
        --help|-h)
            show_help
            exit 0
            ;;
        "")
            # Default: full installation
            ;;
        *)
            print_error "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
    
    print_header "Git Hooks & Autofixers Installation"
    echo ""
    echo "This script will install:"
    echo "  • Git hooks (pre-commit, pre-push)"
    echo "  • Node.js dependencies (prettier, markdownlint-cli, cspell)"
    echo "  • .NET tools (CSharpier)"
    echo ""
    
    install_hooks
    install_node_deps
    install_dotnet_tools
    show_optional_tools
    
    print_header "Installation Complete"
    echo ""
    print_success "Git hooks and autofixers are now installed!"
    echo ""
    echo "Run '$0 --check' to verify installation status."
    echo ""
    echo "Available npm scripts:"
    echo "  npm run hooks:install     - Configure git hooks path"
    echo "  npm run lint:docs         - Lint documentation links"
    echo "  npm run lint:markdown     - Lint markdown files"
    echo "  npm run lint:spelling     - Check spelling"
    echo "  npm run format:md         - Format markdown files"
    echo "  npm run format:json       - Format JSON files"
    echo "  npm run validate:prepush  - Run all pre-push validations"
}

main "$@"

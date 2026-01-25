# Use the .NET devcontainer base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Architecture for downloading binaries (supports amd64 and arm64)
ARG TARGETARCH=amd64

# Set non-interactive frontend for apt to avoid prompts
ENV DEBIAN_FRONTEND=noninteractive

# Fix: Remove the Yarn repository with expired GPG key
# The base image includes yarn repo, but the GPG key (23E7166788B63E1E) expired.
# This causes apt-get update to fail with "EXPKEYSIG" error.
# We don't need Yarn for this project, so simply remove the problematic source.
# If Yarn is needed later, use corepack (bundled with Node.js) or npm instead.
RUN rm -f /etc/apt/sources.list.d/yarn.list \
    && rm -f /etc/apt/keyrings/yarn.gpg \
    && rm -f /usr/share/keyrings/yarn-archive-keyring.gpg 2>/dev/null || true

# Install additional tools from apt repositories
# - ripgrep: Fast regex search (10x faster than grep)
# - jq: JSON processor
# - htop: Interactive process viewer (better than top)
# - ncdu: Disk usage analyzer with ncurses interface
# - tree: Directory listing in tree format
# - tldr: Simplified man pages with practical examples
# - silversearcher-ag: Fast code search (ag command)
# - moreutils: Additional Unix utilities (sponge, parallel, etc.)
# - unzip: Required for extracting some binaries
# - wget: Alternative to curl for downloads
# - ca-certificates: Ensure SSL certificates are available
# - shellcheck: Shell script linter (used by actionlint for enhanced checks)
#
# Additional tools installed from GitHub releases:
# - fd: Fast file finder (better find)
# - bat: Cat with syntax highlighting
# - fzf: Fuzzy finder
# - delta: Better git diffs
# - eza: Modern ls replacement
# - zoxide: Smarter cd command
# - yq: YAML processor
# - duf: Better df
# - sd: Intuitive find-and-replace (better sed)
# - dust: Visual disk usage (better du)
# - procs: Modern process viewer (better ps)
# - tokei: Code statistics
# - actionlint: GitHub Actions workflow linter
RUN apt-get update && apt-get install -y --no-install-recommends \
    ripgrep \
    jq \
    htop \
    ncdu \
    tree \
    tldr \
    silversearcher-ag \
    moreutils \
    unzip \
    wget \
    ca-certificates \
    shellcheck \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install fd-find (faster alternative to find) from GitHub releases
# fd respects .gitignore by default and has intuitive syntax
RUN FD_VERSION="10.2.0" \
    && case "${TARGETARCH}" in \
        amd64) FD_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) FD_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/sharkdp/fd/releases/download/v${FD_VERSION}/fd-v${FD_VERSION}-${FD_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "fd-v${FD_VERSION}-${FD_ARCH}/fd" \
    && chmod +x /usr/local/bin/fd

# Install bat (better cat with syntax highlighting) from GitHub releases
RUN BAT_VERSION="0.24.0" \
    && case "${TARGETARCH}" in \
        amd64) BAT_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) BAT_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/sharkdp/bat/releases/download/v${BAT_VERSION}/bat-v${BAT_VERSION}-${BAT_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "bat-v${BAT_VERSION}-${BAT_ARCH}/bat" \
    && chmod +x /usr/local/bin/bat

# Install fzf (fuzzy finder) from GitHub releases
RUN FZF_VERSION="0.56.3" \
    && case "${TARGETARCH}" in \
        amd64) FZF_ARCH="linux_amd64" ;; \
        arm64) FZF_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/junegunn/fzf/releases/download/v${FZF_VERSION}/fzf-${FZF_VERSION}-${FZF_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin fzf \
    && chmod +x /usr/local/bin/fzf

# Install delta (better git diff viewer) from GitHub releases
RUN DELTA_VERSION="0.18.2" \
    && case "${TARGETARCH}" in \
        amd64) DELTA_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) DELTA_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/dandavison/delta/releases/download/${DELTA_VERSION}/delta-${DELTA_VERSION}-${DELTA_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "delta-${DELTA_VERSION}-${DELTA_ARCH}/delta" \
    && chmod +x /usr/local/bin/delta

# Install eza (modern replacement for ls, successor to exa) from GitHub releases
RUN EZA_VERSION="0.20.14" \
    && case "${TARGETARCH}" in \
        amd64) EZA_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) EZA_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/eza-community/eza/releases/download/v${EZA_VERSION}/eza_${EZA_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin \
    && chmod +x /usr/local/bin/eza

# Install zoxide (smarter cd command) from GitHub releases
RUN ZOXIDE_VERSION="0.9.6" \
    && case "${TARGETARCH}" in \
        amd64) ZOXIDE_ARCH="x86_64-unknown-linux-musl" ;; \
        arm64) ZOXIDE_ARCH="aarch64-unknown-linux-musl" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/ajeetdsouza/zoxide/releases/download/v${ZOXIDE_VERSION}/zoxide-${ZOXIDE_VERSION}-${ZOXIDE_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin zoxide \
    && chmod +x /usr/local/bin/zoxide

# Install yq (YAML processor, like jq for YAML) from GitHub releases
RUN YQ_VERSION="4.44.6" \
    && case "${TARGETARCH}" in \
        amd64) YQ_ARCH="linux_amd64" ;; \
        arm64) YQ_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/mikefarah/yq/releases/download/v${YQ_VERSION}/yq_${YQ_ARCH}" -o /usr/local/bin/yq \
    && chmod +x /usr/local/bin/yq

# Install duf (better df) from GitHub releases
RUN DUF_VERSION="0.8.1" \
    && case "${TARGETARCH}" in \
        amd64) DUF_ARCH="linux_x86_64" ;; \
        arm64) DUF_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/muesli/duf/releases/download/v${DUF_VERSION}/duf_${DUF_VERSION}_${DUF_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin duf \
    && chmod +x /usr/local/bin/duf

# Install sd (intuitive find-and-replace, better sed) from GitHub releases
RUN SD_VERSION="1.0.0" \
    && case "${TARGETARCH}" in \
        amd64) SD_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) SD_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/chmln/sd/releases/download/v${SD_VERSION}/sd-v${SD_VERSION}-${SD_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "sd-v${SD_VERSION}-${SD_ARCH}/sd" \
    && chmod +x /usr/local/bin/sd

# Install dust (intuitive disk usage, better du) from GitHub releases
RUN DUST_VERSION="1.1.1" \
    && case "${TARGETARCH}" in \
        amd64) DUST_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) DUST_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/bootandy/dust/releases/download/v${DUST_VERSION}/dust-v${DUST_VERSION}-${DUST_ARCH}.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin "dust-v${DUST_VERSION}-${DUST_ARCH}/dust" \
    && chmod +x /usr/local/bin/dust

# Install procs (modern replacement for ps) from GitHub releases
RUN PROCS_VERSION="0.14.8" \
    && case "${TARGETARCH}" in \
        amd64) PROCS_ARCH="x86_64-linux" ;; \
        arm64) PROCS_ARCH="aarch64-linux" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/dalance/procs/releases/download/v${PROCS_VERSION}/procs-v${PROCS_VERSION}-${PROCS_ARCH}.zip" -o /tmp/procs.zip \
    && unzip -q /tmp/procs.zip -d /usr/local/bin \
    && chmod +x /usr/local/bin/procs \
    && rm /tmp/procs.zip

# Install tokei (code statistics) from GitHub releases
RUN TOKEI_VERSION="12.1.2" \
    && case "${TARGETARCH}" in \
        amd64) TOKEI_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) TOKEI_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/XAMPPRocky/tokei/releases/download/v${TOKEI_VERSION}/tokei-${TOKEI_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin tokei \
    && chmod +x /usr/local/bin/tokei

# Install lychee (fast link checker) from GitHub releases
# Version should match lychee-action in .github/workflows/lint-doc-links.yml
RUN LYCHEE_VERSION="0.21.0" \
    && case "${TARGETARCH}" in \
        amd64) LYCHEE_ARCH="x86_64-unknown-linux-gnu" ;; \
        arm64) LYCHEE_ARCH="aarch64-unknown-linux-gnu" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/lycheeverse/lychee/releases/download/lychee-v${LYCHEE_VERSION}/lychee-${LYCHEE_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin \
    && chmod +x /usr/local/bin/lychee

# Install actionlint (GitHub Actions workflow linter) from GitHub releases
# https://github.com/rhysd/actionlint - validates workflow syntax and detects common errors
RUN ACTIONLINT_VERSION="1.7.7" \
    && case "${TARGETARCH}" in \
        amd64) ACTIONLINT_ARCH="linux_amd64" ;; \
        arm64) ACTIONLINT_ARCH="linux_arm64" ;; \
        *) echo "Unsupported TARGETARCH: ${TARGETARCH}" >&2; exit 1 ;; \
    esac \
    && curl -fsSL "https://github.com/rhysd/actionlint/releases/download/v${ACTIONLINT_VERSION}/actionlint_${ACTIONLINT_VERSION}_${ACTIONLINT_ARCH}.tar.gz" \
    | tar -xz -C /usr/local/bin actionlint \
    && chmod +x /usr/local/bin/actionlint

# Install PowerShell
RUN wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends powershell \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Git LFS
RUN apt-get update && apt-get install -y --no-install-recommends git-lfs \
    && git lfs install \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install Python and pip
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    python3-pip \
    python3-venv \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install yamllint (YAML linter) via pip
# Using --break-system-packages to allow global install in container
# https://yamllint.readthedocs.io/ - validates YAML syntax and style
RUN pip3 install --break-system-packages yamllint

# Install Node.js (latest LTS) and npm from NodeSource
RUN curl -fsSL https://deb.nodesource.com/setup_lts.x | bash - \
    && apt-get install -y --no-install-recommends nodejs \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && npm install -g npm@latest

# Install CSharpier as a global .NET tool for the vscode user
USER vscode
RUN dotnet tool install -g csharpier

# Switch back to root for remaining setup
USER root

# Configure delta as the default git pager for better diffs
RUN git config --system core.pager delta \
    && git config --system interactive.diffFilter "delta --color-only" \
    && git config --system delta.navigate true \
    && git config --system delta.light false \
    && git config --system delta.line-numbers true \
    && git config --system delta.side-by-side false \
    && git config --system merge.conflictstyle diff3 \
    && git config --system diff.colorMoved default

# Initialize zoxide for bash (add to bashrc for interactive shells)
RUN echo 'eval "$(zoxide init bash)"' >> /etc/bash.bashrc

# Add helpful aliases to bashrc
RUN { \
    echo ''; \
    echo '# Modern CLI tool aliases'; \
    echo 'alias ls="eza --icons --group-directories-first"'; \
    echo 'alias ll="eza -la --icons --group-directories-first"'; \
    echo 'alias lt="eza --tree --icons --group-directories-first"'; \
    echo 'alias cat="bat --paging=never"'; \
    echo 'alias less="bat"'; \
    echo 'alias find="fd"'; \
    echo 'alias grep="rg"'; \
    echo 'alias df="duf"'; \
    echo 'alias sed="sd"'; \
    echo 'alias du="dust"'; \
    echo 'alias ps="procs"'; \
    echo 'alias cd="z"'; \
    echo 'alias cdi="zi"'; \
    } >> /etc/bash.bashrc

# Enable VS Code shell integration
# VS Code automatically injects shell integration when terminal.integrated.shellIntegration.enabled is true.
# This manual fallback ensures shell integration works in edge cases (sub-shells, reconnections, etc.)
# See: https://code.visualstudio.com/docs/terminal/shell-integration
RUN { \
    echo ''; \
    echo '# VS Code shell integration (manual fallback)'; \
    echo '# Automatic injection is enabled via devcontainer.json settings.'; \
    echo '# This provides a fallback for sub-shells and edge cases.'; \
    echo 'if [[ "$TERM_PROGRAM" == "vscode" ]]; then'; \
    echo '    # Check if VS Code has injected the integration script path'; \
    echo '    if [[ -n "$VSCODE_SHELL_INTEGRATION" && -f "$VSCODE_SHELL_INTEGRATION" ]]; then'; \
    echo '        builtin source "$VSCODE_SHELL_INTEGRATION"'; \
    echo '    elif command -v code &> /dev/null; then'; \
    echo '        # Fallback: use code CLI to locate the script'; \
    echo '        _vscode_shell_integration="$(code --locate-shell-integration-path bash 2>/dev/null)"'; \
    echo '        if [[ -n "$_vscode_shell_integration" && -f "$_vscode_shell_integration" ]]; then'; \
    echo '            builtin source "$_vscode_shell_integration"'; \
    echo '        fi'; \
    echo '        unset _vscode_shell_integration'; \
    echo '    fi'; \
    echo 'fi'; \
    } >> /etc/bash.bashrc

# Enable VS Code shell integration for zsh (installed via common-utils feature)
# See: https://code.visualstudio.com/docs/terminal/shell-integration
RUN { \
    echo ''; \
    echo '# VS Code shell integration (manual fallback)'; \
    echo 'if [[ "$TERM_PROGRAM" == "vscode" ]]; then'; \
    echo '    if [[ -n "$VSCODE_SHELL_INTEGRATION" && -f "$VSCODE_SHELL_INTEGRATION" ]]; then'; \
    echo '        builtin source "$VSCODE_SHELL_INTEGRATION"'; \
    echo '    elif command -v code &> /dev/null; then'; \
    echo '        _vscode_shell_integration="$(code --locate-shell-integration-path zsh 2>/dev/null)"'; \
    echo '        if [[ -n "$_vscode_shell_integration" && -f "$_vscode_shell_integration" ]]; then'; \
    echo '            builtin source "$_vscode_shell_integration"'; \
    echo '        fi'; \
    echo '        unset _vscode_shell_integration'; \
    echo '    fi'; \
    echo 'fi'; \
    } >> /etc/zsh/zshrc

# Enable VS Code shell integration for PowerShell
# See: https://code.visualstudio.com/docs/terminal/shell-integration
RUN mkdir -p /opt/microsoft/powershell/7 \
    && { \
    echo '# VS Code shell integration (manual fallback)'; \
    echo 'if ($env:TERM_PROGRAM -eq "vscode") {'; \
    echo '    if ($env:VSCODE_SHELL_INTEGRATION -and (Test-Path $env:VSCODE_SHELL_INTEGRATION)) {'; \
    echo '        . $env:VSCODE_SHELL_INTEGRATION'; \
    echo '    } elseif (Get-Command code -ErrorAction SilentlyContinue) {'; \
    echo '        $script = code --locate-shell-integration-path pwsh 2>$null'; \
    echo '        if ($script -and (Test-Path $script)) { . $script }'; \
    echo '    }'; \
    echo '}'; \
    } >> /opt/microsoft/powershell/7/profile.ps1

# Ensure dotnet tools are on the PATH for both root and vscode users
ENV PATH="${PATH}:/root/.dotnet/tools:/home/vscode/.dotnet/tools"

# Reset to non-root user
USER vscode

# Use the .NET devcontainer base image
FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Architecture for downloading binaries
ARG TARGETARCH=amd64

# Install additional tools from apt repositories
# - ripgrep: Fast regex search (10x faster than grep)
# - jq: JSON processor
# - htop: Interactive process viewer (better than top)
# - ncdu: Disk usage analyzer with ncurses interface
# - tree: Directory listing in tree format
# - tldr: Simplified man pages with practical examples
# - hyperfine: Command-line benchmarking tool
# - silversearcher-ag: Fast code search (ag command)
# - moreutils: Additional Unix utilities (sponge, parallel, etc.)
# - duf: Disk usage/free utility (better df)
RUN apt-get update && apt-get install -y \
    ripgrep \
    jq \
    htop \
    ncdu \
    tree \
    tldr \
    silversearcher-ag \
    moreutils \
    && rm -rf /var/lib/apt/lists/*

# Install fd-find (faster alternative to find) from GitHub releases
# fd respects .gitignore by default and has intuitive syntax
RUN FD_VERSION="10.2.0" \
    && curl -fsSL "https://github.com/sharkdp/fd/releases/download/v${FD_VERSION}/fd-v${FD_VERSION}-x86_64-unknown-linux-gnu.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin fd-v${FD_VERSION}-x86_64-unknown-linux-gnu/fd \
    && chmod +x /usr/local/bin/fd

# Install bat (better cat with syntax highlighting) from GitHub releases
RUN BAT_VERSION="0.24.0" \
    && curl -fsSL "https://github.com/sharkdp/bat/releases/download/v${BAT_VERSION}/bat-v${BAT_VERSION}-x86_64-unknown-linux-gnu.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin bat-v${BAT_VERSION}-x86_64-unknown-linux-gnu/bat \
    && chmod +x /usr/local/bin/bat

# Install fzf (fuzzy finder) from GitHub releases
RUN FZF_VERSION="0.56.3" \
    && curl -fsSL "https://github.com/junegunn/fzf/releases/download/v${FZF_VERSION}/fzf-${FZF_VERSION}-linux_amd64.tar.gz" \
    | tar -xz -C /usr/local/bin fzf \
    && chmod +x /usr/local/bin/fzf

# Install delta (better git diff viewer) from GitHub releases
RUN DELTA_VERSION="0.18.2" \
    && curl -fsSL "https://github.com/dandavison/delta/releases/download/${DELTA_VERSION}/delta-${DELTA_VERSION}-x86_64-unknown-linux-gnu.tar.gz" \
    | tar -xz --strip-components=1 -C /usr/local/bin delta-${DELTA_VERSION}-x86_64-unknown-linux-gnu/delta \
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
    && curl -fsSL "https://github.com/ajeetdsouza/zoxide/releases/download/v${ZOXIDE_VERSION}/zoxide-${ZOXIDE_VERSION}-x86_64-unknown-linux-musl.tar.gz" \
    | tar -xz -C /usr/local/bin zoxide \
    && chmod +x /usr/local/bin/zoxide

# Install yq (YAML processor, like jq for YAML) from GitHub releases
RUN YQ_VERSION="4.44.6" \
    && curl -fsSL "https://github.com/mikefarah/yq/releases/download/v${YQ_VERSION}/yq_linux_amd64" -o /usr/local/bin/yq \
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

# Install PowerShell
RUN wget -q https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb \
    && dpkg -i packages-microsoft-prod.deb \
    && rm packages-microsoft-prod.deb \
    && apt-get update \
    && apt-get install -y powershell \
    && rm -rf /var/lib/apt/lists/*

# Install Git LFS
RUN apt-get update && apt-get install -y git-lfs \
    && git lfs install \
    && rm -rf /var/lib/apt/lists/*

# Install Python and pip
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Install CSharpier as a global .NET tool
RUN dotnet tool install -g csharpier

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
RUN echo '\n# Modern CLI tool aliases' >> /etc/bash.bashrc \
    && echo 'alias ls="eza --icons --group-directories-first"' >> /etc/bash.bashrc \
    && echo 'alias ll="eza -la --icons --group-directories-first"' >> /etc/bash.bashrc \
    && echo 'alias lt="eza --tree --icons --group-directories-first"' >> /etc/bash.bashrc \
    && echo 'alias cat="bat --paging=never"' >> /etc/bash.bashrc \
    && echo 'alias less="bat"' >> /etc/bash.bashrc \
    && echo 'alias find="fd"' >> /etc/bash.bashrc \
    && echo 'alias grep="rg"' >> /etc/bash.bashrc \
    && echo 'alias df="duf"' >> /etc/bash.bashrc \
    && echo 'alias cd="z"' >> /etc/bash.bashrc \
    && echo 'alias cdi="zi"' >> /etc/bash.bashrc

# Ensure dotnet tools are on the PATH
ENV PATH="${PATH}:/root/.dotnet/tools:/home/vscode/.dotnet/tools"

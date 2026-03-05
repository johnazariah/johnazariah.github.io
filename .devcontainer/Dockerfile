FROM mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm

# Install Ruby, Jekyll dependencies, and GitHub CLI
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ruby-full \
        build-essential \
        zlib1g-dev \
    && gem install bundler jekyll \
    && curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
        | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
        | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install -y gh \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Python, uv, and ruff
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-pip python3-venv \
    && apt-get clean && rm -rf /var/lib/apt/lists/* \
    && curl -LsSf https://astral.sh/uv/install.sh | sh \
    && curl -LsSf https://astral.sh/ruff/install.sh | sh

ENV PATH="/root/.local/bin:${PATH}"

# Make uv/ruff available for the vscode user too
RUN cp /root/.local/bin/uv /usr/local/bin/uv \
    && cp /root/.local/bin/uvx /usr/local/bin/uvx \
    && cp /root/.local/bin/ruff /usr/local/bin/ruff

# Install Haskell (GHCup, GHC, Cabal) for the vscode user
USER vscode
RUN curl --proto '=https' --tlsv1.2 -sSf https://get-ghcup.haskell.org \
    | BOOTSTRAP_HASKELL_NONINTERACTIVE=1 \
      BOOTSTRAP_HASKELL_GHC_VERSION=9.6.4 \
      BOOTSTRAP_HASKELL_CABAL_VERSION=3.10.2.1 \
      sh
ENV PATH="/home/vscode/.ghcup/bin:${PATH}"
USER root

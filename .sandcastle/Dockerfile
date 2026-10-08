FROM node:22-bookworm

# Install system dependencies. python3 runs the gate's test discovery
# (.github/scripts/discover_tests.py); the lib* packages are what the .NET SDK
# needs on Debian.
RUN apt-get update && apt-get install -y \
  git \
  curl \
  jq \
  python3 \
  libicu72 \
  libssl3 \
  libgssapi-krb5-2 \
  zlib1g \
  && rm -rf /var/lib/apt/lists/*

# pnpm through corepack, as CI does (.github/ci/prepare.sh). Corepack runs the
# version pinned by "packageManager" in package.json and src/Web/package.json.
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0
RUN corepack enable pnpm

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
  | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
  | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
  && apt-get update && apt-get install -y gh \
  && rm -rf /var/lib/apt/lists/*

# Build-args for UID/GID alignment: sandcastle docker build-image
# defaults these to the host user's UID/GID so image-built files
# and bind-mounted files share an owner without runtime chown.
ARG AGENT_UID=1000
ARG AGENT_GID=1000

# Rename the base image's "node" user to "agent" and align UID/GID.
RUN groupmod -o -g $AGENT_GID node && usermod -o -u $AGENT_UID -g $AGENT_GID -d /home/agent -m -l agent node
USER ${AGENT_UID}:${AGENT_GID}

# Install Claude Code CLI
RUN curl -fsSL https://claude.ai/install.sh | bash

# Install the .NET SDK that global.json pins, so agents can run scripts/gate.sh
# (dotnet build and dotnet test). Keep DOTNET_SDK_VERSION in step with global.json.
ARG DOTNET_SDK_VERSION=10.0.401
ENV DOTNET_ROOT=/home/agent/.dotnet \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_NOLOGO=1
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
  && bash /tmp/dotnet-install.sh --version "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_ROOT" \
  && rm /tmp/dotnet-install.sh

# Fetch the pinned pnpm (and the binary it runs) now, so the onSandboxReady
# hook doesn't download it.
# Keep the version in step with "packageManager" in package.json.
RUN corepack install -g pnpm@12.6.0 && pnpm --version

# Add Claude, .NET and its global tools to PATH
ENV PATH="/home/agent/.local/bin:$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

WORKDIR /home/agent

# In worktree sandbox mode, Sandcastle bind-mounts the git worktree at /home/agent/workspace
# and overrides the working directory to /home/agent/workspace at container start.
# Structure your Dockerfile so that /home/agent/workspace can serve as the project root.
ENTRYPOINT ["sleep", "infinity"]

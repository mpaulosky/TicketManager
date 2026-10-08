FROM node:22-bookworm

# Install system dependencies. python3 runs the test discovery
# .sandcastle/check.sh shares with CI (.github/scripts/discover_tests.py); the
# lib* packages are what the .NET SDK needs on Debian. There's no GitHub CLI
# and no Docker or browser: the sandbox never talks to GitHub (see main.mts),
# and check.sh skips the tests that need Docker or a browser, which CI runs.
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

# Install the .NET SDK that .sandcastle/check.sh builds and tests with.
# global.json pins 10.0.x with rollForward latestMinor, so the latest GA SDK on
# the 10.0 channel satisfies it, and keeps satisfying it as Dependabot bumps
# global.json within 10.0. Rebuild the image to pick up a newer SDK; change the
# channel when global.json moves to a new major version.
ARG DOTNET_CHANNEL=10.0
ENV DOTNET_ROOT=/home/agent/.dotnet \
  DOTNET_CLI_TELEMETRY_OPTOUT=1 \
  DOTNET_NOLOGO=1
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
  && bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --quality GA --install-dir "$DOTNET_ROOT" \
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

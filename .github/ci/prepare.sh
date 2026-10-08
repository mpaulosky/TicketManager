#!/usr/bin/env bash
# Repo-specific CI setup, called by ci.yml after it restores and before it builds.
#
#   prepare.sh build              in the "Build Solution" job
#   prepare.sh test <test-name>   in each "Tests: <test-name>" matrix job
#
# ci.yml is Owned by the repo-ci-baseline Template and is overwritten on every
# Apply; this file is Seed, so it belongs to the repo. Put what only this repo
# needs here: tools the build runs (for example `corepack enable` for pnpm),
# images to pull or projects to publish before a test project runs. To pass
# environment variables to the later steps, append NAME=value lines to
# "$GITHUB_ENV".
#
# The test name comes from a file name in the PR, so treat it as data: quote
# it and never eval it.
set -euo pipefail

job="${1:?usage: prepare.sh build|test [test-name]}"
test_name="${2:-}"

# Building src/Web runs the Tailwind CSS build through pnpm, and every test
# project builds it too. Under CI the csproj skips its own `pnpm install`, so
# install here. Corepack provides the pnpm version pinned by "packageManager"
# in src/Web/package.json. The csproj's TailwindPnpmInstall target still
# runs under CI unless its stamp is newer than pnpm-lock.yaml, so touch it.
install_pnpm_packages() {
  export COREPACK_ENABLE_DOWNLOAD_PROMPT=0
  corepack enable
  (cd src/Web && pnpm install --frozen-lockfile && touch node_modules/.install-stamp)
}

# The tests rely on Node stripping the types from .mts files without a flag
# (Node 22.18 or later) and on globs in `node --test` (Node 21). Fail with a
# clear message on an older Node instead of a syntax error.
require_node_22_18() {
  if ! node -e 'const [a, b] = process.versions.node.split(".").map(Number); process.exit(a > 22 || (a === 22 && b >= 18) ? 0 : 1)'; then
    echo "The Sandcastle tests need Node 22.18 or later; found $(node --version)." >&2
    return 1
  fi
}

# Sandcastle's orchestration code (.sandcastle/): type-check it and run its
# tests. On a PR, only when it changes the paths the tests read (.sandcastle/,
# scripts/check-branch-name.sh) or the root package files, as the local gate's
# .github/ci/gate-checks.sh does (keep the two lists in step). On any other
# run (a push to main, a manual run) or without an origin/main to compare
# with, always: there HEAD is main, so the diff would always be empty. The
# tests run on the runner's own Node, which strips the types itself.
sandcastle_tests() {
  local base
  if [[ "${GITHUB_EVENT_NAME-}" == pull_request ]] \
    && base="$(git merge-base HEAD origin/main 2>/dev/null)" \
    && git diff --quiet --no-renames "$base" HEAD -- .sandcastle package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc scripts/check-branch-name.sh; then
    echo "No Sandcastle or root package changes to test."
    return
  fi
  echo "Sandcastle type check and tests (node $(node --version))"
  require_node_22_18
  pnpm install --frozen-lockfile
  pnpm run test:sandcastle
}

case "$job" in
  build) install_pnpm_packages; sandcastle_tests ;;
  test) : "$test_name"; install_pnpm_packages ;;
  *) echo "prepare.sh: unknown job '$job'" >&2; exit 2 ;;
esac

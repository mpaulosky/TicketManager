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

case "$job" in
  build) install_pnpm_packages ;;
  test) : "$test_name"; install_pnpm_packages ;;
  *) echo "prepare.sh: unknown job '$job'" >&2; exit 2 ;;
esac

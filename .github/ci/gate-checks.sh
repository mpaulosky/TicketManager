#!/usr/bin/env bash
# Repo-specific checks for the local gate, called by scripts/gate.sh after its
# lints and before it builds, so they run in pre-push, in any sandbox that
# runs the gate, and by hand.
#
#   gate-checks.sh <merge-base>
#
# <merge-base> is where this branch left origin/main, or empty when there is
# no origin/main; in that case treat every tracked file as changed. Exit
# non-zero to fail the gate.
#
# scripts/gate.sh is Owned by the repo-ci-baseline Template and is overwritten
# on every Apply; this file is Seed, so it belongs to the repo. Put checks only
# this repo needs here, each guarded by the paths it covers, for example:
#
#   if git diff --quiet "$base" HEAD -- src/Tool; then :; else pnpm run check:tool; fi
set -euo pipefail

base="${1-}"

# changed <pathspec>...: true when any matching path changed since <base>, or
# always when there's no base. Deletions count.
changed() {
  [[ -z "$base" ]] && return 0
  ! git diff --quiet --no-renames "$base" HEAD -- "$@"
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

# Sandcastle's orchestration code: type-check it and run its tests. CI's
# Build Solution job runs the same through .github/ci/prepare.sh. The paths are
# what the tests read (branches.test.mts runs scripts/check-branch-name.sh) and
# what pnpm install reads; keep them in step with prepare.sh's.
if changed .sandcastle package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc scripts/check-branch-name.sh; then
  echo "Sandcastle type check and tests (node $(node --version))"
  require_node_22_18
  pnpm install --frozen-lockfile
  pnpm run test:sandcastle
else
  echo "No Sandcastle or root package changes to check."
fi

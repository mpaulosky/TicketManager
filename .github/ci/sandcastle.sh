#!/usr/bin/env bash
# Shared by .github/ci/gate-checks.sh (the local gate) and .github/ci/prepare.sh
# (CI's Build Solution job), which source it, so the two decide alike when to
# type-check and test Sandcastle's orchestration code (.sandcastle/).

# What the Sandcastle tests read or run: the code itself, the root package
# files pnpm install reads, the branch-name and PR-title scripts
# branches.test.mts and publish.test.mts run, the
# scripts that run the tests (this one included) and the Template-owned files
# that call them: scripts/gate.sh, and ci.yml, which also picks the runner and
# so the Node the tests run on.
SANDCASTLE_PATHS=(
  .sandcastle
  package.json
  pnpm-lock.yaml
  pnpm-workspace.yaml
  .npmrc
  scripts/check-branch-name.sh
  scripts/check-pr-title.sh
  .github/ci/sandcastle.sh
  .github/ci/gate-checks.sh
  .github/ci/prepare.sh
  scripts/gate.sh
  .github/workflows/ci.yml
)

# sandcastle_changed_since <base>: true when any of SANDCASTLE_PATHS changed
# since <base>, or when <base> is empty. Deletions count.
sandcastle_changed_since() {
  [[ -z "${1-}" ]] && return 0
  ! git diff --quiet --no-renames "$1" HEAD -- "${SANDCASTLE_PATHS[@]}"
}

# Type-check .sandcastle/ and run its tests. They need Node to strip the types
# from .mts files without a flag (22.18 on the 22 line, 23.6 on 23) and globs
# in `node --test`, so fail with a clear message on an older Node instead of a
# syntax error.
run_sandcastle_tests() {
  echo "Sandcastle type check and tests (node $(node --version))"
  if ! node -e 'const [a, b] = process.versions.node.split(".").map(Number); process.exit(a > 23 || (a === 23 && b >= 6) || (a === 22 && b >= 18) ? 0 : 1)'; then
    echo "The Sandcastle tests need Node 22.18 or later (23.6 or later on Node 23); found $(node --version)." >&2
    return 1
  fi
  pnpm install --frozen-lockfile
  pnpm run test:sandcastle
}

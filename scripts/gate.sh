#!/usr/bin/env bash
# Local quality gate, run by .github/hooks/pre-push and safe to run by hand.
# Lints the Markdown, YAML, workflow and shell files changed since origin/main
# (every unpushed commit, with the same configs as CI), builds the solution,
# then runs each test project under tests/. Exits non-zero on the first
# failing gate.
set -euo pipefail

ROOT="$(git rev-parse --show-toplevel)"
cd "$ROOT"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; RESET='\033[0m'

step() { echo -e "\n${CYAN}▶ $1${RESET}"; }

# Files added, copied, modified or renamed since this branch left origin/main.
# Without an origin/main (a fresh clone of a fork, say) lint every tracked file.
if BASE="$(git merge-base HEAD origin/main 2>/dev/null)"; then
  CHANGED="$(git diff --name-only --diff-filter=ACMR "$BASE" HEAD)"
else
  CHANGED="$(git ls-files)"
fi

# The same exclusions as the Lint Markdown workflow's globs.
mapfile -t MD_FILES < <(grep -E '\.md$' <<< "$CHANGED" \
  | grep -Ev '(^|/)(node_modules|bin|obj)/|^\.copilot/|^\.github/(agents|skills)/|^\.github/copilot-instructions\.md$' || true)
mapfile -t YAML_FILES < <(grep -E '\.ya?ml$' <<< "$CHANGED" | grep -Ev '^\.squad/' || true)
mapfile -t WORKFLOW_FILES < <(grep -E '^\.github/(workflows/[^/]+\.ya?ml|dependabot\.ya?ml|actionlint\.ya?ml|zizmor\.ya?ml)$' <<< "$CHANGED" || true)
# Shell scripts, plus extensionless scripts and git hooks.
mapfile -t SHELL_FILES < <(grep -E '\.sh$|^scripts/[^/.]+$|^\.github/hooks/((pre|post)-[a-z-]+|(prepare-)?commit-msg)$' <<< "$CHANGED" || true)

# An installed markdownlint-cli2 (as the pre-commit hook uses) when there is
# one; otherwise this pinned version, never whatever the registry serves at push time.
MARKDOWNLINT_CLI2_VERSION="0.23.3"
# The same for the workflow and shell linters. The fallbacks run through uvx or
# docker, so an installed tool only needs to be on PATH to be preferred. Keep
# these in step with .github/workflows/lint-actions.yml.
ACTIONLINT_VERSION="1.7.12"
ZIZMOR_VERSION="1.30.1"
SHELLCHECK_VERSION="v0.11.0"

# Fallback probes: can the fallback actually run here? A docker binary with a
# stopped daemon can't, and must skip rather than fail the gate.
have_docker() { docker info &>/dev/null; }
have_uvx() { command -v uvx &>/dev/null; }

# Runs an installed tool, else its fallback command, else warns that the check
# is skipped locally (CI's Lint Actions workflow still runs it).
# Usage: run_tool <name> <install hint> <probe function> <fallback command...> -- <args...>
run_tool() {
  local name="$1" hint="$2" probe="$3"; shift 3
  local fallback=()
  while [[ $# -gt 0 && "$1" != "--" ]]; do fallback+=("$1"); shift; done
  if [[ $# -eq 0 ]]; then
    echo "run_tool ${name}: missing -- before the tool arguments" >&2
    exit 2
  fi
  shift
  if command -v "$name" &>/dev/null; then
    "$name" "$@"
  elif "$probe"; then
    "${fallback[@]}" "$@"
  else
    echo -e "${YELLOW}⚠️  ${name} not found — skipping. CI's Lint Actions workflow still runs it.${RESET}"
    echo -e "   To enable: ${CYAN}${hint}${RESET}"
  fi
}

step "Markdown lint (${#MD_FILES[@]} changed file(s))"
if [[ ${#MD_FILES[@]} -gt 0 ]]; then
  if [[ -x "$ROOT/node_modules/.bin/markdownlint-cli2" ]]; then
    "$ROOT/node_modules/.bin/markdownlint-cli2" "${MD_FILES[@]}"
  elif command -v markdownlint-cli2 &>/dev/null; then
    markdownlint-cli2 "${MD_FILES[@]}"
  else
    npx --yes "markdownlint-cli2@${MARKDOWNLINT_CLI2_VERSION}" "${MD_FILES[@]}"
  fi
fi

step "YAML lint (${#YAML_FILES[@]} changed file(s))"
if [[ ${#YAML_FILES[@]} -gt 0 ]]; then
  # .yamllint.yml holds the same rules as the Lint YAML workflow's config_data.
  if command -v yamllint &>/dev/null; then
    yamllint -c .yamllint.yml "${YAML_FILES[@]}"
  elif have_docker; then
    docker run --rm -v "$ROOT:/work" -w /work cytopia/yamllint:latest \
      -c .yamllint.yml "${YAML_FILES[@]}"
  else
    echo -e "${YELLOW}⚠️  yamllint not found — skipping. CI's Lint YAML workflow still checks these files.${RESET}"
    echo -e "   To enable: ${CYAN}pipx install yamllint${RESET}"
  fi
fi

# actionlint checks workflow syntax, expressions and contexts, and lints
# every run: block with shellcheck. zizmor audits the workflows for security
# problems: script injection, over-broad permissions, unpinned actions.
step "Workflow lint (${#WORKFLOW_FILES[@]} changed file(s))"
if [[ ${#WORKFLOW_FILES[@]} -gt 0 ]]; then
  # No file arguments: actionlint finds every workflow, and zizmor audits the
  # whole repo (workflows and dependabot.yml), as CI does.
  run_tool actionlint "install actionlint (github.com/rhysd/actionlint), or Docker" have_docker \
    docker run --rm -v "$ROOT:/repo" -w /repo "rhysd/actionlint:${ACTIONLINT_VERSION}" \
    --
  # --offline: no GitHub token needed locally. CI also runs the online audits.
  run_tool zizmor "pipx install zizmor, or install uv" have_uvx \
    uvx "zizmor@${ZIZMOR_VERSION}" \
    -- --offline --min-severity medium .
fi

step "Shell lint (${#SHELL_FILES[@]} changed file(s))"
if [[ ${#SHELL_FILES[@]} -gt 0 ]]; then
  run_tool shellcheck "install shellcheck, or Docker" have_docker \
    docker run --rm -v "$ROOT:/mnt" -w /mnt "koalaman/shellcheck:${SHELLCHECK_VERSION}" \
    -- "${SHELL_FILES[@]}"
fi

# Git exports GIT_DIR and friends to hooks; dotnet (and the tools it runs)
# must see the repo as a normal checkout.
DOTNET_ENV=(env -u GIT_DIR -u GIT_WORK_TREE -u GIT_INDEX_FILE -u GIT_PREFIX)

step "Build"
"${DOTNET_ENV[@]}" dotnet build TicketManager.slnx --configuration Release --nologo --verbosity minimal

step "Tests"
# Discover each test project explicitly. The repo uses Microsoft Testing Platform
# for .NET 10, and running the solution file directly can report "Zero tests ran"
# even while the project-level test runs pass.
mapfile -d '' -t TEST_PROJECTS < <(find "$ROOT/tests" -type f -name '*.csproj' -print0 | sort -z)

if [[ ${#TEST_PROJECTS[@]} -eq 0 ]]; then
  echo -e "${RED}❌ No test project files were found under '$ROOT/tests'.${RESET}"
  exit 1
fi

for project_file in "${TEST_PROJECTS[@]}"; do
  project_name="$(basename "${project_file%.*}")"
  assembly_path="$(dirname "$project_file")/bin/Release/net10.0/${project_name}.dll"
  echo -e "${CYAN}▶ Testing ${project_file#"$ROOT"/}...${RESET}"

  if "${DOTNET_ENV[@]}" dotnet test "$project_file" --configuration Release --no-build \
    --results-directory "$ROOT/.tmp-test-results" --report-xunit-trx \
    --report-xunit-trx-filename "${project_name}.trx"; then
    continue
  fi

  if [[ -f "$assembly_path" ]]; then
    echo -e "${YELLOW}⚠️ dotnet test reported a runner issue for ${project_name}; falling back to the built test assembly.${RESET}"
    if ! "${DOTNET_ENV[@]}" dotnet "$assembly_path"; then
      echo -e "${RED}❌ Tests failed for '${project_file#"$ROOT"/}' using the direct runner fallback.${RESET}"
      exit 1
    fi
    continue
  fi

  echo -e "${RED}❌ Tests failed for '${project_file#"$ROOT"/}'.${RESET}"
  exit 1
done

echo -e "\n${GREEN}✅ Gate passed.${RESET}"

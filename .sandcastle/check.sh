#!/usr/bin/env bash
# The check Sandcastle's agents run before they finish, and the host runs in
# the sandbox before it publishes a branch: the exit code decides, never what
# an agent reports. It builds the solution and runs every test project but the
# ones that need Docker or a browser, which the sandbox doesn't have. CI runs
# the full .NET suite on the pull request, and scripts/gate.sh does before a
# push by hand. The host runs main's copy of this file (see lib/check.mts).
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"
root="$PWD"

# Packages whose tests need Docker (Testcontainers, the Aspire AppHost) or a
# browser (Playwright).
sandbox_unfriendly='Include="(Testcontainers[^"]*|Aspire\.Hosting\.Testing|Microsoft\.Playwright[^"]*)"'

# needs_host <project.csproj>: true when the project, a project it references
# (at any depth), or a Directory.Build.props/.targets above any of them
# references one of those packages. The list is derived, not kept by hand, so a
# new test project that needs Docker or a browser is skipped here without an edit.
declare -A seen=()
needs_host() {
  local project dir file reference
  project="$(realpath -m "$1")"
  [[ -n "${seen[$project]:-}" ]] && return 1
  seen[$project]=1
  [[ -f "$project" ]] || return 1
  grep -Eq "$sandbox_unfriendly" "$project" && return 0

  dir="$(dirname "$project")"
  while [[ "$dir" == "$root"* ]]; do
    for file in "$dir/Directory.Build.props" "$dir/Directory.Build.targets"; do
      [[ -f "$file" ]] && grep -Eq "$sandbox_unfriendly" "$file" && return 0
    done
    [[ "$dir" == "$root" ]] && break
    dir="$(dirname "$dir")"
  done

  while IFS= read -r reference; do
    needs_host "$(dirname "$project")/${reference//\\//}" && return 0
  done < <(grep -oE '<ProjectReference[^>]*Include="[^"]+"' "$project" | sed -E 's/.*Include="([^"]+)"/\1/')
  return 1
}

# global.json names the SDK; the image has whichever was current when it was
# built. Say which one to fetch rather than fail every build.
if ! dotnet --version >/dev/null 2>&1; then
  echo "This image's .NET SDK ($(dotnet --list-sdks | tr '\n' ' ')) doesn't satisfy global.json." >&2
  echo "Rebuild the Sandcastle image without the cache to fetch a newer one." >&2
  exit 1
fi

echo "▶ Build"
mapfile -t solutions < <(find . -maxdepth 1 -name '*.slnx')
dotnet build "${solutions[@]}" --configuration Release -warnaserror

echo "▶ Tests (without Docker or a browser)"
mapfile -t projects < <(python3 .github/scripts/discover_tests.py --list | grep . || true)
if [[ ${#projects[@]} -eq 0 ]]; then
  echo "No test projects found under tests/." >&2
  exit 1
fi
for project in "${projects[@]}"; do
  seen=()
  if needs_host "$project"; then
    echo "Skipping ${project}: it needs Docker or a browser. CI runs it."
    continue
  fi
  dotnet test "$project" --configuration Release
done

echo "▶ Sandcastle tests"
pnpm run test:sandcastle

echo "✅ Sandcastle check passed."

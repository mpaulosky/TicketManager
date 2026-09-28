// Decides what the PR auto-merge workflow should do with a pull request.
//
// GitHub's native auto-merge is meant to be armed while a PR is still waiting
// on its requirements: GitHub then merges it the moment the ruleset's required
// checks pass. So arm it as early as possible, and let the ruleset decide what
// "ready" means. The one exception is a PR that is already CLEAN: GitHub
// refuses to arm auto-merge then ("clean status"), so it is merged directly.

/**
 * @param {{
 *   state: string,              // GraphQL PullRequestState: OPEN | CLOSED | MERGED
 *   merged: boolean,
 *   isDraft: boolean,
 *   isSameRepo: boolean,        // head branch lives in this repository
 *   mergeable: string,          // MERGEABLE | CONFLICTING | UNKNOWN
 *   mergeStateStatus: string,   // CLEAN | BLOCKED | UNSTABLE | BEHIND | DIRTY | UNKNOWN | ...
 *   autoMergeEnabled: boolean,
 * }} pr
 * @returns {{ action: "skip" | "enable" | "merge", reason: string }}
 */
export function decideAutoMerge(pr) {
  if (pr.state !== "OPEN" || pr.merged) {
    return { action: "skip", reason: `PR is ${pr.merged ? "merged" : pr.state.toLowerCase()}` };
  }
  if (pr.isDraft) {
    return { action: "skip", reason: "PR is a draft" };
  }
  if (!pr.isSameRepo) {
    return { action: "skip", reason: "PR comes from a fork" };
  }
  if (pr.autoMergeEnabled) {
    return { action: "skip", reason: "auto-merge is already armed" };
  }
  if (pr.mergeable === "CONFLICTING" || pr.mergeStateStatus === "DIRTY") {
    return { action: "skip", reason: "PR has merge conflicts" };
  }
  if (pr.mergeStateStatus === "CLEAN") {
    return { action: "merge", reason: "PR already meets every requirement" };
  }
  return {
    action: "enable",
    reason: `waiting on requirements (mergeStateStatus=${pr.mergeStateStatus})`,
  };
}

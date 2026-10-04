# Squad is removed; the repo runs on the repo-ci-baseline

TicketManager was built with the squad-team framework: a coordinator agent (`.github/agents/squad.agent.md`) cast a team of named agents whose
state lived in an untracked `.squad/` directory. Earlier PRs had already renamed its `squad-` workflows and dropped its branch cleanup, leaving only
the coordinator agent and a few `.squad/` excludes. Those are now removed too, so the repo can take the shared repo-ci-baseline Template, which
carries no squad support.

No decisions moved over: `.squad/decisions/` held nothing, and the agent histories were session trail, not decisions about the system.

## Considered Options

- **Keep squad alongside the Baseline**: rejected, because the Baseline's hooks, CI and auto-merge replace what squad's workflows did, and the
  coordinator's rules would contradict them.
- **Fold the removal into the Standardize PR**: rejected, so that PR stays exactly the Template's output plus its Adapt commits.

## Consequences

- `.squad/` is no longer ignored. If squad is reinstalled, its state would show up as untracked files.
- Release blog posts that mention squad stay as they are: they are dated history.

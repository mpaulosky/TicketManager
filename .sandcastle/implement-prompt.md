# TASK

Fix issue {{TASK_ID}}: {{ISSUE_TITLE}}

Pull in the issue using `gh issue view <ID>`. If it has a parent PRD, pull that in too.

Only work on the issue specified.

Work on branch {{BRANCH}}. Make commits and run tests.

# CONTEXT

Here are the last 10 commits:

<recent-commits>

!`git log -n 10 --format="%H%n%ad%n%B---" --date=short`

</recent-commits>

# EXPLORATION

Explore the repo and fill your context window with relevant information that will allow you to complete the task.

Pay extra attention to test files that touch the relevant parts of the code.

# EXECUTION

If applicable, use RGR to complete the task.

1. RED: write one test
2. GREEN: write the implementation to pass that test
3. REPEAT until done
4. REFACTOR the code

# FEEDBACK LOOPS

Before committing, run `scripts/gate.sh` to ensure the lints, the build and the tests pass. It is the gate the
pre-push hook and CI run: it builds the solution with `dotnet build` and runs every test project with `dotnet test`.

# COMMIT

Make a git commit that follows `.github/instructions/git-commit-instructions.md`:

1. A subject line `<type>(<scope>): <Summary>`, such as `fix(Web): Return an empty list when a ticket has no comments`:
   imperative, capitalized, no closing period, 72 characters or fewer. The scope is the affected project or folder
   (`Web`, `AppHost`, `Tests`, `ci`, `docs`)
2. A body, wrapped at 72 characters, that gives the task completed and its PRD reference, the key decisions made, the
   files changed, and any blockers or notes for the next iteration
3. `Refs #{{TASK_ID}}` as the body's last line

Keep it concise. Never use `--no-verify`.

# THE ISSUE

If the task is not complete, leave a comment on the issue with what was done.

Do not close the issue - this will be done later.

Once complete, output <promise>COMPLETE</promise>.

# FINAL RULES

ONLY WORK ON A SINGLE TASK.

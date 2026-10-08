# TASK

Merge the following branches into the current branch:

{{BRANCHES}}

For each branch:

1. Write the merge's commit message to a file outside the repo, such as `/tmp/merge-msg.txt`, following
   `.github/instructions/git-commit-instructions.md`: a `<type>(<scope>): <Summary>` subject (imperative, capitalized,
   no closing period, 72 characters or fewer), such as `chore(sandcastle): Merge #12, sort tickets by date`, with the
   issue number and a shortened issue title rather than the branch name, which can be too long. Put the full branch
   name in the body, and `Refs #<ID>` as the body's last line, where `<ID>` is the branch's issue. Use a file, not
   `-m "…"`, so the shell never expands backticks or `$` from an issue title
2. Run `git merge --no-ff -F /tmp/merge-msg.txt <branch>`. A clean merge is committed with that message rather than
   Git's default `Merge branch '…'`
3. If there are merge conflicts, resolve them intelligently by reading both sides and choosing the correct
   resolution, then conclude the merge with `git commit -F /tmp/merge-msg.txt`, so the message doesn't pick up Git's
   `# Conflicts:` lines
4. Run `scripts/gate.sh` (the lints, `dotnet build` and `dotnet test`) to verify everything works. Run it after the
   merge is committed: its lints only check committed changes
5. If the gate fails, fix the issues and fold the fix into the merge commit with `git commit --amend --no-edit`, then
   run the gate again before proceeding to the next branch

Never use `--no-verify`.

# CLOSE ISSUES

For each branch that was merged, close its issue using the following command:

`gh issue close <ID> --comment "Completed by Sandcastle"`

Here are all the issues:

{{ISSUES}}

Once you've merged everything you can, output <promise>COMPLETE</promise>.

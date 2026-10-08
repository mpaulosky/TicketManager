# TASK

Merge the following branches into the current branch:

{{BRANCHES}}

For each branch:

1. Run `git merge --no-ff --no-commit <branch>`, so Git doesn't commit the merge with its default `Merge branch '…'`
   message
2. If there are merge conflicts, resolve them intelligently by reading both sides and choosing the correct resolution
3. Commit the merge with a message that follows `.github/instructions/git-commit-instructions.md`: a
   `<type>(<scope>): <Summary>` subject (imperative, capitalized, no closing period), such as
   `chore(sandcastle): Merge feature/12-sort-tickets`, and `Refs #<ID>` as the body's last line, where `<ID>` is the
   branch's issue. Never use `--no-verify`
4. Run `scripts/gate.sh` (the lints, `dotnet build` and `dotnet test`) to verify everything works. Run it after the
   commit: its lints only check committed changes
5. If the gate fails, fix the issues and fold the fix into the merge commit with `git commit --amend --no-edit`, then
   run the gate again before proceeding to the next branch

# CLOSE ISSUES

For each branch that was merged, close its issue using the following command:

`gh issue close <ID> --comment "Completed by Sandcastle"`

Here are all the issues:

{{ISSUES}}

Once you've merged everything you can, output <promise>COMPLETE</promise>.

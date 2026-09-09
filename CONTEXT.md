# TicketManager

TicketManager lets a user manage GitHub Issues through GitHub Projects without leaving the app. GitHub is the sole system of record — this context adds no data of its own, only a workflow and vocabulary layered on top of GitHub's.

## Language

**Ticket**:
This app's term for a GitHub Issue as presented and acted on inside TicketManager. A Ticket is always backed 1:1 by a real GitHub Issue; TicketManager never stores Ticket data of its own.
_Avoid_: Issue (reserve "Issue" for talking about the underlying GitHub API/object; use "Ticket" for the app's domain language)

**PR**:
This App's term for GitHub Pull Request as presented and acted on inside Ticket. A PR is always backed 1:1 by real GitHub Pull Requests. Ticket Manager never stores PR's data of its own.
_Avoid_: Pull Request (reserve "Pull Request" for talking about the underlying GitHub API/object; use "PR" for the app's domain language)

**Project**:
A GitHub Project the authenticated user has access to. TicketManager lists every Project the user can see across their account and orgs, fetched live from GitHub — there is no separate app-side list of Projects to configure.
_Avoid_: Board

**Current Project**:
The single Project TicketManager is showing Tickets and Pull Requests for right now. There is always exactly one Current Project (never zero, never several at once); switching Projects replaces it outright. Because Triage writes go straight to GitHub with no local draft, switching the Current Project has nothing to save or lose.
_Avoid_: Active project, selected project

**Repository**:
The GitHub repo that a Ticket's underlying Issue actually belongs to. A Current Project can span multiple Repositories, but Labels are scoped to a Repository, not a Project — so the Labels offered when Triaging a Ticket always come from that Ticket's own Repository, never merged across the Current Project's other Repositories.
_Avoid_: Repo (fine in casual speech, but use "Repository" in writing for clarity against "Project")

**Label**:
An existing GitHub label on a Ticket's Repository that can be applied to or removed from the Ticket during Triage. TicketManager only offers Labels that already exist on GitHub — it never creates, renames, or deletes a Label.
_Avoid_: Tag

**Untriaged**:
The state of a Ticket that has not yet been through Triage — typically a newly created Issue with no labels or assignees set via TicketManager. Untriaged is evaluated only within the Current Project; TicketManager does not track whether a Ticket also appears, triaged or not, in some other GitHub Project.
_Avoid_: New, unprocessed

**Triage**:
The workflow performed on an Untriaged Ticket: adding/removing labels and adding/removing Assignees. Triage only exercises GitHub's own Issue capabilities — TicketManager does not add fields or states GitHub doesn't already support. Editing the Body is not part of Triage specifically — see Body.
_Avoid_: Processing, review (review implies a GitHub PR-review feature that Issues don't have and this app doesn't use)

**Assignee**:
A person assigned to a Ticket via GitHub's Assignees field. GitHub exposes one Assignees list per Issue with no distinction between roles — TicketManager does not layer an "Owner" vs "Reviewer" role on top of it. A user asking to "assign an owner" or "request review" both resolve to the same action: add an Assignee.
_Avoid_: Owner, Reviewer (not modeled as distinct roles — both are just intents behind adding an Assignee)

**Body**:
The free-text Markdown content of a Ticket's underlying Issue. Editing the Body is a general Ticket capability available at any time, on any Ticket, regardless of Triage state — not something exposed only during Triage. Like labels and Assignees, edits autosave straight to GitHub with no local draft, so there's nothing to lose when switching the Current Project mid-edit.
_Avoid_: Description, content

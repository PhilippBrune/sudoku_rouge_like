# Tickets

Jira-style ticket notes for requests handled in this workspace.

## Rules
- Create one ticket per user request.
- Before creating a ticket, search `tickets/**/*.md` for similar scope.
- Keep tickets grouped by topic folder (for example `tickets/ui/`, `tickets/audio/`, `tickets/run/`).
- Use fields: `status`, `summary`, `description`, `attachments`.

## Ticket ID format
- `JIRA-0001`, `JIRA-0002`, ...

## Minimal template
```
id: JIRA-0001
status: TODO | IN PROGRESS | DONE | BLOCKED
summary: <short one-line summary>
description:
- <requested changes>
- <implementation notes>
attachments:
- <file/screenshot reference>
```

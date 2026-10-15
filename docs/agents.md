# Using Quiet PC from an assistant

The person controls whether their PC should be quiet. Use the command interface when they ask for it, or when quiet mode is already part of an agreed workflow. Do not silently mute their PC because you infer that they should concentrate.

1. Read `status` before making a change.
2. Prefer `for -Minutes N` for a bounded request, or explicit `on`/`off`.
3. Check the exit code and JSON reply. Do not report success on a timeout or `Ok: false`.
4. If a reply is lost, inspect state before retrying. A retry of `toggle` changes direction; a retry of `for` moves the deadline.
5. Do not edit recovery files or Windows audio/notification settings directly to work around an app error.

Examples:

```powershell
# User: “Keep the PC quiet for an hour.”
.\scripts\quietpc.ps1 for -Minutes 60

# User: “I need sound back.”
.\scripts\quietpc.ps1 off
```

If quiet mode was already active, starting a timer changes its duration. Check that this matches the request. Do not switch quiet mode off at the end of an unrelated task if the user had enabled it before that task began.

Diagnostics contain local device identifiers. Ask for a useful symptom and a minimal status report before requesting a full diagnostic file. Never include recovery files in a public issue by default.

Possible future integrations—session ownership, scoped leases, an MCP server and policy-controlled actions—are proposals in [roadmap](roadmap.md). They are not implemented.

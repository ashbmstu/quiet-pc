# Assistants

Change quiet mode only when requested or included in an agreed workflow.

1. Read `status`.
2. Use `for -Minutes N`, `on` or `off`.
3. Check the exit code and `Ok`.
4. After a timeout, inspect state before retrying. `toggle` reverses state; `for` resets the deadline.
5. Do not edit recovery files or bypass controller errors.

```powershell
.\scripts\quietpc.ps1 for -Minutes 60
.\scripts\quietpc.ps1 off
```

Preserve pre-existing sessions unless asked to change them. Manual unmute releases that output for the session; `ManualAudioDevices` reports the count.

Diagnostics contain local identifiers. Review before sharing.

Session tokens and MCP support are [proposals](roadmap.md).

# Command reference

Use `scripts/quietpc.ps1` from PowerShell. It waits for the command process, prints JSON, propagates an error exit code, and removes its temporary reply file.

```powershell
.\scripts\quietpc.ps1 status
.\scripts\quietpc.ps1 for -Minutes 45
.\scripts\quietpc.ps1 cancel-timer
.\scripts\quietpc.ps1 off
```

| Wrapper command | Executable argument | Effect |
|---|---|---|
| `status` | `--status` | Read saved state, preferences and whether the controller is running. No mutation or launch. |
| `on` | `--on` | Quiet on indefinitely; remove any existing timer. |
| `for -Minutes N` | `--for N` | Quiet on for N whole minutes, 1–10,080. Replace an existing timer. |
| `off` | `--off` | Restore now and clear timer. Safe to repeat. |
| `toggle` | `--toggle` | Invert quiet mode. Turning on starts without a timer. |
| `cancel-timer` | `--cancel-timer` | Keep quiet on, remove its deadline. No effect when off. |
| `settings` | `--settings` | Open settings. |
| `timer` | `--timer` | Open timer controls. |
| `exit` | `--exit` | Restore and exit; fail if restoration is pending. |
| `diagnose` | `--diagnose` | Read actual Windows notification/audio state and recovery data. Device IDs are included. |

Control commands start the tray controller if it is not running. `status` and `diagnose` do not. `--background` runs the persistent controller and is used by Windows startup. `--handoff` is reserved for the installer: it stops the controller without restoring, so the replacement process can resume the same session.

## Direct executable use

Quiet PC is a GUI executable. Shells differ in whether they wait for it or retain its standard output. Prefer the wrapper, or pass `--output` explicitly:

```powershell
$exe = Join-Path $env:LOCALAPPDATA 'Programs\QuietPC\QuietPC.exe'
$reply = Join-Path $env:TEMP 'quiet-pc-reply.json'
$p = Start-Process $exe -ArgumentList ('--for 30 --output "' + $reply + '"') -Wait -PassThru
Get-Content -LiteralPath $reply
$p.ExitCode
```

Exit code **0** means the request completed successfully. **1** means invalid input, unavailable controller, or a setting that could not be applied. A timeout has an uncertain outcome: read `status` before deciding whether to retry. Repeating `for` restarts the countdown from the retry time; repeating `toggle` may undo the first request.

Control replies:

```json
{
  "Ok": true,
  "Error": "",
  "Quiet": true,
  "EndsAtUtc": "2026-01-01T12:30:00.0000000+00:00",
  "PendingAudioRestores": 0,
  "PendingNotificationRestore": false
}
```

`status` also includes `Running`, `Settings` and `Startup`. It reads the persisted state; it does not enforce an expired deadline or certify that hardware is silent. A saved active session with `Running: false` needs the controller to run before it can restore. Use `diagnose` when you need actual endpoint mute values.

Commands are sent through a local named pipe restricted to the current Windows user, with a separate pipe per interactive session. There is no TCP port or web service. Replies describe application state, not message contents or audio content.

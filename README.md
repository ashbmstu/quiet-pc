<p align="center"><img src="assets/banner.svg" alt="Quiet PC — one switch for notifications and audio" width="860"></p>

# Quiet PC

A Windows tray app that switches off notifications and mutes playback audio together. Click again to restore the settings you had before.

Use it for a call, a recording, a focused work session, or any time the PC should stop interrupting you. It runs locally, without an account, telemetry, or administrator access.

## What it does

- One click toggles quiet mode. **Ctrl + Alt + Q** does the same.
- **30 minutes**, **1 hour**, or a custom duration from **1 minute to 7 days**. Settings restore automatically when the timer ends.
- Mutes every active Windows playback device and notices newly connected devices.
- Restores each device's previous mute state. Devices that were already muted stay muted; volume levels are unchanged.
- Keeps the tray icon visible and can start at sign-in.
- Saves recovery state before changing Windows settings. A restart does not lose the original settings or reset a timer.
- Offers a local command interface with JSON replies for scripts and assistants.

## Install a release archive

Extract the archive and run `scripts/install.ps1` from its folder. It uses the included executable. Release builds are unsigned unless a release explicitly says otherwise. If no release is published yet, build from source below.

## Build and install from source

Requirements: Windows x64 with .NET Framework 4.x. The build uses the C# compiler included with Windows; no SDK or NuGet packages are needed. Tested on Windows 11 24H2, build 26100. Other Windows versions need testing, particularly the notification control.

From a PowerShell terminal in this folder:

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
.\scripts\install.ps1
```

The installer copies the app into `%LOCALAPPDATA%\Programs\QuietPC`, creates Desktop and Start menu shortcuts, and enables sign-in startup on a new installation. An upgrade keeps your startup preference and current quiet session.

## Use it

| Control | Action |
|---|---|
| Left-click moon | Toggle quiet mode |
| Right-click → Quiet for | Start 30 minutes, 1 hour, or a custom timer |
| Right-click → Timer | See the deadline, replace the timer, or restore now |
| No timer | Remove the deadline without restoring yet |
| Right-click → Settings | Choose audio/notifications, startup, visibility, resume behaviour and shortcut |
| Restore & exit | Restore first, then close |

Indigo moon means quiet is on. A crossed slate moon means off. Amber with an exclamation means a setting needs attention. Hover over the icon to see the remaining time.

A new timer counts from now and replaces the old deadline. It keeps the original restoration snapshot. Changing which things get muted in Settings takes effect next time quiet mode starts.

## Use it from a script or assistant

```powershell
# Inspect without starting the tray app or changing settings.
.\scripts\quietpc.ps1 status

# Quiet for 30 minutes; return JSON after the command is applied.
.\scripts\quietpc.ps1 for -Minutes 30

# Restore now.
.\scripts\quietpc.ps1 off
```

Prefer explicit `on`, `off`, or `for` commands in automation. `toggle` depends on the current state. Read the returned `Ok` value and process exit code. See [command reference](docs/commands.md) and [guidance for assistants](docs/agents.md).

## Limits to know

Windows notifications and normal Windows playback audio are covered. App-drawn pop-ups, modal dialogues, ASIO and some exclusive-mode audio can bypass those controls. This is not a hardware mute switch.

The app must be running to act at a deadline. If the PC is asleep, off, or the app is closed when time runs out, restoration happens when the app next runs or resumes. A disconnected audio device is restored when it reconnects. New devices can briefly play before Windows delivers their device event.

Notification control uses an internal Windows interface. Future Windows updates may require a compatibility fix. The app reports failures instead of presenting a successful quiet state.

## Remove it

```powershell
.\scripts\uninstall.ps1
```

Uninstall restores settings before removing startup entries and program files. It refuses to proceed if restoration is still pending. Preferences and recovery history stay in `%LOCALAPPDATA%\QuietPC`.

## Project notes

- [User guide and troubleshooting](docs/user-guide.md)
- [Commands](docs/commands.md)
- [Architecture and recovery](docs/architecture.md)
- [Enhancement ideas](docs/roadmap.md)
- [Branding](docs/branding.md)
- [Build and contribution guide](CONTRIBUTING.md)
- [Release checklist](docs/releasing.md)

Licensed under [MIT](LICENSE).

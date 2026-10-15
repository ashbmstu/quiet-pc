# Contributing

Build on Windows x64:

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
```

The project uses C# syntax accepted by the .NET Framework compiler shipped with Windows. Keep it buildable without a separate SDK, network restore or NuGet dependency unless there is a clear reason to change that requirement.

`scripts/test.ps1` uses fake audio and notification controls. It does not mute the PC. `-Native` also runs a live round trip and refuses to run while the tray controller is active. Use live tests deliberately on a desktop where a brief mute is acceptable.

For a UI change, check the real window, keyboard navigation, long labels and scaling. For a state change, test restoration and failure recovery, not just the happy path. Do not modify the current user's preferences as a test fixture.

Keep these invariants:

1. Save original settings before changing Windows.
2. Never replace a session's original snapshot when extending or replacing a timer.
3. Retain failed or disconnected-device restorations.
4. Never report complete silence when a Windows operation failed.
5. Keep machine-specific files, IDs, paths and logs out of Git.

Write issues around a reproducible symptom: Windows build, app version, steps, expected result, actual result. Review diagnostic output before attaching it. Explain the change and how you checked it in a pull request; screenshots help for UI changes.

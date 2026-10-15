# Contributing

Windows x64; C# 5 and the bundled .NET Framework compiler. No SDK or NuGet dependencies.

```powershell
.\scripts\build.ps1
.\scripts\test.ps1
```

Tests use fake devices. `-Native` changes real audio and notifications; exit the tray app first.

## Requirements

- Save original settings before mutation.
- Preserve the snapshot when replacing a timer.
- Respect manual audio changes.
- Retain failed and disconnected-device restorations.
- Report Windows errors.
- Exclude local state, device IDs, paths and logs from Git.
- Check UI layout, keyboard use and scaling after visual changes.
- Test restoration and failure recovery after state changes.

Issues: Windows build, app version, steps, expected and actual result. Remove private data from diagnostics.

Pull requests: state the change and validation. Add screenshots for UI changes. Keep documentation and commit messages short and factual.

# Architecture and recovery

Quiet PC is a C# WinForms executable built against the .NET Framework included with Windows. It has no third-party runtime dependencies and makes no network requests.

| File | Responsibility |
|---|---|
| `src/Program.cs` | Controller lifetime, tray actions, hotkeys, status and windows |
| `src/QuietEngine.cs` | Quiet session, persisted deadline, snapshot and restoration |
| `src/AudioControl.cs` | Windows Core Audio playback endpoints and device events |
| `src/NotificationControl.cs` | Global notification setting through Windows COM |
| `src/CommandChannel.cs` | Local per-user command pipe and acknowledged replies |
| `src/Settings.cs` | Preferences, startup and icon promotion |
| `src/SettingsWindow.cs` | Settings UI |
| `src/SessionWindow.cs` | Timer UI |
| `src/Brand.cs` | Icon rendering and UI colours |

## State transitions

Starting quiet mode reads the selected Windows settings, writes a durable snapshot, then changes the settings. State writes use a temporary file, flush it to disk, and atomically replace the previous file. Failure to save prevents the corresponding new mutation.

The snapshot contains the original global notification setting, each captured device's mute state, audio ownership, the active session's audio option, and an optional UTC deadline. Preferences for future sessions are stored separately. Changing preferences while active does not change the snapshot.

Audio ownership is per output: an absent entry needs its first mute; `true` means Quiet PC has applied mute; `false` means an external unmute has released control. Once released, the app neither mutes nor restores that output during the session. A new session resets ownership. Ownership changes are persisted and retried after a write error, without reversing a manual unmute. An unmute observed during restoration is also respected. Older active snapshots without ownership entries are treated as already controlled.

A new timer on an active session changes only its deadline. On every one-second refresh, expiry is checked before quiet settings are re-applied. An expired deadline calls the same restoration path as manual off. The timer runs on elapsed wall-clock time, so system-clock changes can affect it.

Restoration marks the session inactive first, then restores notification and audio settings. Successfully restored entries are removed from the pending set. Unavailable devices stay pending and are retried when they return. An error does not erase the saved original value.

Normal exit restores. Sign-out, restart and update handoff retain state. On startup, an overdue timer restores immediately. A user preference can instead request restoration on every startup.

## Windows interfaces

Audio uses `IMMDeviceEnumerator` and `IAudioEndpointVolume`, with endpoint callbacks and a one-second reconciliation pass. The app does not change volume levels, microphones, app mixer settings or default devices. Software mute may not affect exclusive-mode/ASIO audio.

Notifications use `INotificationSettings` and its global setting 0. This is an internal interface, verified on Windows 11 build 26100; compatibility is not guaranteed by Microsoft. The source includes its ABI reference. Direct registry changes were avoided because they may not apply immediately.

Icon promotion uses the current app's `NotifyIconSettings` entry. It is also Windows implementation detail, so verify it after shell updates.

## Storage and privacy

`%LOCALAPPDATA%\QuietPC` contains preferences, recovery state and a bounded local log. The executable lives separately under `%LOCALAPPDATA%\Programs\QuietPC`. Runtime state is never part of the source repository or release archive.

An optional `integration.json` can point to a local Markdown automation register. It is only for machines that need this bookkeeping. A normal installation has no such dependency. The configuration is outside the repository and must not be published with local paths.

## Known edges

- A newly connected endpoint can emit sound before its callback is handled.
- External unmute is detected by the one-second reconciliation pass. An unmute followed by a remute entirely between passes may not be observed; Windows and other applications are treated alike.
- The controller cannot act while Windows is off, asleep, or the process is stopped.
- If a device is removed permanently while pending restoration, normal exit remains blocked. A future explicit “forget missing device” flow must explain the lost restoration state.
- A failed endpoint enumeration currently prevents that pass from applying audio changes; the tray reports attention and later retries. Per-endpoint failure isolation is a planned improvement.
- Multiple concurrent users/sessions controlling the same physical device are not a supported coordination scheme.

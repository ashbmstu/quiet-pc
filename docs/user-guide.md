# Using Quiet PC

## Start a timed session

Right-click the moon icon and choose **Quiet for → 30 min** or **1 hour**. For another duration, choose **Custom** and enter minutes. The accepted range is 1–10,080 minutes.

The timer window shows when the session ends and how much time remains. Starting a new timer replaces the deadline with “now plus the new duration”. It does not replace the original notification or audio settings saved for restoration.

**No timer** removes the deadline. **Restore** turns quiet mode off and restores the saved settings immediately. Clicking the tray icon while quiet is on also restores immediately and clears the timer.

Timers use an absolute UTC deadline. Changing time zones does not change the deadline, but changing the system clock can affect when it fires. Sleep counts towards the duration. There is no wake timer: if time runs out during sleep, restoration happens after resume. A stopped app cannot act until it starts again.

## Choose what to suppress

Open **Settings** and select notifications, audio, or both. These two choices apply to the next quiet session. This keeps a session's saved restoration state consistent when preferences change mid-session.

Other settings apply when you save:

- **Show tray icon:** re-applies this app's visible state every five seconds. It does not change other icons.
- **Start with Windows:** a per-user Windows startup entry; no administrator access.
- **Remember quiet mode:** resumes a saved session. An expired session restores instead. If disabled, starting the app restores any old session.
- **Shortcut:** Ctrl+Alt+Q, Ctrl+Shift+F12, Ctrl+Alt+F12, or none. An unavailable shortcut is rejected when changed in Settings.

## Audio devices

All active playback endpoints are included, even when they are not the default output. Microphones are untouched. Mute state is saved separately for each endpoint; volume sliders and per-app mixer levels are not changed.

If a device disconnects before quiet mode ends, the app remembers its restoration state. It restores that state when the device reconnects. Keep the app running until those devices return. The normal exit and uninstaller refuse to discard pending restoration.

While quiet mode is on, manual unmuting is reversed by the next check. Turn quiet mode off if you want sound again.

## When something looks wrong

| Symptom | Check |
|---|---|
| Icon is amber | Right-click → Details. A Windows setting or audio device did not accept the change. |
| Icon is missing | Check Windows' tray overflow. Ensure Show tray icon is enabled. |
| A window still appears | Some apps draw their own notifications. Configure those in the app. |
| Audio still plays | Check for ASIO or exclusive-mode playback that bypasses Windows software mute. |
| Exit is refused | Reconnect devices that still need their original mute state restored. |
| Shortcut does nothing | Another app may own it. Choose a different shortcut in Settings. |
| Timer ended while the app was closed | Start Quiet PC. It checks the saved deadline before reapplying quiet mode. |

Recovery and log files are in `%LOCALAPPDATA%\QuietPC`. Do not delete `state.json` while quiet mode is active or restoration is pending. The `.bak` file is a previous write, not necessarily the original session state.

Diagnostics can contain audio-device IDs and local paths. Review them before sharing. The app does not upload them.

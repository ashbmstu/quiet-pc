# Changelog

## 1.2.0 — 2026-10-15

- Added 30-minute, 1-hour and custom quiet timers with automatic restoration.
- Persisted timer deadlines across restart and sleep; replacing a timer keeps the original restoration snapshot.
- Added countdown, custom timer window, cancel-timer and restore-now actions.
- Added acknowledged local commands, JSON replies and a PowerShell wrapper.
- Added a minimal neumorphic interface, shorter labels and reusable SVG branding assets.
- Separated the app from machine-specific startup bookkeeping.
- Prepared source layout, documentation, MIT licence and Windows CI.

## 1.1.0 — 2026-10-15

- Added settings for notifications/audio, startup, resume, icon visibility and hotkeys.
- Added periodic tray-icon promotion.

## 1.0.0 — 2026-10-15

- Initial tray toggle for notifications and playback mute, with saved restoration state.

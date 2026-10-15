# Enhancement ideas

These are proposals, not current features. The order favours reliable daily use before more integrations.

## First: make recovery easier

| Idea | Who it helps | Done when |
|---|---|---|
| Per-device error isolation | Everyone with docks, USB headsets or virtual audio | One broken endpoint no longer prevents other endpoints from being muted; the failed device is named. |
| Recovery screen | People who change audio hardware | Pending devices have readable names, last-seen times and an explicit way to forget a permanently removed device. |
| Better status | People and scripts | Status reports what was actually applied, including notification errors, unavailable devices and a stale controller. |
| More Windows validation | Everyone | CI builds are complemented by a published manual matrix for Windows versions, sleep, multiple monitors, DPI and accessibility. |

## Next: everyday controls

**Profiles.** “Everything”, “Notifications only”, and user-created profiles with selected outputs. A profile should show exactly what it changes before activation. Do not mute microphones or hide call audio by surprise.

**Extend this session.** Add 5, 15 or 30 minutes to the existing deadline. Today's duration buttons replace the deadline from now; an extension action should be labelled differently.

**Optional end notice.** A small visual notice when a timer ends. No sound by default. The user should be able to turn it off.

**Schedules.** Local weekday quiet hours, with an obvious temporary override. Define how a schedule interacts with a manual timer before writing code: a schedule must not silently undo a person's newer choice.

**Call and recording shortcuts.** Launch a bounded quiet session before screen recording, then restore after recording stops. Use explicit integrations and avoid broad process monitoring by default.

**Distribution.** Signed releases and an installer accepted by Windows package managers. Keep the unsigned source build available and publish checksums. A reputation warning is not something the app should try to bypass.

## For assistants and automation

**Session ownership and leases.** Return an opaque session token when an assistant starts quiet mode. Releasing that token should not disable a session that the person started or extended afterwards. This is the most useful protection against two workflows undoing each other.

**Idempotent requests.** Accept a request ID and return the stored result when that ID is retried. This prevents a timeout retry from toggling twice or extending a timer unintentionally.

**Event stream.** Let a local client subscribe to changes such as `quiet.started`, `timer.expired` and `restore.pending`, without polling. Events should contain state and reason, not notification text or audio.

**Small MCP adapter.** Expose only `get_status`, `start_quiet`, `extend_session` and `restore_session`. Use the same controller and ownership rules as the app. Keep permission to act explicit; reading status should be separate from changing it.

**An agent-facing health check.** Report supported Windows interfaces, controller version, reachable audio outputs and known limitations. It should diagnose a compatibility failure without changing any settings.

## Avoid adding

- Automatic “productivity” judgements based on private app usage.
- A cloud account just to mute a local PC.
- Reading notification messages or recording audio.
- Silent microphone muting.
- Turning quiet mode off merely because an unrelated assistant task finished.
- Installing system-wide hooks when a user-level interface is enough.

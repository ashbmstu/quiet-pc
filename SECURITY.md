# Security and privacy

Quiet PC changes the current user's notification preference and playback-device mute state. It does not request administrator privileges, read notification contents, capture audio, or send data over the network.

The local command pipe is restricted to the current Windows user and scoped by interactive session. Other applications running as that user can issue commands, just as they can change many of the same Windows settings themselves. This is not a boundary against a compromised Windows account.

Recovery and diagnostic files can contain local paths and audio-device identifiers. Do not post them without checking the contents. Release archives must exclude runtime data.

For a security report, use the repository's private vulnerability reporting feature when available. Do not put secrets or a working exploit in a public issue. If no private channel is available yet, ask for a private reporting channel without including sensitive details.

The notification and tray-promotion interfaces depend on Windows implementation details. Unsupported or changed interfaces should produce an error, not trigger privilege escalation, service termination, or a security-setting workaround.

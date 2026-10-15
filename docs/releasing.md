# Releasing

## Repository

Source: [ashbmstu/quiet-pc](https://github.com/ashbmstu/quiet-pc). Keep installed executables and personal runtime state out of Git; release archives are built separately.

Before pushing:

- Review the files Git will track. There should be no `build`, `dist`, `.local`, settings, recovery state or logs.
- Confirm the MIT licence and the intended commit author.
- Check README links, app version and changelog.
- Run the build and deterministic tests.

## Release checks

1. Run `scripts/build.ps1`, `scripts/test.ps1`, then `scripts/package.ps1`.
2. Check 30 minutes, 1 hour and custom entry in the real tray UI. Use a short timer for a real expiry test.
3. Check replacement, cancellation, manual off, sleep/resume and overdue restart.
4. Check audio devices that start muted, reconnect during quiet, and disconnect before restoration.
5. Check keyboard-only use, 100%/150%/200% DPI and Windows high contrast.
6. Test install, update, and uninstall on a separate user profile or VM. Confirm startup preference and recovery state survive update.
7. Review the archive contents and checksum. Publish only after the owner authorises a release.

Release assets: the versioned Windows x64 ZIP and its `.sha256` file. The ZIP includes `Install.cmd`; users extract the archive and double-click it. Create a draft, upload and verify both assets, then publish.

CI builds and runs deterministic tests on Windows. It does not test physical audio hardware, visual layout, or the internal notification interface on every supported Windows build. A workflow file in a local repository is not evidence of a passing GitHub run.

Current local validation covers Windows 11 24H2 build 26100. Broader Windows, DPI and high-contrast validation remains release work. Describe this scope honestly in release notes.

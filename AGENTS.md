# Working on Quiet PC

Read README.md and docs/architecture.md before changing controller behaviour.

- Build with scripts/build.ps1. Run scripts/test.ps1 for changes to session, timer, persistence or IPC behaviour.
- Do not run native tests, stop the installed app, change startup, or install a build unless the user's task calls for it. Preserve the active quiet session during an authorised upgrade.
- Keep Windows-specific dependencies behind the existing audio and notification interfaces.
- Snapshot before mutation. Never overwrite original settings with already-muted state.
- Do not add runtime files, local device identifiers, paths or diagnostic output to Git.
- Use plain descriptions. Mark roadmap ideas as proposals; do not present them as shipped features.
- Keep documentation, commit messages and PR text brief and factual. No slogans, promotional copy or repeated explanations.
- Keep controls keyboard-accessible and test real UI layout after visual changes.
- Do not publish releases or push a remote unless the user asked for that action.

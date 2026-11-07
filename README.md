# Windows Startup Guard

A free, open-source Windows desktop utility for discovering and managing common startup applications.

## Version 0.2.2

- Modern dark desktop interface.
- Hebrew / English interface with RTL / LTR layout, including translated controls, inventory columns, and status labels.
- Discovery of current-user and all-user Windows Run and RunOnce registry entries.
- Discovery of per-user and shared Startup folder items.
- Search by application name, publisher, or startup command.
- Filter inventory by current user or all users.
- Publisher/company name and file-version metadata when the executable is available.
- Export the currently displayed inventory, including disabled items and their status, to UTF-8 CSV.
- Best-effort RAM estimate for matching running processes (shared process memory is counted once per executable name in the dashboard total).
- Disable selected entries with a local backup.
- View disabled items in the inventory and restore only selected backups, or restore all backups.
- Clear status-bar indication when running as administrator or a standard user, including a note that all-user changes may require elevation.
- Restore registry values and Startup folder files disabled by this app, with a per-item result summary showing restored, skipped, and failed items.
- Optional process-close action with an explicit confirmation.
- No silent cleanup, no automatic deletion, and no external services.

## Safety notes

The first release intentionally limits its changes to common Run registry values and Startup folder items. It does not disable Windows services, drivers, scheduled tasks, security products, or system components. The status bar indicates whether the app is running as administrator or a standard user. Disabling machine-wide registry entries may require administrator permissions; the app does not silently elevate itself. Backups are stored under `%LOCALAPPDATA%\\WindowsStartupGuard\\Backups` and in the current user's registry hive.

Publisher information comes from executable version metadata; it is not digital-signature verification. CSV export includes the currently displayed rows. Registry backups preserve common string, expandable-string, DWORD, QWORD, and binary values.

RAM is an estimate: the app sums the working-set memory of processes whose process name matches the executable name in a startup command. This is not a precise measurement of memory attributable to that startup entry.

## Build

Building requires Windows and the .NET 10 SDK. The published win-x64 package is self-contained and does not require a separate .NET Desktop Runtime installation.

```powershell
dotnet build WindowsStartupGuard.csproj -c Release
dotnet run --project WindowsStartupGuard.csproj
```

## Planned

- Add scheduled tasks and service inventory in a read-only advanced view.
- Publisher and digital-signature information.
- Startup impact history and launch-delay measurements.
- Optional logon-time re-check (opt-in only).
- Import/export of configuration and restore profiles.
- Portable and self-contained release packages.

## License

MIT

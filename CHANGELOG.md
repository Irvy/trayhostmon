# Changelog

All notable changes to TrayHostMon, newest first.

## [1.0.3] - 2026-11-13

### Added
- `build.cmd` now stops a running instance cleanly before rebuilding the exe,
  so you don't hit "file in use" when the app is still open. It only acts on an
  instance launched from the build directory — a copy running from anywhere else
  is left alone. The app now listens on a named shutdown event and exits through
  the normal clean-shutdown path.

## [1.0.2] - 2026-11-12

### Changed
- The tray icon is now generated at build time. `build.cmd` compiles
  `tools/MakeIcon.cs` into a small helper and renders `TrayHostMon.ico` from the
  same drawing code the app uses at runtime, so the exe icon always matches the
  real tray-state colours.

## [1.0.1] - 2026-11-10

### Added
- Enable/disable individual hosts by clicking their status dot in the widget;
  disabled hosts are dimmed and excluded from the tray icon state.
- Per-host ping settings — interval, timeout, and payload size — editable from the
  **Add host** dialog and applied to all hosts.
- **Clear log** item in the tray menu.

### Changed
- Renamed the project from **pingmon** to **TrayHostMon** — the old name was too
  generic and collided with other projects. This touched namespaces, the runtime
  file names, the single-instance mutex, and the user-facing strings.

### Fixed
- Widget behaviour: click targets narrowed to the remove button and the status dot,
  double-click no longer maximizes the borderless window, and toggling a host
  repaints immediately.
- Clearing the log now recreates the file with a `LOG CLEARED` record.

## [1.0.0] - 2026-11-09

### Added
- First working release: a windowless tray app that pings a list of hosts in the
  background and shows the worst state in the tray icon.
- Floating widget with per-host status, latency, and "down since" time.
- CSV log of state changes and app events.
- INI config, autostart via `shell:startup`, and a single-instance mutex.

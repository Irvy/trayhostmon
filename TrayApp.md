# TrayApp.cs

Tray icon, menu and monitor ownership.

## Class `TrayApp : ApplicationContext`
- Creates a `NotifyIcon` whose icon is the worst state (`Overall`) from `IconFactory`.
- Tooltip: `pingmon: X/Y up`.
- Menu: Acknowledge all (enabled only when not paused and an unacknowledged Down host exists), Pause/Resume monitoring (text follows state), Add host..., Open log, Reload config, Start with Windows (checkmark follows `Autostart.IsEnabled`), About..., Exit.
- Add host: opens `AddHostForm`. Each new host is added to the running list, monitoring starts (unless paused), logged as `ADDED`, and the config is written once. If some entries were already listed, a balloon tip names them.
- Shift+click or right-click on About... opens a small debug form with "Memory info" and "Force cleanup". See `Diagnostics.md`.
- About...: shows a message box (`PingMon` / `A simple host ping utility` / `Made by Claude/Cline/irvy in 2026`). An invisible topmost owner keeps it in front; a flag stops a second copy opening.
- Remove host: from the widget's `×`. Stops that host's monitor, removes it, logs `REMOVED`, writes the config. No confirmation.
- Config writes: on add, remove, widget move (widget closed after a drag) and exit (when the widget was moved). Each write regenerates the whole file from running values.
- Reload config: reads the file only (no write). Keeps state for hosts still listed; stops monitors of removed hosts. Skipped lines are shown in a balloon tip.
- Startup: shows a balloon tip if any host lines were skipped.
- Exit: runs once (guarded), cancels all monitors via the shared lifetime token, writes `STOP`, and unhooks the Windows session-end event.
- Pause: stops monitors, logs `PAUSE`, keeps host states, tray icon grey, tooltip `pingmon: paused`. Widget shows dimmed rows with "as of" time.
- Resume: logs `RESUME`, resets fail counts, restarts monitors on the current host list.
- Reload config while paused loads the new list and stays paused.
- Widget position: saved to `[widget]` in `pingmon.ini` when the widget was dragged and then closed. Restored on open, falling back to the cursor if the saved spot is off all screens.
- Tray icon: worst state from `HostState.Worst` (Unknown hosts ignored). Tooltip and icon are updated on every refresh.
- Left click toggles the `WidgetForm`. A click within 300 ms of the widget closing is ignored, so the click that closed it doesn't reopen it.
- Reload config: re-reads `pingmon.ini`, restarts monitors, keeps state for hosts with unchanged name and address. Invalid or empty config shows a balloon and keeps the old config.
- Starts a `HostMonitor` per host from `pingmon.ini`. State changes are marshalled to the UI thread and written to `pingmon.log`.
- Exit: cancels monitors, writes `STOP`, removes the tray icon.
- Also exits on `SystemEvents.SessionEnding`.

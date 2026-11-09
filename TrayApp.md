# TrayApp.cs

Tray icon, menu and monitor ownership.

## Class `TrayApp : ApplicationContext`
- Creates a `NotifyIcon` whose icon is the worst state (`Overall`) from `IconFactory`.
- Tooltip: `pingmon: X/Y up` (only counts enabled hosts when not paused).
- Menu: Acknowledge all (enabled only when not paused and an unacknowledged Down host exists), Pause/Resume monitoring (text follows state), Add host..., Open log, Clear log (enabled only when log file exists), Reload config, Start with Windows (checkmark follows `Autostart.IsEnabled`), About..., Exit.
- Add host: opens `AddHostForm`. Shows current settings (interval, timeout, buffer). Each new host is added to the running list, monitoring starts (unless paused), logged as `ADDED`, and the config is written once. Settings changes apply to all existing hosts. If some entries were already listed, a balloon tip names them. If no hosts are entered but settings changed, only settings are applied.
- Shift+click or right-click on About... opens a small debug form with "Memory info" and "Force cleanup". See `Diagnostics.md`.
- About...: shows a message box (`PingMon` / `A simple host ping utility` / `Made by Claude/Cline/irvy in 2026`). An invisible topmost owner keeps it in front; a flag stops a second copy opening.
- Remove host: from the widget's `×`. Stops that host's monitor, removes it, logs `REMOVED`, writes the config. No confirmation.
- Clear log: deletes `pingmon.log` (if it exists) and shows balloon tip. Available from tray menu when log exists.
- Enable/disable host: clicking a host's row in the widget (not the `×` button) toggles the enabled state. Disabling stops the monitor and logs `DISABLED`. Enabling starts the monitor (if not paused) and logs `ENABLED`. Disabled hosts are dimmed in the widget and don't affect the tray icon state.
- Config writes: on add, remove, enable/disable, widget move (widget closed after a drag) and exit (when the widget was moved). Each write regenerates the whole file from running values. Disabled hosts are saved with a leading `-` prefix.
- Reload config: reads the file only (no write). Keeps state for hosts still listed; stops monitors of removed hosts. Restores enabled/disabled state from config. Skipped lines are shown in a balloon tip.
- Startup: shows a balloon tip if any host lines were skipped.
- Exit: runs once (guarded), cancels all monitors via the shared lifetime token, writes `STOP`, and unhooks the Windows session-end event.
- Pause: stops monitors, logs `PAUSE`, keeps host states, tray icon grey, tooltip `pingmon: paused`. Widget shows dimmed rows with "as of" time. Disabled hosts also appear dimmed.
- Resume: logs `RESUME`, resets fail counts, restarts monitors on enabled hosts only. Disabled hosts remain disabled.
- Reload config while paused loads the new list and stays paused.
- Widget position: saved to `[widget]` in `pingmon.ini` when the widget was dragged and then closed. Restored on open, falling back to the cursor if the saved spot is off all screens.
- Tray icon: worst state from `HostState.Worst` (Unknown and disabled hosts are ignored). Tooltip and icon are updated on every refresh. Tooltip counts only enabled hosts.
- Left click toggles the `WidgetForm`. A click within 300 ms of the widget closing is ignored, so the click that closed it doesn't reopen it.
- Reload config: re-reads `pingmon.ini`, restarts monitors (enabled hosts only), keeps state for hosts with unchanged name and address, respects enabled/disabled state. Invalid or empty config shows a balloon and keeps the old config.
- Starts a `HostMonitor` per enabled host from `pingmon.ini`. State changes are marshalled to the UI thread and written to `pingmon.log`.
- Exit: cancels monitors, writes `STOP`, removes the tray icon.
- Also exits on `SystemEvents.SessionEnding`.

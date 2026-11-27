# TrayApp.cs

Tray icon, menu and monitor ownership.

## Class `TrayApp : ApplicationContext`
- Creates a `NotifyIcon` whose icon is the worst state (`Overall`) from `IconFactory`.
- Tooltip: `TrayHostMon: X/Y up` (only counts enabled hosts when not paused).
- Menu: Acknowledge all (enabled only when not paused and an unacknowledged Down host exists), Pause/Resume monitoring (text follows state), `Notify down`, `Notify up`, `Keep widget open`, Add host..., Open log, Clear log (enabled only when log file exists), Reload config, Start with Windows (checkmark follows `Autostart.IsEnabled`), About..., Exit.
- Add host: opens `AddHostForm`. Shows current settings (interval, timeout, buffer). Each new host is added to the running list, monitoring starts (unless paused), logged as `ADDED`, and the config is written once. Settings changes apply to all existing hosts. If some entries were already listed, a balloon tip names them. If no hosts are entered but settings changed, only settings are applied.
- Shift+click or right-click on About... opens a small debug form with "Memory info" and "Force cleanup". See `Diagnostics.md`.
- About...: shows the built release version, `A simple tray host ping monitor`, and credits Claude, DeepSeek, Cline, Terra and Irvy. An invisible topmost owner keeps it in front; a flag stops a second copy opening.
- Remove host: from the widget's `×`. Stops that host's monitor, removes it, logs `REMOVED`, writes the config. No confirmation.
- Clear log: deletes `TrayHostMon.log` (if it exists), then writes a fresh `LOG CLEARED` record so the file always exists afterward. Shows balloon tip. Available from tray menu when log exists.
- State-dot actions: clicking an unacknowledged red Down dot acknowledges that host only. All other dots toggle the enabled state. Disabling stops the monitor and logs `DISABLED`; enabling starts it (if not paused) and logs `ENABLED`. Disabled hosts are dimmed and don't affect the tray icon state.
- Notifications: enabled Down transitions and recovery-only (Down -> Up) transitions are collected for one second and then shown as one tray balloon. After an alert is shown, later queued events are collected and held until five seconds have elapsed. `Notify down` and `Notify up` are saved in the INI and default off.
- `Keep widget open`: saved in the INI and defaults off. The widget remains topmost; when enabled, it ignores focus loss and outside clicks until closed by the tray click or Escape.
- Config writes: on add, remove, enable/disable, notification/widget setting changes, widget move (widget closed after a drag), and every clean exit. Each write regenerates the whole file from running values. A clean exit also writes defaults for settings introduced by a newer compatible release. Disabled hosts are saved with a leading `-` prefix.
- Reload config: reads the file only (no write). Keeps state for hosts still listed; stops monitors of removed hosts. Restores enabled/disabled state from config. Skipped lines are shown in a balloon tip.
- Startup: shows a balloon tip if any host lines were skipped.
- Exit: runs once (guarded), cancels all monitors via the shared lifetime token, writes `STOP`, and unhooks the Windows session-end event.
- Pause: stops monitors, logs `PAUSE`, keeps host states, tray icon grey, tooltip `TrayHostMon: paused`. Widget shows dimmed rows with "as of" time. Disabled hosts also appear dimmed.
- Resume: logs `RESUME`, resets fail counts, restarts monitors on enabled hosts only. Disabled hosts remain disabled.
- Reload config while paused loads the new list and stays paused.
- Widget position: saved to `[widget]` in `TrayHostMon.ini` when the widget was dragged and then closed. Restored on open, falling back to the cursor if the saved spot is off all screens.
- Tray icon: worst state from `HostState.Worst` (Unknown and disabled hosts are ignored). Tooltip and icon are updated on every refresh. Tooltip counts only enabled hosts.
- Left click toggles the `WidgetForm`. A click within 300 ms of the widget closing is ignored, so the click that closed it doesn't reopen it.
- Reload config: re-reads `TrayHostMon.ini`, restarts monitors (enabled hosts only), keeps state for hosts with unchanged name and address, respects enabled/disabled state. Invalid or empty config shows a balloon and keeps the old config.
- Starts a `HostMonitor` per enabled host from `TrayHostMon.ini`. State changes are marshalled to the UI thread and written to `TrayHostMon.log`.
- Exit: cancels monitors, writes `STOP`, removes the tray icon.
- Also exits on `SystemEvents.SessionEnding`.

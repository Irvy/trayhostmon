# TrayHostMon: IPv4 host monitor for Windows

## Goal
Windowless tray app that monitors IPv4 connectivity of 5-10 remote hosts on Win10/Win11. Tray icon shows the worst state (acknowledgeable); clicking it opens a floating widget. State changes are logged.

## Constraints
- Single small exe, no installer, no runtime install: .NET Framework 4.8 (built into Win10 1903+ and Win11)
- WinForms, no NuGet packages, no admin rights
- Build: `build.cmd` runs csc.exe from Windows (C# 5 only), no SDK
- Files: `TrayHostMon.exe`, `TrayHostMon.ini` (config), `TrayHostMon.log` (CSV), all next to the exe
- No JSON: INI/text config

## Components
1. **Config** (`TrayHostMon.ini` next to exe): `[settings]` for interval, timeout, fail-threshold, buffer; `[hosts]` with one IP or hostname per line (optional leading `-` disables it)
2. **Monitor loop**: async `System.Net.NetworkInformation.Ping` per host; Down only after N consecutive failures (anti-flapping)
3. **State model**: per host state, last change time, acknowledged flag. Worst = Down (unacked) > Down (acked) > Up; disabled hosts ignored
4. **Tray icon**: `NotifyIcon`, generated green/red/grey icons. Menu: Acknowledge all, Pause/Resume, Add host, Open log, Clear log, Reload config, Start with Windows, About, Exit
5. **Floating widget**: borderless, topmost `Form` near the tray on click; per host: status dot, latency, "down since"; dot toggles enable/disable, `×` removes; closes on focus loss
6. **Log**: CSV (`timestamp, host, old→new state, duration`), only state changes plus app start/stop
7. **Autostart**: shortcut in `shell:startup`
8. **Single instance**: named mutex

## Build order
1. Monitor loop
2. CSV logging
3. Tray icon + worst-state logic
4. Widget form
5. Acknowledge + config reload
6. Autostart + publish

## Deploy
Copy `TrayHostMon.exe` to any folder. Optionally copy `TrayHostMon.ini` next to it (a sample is created on first run if missing). The log `TrayHostMon.log` is created next to the exe.

## Status
Basic functions done (build-checked): monitor loop, CSV log, tray icon and menu, widget, reload config, autostart. See Known issues.

## Known issues (deferred)
1. Tray logging and the Exit/STOP path haven't been tested on their own.
2. Fixed: Exit runs once and the session-end handler is removed.
3. Fixed: invalid host lines are reported in a balloon tip, and `TryNormalizeHost` is the single check (`10.1` and `999.999.999.999` are rejected, underscores allowed).
4. Config edits made by hand while running are overwritten on the next app write; use Reload config first.

## Working preferences
- Keep replies short; no code or long explanations unless asked
- Say so if something already exists and is well-known
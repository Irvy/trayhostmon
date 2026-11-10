# TrayHostMon

A tiny, windowless Windows tray application that monitors IPv4 connectivity of a
small list of remote hosts by pinging them in the background.

The tray icon shows the *worst* state across all monitored hosts, so a single
glance tells you whether anything is down. Left-click opens a floating widget
with per-host detail; state changes are written to a CSV log.

## Features

- **Tray icon** reflecting the worst host state (all up / ack'd down / unack'd down / paused)
- **Floating widget** showing per host: status dot, latency, and "down since" time
- **Enable/disable individual hosts** by clicking their status dot (disabled hosts don't affect the tray state)
- **Add/remove hosts** at runtime, with configurable ping interval, timeout and payload size
- **Acknowledge all** to turn a red "down-unacknowledged" into an orange "down-acknowledged" state
- **Pause/resume** monitoring
- **Clear log**, open log, and reload config from the tray menu
- **Start with Windows** (shortcut in `shell:startup`)
- **Single instance** via a named mutex
- **CSV log** of state changes and app lifecycle events

## Requirements

- Windows 10 1903+ or Windows 11
- .NET Framework 4.8 (built into Windows, nothing to install)

No installer, no admin rights, no NuGet dependencies — it's a single self-contained exe.

## Build

Run `build.cmd` from the project root. It invokes `csc.exe` directly (no SDK needed):

```
build.cmd
```

This produces `TrayHostMon.exe`.

## Usage

1. Put `TrayHostMon.exe` anywhere and run it.
2. On first run it creates `TrayHostMon.ini` (a sample config) next to the exe.
3. Edit hosts via the tray menu's **Add host...** (or edit `TrayHostMon.ini` and use **Reload config**).

## Files (next to the exe)

- `TrayHostMon.ini` — settings and host list (INI format)
- `TrayHostMon.log` — CSV of state changes and lifecycle events

## Configuration

`TrayHostMon.ini`:

```ini
[settings]
interval=5        ; seconds between pings
timeout=1000      ; ping timeout in ms
threshold=3       ; consecutive failures before a host is "down"
buffer=32         ; ping payload size in bytes (0 = system default)

[hosts]
google.com
-localhost  ; leading '-' disables the host
```

## License

No license chosen yet.

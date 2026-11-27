# TrayHostMon

A small tray app for Windows that quietly pings a handful of hosts and shows you,
at a glance, whether anything's gone down.

The tray icon always reflects the worst state of your hosts, so you don't have to
think about it — green means everything's fine. Left-click it and a little floating
panel drops down with the details: each host's status, its latency, and how long it's
been down.

## Why it exists

I wanted something tiny that sits in the system tray and pings a few servers
without needing an installer, admin rights, or a web dashboard. Just one exe, a
plain-text config, and a CSV log.

## What it does

- A tray icon that's **green** when everything's up, **orange** when something's
  down but acknowledged, **red** when something's down and you haven't acknowledged
  it, and **grey** while paused.
- A floating widget (left-click the tray icon) listing each host with a status dot,
  its latency, and its "down since" or "back since" time.
- Click a red status dot to acknowledge that host alone; other status dots turn
  monitoring for that host on or off. Disabled hosts are dimmed and don't affect
  the tray icon.
- Add and remove hosts on the fly, each with its own ping interval, timeout, and
  payload size — or change the defaults for all of them.
- **Acknowledge all** when something's down, to turn the red into a calmer orange
  once you know about it.
- Pause/resume, open or clear the log, and reload the config, all from the tray menu.
- Optional grouped tray notifications for hosts going down or coming back up: changes
  within one second are combined, and alerts are spaced at least five seconds apart.
  The widget can also remain open when it loses focus.
- Start with Windows via a shortcut in `shell:startup`.
- Only one copy runs at a time.
- State changes and app events are written to a CSV log.

## What you need

- Windows 10 (1903 or newer) or Windows 11.
- .NET Framework 4.8 — already part of Windows, nothing to install.

No installer, no admin rights, no NuGet packages. It's one self-contained
`TrayHostMon.exe`.

## Building it

From the project root, run:

```
build.cmd
```

That's it — the script calls `csc.exe` directly, so you don't need a full SDK. It
also rebuilds the tray icon from the same drawing code the app uses at runtime
(see `tools/`). If a copy is already running from this folder, the script stops
it first so it can overwrite the exe. The result is `TrayHostMon.exe`.

## Using it

1. Drop `TrayHostMon.exe` wherever you like and run it.
2. The first run writes a sample `TrayHostMon.ini` next to the exe.
3. Add hosts from the tray menu (**Add host…**), or edit the `.ini` by hand and
   pick **Reload config**.

### Upgrading

When replacing the exe with a newer release of the same major version, run it and
then exit it cleanly from the tray menu (or restart it). Existing settings and hosts
are retained, and newly introduced settings are written to `TrayHostMon.ini` with
their default values.

## Files it keeps (next to the exe)

- `TrayHostMon.ini` — settings and the host list, in INI format.
- `TrayHostMon.log` — a CSV of state changes and app lifecycle events.

## Configuration

`TrayHostMon.ini` looks like this:

```ini
[settings]
interval=5        ; seconds between pings
timeout=1000      ; ping timeout in ms
threshold=3       ; consecutive failures before a host counts as down
buffer=32         ; ping payload size in bytes (0 = system default)
notify_down=false ; show grouped notifications for Down transitions
notify_up=false   ; show grouped notifications for recovery transitions
keep_widget_open=false ; keep the widget open when it loses focus

[hosts]
google.com
-localhost        ; a leading '-' disables that host
```

## License

Licensed under the [MIT License](LICENSE).

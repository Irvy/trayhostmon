# StateLog.cs

CSV log of state changes, written to `TrayHostMon.log` next to the exe.

## Class `StateLog` (static)
- `Path`: full path to `TrayHostMon.log`.
- `Write(string host, string transition, TimeSpan? duration)`: appends one line. Writes the header `timestamp,host,transition,duration` when the file is new. Thread-safe.

## Format
`yyyy-MM-dd HH:mm:ss,host,Up->Down,d.hh:mm:ss`
- Host names containing commas are quoted.
- `duration` is empty when there is no previous state (Unknown).
- App lifecycle uses host `TrayHostMon` with transitions `START`, `STOP`, `STOP (paused)`, `PAUSE` and `RESUME`.
- Host list changes use the host address with transitions `ADDED`, `REMOVED`, `ENABLED` and `DISABLED`.

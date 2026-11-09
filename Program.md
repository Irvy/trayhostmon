# Program.cs

Entry point.

## Behaviour
- `Main` is `[STAThread]`.
- `#if CONSOLE` (built by `build.cmd console`):
  - Loads `pingmon.ini` with `Config.Load`, writes `START` to the log, starts a monitor per host.
  - Writes `STOP` on exit (known issue #4 fixed).
  - Prints each state change as `HH:mm:ss name old->new (duration)` and appends it to `pingmon.log` (duration empty when old state is Unknown).
  - Prints a summary line every 5 s and stops when Enter is pressed.
  - `STOP` is not yet written in console mode; it is added with the tray app's exit handling in Step 3.
- Without `CONSOLE` (built by `build.cmd`):
  - Single instance via named mutex `Local\pingmon`; a second copy exits silently.
  - Runs `TrayApp` under `Application.Run`.

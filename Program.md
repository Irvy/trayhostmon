# Program.cs

Entry point.

## Behaviour
- `Main` is `[STAThread]`.
- Single instance via named mutex `Local\TrayHostMon`; a second copy exits silently.
- Runs `TrayApp` under `Application.Run`.

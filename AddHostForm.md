# AddHostForm.cs

Dialog for adding a host to the running list and editing settings.

## Class `AddHostForm : Form`
- Constructor: `AddHostForm(Func<string, bool> isListed, int currentInterval = 5, int currentTimeout = 1000, int currentBuffer = 32)`
  - `isListed` tells whether an address is already in the running list.
  - `currentInterval`, `currentTimeout`, `currentBuffer` are the current settings to display.
- `List<string> Hosts`: new hosts accepted by OK (normalized, no duplicates).
- `List<string> AlreadyListed`: valid entries that were already listed.
- `int? IntervalSec`, `int? TimeoutMs`, `int? Buffer`: setting values (null means unchanged from current).
- Input is split on spaces, commas, semicolons or tabs. Each entry is checked with `Config.TryNormalizeHost`.
- Settings inputs:
  - Ping interval (seconds): 1-3600, default 5
  - Timeout (ms): 500-10000, default 1000
  - Payload size (bytes): 0-65500, default 32 (0 = use system default)
- OK (or Enter) validates settings and messages, and the dialog stays open:
  - invalid interval/timeout/buffer: shows validation error
  - hosts entered but all are invalid: `invalid: <entries>` (nothing is added)
  - hosts entered but all valid entries already listed: `already in list: <hosts>`
- OK with at least one new host closes the dialog.
- OK with no hosts but with changed settings: closes the dialog with settings only.
- Cancel or Esc closes without adding/applying changes.

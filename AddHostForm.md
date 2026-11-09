# AddHostForm.cs

Dialog for adding a host to the running list.

## Class `AddHostForm : Form`
- Constructor `AddHostForm(Func<string, bool> isListed)`: `isListed` tells whether an address is already in the running list.
- `List<string> Hosts`: new hosts accepted by OK (normalized, no duplicates).
- `List<string> AlreadyListed`: valid entries that were already listed.
- Input is split on spaces, commas, semicolons or tabs. Each entry is checked with `Config.TryNormalizeHost`.
- OK (or Enter) messages, and the dialog stays open:
  - nothing entered: `enter at least one IP or hostname`
  - any invalid entries: `invalid: <entries>` (nothing is added)
  - all valid entries already listed: `already in list: <hosts>`
- OK with at least one new host closes the dialog. Cancel or Esc closes without adding.

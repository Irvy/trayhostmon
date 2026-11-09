# IconFactory.cs

Generates the tray icons in memory (no resource files).

## Class `IconFactory` (static)
- `Icon Get(Overall overall)`: returns a cached 16x16 filled-circle icon.
  - AllUp: green
  - DownUnacked: red
  - DownAcked: orange
  - Paused: grey

Uses `NativeMethods.DestroyIcon` (see `NativeMethods.cs`) to free the GDI handle after cloning.

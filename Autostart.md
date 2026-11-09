# Autostart.cs

Start-with-Windows shortcut.

## Class `Autostart` (static)
- `LinkPath`: `pingmon.lnk` in the user's Startup folder.
- `bool IsEnabled`: true when the shortcut exists.
- `void Enable()`: creates the shortcut via `WScript.Shell` (late-bound COM). Target is the running exe; working directory is its folder.
- `void Disable()`: deletes the shortcut.

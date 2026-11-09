# Diagnostics.cs

Memory diagnostics, compiled into the main build (`build.cmd` defines `DEBUG_MEM`).

## Class `Diagnostics` (static)
- `string Snapshot()`: private memory, working set, .NET heap size, GC collection counts per generation, thread and handle counts.
- `string ForceCleanup()`: takes a snapshot, runs a full GC with finalizer wait, takes another snapshot, and returns both as text.

## Use
- Build with `build.cmd`, which writes `pingmon.exe`.
- Shift+click or right-click the tray menu's About... item. A small debug form opens with "Memory info" and "Force cleanup"; results show in a message box.

# WidgetForm.cs

Floating status window opened from the tray icon.

## Class `WidgetForm : Form`
- Constructor `WidgetForm(List<HostState>, bool paused)`: borderless, topmost, hidden from the taskbar, no control box. Closes on deactivate (focus loss) or Esc.
- Dragging: the whole window acts as a title bar (`WM_NCHITTEST` -> `HTCAPTION`), so left-button drag moves it. `Moved` becomes true when the window is moved.
- `void PlaceAt(Point? saved)`:
  - With a saved position: if it overlaps a screen's work area, the window is clamped into that screen. If it's off all screens, it falls back to the cursor.
  - Without one: bottom-right corner at the cursor, kept inside the work area.
- Paused: a "paused" header line is shown, dot and text are dimmed, and status reads `<state> · as of HH:mm:ss` from `LastCheck`.
- Remove buttons: a small `×` at the start (left edge) of each row, grey, red while hovered. Clicking it raises `RemoveRequested(HostState)`. The button area stays clickable; the rest of the window still drags.
- Row layout from the left: `×`, status dot, address, latency, status text. Everything after the `×` is indented by `RowIndent` (18 px).
- `void Relayout()`: recalculates size after the host list changes.
- Empty list: shows `no hosts - use 'Add host' in the tray menu`.
- Outside clicks: closes on focus loss (`Deactivate`), and a 50 ms fallback timer closes it when a mouse button is pressed outside its bounds. `BringToFront(IntPtr)` makes the window foreground so this works.
- Rows (20 px each) drawn in `OnPaint`: status dot, name, latency, status text.
  - Status text: `up`, `unknown`, or `down since HH:mm` (`dd.MM HH:mm` if not today), plus ` (ack)` when acknowledged.
- Redraws every 1 s.

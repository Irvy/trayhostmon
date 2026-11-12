# WidgetForm.cs

Floating status window opened from the tray icon.

## Class `WidgetForm : Form`
- Constructor `WidgetForm(List<HostState>, bool paused)`: borderless, topmost, hidden from the taskbar, no control box. Closes on deactivate (focus loss) or Esc.
- Dragging: the header (when paused), the row text, and the padding all act as a title bar (`WM_NCHITTEST` -> `HTCAPTION`), so left-button drag moves the window. Only the remove button (`×`) and the state dot are clickable (`HTCLIENT`). `Moved` becomes true when the window is moved.
- A double-click on the caption area is swallowed (`WM_NCLBUTTONDBLCLK` on `HTCAPTION`), so the borderless widget can only be dragged, never maximized.
- Maximize is additionally blocked at the window-style level: `OnHandleCreated` strips `WS_MAXIMIZEBOX` and `WS_THICKFRAME`, and `WM_SYSCOMMAND`/`SC_MAXIMIZE` is rejected in `WndProc`. Together these guarantee the widget cannot maximize or resize.
- `void PlaceAt(Point? saved)`:
  - With a saved position: if it overlaps a screen's work area, the window is clamped into that screen. If it's off all screens, it falls back to the cursor.
  - Without one: bottom-right corner at the cursor, kept inside the work area.
- Paused: a "paused" header line is shown, dot and text are dimmed, and status reads `<state> · as of HH:mm:ss` from `LastCheck`.
- Remove buttons: a small `×` at the start (left edge) of each row, grey, red while hovered. Clicking it raises `RemoveRequested(HostState)`.
- Enable/disable toggle: clicking the state dot raises `ToggleEnabledRequested(HostState)`, toggling the host's enabled/disabled state. The dot is the only toggle target; the rest of the row is a drag area.
- Row layout from the left: `×`, status dot, address, latency, status text. Everything after the `×` is indented by `RowIndent` (18 px).
- `void Relayout()`: recalculates size after the host list changes.
- Empty list: shows `no hosts - use 'Add host' in the tray menu`.
- Outside clicks: closes on focus loss (`Deactivate`), and a 50 ms fallback timer closes it when a mouse button is pressed outside its bounds. `BringToFront(IntPtr)` makes the window foreground so this works.
- Rows (20 px each) drawn in `OnPaint`: status dot, name, latency, status text.
  - Status text: `up`, `unknown`, `down since HH:mm` (`dd.MM HH:mm` if not today), plus ` (ack)` when acknowledged, or `disabled` for disabled hosts.
  - Disabled hosts are dimmed (similar to paused state): grey status dot, dimmed text.
- Redraws every 1 s.

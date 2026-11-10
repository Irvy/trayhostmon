# NativeMethods.cs

P/Invoke declarations.

## Class `NativeMethods` (internal static)
- `bool DestroyIcon(IntPtr hIcon)`: from `user32.dll`. Frees an icon handle created by `Bitmap.GetHicon`.
- `bool SetForegroundWindow(IntPtr hWnd)`: from `user32.dll`. Brings the widget to the front so outside clicks are detected.
- `short GetAsyncKeyState(int vKey)`: from `user32.dll`. Reads the current mouse button state for the outside-click fallback.
- `int GWL_STYLE`, `int WS_MAXIMIZEBOX`, `int WS_THICKFRAME`: window style constants.
- `GetWindowLong32` / `GetWindowLongPtr64`, `SetWindowLong32` / `SetWindowLongPtr64`: pointer-size-aware get/set of a window's style bits. Used by `WidgetForm` to strip `WS_MAXIMIZEBOX`/`WS_THICKFRAME` so it cannot maximize or resize.

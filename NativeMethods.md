# NativeMethods.cs

P/Invoke declarations.

## Class `NativeMethods` (internal static)
- `bool DestroyIcon(IntPtr hIcon)`: from `user32.dll`. Frees an icon handle created by `Bitmap.GetHicon`.
- `bool SetForegroundWindow(IntPtr hWnd)`: from `user32.dll`. Brings the widget to the front so outside clicks are detected.
- `short GetAsyncKeyState(int vKey)`: from `user32.dll`. Reads the current mouse button state for the outside-click fallback.

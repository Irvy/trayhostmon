# Change Log

## Remove console build remnants

The `#if CONSOLE` build mode (used only for agentic testing) was removed. `Program.cs` now has a single tray-app `Main` with no conditional compilation, and the `TrayHostMon-console.exe` artifact reference was dropped from `.gitignore`.

## Rename: pingmon → TrayHostMon

The app was renamed from `pingmon` to `TrayHostMon` (there were too many other projects already called "pingmon"). This touched:
- Namespace `PingMon` → `TrayHostMon` across all `.cs` files.
- Runtime files: `pingmon.exe` → `TrayHostMon.exe`, `pingmon.ini` → `TrayHostMon.ini`, `pingmon.log` → `TrayHostMon.log`, `pingmon.lnk` → `TrayHostMon.lnk`, icon `pingmon.ico` → `TrayHostMon.ico`.
- Single-instance mutex: `Local\pingmon` → `Local\TrayHostMon`.
- User-facing strings: About dialog, tooltip, balloon title, dialog captions, log owner name.
- About text now: `TrayHostMon` / `A simple tray host ping monitor` / `Made by Claude/Deepseek/Cline and Irvy in 2026`.
- `.gitignore` updated to the new artifact names.

## Follow-up Fixes

1. **Widget click-to-toggle:** the `WM_NCHITTEST` handling now returns `HTCAPTION` only for the header/padding, keeping the host rows as the clickable client area so enable/disable toggling works. `ToggleHostEnabled` also invalidates the widget immediately for instant repaint.
2. **Clear log:** after deleting `TrayHostMon.log`, `ClearLog` writes a fresh `LOG CLEARED` record so the file is recreated immediately.
3. **Add host form:** layout is now a compact horizontal row of `period` / `timeout` / `size` inputs beneath the host field. The Cancel button was removed (the window close button cancels). The host input was lowered to clear its label, and the OK button was moved up to align with the settings input row, trimming the empty bottom space.
4. **Widget maximize on double-click:** the widget now swallows `WM_NCLBUTTONDBLCLK` when the hit target is the caption, so double-clicking the padding/header can no longer maximize the borderless window. Dragging and row clicks are unaffected. A follow-up hardening added two more layers: `OnHandleCreated` strips the `WS_MAXIMIZEBOX`/`WS_THICKFRAME` style bits (so the window physically cannot maximize/resize), and `WM_SYSCOMMAND`/`SC_MAXIMIZE` is rejected in `WndProc`.
5. **Widget click targets narrowed:** clicking is now limited to the `×` remove button and the state dot only. The rest of the row (name, latency, status) and padding are draggable again, restoring smooth dragging while keeping remove/toggle reachable.

## Changes Implemented

### 1. Host Enable/Disable Feature

**What Changed:**
- Added `Enabled` property to `HostState` class (default: `true`)
- Modified `HostState.Worst()` to ignore disabled hosts when calculating tray icon state
- Updated config file format: disabled hosts are prefixed with `-` (e.g., `-host.example.com`)
- Tray icon tooltip now counts only enabled hosts

**Widget Changes:**
- Click on a host row (not the `×` button) toggles enable/disable
- Disabled hosts are visually dimmed (similar to paused state)
- Status text shows "disabled" for disabled hosts
- Status dot appears grey for disabled hosts

**TrayApp Changes:**
- `ToggleHostEnabled()` method handles enable/disable toggling
- Enabled hosts get monitors started/stopped on toggle
- Logs `ENABLED` / `DISABLED` state changes to `TrayHostMon.log`
- Config saves include the `-` prefix for disabled hosts

**Config Changes:**
- `IsDisabled(hostSpec)` - checks if hostspec starts with `-`
- `GetAddress(hostSpec)` - extracts address without `-` prefix
- `MakeHostSpec(address, enabled)` - creates hostspec with appropriate prefix
- Loading config preserves enabled/disabled state

---

### 2. Add Host Form - Settings Inputs

**What Changed:**
- Added three new input fields to `AddHostForm`:
  - **Ping interval (seconds)**: 1-3600, default 5
  - **Timeout (ms)**: 500-10000, default 1000
  - **Payload size (bytes)**: 0-65500, default 32 (0 = system default)
- Form title changed to "TrayHostMon - add host / settings"
- Form height increased to accommodate new controls

**Behavior:**
- Settings are pre-populated with current values
- OK validates all settings ranges
- OK can be pressed with NO hosts entered - only settings are applied
- Settings changes apply to ALL existing hosts (monitors restarted)
- Balloon tip shows "Settings updated" when only settings changed

**TrayApp Changes:**
- `AddHost()` now handles settings-only mode
- Settings changes update `AppConfig`
- When settings change, all existing hosts get updated configs
- Monitors are restarted to apply new settings

**Config Changes:**
- Added `Buffer` field to `HostConfig` and `AppConfig`
- Config file now includes `buffer=` in `[settings]` section
- Buffer value clamped to 0-65500 range

---

### 3. Clear Log Feature

**What Changed:**
- Added "Clear log" menu item in tray menu between "Open log" and "Reload config"
- Menu item is enabled only when `TrayHostMon.log` exists

**TrayApp Changes:**
- `ClearLog()` method:
  - Checks if log file exists
  - Deletes the log file
  - Shows balloon tip "Log cleared"
  - Handles errors gracefully

---

## Files Modified

### Source Files:
1. **HostState.cs**
   - Added `bool Enabled` property
   - Updated `Worst()` to filter disabled hosts

2. **Config.cs**
   - Added `Buffer` field to `HostConfig` and `AppConfig`
   - Added helper methods: `GetAddress()`, `IsDisabled()`, `MakeHostSpec()`
   - Updated `Load()` to parse disabled hosts (leading `-`)
   - Updated `Save()` to write `buffer=` setting
   - Updated `ToHostConfig()` to handle hostSpec with `-` prefix

3. **HostMonitor.cs**
   - Added `using System.Text;` for buffer creation
   - Updated `RunAsync()` to use buffer size in ping

4. **AddHostForm.cs**
   - Added settings input controls (interval, timeout, buffer)
   - Added properties: `IntervalSec`, `TimeoutMs`, `Buffer` (nullable)
   - Updated constructor to accept current settings
   - Modified `Accept()` to validate settings and allow settings-only OK

5. **WidgetForm.cs**
   - Added `ToggleEnabledRequested` event
   - Modified `OnRowClick()` to handle enable/disable toggle
   - Updated `OnPaint()` to dim disabled hosts
   - Updated `StatusText()` to show "disabled" for disabled hosts

6. **TrayApp.cs**
   - Added "Clear log" menu item
   - Added `ClearLog()` method
   - Added `ToggleHostEnabled()` method
   - Updated `ToggleWidget()` to hook up `ToggleEnabledRequested`
   - Updated `AddHost()` to pass settings and handle settings-only mode
   - Updated `LoadAndStart()` to respect enabled/disabled state
   - Updated `StartMonitor()` to check enabled state
   - Updated `SaveConfig()` to use `MakeHostSpec()` for disabled markers
   - Updated `ReloadConfig()` to preserve enabled/disabled state
   - Updated `Tooltip()` to count only enabled hosts
   - Updated menu opening to toggle "Clear log" visibility

### Documentation Files:
1. **AddHostForm.md** - Updated to reflect new settings inputs
2. **Config.md** - Updated with buffer setting and disabled host syntax
3. **HostState.md** - Updated with Enabled property
4. **HostMonitor.md** - Updated with buffer usage
5. **WidgetForm.md** - Updated with enable/disable toggle
6. **TrayApp.md** - Updated with clear log and enable/disable features

---

## Backward Compatibility

**Config File:**
- Existing INI files without `buffer=` will use default 32 bytes
- Hosts without leading `-` are assumed enabled
- Newly generated INI files will include `buffer=32` by default

**State Log:**
- New log entries: `ENABLED`, `DISABLED` (in addition to existing `ADDED`, `REMOVED`, state changes)

---

## Testing Recommendations

1. **Enable/Disable:**
   - Toggle a host on/off in widget → verify monitor starts/stops
   - Verify disabled hosts are dimmed in widget
   - Verify disabled hosts don't affect tray icon state
   - Verify tray icon tooltip counts only enabled hosts
   - Reload config → verify disabled hosts remain disabled

2. **Settings Inputs:**
   - Add hosts with custom interval/timeout/buffer
   - Change settings without adding hosts
   - Verify validation errors for invalid ranges
   - Verify settings apply to all existing hosts

3. **Clear Log:**
   - Clear an existing log file
   - Verify log is deleted
   - Verify menu item is disabled when no log exists

4. **Combined:**
   - Disable hosts while paused
   - Reload config with mixed enabled/disabled hosts
   - Verify config file preserves disabled state with `-` prefix
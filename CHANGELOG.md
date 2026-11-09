# Change Log

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
- Logs `ENABLED` / `DISABLED` state changes to `pingmon.log`
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
- Form title changed to "pingmon - add host / settings"
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
- Menu item is enabled only when `pingmon.log` exists

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
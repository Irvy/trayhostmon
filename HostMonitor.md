# HostMonitor.cs

Pings one host in a loop and reports state changes.

## Class `HostMonitor`
- `HostMonitor(HostState host)`
- `void Start(CancellationToken parent)`: runs the loop on a background thread. Uses its own linked token, so `Stop` works without affecting other hosts.
- `void Stop()`: cancels this host's loop only.
- `HostState Host`: the state object this monitor updates.
- `event Action<HostState, State oldState, TimeSpan duration> StateChanged`: raised on every Up/Down change.

## Logic
- Each cycle: `Ping.SendPingAsync(address, timeoutMs, buffer)`. Sets `HostState.LastCheck` to the time of the result.
- Buffer size from `HostConfig.Buffer` (default 32 bytes). A buffer of 0 uses the system default ping size.
- Success resets `FailCount` and sets Up.
- Failure (or exception) increments `FailCount`; Down is set when `FailCount >= threshold`.
- Going Down clears `Acked`.
- Waits `intervalSec` between pings.

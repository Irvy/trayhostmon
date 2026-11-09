# HostMonitor.cs

Pings one host in a loop and reports state changes.

## Class `HostMonitor`
- `HostMonitor(HostState host)`
- `void Start(CancellationToken parent)`: runs the loop on a background thread. Uses its own linked token, so `Stop` works without affecting other hosts.
- `void Stop()`: cancels this host's loop only.
- `HostState Host`: the state object this monitor updates.
- `event Action<HostState, State oldState, TimeSpan duration> StateChanged`: raised on every Up/Down change.

## Logic
- Each cycle: `Ping.SendPingAsync(address, timeoutMs)`. Sets `HostState.LastCheck` to the time of the result.
- Success resets `FailCount` and sets Up.
- Failure (or exception) increments `FailCount`; Down is set when `FailCount >= threshold`.
- Going Down clears `Acked`.
- Waits `intervalSec` between pings.

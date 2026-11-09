# HostState.cs

Runtime state of each host.

## Types
- `enum State { Unknown, Up, Down }`
- `enum Overall { AllUp, DownAcked, DownUnacked, Paused }`
- `class HostState`:
  - `Cfg` (HostConfig), `State`, `Since` (time of last change), `LastCheck` (time of last ping result), `Acked`, `FailCount`, `LastRttMs` (-1 = none), `Enabled` (true = actively monitored).
  - Constructor `HostState(HostConfig)`.
- `static Overall HostState.Worst(IEnumerable<HostState>)`: DownUnacked > DownAcked > AllUp. Unknown hosts and disabled hosts are ignored.

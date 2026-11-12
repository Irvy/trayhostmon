# tools\MakeIcon.cs

Standalone build-time helper that renders TrayHostMon tray-state circles into an
`.ico` file. It reuses the **exact** drawing from `IconFactory.cs` (`Make`):
transparent background, anti-aliased filled ellipse plus a `rgb(60,60,60)`
outline. At size 16 the geometry is identical (margin 1, diameter 13, pen 1).

The generator writes the ICO itself as **32-bpp BGRA** DIB images plus a 1-bpp
AND mask, so colour and per-pixel transparency are preserved. (Do not use
`System.Drawing.Icon.Save` here: it re-encodes to a 16-colour/4-bpp DIB and
loses the alpha.)

It lives in `tools\` so `build.cmd`'s `*.cs` glob does **not** pick it up
(which would otherwise cause a duplicate `Main`). No dependency on app sources.

## Usage

```
makeicon.exe [state] [out.ico] [sizes]
```

- `state` (default `allup`):
  - `allup`       green   `rgb(46,160,67)`   (healthy)
  - `downunacked` red     `rgb(218,54,51)`   (down, not acknowledged)
  - `downacked`   orange  `rgb(227,140,26)`  (down, acknowledged)
  - `paused`      grey    `rgb(140,140,140)`
- `out.ico` (default `TrayHostMon.ico`, relative to the current directory)
- `sizes` (default `16,32,48`): comma-separated pixel sizes, 1..256.
  Each size is drawn with the same scaled geometry.

## Build & run (from the project root)

```
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
"%CSC%" /nologo /target:exe /out:tools\makeicon.exe tools\MakeIcon.cs /r:System.dll /r:System.Drawing.dll
tools\makeicon.exe allup TrayHostMon.ico
```

The resulting `TrayHostMon.ico` is embedded into `TrayHostMon.exe` by `build.cmd`
(`/win32icon:TrayHostMon.ico`). Re-run with another `state` (and/or `sizes`) to
produce additional icons later, e.g. `tools\makeicon.exe downunacked down.ico`.

Note: 256 is allowed but stored as an uncompressed BMP, which adds ~256 KB to
the exe; the default `16,32,48` keeps it small while staying crisp up to the
Explorer "large" view.

@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll
set SRC=*.cs

:: --- 1. Build the icon generator (makeicon.exe) ----------------------------
:: tools\MakeIcon.cs needs its own explicit csc invocation because it lives in
:: tools\ and is intentionally excluded from the app's `*.cs` build.
"%CSC%" /nologo /target:exe /out:tools\makeicon.exe tools\MakeIcon.cs /r:System.dll /r:System.Drawing.dll
if errorlevel 1 (
  echo BUILD FAILED: makeicon.exe
  exit /b 1
)

:: --- 2. Generate TrayHostMon.ico -------------------------------------------
:: "allup" = green / healthy. Pass a different state and/or sizes manually for
:: a different icon; see tools\MakeIcon.md.
tools\makeicon.exe allup TrayHostMon.ico
if errorlevel 1 (
  echo BUILD FAILED: TrayHostMon.ico generation
  exit /b 1
)

:: --- 3. Build the app ------------------------------------------------------
"%CSC%" /nologo /optimize+ /define:DEBUG_MEM /target:winexe /win32icon:TrayHostMon.ico /out:TrayHostMon.exe %REFS% %SRC%
if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)

echo BUILD OK
endlocal

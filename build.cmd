@echo off
setlocal
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set REFS=/r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll
set SRC=*.cs
"%CSC%" /nologo /optimize+ /define:DEBUG_MEM /target:winexe /win32icon:pingmon.ico /out:pingmon.exe %REFS% %SRC%
if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)
echo BUILD OK
endlocal

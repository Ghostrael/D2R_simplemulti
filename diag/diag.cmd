@echo off
rem Read-only: lists lock candidates (named handles) inside running D2R processes. Closes nothing.
rem Run with one D2R window open when the second client stops starting after a game update.
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 /out:Diag.exe Diag.cs || exit /b 1
.\Diag.exe
pause

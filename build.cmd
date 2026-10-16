@echo off
rem Builds with the .NET Framework compiler that ships with Windows (nothing is downloaded).
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /platform:x64 /optimize /codepage:65001 /reference:System.Security.dll /out:D2RMulti.exe Program.cs Unlocker.cs Creds.cs Token.cs Profiles.cs Lang.cs

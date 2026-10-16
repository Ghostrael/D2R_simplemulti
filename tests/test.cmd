@echo off
rem Builds and runs the tests. Uses a test registry key and a temp folder; the real Battle.net key and Settings.json are not modified.
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:exe /platform:x64 /codepage:65001 /main:Test /reference:System.Security.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:Test.exe Test.cs ..\Program.cs ..\Unlocker.cs ..\Creds.cs ..\Token.cs ..\Profiles.cs ..\Lang.cs || exit /b 1
.\Test.exe

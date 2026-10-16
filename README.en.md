# D2R SimpleMulti

[한국어](README.md) · **English**

A multi-client launcher for Diablo II: Resurrected that starts one game window per account. The source is six C# files, small enough to read; use the release exe or build it yourself.

The UI is available in Korean and English. It follows the Windows language at first; change it with the language box at the bottom right, and the launcher restarts in that language (`Lang.cs`; each UI string is paired in place as `L.T("한국어", "English")`).

## Download

Unzip `D2R_SimpleMulti-<version>-win-x64.zip` from [Releases](https://github.com/Ghostrael/D2R_simplemulti/releases) and run `D2RMulti.exe` (Windows 10/11, x64).
The exe is built by GitHub Actions from the tagged source ([release.yml](.github/workflows/release.yml)) and carries a build provenance attestation. To check that a download was built from this repository:

```
gh attestation verify D2RMulti.exe --repo Ghostrael/D2R_simplemulti
```

It is not code-signed, so Windows SmartScreen may warn on first run ("More info" → "Run anyway"). If you would rather not trust someone else's exe, build it yourself as below.

## Build · Run

1. Double-click `build.cmd` → `D2RMulti.exe` appears in the same folder (it uses the .NET Framework compiler that ships with Windows; nothing is downloaded). Close the launcher first if it is running, or the exe cannot be overwritten.
2. Run `D2RMulti.exe` → **Add account**
   - Name · email (only a label, never sent anywhere) · region kr/us/eu
   - **Log in in browser** → log in with that account in the Edge InPrivate window (approve 2FA there too)
   - When login finishes an error page appears (expected). Copy its address (`http://localhost:0/?ST=…`) into the **Address** field and save
   - Use a **new InPrivate window** for each account (the same window logs in as the previous account)
3. Select an account and press **Launch** (or double-click). From the second account on, the lock is released automatically before launching.
   Launch the next account **only after the previous window reaches character select** — there is a single token slot, so launching too early overwrites it.

The launcher's **Help** button shows the same instructions, troubleshooting, and where things are stored.

## Mods · Graphics (per account)

In **Edit account**, choose a mod and a graphics profile for each account (leave the token field empty to keep the current token).

- **Mod**: only folders with `Mods\<name>\<name>.mpq` in the game folder are listed (e.g. `comg`). The game is launched with `-mod <name> -txt`.
  Blizzard's terms do not allow client modifications online — using mods online is your own call.
- **Graphics profile**: every D2R window reads the single file `Saved Games\Diablo II Resurrected\Settings.json`. A profile is a `Settings*.json` copy in the same folder (existing files such as `Settings01.json` show up too), and the chosen profile is copied over `Settings.json` right before launch. "(keep current settings)" leaves it untouched.
- **Graphics** button:
  - **Save current as profile** — saves what you set in game as `Settings-<name>.json`. This is the most reliable way.
  - The table lets you edit 26 values directly (window mode, windowed resolution, framerate cap, quality levels, …). There is no public documentation of which menu option each number means, so raw numbers are shown (start with the obvious ones such as VSync 0/1, resolution `1280x720`, scale in %). Only the value on that line is changed; the rest of the file is left as is.
- Settings changed in game are saved to `Settings.json`, not to the profile file. To keep them as a profile, use "Save current as profile" again.

## Tests · Diagnostics

- `tests\test.cmd` — builds and runs 26 tests. It only uses a test registry key and a temp folder; the real Battle.net key and `Settings.json` are not touched (a copy of `Settings.json` is used). A test entry is briefly created in Credential Manager and then deleted.
- `diag\diag.cmd` — if the second window stops starting after a game update, run this with one D2R window open. It lists lock candidates inside D2R (named Events, Mutants, …) and closes nothing. If the lock was renamed, the new name shows up here (that is how the current name was found in 2026-10).

## Login: a token instead of a password

The first version logged in with the `-username -password -address` launch arguments and hung at "connecting" from the very first window. That method does not work for accounts with two-factor authentication and gets no response when Blizzard asks for a captcha (per the Diablo2RLoader docs). So it was switched to what the Battle.net launcher itself does:

the token from a web login is encrypted with DPAPI and written to `WEB_TOKEN` under `HKCU\SOFTWARE\Blizzard Entertainment\Battle.net\Launch Options\OSI`, and D2R is started with `-uid osi`. On the author's PC, Battle.net was confirmed to write the same location in the same format (binary value · `REGION`).

## Security problems of the usual methods, and how this launcher handles them

| Problem | This launcher |
|---|---|
| Executables of unknown origin | Release exes are built by GitHub Actions from the public source with a provenance attestation (check with `gh attestation verify`). To build it yourself, read `Unlocker.cs` (lock release) · `Token.cs` (token hand-off) · `Creds.cs` (token storage) · `Profiles.cs` (graphics profiles · mod list) · `Lang.cs` (language) · `Program.cs` (UI) and build it yourself. No network code (the browser logs in to Blizzard's site directly) |
| Administrator rights (handle.exe · Process Explorer) | Not needed. A D2R process started by the same user can be opened with `PROCESS_QUERY_INFORMATION \| PROCESS_DUP_HANDLE`. Tested from a non-elevated shell |
| Account data stored in plain text | The launcher **never takes a password**. Tokens are kept only in **Windows Credential Manager** (`D2RMulti:<email>`, DPAPI-encrypted), and are DPAPI-encrypted when written to the registry too. The command line is just `-uid osi`. `%APPDATA%\D2RMulti\settings.txt` holds only language · name · email · region · mod · graphics profile · game path |
| Account bans | It does not read or write game memory, injects no DLLs, and automates no input. All it does is close one lock event handle (name ending in `Check For Other Instances` — measured as `DiabloII Check For Other Instances` in 2026-10, formerly `DiaBlo_II …`) and launch with the token in the same place the Battle.net launcher uses |

## Remaining risks · unknowns

- **A token is as good as a login**: if it leaks, someone can log in to that account without the password. Never show the pasted address to anyone. Malware running as the same Windows user can decrypt Credential Manager and the registry (the same holds for Battle.net's own token).
- **Two-factor authentication (OTP · authenticator)**: the saying "OTP accounts can only log in through the Battle.net launcher" is about the launch-argument (`-username -password`) method, which has nowhere to take an OTP. This launcher has you enter the OTP once during the browser login that issues the token, and launching does not ask for it (Diablo2RLoader docs: the token method works for MFA accounts). **However, it has not been tested with an OTP account.** If it fails, start the game from the Battle.net launcher and use **Unlock only** — Battle.net does the login, so OTP does not matter.
- **Token lifetime is unknown**: once it expires, connecting fails. Then log in again in **Edit account** to get a new token.
- **Terms of service**: Blizzard does not officially support multi-client play. No bans are known for manual multi-client play without automation, but there is no guarantee.
- If D2R runs **as administrator**, the lock cannot be released. The launcher would then have to run as administrator too (not recommended).

## Uninstall

The Remove button also deletes the account's Credential Manager entry. To remove the launcher completely, delete the `%APPDATA%\D2RMulti` folder and, under Control Panel → Credential Manager → Windows Credentials, the entries starting with `D2RMulti:`.

## License · Credits

[MIT](LICENSE). The token registry location (`OSI\WEB_TOKEN`), the DPAPI entropy value, and the `-uid osi` launch method were learned from [shupershuff/Diablo2RLoader](https://github.com/shupershuff/Diablo2RLoader) (MIT). The code was written separately.

Diablo II: Resurrected and Battle.net are trademarks of Blizzard Entertainment. This project is not affiliated with Blizzard.

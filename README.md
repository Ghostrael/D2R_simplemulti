# D2R SimpleMulti

**한국어** · [English](README.en.md)

디아블로 2 레저렉션을 계정별로 여러 개 띄우는 다클라 런처. 소스는 C# 여섯 파일로 직접 읽을 수 있는 크기이고, 릴리즈 exe 를 받거나 직접 빌드해 쓴다.

화면 언어는 한국어 · English. 처음에는 Windows 언어를 따르고, 런처 오른쪽 아래의 언어 선택에서 바꾸면 런처가 다시 시작되며 적용된다 (`Lang.cs`, 문구는 쓰는 자리에서 `L.T("한국어", "English")` 로 짝지어 있다).

## 받기

[Releases](https://github.com/Ghostrael/D2R_simplemulti/releases) 의 `D2R_SimpleMulti-<버전>-win-x64.zip` 을 풀어 `D2RMulti.exe` 를 실행한다 (Windows 10/11, x64).
이 exe 는 GitHub Actions 가 태그의 소스로 빌드한 것이고 ([release.yml](.github/workflows/release.yml)), 출처 증명이 붙어 있다. 받은 파일이 이 저장소에서 빌드된 것인지 확인하려면:

```
gh attestation verify D2RMulti.exe --repo Ghostrael/D2R_simplemulti
```

코드 서명은 하지 않았으므로 처음 실행할 때 Windows SmartScreen 경고가 뜰 수 있다 ("추가 정보" → "실행"). 남이 만든 exe 를 믿고 싶지 않으면 아래처럼 직접 빌드한다.

## 빌드 · 실행

1. `build.cmd` 를 더블클릭 → 같은 폴더에 `D2RMulti.exe` 가 생긴다 (Windows 에 들어 있는 .NET Framework 컴파일러 사용, 내려받는 것 없음). 런처가 켜져 있으면 덮어쓰지 못하니 먼저 닫는다.
2. `D2RMulti.exe` 실행 → **계정 추가**
   - 별칭 · 이메일 (구분용, 어디에도 보내지 않음) · 지역 kr/us/eu
   - **브라우저에서 로그인** → Edge InPrivate 창에서 그 계정으로 로그인 (2단계 인증도 여기서 승인)
   - 로그인이 끝나면 오류 페이지가 뜬다 (정상). 그 주소 (`http://localhost:0/?ST=…`) 를 복사해 **주소** 칸에 붙여넣고 저장
   - 계정마다 **새 InPrivate 창** 으로 한다 (같은 창이면 앞 계정으로 로그인된다)
3. 계정을 골라 **실행** (또는 더블클릭). 두 번째 계정부터는 실행 전에 잠금을 자동으로 푼다.
   **앞 창이 캐릭터 선택 화면까지 간 뒤에** 다음 계정을 실행한다 — 토큰 자리가 하나뿐이라 서두르면 덮인다.

런처의 **도움말** 버튼을 누르면 실행 방법 · 안 될 때 · 저장 위치가 같은 내용으로 나온다.

## 모드 · 그래픽 (계정마다)

**계정 편집** 에서 계정마다 모드와 그래픽 프로필을 고른다 (토큰 칸은 비워 두면 그대로).

- **모드**: 게임 폴더의 `Mods\<이름>\<이름>.mpq` 가 있는 것만 목록에 나온다 (예: `comg`). 고르면 `-mod <이름> -txt` 로 실행한다.
  블리자드 약관은 온라인에서 클라이언트 변경을 허용하지 않는다 — 온라인 사용은 본인 판단.
- **그래픽 프로필**: D2R 은 모든 창이 `Saved Games\Diablo II Resurrected\Settings.json` 하나를 읽는다. 프로필은 같은 폴더의 `Settings*.json` 사본이고 (`Settings01.json` 같은 기존 파일도 그대로 보인다), 실행 직전에 고른 프로필을 `Settings.json` 으로 복사한다. "(지금 설정 그대로)" 면 건드리지 않는다.
- **그래픽 설정** 버튼:
  - **지금 설정을 새 프로필로** — 게임 안에서 맞춘 설정을 `Settings-<이름>.json` 으로 저장. 이것이 가장 확실한 방법이다.
  - 표에서 창 모드 · 창 해상도 · 프레임 제한 · 품질 등 26개 값을 직접 고칠 수 있다. 숫자가 메뉴의 어느 선택지인지는 공개 자료가 없어 숫자 그대로 보여 준다 (VSync 0/1, 해상도 `1280x720`, 배율 % 처럼 뜻이 분명한 것부터 쓰는 것을 권한다). 그 줄의 값만 바꾸고 파일의 나머지는 그대로 둔다.
- 게임 안에서 바꾼 설정은 `Settings.json` 에 저장되고 프로필 파일에는 반영되지 않는다. 프로필로 남기려면 다시 "지금 설정을 새 프로필로".

## 시험 · 진단

- `tests\test.cmd` — 시험 26개를 빌드해 돌린다. 시험용 레지스트리 키 · 임시 폴더만 쓰고, 진짜 Battle.net 키와 `Settings.json` 은 건드리지 않는다 (`Settings.json` 은 사본을 쓴다). 자격 증명 관리자에 시험 항목을 잠깐 만들었다가 지운다.
- `diag\diag.cmd` — 게임 업데이트 뒤 두 번째 창이 다시 안 뜨면, D2R 창 하나를 띄운 채 실행한다. D2R 안의 잠금 후보 (이름 붙은 Event · Mutant 등) 를 나열만 하고 아무것도 닫지 않는다. 잠금 이름이 바뀌었으면 여기서 보인다 (2026-10 에 실제로 그렇게 찾았다).

## 로그인 방식: 비밀번호 대신 토큰

처음 판은 `-username -password -address` 실행 인자로 로그인했는데, 첫 창부터 "접속 중" 에서 끝나지 않았다. 이 방식은 2단계 인증 계정에서 동작하지 않고, 블리자드가 캡차를 요구하면 응답이 없다 (Diablo2RLoader 문서). 그래서 Battle.net 런처와 같은 방식으로 바꿨다:

웹 로그인으로 받은 토큰을 DPAPI 로 암호화해 `HKCU\SOFTWARE\Blizzard Entertainment\Battle.net\Launch Options\OSI` 의 `WEB_TOKEN` 에 쓰고, D2R 을 `-uid osi` 로 띄운다. 이 PC 의 Battle.net 도 같은 자리에 같은 형식 (이진값 · `REGION`) 으로 쓰는 것을 확인했다.

## 기존 방법의 보안 문제와 이 런처의 대응

| 문제 | 이 런처 |
|---|---|
| 출처 불명 exe | 릴리즈 exe 는 GitHub Actions 가 공개된 소스로 빌드하고 출처 증명을 붙인다 (`gh attestation verify` 로 확인). 직접 빌드하려면 `Unlocker.cs` (잠금 해제) · `Token.cs` (토큰 넘기기) · `Creds.cs` (토큰 저장) · `Profiles.cs` (그래픽 프로필 · 모드 목록) · `Lang.cs` (언어) · `Program.cs` (창) 를 읽고 직접 빌드. 네트워크 코드 없음 (로그인은 브라우저가 블리자드 사이트와 직접 한다) |
| 관리자 권한 (handle.exe · Process Explorer) | 필요 없다. 같은 사용자가 띄운 D2R 프로세스는 `PROCESS_QUERY_INFORMATION \| PROCESS_DUP_HANDLE` 로 열 수 있다. 관리자가 아닌 셸에서 시험해 통과 |
| 계정 정보 평문 저장 | 런처는 **비밀번호를 받지 않는다**. 토큰은 **Windows 자격 증명 관리자** 에만 (`D2RMulti:<이메일>`, DPAPI 암호화). 레지스트리에 쓸 때도 DPAPI 로 암호화. 명령줄에는 `-uid osi` 뿐. `%APPDATA%\D2RMulti\settings.txt` 에는 언어 · 별칭 · 이메일 · 지역 · 모드 · 그래픽 프로필 · 게임 경로만 |
| 계정 제재 | 게임 메모리를 읽거나 쓰지 않고, DLL 주입 · 입력 자동화도 없다. 하는 일은 잠금 이벤트 핸들 하나 (이름 끝이 `Check For Other Instances` — 2026-10 실측 `DiabloII Check For Other Instances`, 예전 판 `DiaBlo_II …`) 를 닫는 것과, Battle.net 런처와 같은 자리에 토큰을 두고 실행하는 것뿐 |

## 남는 위험 · 모르는 것

- **토큰은 로그인과 같다**: 토큰이 새면 비밀번호 없이 그 계정으로 접속될 수 있다. 붙여넣은 주소를 남에게 보이지 말 것. 같은 Windows 사용자로 도는 악성 프로그램은 자격 증명 관리자와 레지스트리를 풀 수 있다 (Battle.net 자체의 토큰도 마찬가지).
- **2단계 인증 (OTP · 인증기)**: "OTP 계정은 Battle.net 런처로만 접속된다" 는 말은 실행 인자 (`-username -password`) 방식 이야기다 — 그 방식에는 OTP 를 받을 자리가 없다. 이 런처는 OTP 를 토큰을 받는 브라우저 로그인에서 한 번 입력하고, 실행할 때는 묻지 않는다 (Diablo2RLoader 문서: 토큰 방식은 MFA 계정에서 동작). **단, OTP 계정으로 직접 시험하지는 못했다.** 안 되면 Battle.net 런처로 띄우고 **잠금만 풀기** 를 쓴다 — 로그인을 Battle.net 이 하므로 OTP 와 무관하다.
- **토큰의 수명은 확인하지 못했다**: 언젠가 만료되면 접속이 실패한다. 그때는 **계정 편집** 에서 다시 로그인해 새 토큰을 받는다.
- **약관**: 블리자드는 다클라를 공식 지원하지 않는다. 자동화 없이 손으로 플레이하는 다클라로 제재받은 사례는 알려진 것이 없지만, 보장되지는 않는다.
- D2R 이 **관리자 권한으로** 실행 중이면 잠금을 풀 수 없다. 그때는 런처도 관리자로 실행해야 한다 (권장하지 않음).

## 지우기

계정 삭제 버튼은 자격 증명 관리자의 항목까지 지운다. 런처를 통째로 없앨 때는 `%APPDATA%\D2RMulti` 폴더와, 제어판 → 자격 증명 관리자 → Windows 자격 증명에서 `D2RMulti:` 로 시작하는 항목을 지운다.

## 라이선스 · 출처

[MIT](LICENSE). 토큰을 넘기는 레지스트리 자리 (`OSI\WEB_TOKEN`) · DPAPI 엔트로피 값 · `-uid osi` 실행 방식은 [shupershuff/Diablo2RLoader](https://github.com/shupershuff/Diablo2RLoader) (MIT) 에서 알게 되었다. 코드는 따로 썼다.

Diablo II: Resurrected · Battle.net 은 Blizzard Entertainment 의 상표이며, 이 프로젝트는 Blizzard 와 관계없다.

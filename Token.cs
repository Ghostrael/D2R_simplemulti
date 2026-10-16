// 토큰 로그인. Battle.net 런처가 하는 방식과 같다:
// 웹 로그인으로 받은 토큰을 DPAPI(현재 Windows 사용자 · 고정 엔트로피)로 암호화해
// HKCU\...\Battle.net\Launch Options\OSI 의 WEB_TOKEN 에 쓰고, D2R 을 "-uid osi" 로 띄우면 D2R 이 그것을 읽는다.
// 비밀번호를 쓰지 않으므로 2단계 인증 계정도 되고, 명령줄에 비밀 정보가 남지 않는다.
// (엔트로피 값 · 키 이름 출처: github.com/shupershuff/Diablo2RLoader 의 D2Loader.ps1)
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

static class Token
{
    public const string OsiKey = @"SOFTWARE\Blizzard Entertainment\Battle.net\Launch Options\OSI";
    public const string LoginUrl = "https://us.battle.net/login/en/?externalChallenge=login&app=OSI";
    static readonly byte[] Entropy = { 0xc8, 0x76, 0xf4, 0xae, 0x4c, 0x95, 0x2e, 0xfe, 0xf2, 0xfa, 0x0f, 0x54, 0x19, 0xc0, 0x9c, 0x43 };

    // 로그인 끝의 오류 페이지 주소 (http://localhost:0/?ST=...) 나 토큰 자체에서 토큰을 꺼낸다. 못 꺼내면 null.
    public static string Extract(string input)
    {
        input = input.Trim();
        Match m = Regex.Match(input, @"[?&]ST=([^&#\s]+)");
        if (m.Success) return Uri.UnescapeDataString(m.Groups[1].Value);
        if (input.Length > 0 && !input.Contains("://") && !Regex.IsMatch(input, @"[\s&?=]")) return input;
        return null;
    }

    public static void Write(string keyPath, string region, string token)
    {
        byte[] plain = Encoding.UTF8.GetBytes(token);
        byte[] sealedBytes = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
        Array.Clear(plain, 0, plain.Length);
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(keyPath))
        {
            k.SetValue("REGION", region.ToUpperInvariant(), RegistryValueKind.String);
            k.SetValue("WEB_TOKEN", sealedBytes, RegistryValueKind.Binary);
        }
    }
}

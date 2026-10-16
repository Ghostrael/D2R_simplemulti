using System;
using System.Diagnostics;
using System.Threading;

static class Test
{
    static int fails = 0;
    static void Check(bool ok, string what) { Console.WriteLine((ok ? "PASS " : "FAIL ") + what); if (!ok) fails++; }

    static int Main()
    {
        // 1) 잠금 해제: 자식 프로세스가 이름 붙은 이벤트를 잡고 있다
        string name = "D2RMultiTest_" + Guid.NewGuid().ToString("N") + " check for OTHER instances";
        var psi = new ProcessStartInfo("powershell.exe",
            "-NoProfile -Command \"$e = New-Object System.Threading.EventWaitHandle($false, 'ManualReset', '" + name + "'); Start-Sleep 30\"");
        psi.UseShellExecute = false; psi.CreateNoWindow = true;
        var child = Process.Start(psi);
        EventWaitHandle probe = null;
        for (int i = 0; i < 100 && !EventWaitHandle.TryOpenExisting(name, out probe); i++) Thread.Sleep(100);
        Check(probe != null, "자식이 이벤트를 만들었다");
        if (probe != null) probe.Dispose();

        int n = Unlocker.Unlock(child.Id, Unlocker.LockName);
        Check(n == 1, "핸들 1개를 닫았다 (" + n + ")");
        EventWaitHandle after;
        Check(!EventWaitHandle.TryOpenExisting(name, out after), "닫은 뒤 이벤트가 사라졌다");
        Check(!child.HasExited, "자식 프로세스는 살아 있다");
        Check(Unlocker.Unlock(child.Id, Unlocker.LockName) == 0, "두 번째 실행은 0개");
        child.Kill();

        // 2) 자격 증명 저장소 왕복
        string email = "d2rmulti-test@example.invalid";
        string pw = "p a\"ss\\ 한글!";
        Creds.Save(email, pw);
        Check(Creds.Load(email) == pw, "자격 증명 왕복");
        Creds.Delete(email);
        Check(Creds.Load(email) == null, "자격 증명 삭제");

        // 3) 토큰 꺼내기
        Check(Token.Extract("http://localhost:0/?ST=KR-abc123-XYZ&foo=1") == "KR-abc123-XYZ", "주소에서 토큰");
        Check(Token.Extract("  localhost:0/?ST=US-a%2Bb  ") == "US-a+b", "스킴 없는 주소 · 인코딩");
        Check(Token.Extract("KR-abc123") == "KR-abc123", "토큰만 붙여넣기");
        Check(Token.Extract("https://us.battle.net/login/en/?app=OSI") == null, "토큰 없는 주소는 거절");
        Check(Token.Extract("") == null, "빈 입력 거절");

        // 4) 레지스트리 쓰기 (시험용 키) — 같은 엔트로피로 풀리는가
        string key = @"SOFTWARE\D2RMultiTest\OSI";
        Token.Write(key, "kr", "KR-test-토큰");
        using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(key))
        {
            byte[] ent = { 0xc8, 0x76, 0xf4, 0xae, 0x4c, 0x95, 0x2e, 0xfe, 0xf2, 0xfa, 0x0f, 0x54, 0x19, 0xc0, 0x9c, 0x43 };
            byte[] plain = System.Security.Cryptography.ProtectedData.Unprotect((byte[])k.GetValue("WEB_TOKEN"), ent, System.Security.Cryptography.DataProtectionScope.CurrentUser);
            Check(System.Text.Encoding.UTF8.GetString(plain) == "KR-test-토큰", "WEB_TOKEN 복호화 일치");
            Check((string)k.GetValue("REGION") == "KR", "REGION = KR");
            Check(k.GetValueKind("WEB_TOKEN") == Microsoft.Win32.RegistryValueKind.Binary, "WEB_TOKEN 은 이진값");
        }
        Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\D2RMultiTest");
        // 5) 그래픽 프로필 — 진짜 Settings.json 의 사본에서 (없는 PC 에서는 만든 파일로)
        string pd = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "d2rmulti_profiles_test");
        if (System.IO.Directory.Exists(pd)) System.IO.Directory.Delete(pd, true);
        System.IO.Directory.CreateDirectory(pd);
        string real = System.IO.Path.Combine(Profiles.Dir, "Settings.json");
        bool hasReal = System.IO.File.Exists(real);
        if (hasReal) System.IO.File.Copy(real, System.IO.Path.Combine(pd, "Settings.json"));
        else
        {
            var sb = new System.Text.StringBuilder("{\n");
            foreach (string[] k in Profiles.GraphicsKeys)
                sb.Append("    \"" + k[0] + "\": " + (k[0] == "Screen Resolution (Windowed)" ? "\"1920x1080\"" : "1") + ",\n");
            sb.Append("    \"Music Volume\": 10\n}");
            System.IO.File.WriteAllText(System.IO.Path.Combine(pd, "Settings.json"), sb.ToString());
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(pd, "Settings01.json"), "{\n    \"VSync\": 0\n}");
        Profiles.Dir = pd;
        Check(string.Join(",", Profiles.List().ToArray()) == "Settings01", "목록은 Settings.json 을 뺀다");
        var vals = Profiles.Read("Settings");
        Check(vals.Count == Profiles.GraphicsKeys.Length, "그래픽 키 " + vals.Count + "/" + Profiles.GraphicsKeys.Length + " 개를 읽음");
        byte[] before = System.IO.File.ReadAllBytes(Profiles.PathOf("Settings"));
        var change = new System.Collections.Generic.Dictionary<string, string>();
        change["VSync"] = "0"; change["Screen Resolution (Windowed)"] = "1280x720";
        Profiles.Write("Settings", change);
        var changed = Profiles.Read("Settings");
        Check(changed["VSync"] == "0" && changed["Screen Resolution (Windowed)"] == "1280x720", "값 바꾸기");
        string[] la = System.Text.Encoding.UTF8.GetString(before).Split('\n'), lb = System.IO.File.ReadAllText(Profiles.PathOf("Settings")).Split('\n');
        int diff = 0; for (int i = 0; i < la.Length; i++) if (la[i] != lb[i]) diff++;
        Check(la.Length == lb.Length && diff == 2, "바뀐 줄은 2줄뿐 (" + diff + ")");
        byte[] raw = System.IO.File.ReadAllBytes(Profiles.PathOf("Settings"));
        Check(raw[0] == (byte)'{', "BOM 없음 유지");
        bool threw = false; try { change.Clear(); change["VSync"] = "켜기"; Profiles.Write("Settings", change); } catch (ArgumentException) { threw = true; }
        Check(threw, "숫자 칸에 글자는 거절");
        Check(Profiles.SaveCurrentAs("작은창") == "Settings-작은창" && System.IO.File.Exists(Profiles.PathOf("Settings-작은창")), "새 프로필 이름 = Settings-작은창");
        threw = false; try { Profiles.SaveCurrentAs("작은창"); } catch (System.IO.IOException) { threw = true; }
        Check(threw, "같은 이름 덮어쓰기 거절");
        Profiles.Apply("Settings01");
        Check(Profiles.Read("Settings")["VSync"] == "0" && Profiles.Read("Settings").Count == 1, "프로필 적용 = Settings.json 으로 복사");
        if (hasReal) Check(System.IO.File.ReadAllBytes(real).Length > 1000, "진짜 Settings.json 은 그대로");

        // 6) 모드 목록
        string gd = System.IO.Path.Combine(pd, "game");
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(gd, @"Mods\comg\comg.mpq"));
        System.IO.Directory.CreateDirectory(System.IO.Path.Combine(gd, @"Mods\Comg SKINPACK"));
        Check(string.Join(",", Profiles.Mods(gd).ToArray()) == "comg", "mpq 있는 폴더만 모드");
        System.IO.Directory.Delete(pd, true);
        Console.WriteLine(fails == 0 ? "ALL PASS" : fails + " FAIL");
        return fails;
    }
}

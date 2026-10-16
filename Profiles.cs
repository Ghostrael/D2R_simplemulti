// 그래픽 프로필 · 모드 목록.
// D2R 은 모든 창이 Saved Games\Diablo II Resurrected\Settings.json 하나를 읽는다.
// 프로필은 같은 폴더의 Settings*.json 사본이고, 실행 직전에 고른 프로필을 Settings.json 으로 복사한다.
// 값 편집은 해당 줄의 값만 바꿔 파일의 나머지 형식을 그대로 둔다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

static class Profiles
{
    public static string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"Saved Games\Diablo II Resurrected");
    public const string Current = "Settings";

    // 런처에서 편집하는 항목: { 파일 키 (= 영어 화면 이름), 한국어 이름 }. 값의 뜻은 공개 자료가 없어 숫자 그대로 보여 준다.
    public static readonly string[][] GraphicsKeys =
    {
        new[] { "Window Mode", "창 모드" },
        new[] { "Screen Resolution (Windowed)", "창 해상도 (가로x세로)" },
        new[] { "Game Resolution", "게임 해상도" },
        new[] { "Resolution Scale", "해상도 배율 (%)" },
        new[] { "Dynamic Resolution Scaling", "동적 해상도" },
        new[] { "Framerate Cap", "프레임 제한" },
        new[] { "Framerate Target", "목표 프레임" },
        new[] { "VSync", "수직 동기화 (0/1)" },
        new[] { "NVIDIA DLSS", "DLSS" },
        new[] { "Anti Aliasing", "안티에일리어싱" },
        new[] { "Sharpening", "선명도" },
        new[] { "Graphic Presets", "그래픽 프리셋" },
        new[] { "Texture Quality", "텍스처 품질" },
        new[] { "Texture Anisotropy", "텍스처 비등방성" },
        new[] { "Environment Detail", "환경 디테일" },
        new[] { "Character Detail", "캐릭터 디테일" },
        new[] { "Shadow Quality", "그림자 품질" },
        new[] { "Blended Shadows", "혼합 그림자" },
        new[] { "Light Quality", "조명 품질" },
        new[] { "Vfx Quality", "시각 효과 품질" },
        new[] { "Vfx Lighting Quality", "시각 효과 조명" },
        new[] { "Ambient Occlusion Quality", "주변광 차폐" },
        new[] { "Atmospherics Quality", "대기 효과" },
        new[] { "Transparency Quality", "투명도 품질" },
        new[] { "Gamma", "감마" },
        new[] { "Contrast", "대비" },
    };

    public static string PathOf(string name) { return Path.Combine(Dir, name + ".json"); }

    // Settings.json 을 뺀 Settings*.json 의 이름 (확장자 없이)
    public static List<string> List()
    {
        var names = new List<string>();
        if (!Directory.Exists(Dir)) return names;
        foreach (string f in Directory.GetFiles(Dir, "Settings*.json"))
        {
            string n = Path.GetFileNameWithoutExtension(f);
            if (n != Current) names.Add(n);
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names;
    }

    public static void Apply(string name)
    {
        File.Copy(PathOf(name), PathOf(Current), true);
    }

    // 지금 Settings.json 을 새 프로필로 저장한다. 이름은 Settings 로 시작하게 맞춘다.
    public static string SaveCurrentAs(string name)
    {
        name = name.Trim();
        if (name == "" || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new ArgumentException(L.T("쓸 수 없는 이름입니다", "Invalid name"));
        if (!name.StartsWith(Current, StringComparison.OrdinalIgnoreCase)) name = Current + "-" + name;
        if (name.Equals(Current, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(L.T("Settings 는 게임이 쓰는 파일입니다", "Settings is the game's own file"));
        File.Copy(PathOf(Current), PathOf(name), false);
        return name;
    }

    static Regex ValueRe(string key)
    {
        return new Regex("(\"" + Regex.Escape(key) + "\"\\s*:\\s*)(-?\\d+|\"[^\"]*\")");
    }

    // 키 → 값 (문자열 값은 따옴표 없이). 파일에 없는 키는 빠진다.
    public static Dictionary<string, string> Read(string name)
    {
        string text = File.ReadAllText(PathOf(name), Encoding.UTF8);
        var d = new Dictionary<string, string>();
        foreach (string[] k in GraphicsKeys)
        {
            Match m = ValueRe(k[0]).Match(text);
            if (m.Success) d[k[0]] = m.Groups[2].Value.Trim('"');
        }
        return d;
    }

    // 값을 바꿔 쓴다. 원래 숫자였던 값은 정수만, 문자열이던 값은 문자열로 받는다.
    public static void Write(string name, Dictionary<string, string> values)
    {
        string path = PathOf(name);
        string text = File.ReadAllText(path, Encoding.UTF8);
        foreach (var kv in values)
        {
            Regex re = ValueRe(kv.Key);
            Match m = re.Match(text);
            if (!m.Success) continue;
            bool isString = m.Groups[2].Value.StartsWith("\"");
            string v = kv.Value.Trim();
            if (isString)
            {
                if (v.Contains("\"") || v.Contains("\\")) throw new ArgumentException(kv.Key + L.T(": 따옴표 · 역슬래시는 쓸 수 없습니다", ": quotes and backslashes are not allowed"));
                v = "\"" + v + "\"";
            }
            else
            {
                int n;
                if (!int.TryParse(v, out n)) throw new ArgumentException(kv.Key + L.T(": 정수여야 합니다", ": must be an integer"));
                v = n.ToString();
            }
            text = re.Replace(text, "${1}" + v, 1);
        }
        File.WriteAllText(path, text, new UTF8Encoding(false));
    }

    // 게임 폴더의 Mods\<이름>\<이름>.mpq 가 있는 것만 모드로 친다
    public static List<string> Mods(string gameDir)
    {
        var mods = new List<string>();
        string root = Path.Combine(gameDir, "Mods");
        if (!Directory.Exists(root)) return mods;
        foreach (string d in Directory.GetDirectories(root))
        {
            string n = Path.GetFileName(d);
            string mpq = Path.Combine(d, n + ".mpq");
            if (Directory.Exists(mpq) || File.Exists(mpq)) mods.Add(n);
        }
        return mods;
    }
}

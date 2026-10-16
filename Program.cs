// D2R SimpleMulti — D2R 다중 실행 런처. 계정을 고르면 잠금을 풀고 그 계정으로 D2R 을 바로 띄운다.
// 설정 파일 (%APPDATA%\D2RMulti\settings.txt) 에는 언어 · 별칭 · 이메일 · 지역 · 모드 · 그래픽 프로필 · 게임 경로만 있다.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

class Account
{
    public string Alias, Email, Region, Mod = "", Profile = "";
    public override string ToString()
    {
        return Alias + "  (" + Email + ", " + Region
            + (Mod != "" ? ", " + L.T("모드 ", "mod ") + Mod : "")
            + (Profile != "" ? ", " + Profile : "") + ")";
    }
}

class MainForm : Form
{
    public const string AppName = "D2R SimpleMulti";
    static readonly string Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "D2RMulti");
    static readonly string SettingsFile = Path.Combine(Dir, "settings.txt");

    readonly TextBox pathBox = new TextBox();
    readonly ListBox list = new ListBox();
    readonly Label status = new Label();
    readonly ComboBox lang = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };

    MainForm()
    {
        Text = AppName;
        ClientSize = new Size(520, 360);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Font = new Font("Malgun Gothic", 9f);

        var pathLabel = new Label { Text = "D2R.exe", Left = 10, Top = 14, Width = 55 };
        pathBox.SetBounds(65, 10, 350, 23);
        var browse = new Button { Text = L.T("찾기", "Browse"), Left = 420, Top = 9, Width = 90 };
        browse.Click += delegate { Browse(); };

        list.SetBounds(10, 42, 405, 270);
        list.DoubleClick += delegate { Launch(); };

        var launch = new Button { Text = L.T("실행", "Launch"), Left = 420, Top = 42, Width = 90, Height = 40 };
        launch.Click += delegate { Launch(); };
        var add = new Button { Text = L.T("계정 추가", "Add account"), Left = 420, Top = 92, Width = 90 };
        add.Click += delegate { AddAccount(); };
        var edit = new Button { Text = L.T("계정 편집", "Edit account"), Left = 420, Top = 122, Width = 90 };
        edit.Click += delegate { EditAccount(); };
        var remove = new Button { Text = L.T("계정 삭제", "Remove"), Left = 420, Top = 152, Width = 90 };
        remove.Click += delegate { RemoveAccount(); };
        var graphics = new Button { Text = L.T("그래픽 설정", "Graphics"), Left = 420, Top = 202, Width = 90 };
        graphics.Click += delegate { using (var d = new GraphicsDialog()) d.ShowDialog(this); };
        var unlock = new Button { Text = L.T("잠금만 풀기", "Unlock only"), Left = 420, Top = 232, Width = 90 };
        unlock.Click += delegate { Unlock(); };
        var help = new Button { Text = L.T("도움말", "Help"), Left = 420, Top = 262, Width = 90 };
        help.Click += delegate { using (var d = new HelpDialog()) d.ShowDialog(this); };

        // 언어: 바꾸면 저장하고 다시 시작해 모든 창에 적용한다
        lang.Items.AddRange(new object[] { "한국어", "English" });
        lang.SelectedIndex = L.Ko ? 0 : 1;
        lang.SetBounds(420, 292, 90, 23);
        lang.SelectedIndexChanged += delegate
        {
            if ((lang.SelectedIndex == 0) == L.Ko) return;
            L.Ko = lang.SelectedIndex == 0;
            SaveSettings();
            Application.Restart();
        };

        status.SetBounds(10, 322, 500, 36);

        Controls.AddRange(new Control[] { pathLabel, pathBox, browse, list, launch, add, edit, remove, graphics, unlock, help, lang, status });
        LoadSettings();
    }

    string GameDir { get { return Path.GetDirectoryName(pathBox.Text); } }

    // 창을 만들기 전에 언어부터 읽는다 (없으면 Windows 언어)
    static void LoadLang()
    {
        if (!File.Exists(SettingsFile)) return;
        foreach (string line in File.ReadAllLines(SettingsFile, Encoding.UTF8))
        {
            string[] f = line.Split('\t');
            if (f[0] == "lang" && f.Length == 2) L.Ko = f[1] == "ko";
        }
    }

    void LoadSettings()
    {
        if (File.Exists(SettingsFile))
        {
            foreach (string line in File.ReadAllLines(SettingsFile, Encoding.UTF8))
            {
                string[] f = line.Split('\t');
                if (f[0] == "path" && f.Length == 2) pathBox.Text = f[1];
                if (f[0] == "acct" && f.Length >= 4)
                    list.Items.Add(new Account
                    {
                        Alias = f[1], Email = f[2], Region = f[3],
                        Mod = f.Length > 4 ? f[4] : "", Profile = f.Length > 5 ? f[5] : "",
                    });
            }
        }
        if (pathBox.Text == "")
        {
            object loc = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Diablo II Resurrected", "InstallLocation", null);
            if (loc != null) pathBox.Text = Path.Combine((string)loc, "D2R.exe");
        }
    }

    void SaveSettings()
    {
        Directory.CreateDirectory(Dir);
        var lines = new List<string> { "lang\t" + L.Code, "path\t" + pathBox.Text };
        foreach (Account a in list.Items)
            lines.Add("acct\t" + a.Alias + "\t" + a.Email + "\t" + a.Region + "\t" + a.Mod + "\t" + a.Profile);
        File.WriteAllLines(SettingsFile, lines.ToArray(), Encoding.UTF8);
    }

    void Browse()
    {
        using (var d = new OpenFileDialog { Filter = "D2R.exe|D2R.exe" })
            if (d.ShowDialog(this) == DialogResult.OK) { pathBox.Text = d.FileName; SaveSettings(); }
    }

    int Unlock()
    {
        try
        {
            int n = Unlocker.UnlockAll("D2R", Unlocker.LockName);
            status.Text = L.T("잠금 " + n + "개 해제", n + " lock(s) released");
            return n;
        }
        catch (Exception e) { status.Text = e.Message; return -1; }
    }

    void Launch()
    {
        var a = list.SelectedItem as Account;
        if (a == null) { status.Text = L.T("계정을 고르세요", "Select an account"); return; }
        if (!File.Exists(pathBox.Text)) { status.Text = L.T("D2R.exe 경로를 확인하세요", "Check the D2R.exe path"); return; }
        string token = Creds.Load(a.Email);
        if (token == null) { status.Text = L.T("자격 증명 관리자에 토큰이 없습니다 — 계정을 다시 추가하세요", "No token in Credential Manager — add the account again"); return; }
        if (a.Profile != "" && !File.Exists(Profiles.PathOf(a.Profile)))
        {
            status.Text = L.T("그래픽 프로필 " + a.Profile + " 이 없습니다 — 계정 편집에서 다시 고르세요", "Graphics profile " + a.Profile + " is missing — pick another in Edit account");
            return;
        }
        if (a.Mod != "" && !Profiles.Mods(GameDir).Contains(a.Mod)) { status.Text = L.T("모드 " + a.Mod + " 가 Mods 폴더에 없습니다", "Mod " + a.Mod + " is not in the Mods folder"); return; }
        if (Unlock() < 0) return;

        // 모든 창이 Settings.json 하나를 읽으므로, 실행 직전에 이 계정의 프로필을 깔아 둔다.
        if (a.Profile != "") Profiles.Apply(a.Profile);
        // 토큰 자리는 하나뿐이라, 앞 창이 캐릭터 선택까지 가기 전에 다음 계정을 띄우면 토큰이 덮인다.
        Token.Write(Token.OsiKey, a.Region, token);
        string args = "-uid osi" + (a.Mod != "" ? " -mod " + a.Mod + " -txt" : "");
        var psi = new ProcessStartInfo(pathBox.Text, args);
        psi.WorkingDirectory = GameDir;
        psi.UseShellExecute = false;
        Process.Start(psi).Dispose();
        status.Text = L.T(a.Alias + " 실행 — 캐릭터 선택 화면이 뜬 뒤 다음 계정을 실행하세요",
            a.Alias + " launched — wait for character select before launching the next account");
        SaveSettings();
    }

    void AddAccount()
    {
        using (var d = new AccountDialog(null, Profiles.Mods(GameDir), Profiles.List()))
        {
            if (d.ShowDialog(this) != DialogResult.OK) return;
            Creds.Save(d.Email, d.Token);
            list.Items.Add(d.Result);
            SaveSettings();
        }
    }

    void EditAccount()
    {
        var a = list.SelectedItem as Account;
        if (a == null) { status.Text = L.T("계정을 고르세요", "Select an account"); return; }
        using (var d = new AccountDialog(a, Profiles.Mods(GameDir), Profiles.List()))
        {
            if (d.ShowDialog(this) != DialogResult.OK) return;
            if (d.Token != null) Creds.Save(d.Email, d.Token);   // 비워 두면 기존 토큰 유지
            list.Items[list.SelectedIndex] = d.Result;
            SaveSettings();
        }
    }

    void RemoveAccount()
    {
        var a = list.SelectedItem as Account;
        if (a == null) return;
        if (MessageBox.Show(this, L.T(a.Alias + " 계정과 저장된 토큰을 지울까요?", "Remove " + a.Alias + " and its saved token?"), Text, MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        Creds.Delete(a.Email);
        list.Items.Remove(a);
        SaveSettings();
    }

    [STAThread]
    static void Main()
    {
        LoadLang();
        Application.EnableVisualStyles();
        Application.Run(new MainForm());
    }
}

class AccountDialog : Form
{
    readonly TextBox alias = new TextBox(), email = new TextBox(), tokenBox = new TextBox { UseSystemPasswordChar = true };
    readonly ComboBox region = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox mod = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly ComboBox profile = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly bool editing;

    public string Alias { get { return alias.Text.Trim(); } }
    public string Email { get { return email.Text.Trim(); } }
    public string Token { get { return global::Token.Extract(tokenBox.Text); } }
    public Account Result
    {
        get
        {
            return new Account
            {
                Alias = Alias, Email = Email, Region = (string)region.SelectedItem,
                Mod = mod.SelectedIndex > 0 ? (string)mod.SelectedItem : "",
                Profile = profile.SelectedIndex > 0 ? (string)profile.SelectedItem : "",
            };
        }
    }

    // existing 이 있으면 편집: 이메일은 고정, 토큰 칸은 비워 두면 기존 토큰을 쓴다.
    public AccountDialog(Account existing, List<string> mods, List<string> profiles)
    {
        editing = existing != null;
        Text = editing ? L.T("계정 편집", "Edit account") : L.T("계정 추가", "Add account");
        ClientSize = new Size(330, 340);
        Font = new Font("Malgun Gothic", 9f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        region.Items.AddRange(new object[] { "kr", "us", "eu" });
        region.SelectedIndex = 0;
        mod.Items.Add(L.T("(없음)", "(none)"));
        foreach (string m in mods) mod.Items.Add(m);
        mod.SelectedIndex = 0;
        profile.Items.Add(L.T("(지금 설정 그대로)", "(keep current settings)"));
        foreach (string p in profiles) profile.Items.Add(p);
        profile.SelectedIndex = 0;

        if (editing)
        {
            alias.Text = existing.Alias;
            email.Text = existing.Email;
            email.ReadOnly = true;   // 토큰이 이메일 이름으로 저장되어 있다
            region.SelectedItem = existing.Region;
            if (mod.Items.Contains(existing.Mod)) mod.SelectedItem = existing.Mod;
            if (profile.Items.Contains(existing.Profile)) profile.SelectedItem = existing.Profile;
        }

        string[] labels = { L.T("별칭", "Name"), L.T("이메일", "Email"), L.T("지역", "Region"), L.T("모드", "Mod"), L.T("그래픽", "Graphics") };
        Control[] boxes = { alias, email, region, mod, profile };
        for (int i = 0; i < labels.Length; i++)
        {
            Controls.Add(new Label { Text = labels[i], Left = 10, Top = 14 + i * 30, Width = 70 });
            boxes[i].SetBounds(85, 10 + i * 30, 230, 23);
            Controls.Add(boxes[i]);
        }

        // 토큰 받기: InPrivate 창이어야 계정마다 새로 로그인된다
        var help = new Label
        {
            Text = (editing ? L.T("토큰을 바꿀 때만: ", "Only to replace the token: ") : "") + L.T(
                "① 아래 버튼으로 Edge InPrivate 창에서 이 계정으로 로그인\n② 끝에 뜨는 오류 페이지의 주소(localhost:0/?ST=…)를 복사해 붙여넣기",
                "1) Log in with this account in the Edge InPrivate window below\n2) Copy the error page address (localhost:0/?ST=…) and paste it"),
            Left = 10, Top = 164, Width = 310, Height = 64,
        };
        var login = new Button { Text = L.T("브라우저에서 로그인", "Log in in browser"), Left = 10, Top = 232, Width = 150 };
        login.Click += delegate
        {
            Process.Start(new ProcessStartInfo("msedge.exe", "--inprivate \"" + global::Token.LoginUrl + "\"") { UseShellExecute = true });
        };
        Controls.Add(new Label { Text = L.T("주소", "Address"), Left = 10, Top = 270, Width = 70 });
        tokenBox.SetBounds(85, 266, 230, 23);
        Controls.AddRange(new Control[] { help, login, tokenBox });

        var ok = new Button { Text = L.T("저장", "Save"), Left = 155, Top = 305, Width = 75 };
        ok.Click += delegate
        {
            if (Alias == "" || Email == "" || Alias.Contains("\t") || Email.Contains("\t")) return;
            bool tokenNeeded = !editing || tokenBox.Text.Trim() != "";
            if (tokenNeeded && Token == null) { MessageBox.Show(this, L.T("주소에서 토큰(ST=…)을 찾지 못했습니다.", "No token (ST=…) found in the address."), Text); return; }
            DialogResult = DialogResult.OK;
        };
        var cancel = new Button { Text = L.T("취소", "Cancel"), Left = 240, Top = 305, Width = 75, DialogResult = DialogResult.Cancel };
        Controls.AddRange(new Control[] { ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

// 프로필 고르기 · 지금 설정을 프로필로 저장 · 주요 그래픽 값 편집
class GraphicsDialog : Form
{
    readonly ComboBox profile = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    readonly DataGridView grid = new DataGridView();

    public GraphicsDialog()
    {
        Text = L.T("그래픽 설정", "Graphics settings");
        ClientSize = new Size(440, 520);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Malgun Gothic", 9f);

        Controls.Add(new Label { Text = L.T("프로필", "Profile"), Left = 10, Top = 14, Width = 50 });
        profile.SetBounds(65, 10, 190, 23);
        profile.SelectedIndexChanged += delegate { LoadGrid(); };
        var saveAs = new Button { Text = L.T("지금 설정을 새 프로필로", "Save current as profile"), Left = 260, Top = 9, Width = 170 };
        saveAs.Click += delegate { SaveAs(); };

        var note = new Label
        {
            Text = L.T(
                "Settings = 게임이 지금 쓰는 설정. 숫자의 뜻은 공개 자료가 없어 그대로 보여 줍니다. 확실한 방법은 게임에서 맞추고 '지금 설정을 새 프로필로' 저장하는 것입니다.",
                "Settings = what the game uses now. Values are raw numbers (no public docs). The reliable way is to set them in game and use 'Save current as profile'."),
            Left = 10, Top = 40, Width = 420, Height = 50,
        };

        grid.SetBounds(10, 94, 420, 379);
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.Columns.Add("key", "key");
        grid.Columns.Add("label", L.T("항목", "Setting"));
        grid.Columns.Add("value", L.T("값", "Value"));
        grid.Columns["key"].Visible = false;
        grid.Columns["label"].ReadOnly = true;

        var save = new Button { Text = L.T("저장", "Save"), Left = 270, Top = 482, Width = 75 };
        save.Click += delegate { Save(); };
        var close = new Button { Text = L.T("닫기", "Close"), Left = 355, Top = 482, Width = 75, DialogResult = DialogResult.Cancel };
        Controls.AddRange(new Control[] { profile, saveAs, note, grid, save, close });
        CancelButton = close;
        FillProfiles(Profiles.Current);
    }

    string Selected { get { return (string)profile.SelectedItem; } }

    void FillProfiles(string select)
    {
        profile.Items.Clear();
        if (File.Exists(Profiles.PathOf(Profiles.Current))) profile.Items.Add(Profiles.Current);
        foreach (string p in Profiles.List()) profile.Items.Add(p);
        if (profile.Items.Contains(select)) profile.SelectedItem = select;
        else if (profile.Items.Count > 0) profile.SelectedIndex = 0;
    }

    void LoadGrid()
    {
        grid.Rows.Clear();
        if (Selected == null) return;
        Dictionary<string, string> values = Profiles.Read(Selected);
        foreach (string[] k in Profiles.GraphicsKeys)
            if (values.ContainsKey(k[0])) grid.Rows.Add(k[0], L.T(k[1], k[0]), values[k[0]]);
    }

    void Save()
    {
        if (Selected == null) return;
        grid.EndEdit();
        var values = new Dictionary<string, string>();
        foreach (DataGridViewRow r in grid.Rows) values[(string)r.Cells[0].Value] = Convert.ToString(r.Cells[2].Value);
        try
        {
            Profiles.Write(Selected, values);
            MessageBox.Show(this, Selected + L.T(" 저장", " saved") + (Selected == Profiles.Current ? L.T(" — 다음에 띄우는 창부터 적용됩니다", " — applies to the next window you launch") : ""), Text);
        }
        catch (Exception e) { MessageBox.Show(this, e.Message, Text); }
    }

    void SaveAs()
    {
        string name = Prompt(L.T("새 프로필 이름 (예: 작은창)", "New profile name (e.g. small)"));
        if (name == null) return;
        try { FillProfiles(Profiles.SaveCurrentAs(name)); }
        catch (Exception e) { MessageBox.Show(this, e.Message, Text); }
    }

    string Prompt(string question)
    {
        using (var f = new Form { Text = Text, ClientSize = new Size(300, 95), FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false, Font = Font })
        {
            var box = new TextBox { Left = 10, Top = 32, Width = 280 };
            var ok = new Button { Text = L.T("확인", "OK"), Left = 215, Top = 62, Width = 75, DialogResult = DialogResult.OK };
            f.Controls.AddRange(new Control[] { new Label { Text = question, Left = 10, Top = 10, Width = 280 }, box, ok });
            f.AcceptButton = ok;
            return f.ShowDialog(this) == DialogResult.OK ? box.Text : null;
        }
    }
}

// 실행 방법 안내
class HelpDialog : Form
{
    public HelpDialog()
    {
        Text = L.T("도움말", "Help");
        ClientSize = new Size(576, 440);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Malgun Gothic", 9f);
        var text = new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = SystemColors.Window,
            Left = 10, Top = 10, Width = 556, Height = 385,
            Text = L.T(
                "[처음 한 번: 계정 추가]\r\n" +
                "1. 계정 추가 → 별칭 · 이메일 · 지역(kr/us/eu)을 적는다. 이메일은 구분용이며 어디에도 보내지 않는다.\r\n" +
                "2. '브라우저에서 로그인' → Edge InPrivate 창에서 그 계정으로 로그인한다 (2단계 인증도 여기서 승인).\r\n" +
                "3. 로그인이 끝나면 '페이지에 연결할 수 없습니다' 오류가 뜬다 (정상). 주소창의 주소(localhost:0/?ST=…)를 복사해 '주소' 칸에 붙여넣고 저장한다.\r\n" +
                "4. 계정마다 새 InPrivate 창으로 반복한다. 같은 창이면 앞 계정으로 로그인된다.\r\n" +
                "※ 그 주소는 로그인과 같다. 남에게 보이지 말 것.\r\n" +
                "※ OTP(인증기) 계정: OTP 는 2번의 브라우저 로그인에서 한 번 입력한다. 실행할 때는 묻지 않는다. 토큰이 만료되면 다시 로그인하며 OTP 도 다시 입력한다.\r\n\r\n" +
                "[실행]\r\n" +
                "1. 계정을 고르고 '실행' (또는 더블클릭).\r\n" +
                "2. 그 창이 캐릭터 선택 화면까지 간 뒤에 다음 계정을 실행한다. 서두르면 앞 계정의 로그인이 덮인다.\r\n" +
                "3. 두 번째 계정부터는 실행 전에 잠금을 자동으로 푼다. 아래 상태 줄에 '잠금 1개 해제' 가 보이면 정상.\r\n\r\n" +
                "[모드 · 그래픽]\r\n" +
                "· 계정 편집에서 계정마다 모드(Mods 폴더에 있는 것)와 그래픽 프로필을 고른다. 토큰 칸은 비워 두면 그대로.\r\n" +
                "· 그래픽 프로필 만들기: 게임에서 원하는 그래픽으로 맞춘 뒤 '그래픽 설정' → '지금 설정을 새 프로필로'.\r\n" +
                "· 실행 직전에 고른 프로필이 게임 설정(Settings.json)으로 복사된다.\r\n\r\n" +
                "[잠금만 풀기]\r\n" +
                "Battle.net 런처로 띄운 창이 있을 때, 이 버튼을 누른 뒤 Battle.net 에서 한 번 더 실행하면 두 번째 창이 뜬다. 로그인을 Battle.net 이 직접 하므로 토큰 로그인이 안 되는 계정(예: OTP 계정에서 문제가 생길 때)도 이 방법으로 띄울 수 있다.\r\n\r\n" +
                "[안 될 때]\r\n" +
                "· '잠금 0개 해제' 인데 두 번째 창이 안 뜸 → 게임 업데이트로 잠금 이름이 바뀌었을 수 있다. 창 하나를 띄운 채 diag\\diag.cmd 를 실행해 이름을 확인한다.\r\n" +
                "· '프로세스를 열 수 없습니다' → 게임이 관리자 권한으로 실행 중이다. 일반 권한으로 띄운다.\r\n" +
                "· 접속 중에서 멈추거나 싱글 플레이로 넘어감 → 토큰이 만료되었을 수 있다. 계정 편집에서 새 토큰을 받는다. 그래도 안 되면 '잠금만 풀기' + Battle.net 런처로 띄운다.\r\n\r\n" +
                "[저장 위치]\r\n" +
                "계정 목록: %APPDATA%\\D2RMulti\\settings.txt · 토큰: Windows 자격 증명 관리자 (D2RMulti:<이메일>) · 그래픽 프로필: Saved Games\\Diablo II Resurrected\\Settings*.json",

                "[First time: add an account]\r\n" +
                "1. Add account → enter a name, email and region (kr/us/eu). The email is only a label and is never sent anywhere.\r\n" +
                "2. 'Log in in browser' → log in with that account in the Edge InPrivate window (approve 2FA there too).\r\n" +
                "3. After login a 'can't reach this page' error appears (expected). Copy the address (localhost:0/?ST=…) into the 'Address' field and save.\r\n" +
                "4. Use a new InPrivate window for each account; the same window logs in as the previous account.\r\n" +
                "※ That address works like a login. Never share it.\r\n" +
                "※ OTP (authenticator) accounts: enter the OTP once during the browser login in step 2. Launching does not ask for it. When the token expires, log in again (with OTP).\r\n\r\n" +
                "[Launch]\r\n" +
                "1. Select an account and press 'Launch' (or double-click).\r\n" +
                "2. Wait until that window reaches character select before launching the next account, or its login gets overwritten.\r\n" +
                "3. From the second account on, the lock is released automatically. '1 lock(s) released' in the status line means it worked.\r\n\r\n" +
                "[Mods · Graphics]\r\n" +
                "· In Edit account, choose a mod (from the Mods folder) and a graphics profile per account. Leave the token field empty to keep it.\r\n" +
                "· To make a graphics profile: set graphics in game, then 'Graphics' → 'Save current as profile'.\r\n" +
                "· The chosen profile is copied to the game's Settings.json right before launch.\r\n\r\n" +
                "[Unlock only]\r\n" +
                "If a window was started from the Battle.net launcher, press this, then launch again from Battle.net to get a second window. Battle.net does the login itself, so this also works for accounts where token login fails (e.g. if an OTP account has trouble).\r\n\r\n" +
                "[Troubleshooting]\r\n" +
                "· '0 lock(s) released' and no second window → a game update may have renamed the lock. With one window open, run diag\\diag.cmd to see the name.\r\n" +
                "· 'Cannot open process' → the game runs as administrator. Start it normally.\r\n" +
                "· Stuck connecting or dropped to single player → the token may have expired. Get a new one in Edit account. If that fails, use 'Unlock only' + the Battle.net launcher.\r\n\r\n" +
                "[Where things are stored]\r\n" +
                "Account list: %APPDATA%\\D2RMulti\\settings.txt · Tokens: Windows Credential Manager (D2RMulti:<email>) · Graphics profiles: Saved Games\\Diablo II Resurrected\\Settings*.json"),
        };
        var close = new Button { Text = L.T("닫기", "Close"), Left = 491, Top = 405, Width = 75, DialogResult = DialogResult.Cancel };
        Controls.AddRange(new Control[] { text, close });
        AcceptButton = close;
        CancelButton = close;
        Shown += delegate { text.SelectionLength = 0; close.Focus(); };
    }
}
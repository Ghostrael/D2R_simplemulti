// 로그인 토큰은 Windows 자격 증명 관리자에만 둔다 (DPAPI 로 현재 Windows 사용자에게 묶여 암호화).
// 설정 파일에는 토큰이 들어가지 않는다.
using System;
using System.Runtime.InteropServices;

static class Creds
{
    const string Prefix = "D2RMulti:";
    const int CRED_TYPE_GENERIC = 1;
    const int CRED_PERSIST_LOCAL_MACHINE = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public long LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredWriteW(ref CREDENTIAL cred, int flags);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredReadW(string target, int type, int flags, out IntPtr cred);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CredDeleteW(string target, int type, int flags);
    [DllImport("advapi32.dll")]
    static extern void CredFree(IntPtr cred);

    public static void Save(string email, string password)
    {
        byte[] blob = System.Text.Encoding.Unicode.GetBytes(password);
        IntPtr p = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, p, blob.Length);
            var c = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = Prefix + email,
                UserName = email,
                CredentialBlob = p,
                CredentialBlobSize = blob.Length,
                Persist = CRED_PERSIST_LOCAL_MACHINE,
            };
            if (!CredWriteW(ref c, 0))
                throw new InvalidOperationException(L.T("자격 증명 저장 실패 (오류 ", "Saving credential failed (error ") + Marshal.GetLastWin32Error() + ")");
        }
        finally
        {
            for (int i = 0; i < blob.Length; i++) Marshal.WriteByte(p, i, 0);
            Marshal.FreeHGlobal(p);
            Array.Clear(blob, 0, blob.Length);
        }
    }

    public static string Load(string email)
    {
        IntPtr p;
        if (!CredReadW(Prefix + email, CRED_TYPE_GENERIC, 0, out p)) return null;
        try
        {
            var c = (CREDENTIAL)Marshal.PtrToStructure(p, typeof(CREDENTIAL));
            return Marshal.PtrToStringUni(c.CredentialBlob, c.CredentialBlobSize / 2);
        }
        finally { CredFree(p); }
    }

    public static void Delete(string email)
    {
        CredDeleteW(Prefix + email, CRED_TYPE_GENERIC, 0);
    }
}

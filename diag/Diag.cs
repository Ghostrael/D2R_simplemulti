// 읽기 전용 진단: D2R 프로세스의 이름 붙은 핸들 중 잠금 후보를 나열한다. 아무것도 닫지 않는다.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

static class Diag
{
    [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint a, bool i, int pid);
    [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll")] static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool DuplicateHandle(IntPtr sp, IntPtr s, IntPtr dp, out IntPtr d, uint a, bool i, uint o);
    [DllImport("ntdll.dll")] static extern int NtQueryInformationProcess(IntPtr p, int c, IntPtr b, int l, out int r);
    [DllImport("ntdll.dll")] static extern int NtQueryObject(IntPtr h, int c, IntPtr b, int l, out int r);

    static string Q(IntPtr h, int cls)
    {
        IntPtr b = Marshal.AllocHGlobal(0x1000);
        try { int r; if (NtQueryObject(h, cls, b, 0x1000, out r) != 0) return null; IntPtr s = Marshal.ReadIntPtr(b, 8); return s == IntPtr.Zero ? "" : Marshal.PtrToStringUni(s, Marshal.ReadInt16(b, 0) / 2); }
        finally { Marshal.FreeHGlobal(b); }
    }

    static int Main()
    {
        var procs = Process.GetProcessesByName("D2R");
        Console.WriteLine("D2R processes: " + procs.Length);
        foreach (var p in procs)
        {
            IntPtr proc = OpenProcess(0x0400 | 0x0040, false, p.Id);
            Console.WriteLine("pid " + p.Id + " open=" + (proc != IntPtr.Zero) + " err=" + Marshal.GetLastWin32Error());
            if (proc == IntPtr.Zero) continue;
            int len = 0x100000; IntPtr buf = Marshal.AllocHGlobal(len); int ret;
            int st = NtQueryInformationProcess(proc, 51, buf, len, out ret);
            if (st != 0) { Console.WriteLine("handle list status 0x" + st.ToString("X8")); continue; }
            long n = Marshal.ReadInt64(buf, 0); int named = 0;
            var types = new System.Collections.Generic.Dictionary<string, int>();
            for (long i = 0; i < n; i++)
            {
                IntPtr h = Marshal.ReadIntPtr(buf, (int)(16 + i * 40)); IntPtr d;
                if (!DuplicateHandle(proc, h, GetCurrentProcess(), out d, 0, false, 2)) continue;
                string t = Q(d, 2);
                if (t == "Event" || t == "Mutant" || t == "Semaphore" || t == "Section")
                {
                    string name = Q(d, 1);
                    if (!string.IsNullOrEmpty(name))
                    {
                        named++;
                        string low = name.ToLowerInvariant();
                        if (low.Contains("instance") || low.Contains("diablo") || low.Contains("check") || low.Contains("d2r"))
                            Console.WriteLine("  CANDIDATE " + t + " 0x" + h.ToInt64().ToString("X") + " " + name);
                    }
                }
                if (t != null) { int c; types.TryGetValue(t, out c); types[t] = c + 1; }
                CloseHandle(d);
            }
            Console.WriteLine("  handles=" + n + " named(sync/section)=" + named);
            foreach (var kv in types) if (kv.Key == "Event" || kv.Key == "Mutant") Console.WriteLine("  type " + kv.Key + "=" + kv.Value);
            Marshal.FreeHGlobal(buf); CloseHandle(proc);
        }
        return 0;
    }
}

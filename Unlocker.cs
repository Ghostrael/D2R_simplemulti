// D2R 의 "이미 실행 중" 잠금을 푼다.
// D2R 은 이름 붙은 이벤트 "DiabloII Check For Other Instances" 를 만들어 두고 (2026-10 실측;
// 예전 판은 "DiaBlo_II ..."), 다음 실행이 그 이벤트를 찾으면 꺼진다.
// 이 핸들을 D2R 프로세스 안에서 닫으면 두 번째 창이 뜬다. 앞부분이 바뀌어 온 탓에 이름 끝으로 찾는다.
//
// 관리자 권한이 필요 없다: 같은 사용자가 띄운 (승격되지 않은) D2R 이면
// PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE 로 열 수 있다.
// 게임 메모리는 읽지도 쓰지도 않는다 — 핸들 하나를 닫을 뿐이다.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

static class Unlocker
{
    public const string LockName = "Check For Other Instances";   // 이벤트 이름의 끝 (대소문자 무시)

    const uint PROCESS_QUERY_INFORMATION = 0x0400;
    const uint PROCESS_DUP_HANDLE = 0x0040;
    const uint DUPLICATE_CLOSE_SOURCE = 0x1;
    const uint DUPLICATE_SAME_ACCESS = 0x2;
    const int ProcessHandleInformation = 51;   // Windows 8 이상
    const int ObjectNameInformation = 1;
    const int ObjectTypeInformation = 2;
    const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr h);
    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DuplicateHandle(IntPtr srcProc, IntPtr src, IntPtr dstProc,
        out IntPtr dst, uint access, bool inherit, uint options);
    [DllImport("ntdll.dll")]
    static extern int NtQueryInformationProcess(IntPtr proc, int cls, IntPtr buf, int len, out int retLen);
    [DllImport("ntdll.dll")]
    static extern int NtQueryObject(IntPtr h, int cls, IntPtr buf, int len, out int retLen);

    // 실행 중인 모든 processName 프로세스에서 잠금 핸들을 닫는다. 닫은 개수를 돌려준다.
    // 열 수 없는 프로세스가 있으면 (예: D2R 이 관리자로 실행됨) 예외를 던진다.
    public static int UnlockAll(string processName, string lockName)
    {
        int closed = 0;
        foreach (Process p in Process.GetProcessesByName(processName))
        {
            closed += Unlock(p.Id, lockName);
            p.Dispose();
        }
        return closed;
    }

    public static int Unlock(int pid, string lockName)
    {
        IntPtr proc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_DUP_HANDLE, false, pid);
        if (proc == IntPtr.Zero)
            throw new InvalidOperationException(string.Format(L.T(
                "프로세스 {0} 을 열 수 없습니다 (오류 {1}). 게임이 관리자 권한으로 실행 중이면 이 런처도 같은 권한이어야 합니다.",
                "Cannot open process {0} (error {1}). If the game runs as administrator, this launcher must too."),
                pid, Marshal.GetLastWin32Error()));
        try
        {
            int closed = 0;
            foreach (IntPtr h in ListHandles(proc))
            {
                if (IsLock(proc, h, lockName))
                {
                    IntPtr ignored;
                    if (DuplicateHandle(proc, h, IntPtr.Zero, out ignored, 0, false, DUPLICATE_CLOSE_SOURCE))
                        closed++;
                }
            }
            return closed;
        }
        finally { CloseHandle(proc); }
    }

    static List<IntPtr> ListHandles(IntPtr proc)
    {
        int len = 0x10000;
        while (true)
        {
            IntPtr buf = Marshal.AllocHGlobal(len);
            try
            {
                int ret;
                int st = NtQueryInformationProcess(proc, ProcessHandleInformation, buf, len, out ret);
                if (st == STATUS_INFO_LENGTH_MISMATCH) { len = Math.Max(len * 2, ret + 0x1000); continue; }
                if (st != 0) throw new InvalidOperationException(string.Format(L.T("핸들 목록 실패 (NTSTATUS 0x{0:X8})", "Handle listing failed (NTSTATUS 0x{0:X8})"), st));
                // PROCESS_HANDLE_SNAPSHOT_INFORMATION: NumberOfHandles, Reserved, 그 뒤 항목 배열
                // 항목 (x64, 40바이트): HandleValue, HandleCount, PointerCount, GrantedAccess, ObjectTypeIndex, HandleAttributes, Reserved
                long n = Marshal.ReadInt64(buf, 0);
                var list = new List<IntPtr>((int)n);
                for (long i = 0; i < n; i++)
                    list.Add(Marshal.ReadIntPtr(buf, (int)(16 + i * 40)));
                return list;
            }
            finally { Marshal.FreeHGlobal(buf); }
        }
    }

    static bool IsLock(IntPtr proc, IntPtr h, string lockName)
    {
        IntPtr dup;
        if (!DuplicateHandle(proc, h, GetCurrentProcess(), out dup, 0, false, DUPLICATE_SAME_ACCESS))
            return false;
        try
        {
            // 이름 조회는 일부 파일(파이프) 핸들에서 멈출 수 있으므로 종류부터 보고 Event 만 이름을 본다.
            if (QueryString(dup, ObjectTypeInformation) != "Event") return false;
            string name = QueryString(dup, ObjectNameInformation);
            return name != null && name.EndsWith(lockName, StringComparison.OrdinalIgnoreCase);
        }
        finally { CloseHandle(dup); }
    }

    // 두 정보 모두 UNICODE_STRING 으로 시작한다: Length(2) MaxLength(2) 패딩(4) Buffer(8)
    static string QueryString(IntPtr h, int cls)
    {
        int len = 0x1000;
        IntPtr buf = Marshal.AllocHGlobal(len);
        try
        {
            int ret;
            if (NtQueryObject(h, cls, buf, len, out ret) != 0) return null;
            int chars = Marshal.ReadInt16(buf, 0) / 2;
            IntPtr s = Marshal.ReadIntPtr(buf, 8);
            return s == IntPtr.Zero ? "" : Marshal.PtrToStringUni(s, chars);
        }
        finally { Marshal.FreeHGlobal(buf); }
    }
}

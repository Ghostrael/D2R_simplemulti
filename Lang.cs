// 화면 문구의 언어 (한국어 · English). 문구는 쓰는 자리에서 L.T("한국어", "English") 로 짝지어 둔다.
using System.Globalization;

static class L
{
    public static bool Ko = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko";
    public static string Code { get { return Ko ? "ko" : "en"; } }
    public static string T(string ko, string en) { return Ko ? ko : en; }
}

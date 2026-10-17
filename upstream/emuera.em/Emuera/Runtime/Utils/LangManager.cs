using System.Text;

namespace MinorShift.Emuera.Runtime.Utils;

//マルチ言語に対応可能な形式に変更
internal static class LangManager
{
    static Encoding lang;
    static Encoding japanese;

    public static void setEncode(int code)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        lang = Encoding.GetEncoding(code);
        japanese = Encoding.GetEncoding(932);
    }
    private static int GetByteCountLang(string str)
    {
        int length = 0;
        foreach (char c in str)
        {
            string value = c.ToString();
            byte[] bytes = lang.GetBytes(value);

            if (lang.GetString(bytes) == value)
            {
                length += bytes.Length;
                continue;
            }

            byte[] japaneseBytes = japanese.GetBytes(value);

            if (japanese.GetString(japaneseBytes) == value)
            {
                length += japaneseBytes.Length;
                continue;
            }

            length += bytes.Length;
        }
        return length;
    }

    public static int GetStrlenLang(string str)
    {
        if (Ascii.IsValid(str))
        {
            return str.Length;
        }
        return GetByteCountLang(str);
    }
    public static int GetUFTIndex(string str, int LangIndex)
    {
        if (LangIndex <= 0)
            return 0;
        int totalByte = GetStrlenLang(str);
        if (LangIndex >= totalByte)
            return str.Length;
        int UTFcnt = 0;
        int JIScnt = 0;
        for (int i = 0; i < str.Length; i++)
        {
            JIScnt += GetByteCountLang(str[UTFcnt].ToString());
            UTFcnt++;
            if (JIScnt >= LangIndex)
                break;
        }
        return UTFcnt;
    }

    public static string GetSubStringLang(string str, int startindex, int length)
    {
        int totalByte = GetStrlenLang(str);
        if (startindex >= totalByte || length == 0)
            return "";
        if (length < 0 || length > totalByte)
            length = totalByte;

        StringBuilder ret = new();
        int UTFcnt = 0;
        int JIScnt = 0;

        if (startindex <= 0)
        {
            if (length == totalByte)
                return str;
        }
        else
        {
            for (int i = 0; i < str.Length; i++)
            {
                JIScnt += GetByteCountLang(str[UTFcnt].ToString());
                UTFcnt++;
                if (JIScnt >= startindex)
                    break;
            }
            if (UTFcnt >= str.Length)
                return "";
        }

        JIScnt = 0;
        while (true)
        {
            ret.Append(str[UTFcnt]);
            JIScnt += GetByteCountLang(str[UTFcnt].ToString());
            UTFcnt++;
            if (JIScnt >= length)
                break;
            if (UTFcnt >= str.Length)
                break;
        }
        return ret.ToString();
    }
}

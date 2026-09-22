namespace DanchiManager.Services;

public static class KanaService
{
    public static string ToKatakana(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var chars = input.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (chars[i] is >= '\u3041' and <= '\u3096')
                chars[i] = (char)(chars[i] + 0x60);
        }
        return new string(chars);
    }
}

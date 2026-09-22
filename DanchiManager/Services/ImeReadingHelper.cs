using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DanchiManager.Services;

public static class ImeReadingHelper
{
    const int WM_IME_COMPOSITION = 0x010F;
    const int GCS_RESULTREADSTR = 0x0200;
    const int GCS_COMPREADSTR = 0x0002;

    [DllImport("imm32.dll")] static extern nint ImmGetContext(nint hwnd);
    [DllImport("imm32.dll")] static extern bool ImmReleaseContext(nint hwnd, nint himc);
    [DllImport("imm32.dll", CharSet = CharSet.Unicode)]
    static extern int ImmGetCompositionStringW(nint himc, int flag, byte[]? buf, int len);

    public static void Attach(TextBox box, Action<string> onReading)
    {
        var pending = "";
        var justEmitted = false;

        void Emit(string reading)
        {
            reading = KanaService.ToKatakana(reading).Trim();
            if (!IsKana(reading)) return;
            box.Dispatcher.Invoke(() => onReading(reading));
            justEmitted = true;
        }

        TextCompositionManager.AddPreviewTextInputUpdateHandler(box, (_, e) =>
        {
            var read = e.TextComposition.ControlText;
            if (IsKana(read))
            {
                pending = read;
                return;
            }
            var t = e.TextComposition.CompositionText;
            if (IsKana(t))
                pending = t;
        });
        TextCompositionManager.AddPreviewTextInputHandler(box, (_, _) =>
        {
            justEmitted = false;
            if (!IsKana(pending))
            {
                pending = "";
                return;
            }
            Emit(pending);
            pending = "";
        });

        void HookHwnd()
        {
            if (PresentationSource.FromVisual(box) is not HwndSource src) return;
            src.AddHook((nint hwnd, int msg, nint w, nint l, ref bool handled) =>
            {
                if (msg != WM_IME_COMPOSITION) return nint.Zero;
                var flags = l.ToInt64();
                if ((flags & GCS_COMPREADSTR) != 0)
                {
                    var comp = ReadImeString(hwnd, GCS_COMPREADSTR);
                    if (IsKana(comp)) pending = comp;
                }
                if ((flags & GCS_RESULTREADSTR) == 0) return nint.Zero;
                var reading = ReadImeString(hwnd, GCS_RESULTREADSTR);
                if (!IsKana(reading)) return nint.Zero;
                if (justEmitted)
                {
                    justEmitted = false;
                    return nint.Zero;
                }
                pending = "";
                Emit(reading);
                return nint.Zero;
            });
        }

        if (box.IsLoaded) HookHwnd();
        else box.Loaded += (_, _) => HookHwnd();
    }

    static bool IsKana(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        foreach (var c in s)
        {
            if (c is >= '\u3041' and <= '\u3096') continue;
            if (c is >= '\u30A1' and <= '\u30F6') continue;
            if (c is '\u30FC' or '\u30FB' or '\u30A0' or '\u309B' or '\u309C') continue;
            if (c is ' ' or '\u3000' or '-' or 'ー') continue;
            return false;
        }
        return true;
    }

    static string ReadImeString(nint hwnd, int flag)
    {
        var himc = ImmGetContext(hwnd);
        if (himc == 0) return "";
        try
        {
            var len = ImmGetCompositionStringW(himc, flag, null, 0);
            if (len <= 0) return "";
            var buf = new byte[len];
            ImmGetCompositionStringW(himc, flag, buf, len);
            return Encoding.Unicode.GetString(buf).TrimEnd('\0');
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }
}

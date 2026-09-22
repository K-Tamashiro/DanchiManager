using System.Windows;

namespace DanchiManager.Views;

internal static class DialogHook
{
    public static void Attach(Window window)
    {
        window.DataContextChanged += (_, e) =>
        {
            if (e.NewValue is null) return;
            var ev = e.NewValue.GetType().GetEvent("RequestClose");
            if (ev is null) return;
            EventHandler<bool> handler = (_, ok) =>
            {
                try { window.DialogResult = ok; }
                catch { /* not shown as dialog */ }
                window.Close();
            };
            ev.AddEventHandler(e.NewValue, handler);
        };
    }
}

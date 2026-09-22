using System.Windows;

namespace DanchiManager.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

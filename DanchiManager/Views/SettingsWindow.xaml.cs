namespace DanchiManager.Views;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

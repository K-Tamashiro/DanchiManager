namespace DanchiManager.Views;

public partial class FeesWindow
{
    public FeesWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

namespace DanchiManager.Views;

public partial class FeeListPreviewWindow
{
    public FeeListPreviewWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

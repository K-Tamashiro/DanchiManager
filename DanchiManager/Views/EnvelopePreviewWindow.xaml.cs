namespace DanchiManager.Views;

public partial class EnvelopePreviewWindow
{
    public EnvelopePreviewWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

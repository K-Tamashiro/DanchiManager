using System.Windows;

namespace DanchiManager.Views;

public partial class EnvelopePreviewWindow : Window
{
    public EnvelopePreviewWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

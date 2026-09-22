using System.Windows;

namespace DanchiManager.Views;

public partial class FeeListPreviewWindow : Window
{
    public FeeListPreviewWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

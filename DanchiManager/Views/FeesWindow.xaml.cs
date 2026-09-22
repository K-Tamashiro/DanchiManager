using System.Windows;

namespace DanchiManager.Views;

public partial class FeesWindow : Window
{
    public FeesWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

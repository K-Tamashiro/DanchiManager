using System.Windows;

namespace DanchiManager.Views;

public partial class TotalsWindow : Window
{
    public TotalsWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

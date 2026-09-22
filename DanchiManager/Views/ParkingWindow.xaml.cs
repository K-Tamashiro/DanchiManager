using System.Windows;

namespace DanchiManager.Views;

public partial class ParkingWindow : Window
{
    public ParkingWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

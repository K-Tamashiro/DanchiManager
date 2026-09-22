using System.Windows;

namespace DanchiManager.Views;

public partial class ParkingPickWindow : Window
{
    public ParkingPickWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

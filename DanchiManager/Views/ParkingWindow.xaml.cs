namespace DanchiManager.Views;

public partial class ParkingWindow
{
    public ParkingWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

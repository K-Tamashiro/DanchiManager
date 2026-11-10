namespace DanchiManager.Views;

public partial class ParkingPickWindow
{
    public ParkingPickWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

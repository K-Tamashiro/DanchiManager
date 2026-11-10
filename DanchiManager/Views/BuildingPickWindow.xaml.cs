namespace DanchiManager.Views;

public partial class BuildingPickWindow
{
    public BuildingPickWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

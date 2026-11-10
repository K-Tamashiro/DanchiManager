namespace DanchiManager.Views;

public partial class BuildingSheetWindow
{
    public BuildingSheetWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

namespace DanchiManager.Views;

public partial class BuildingEditorWindow
{
    public BuildingEditorWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

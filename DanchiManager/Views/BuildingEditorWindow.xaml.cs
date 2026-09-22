using System.Windows;

namespace DanchiManager.Views;

public partial class BuildingEditorWindow : Window
{
    public BuildingEditorWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

using System.Windows;

namespace DanchiManager.Views;

public partial class BuildingSheetWindow : Window
{
    public BuildingSheetWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

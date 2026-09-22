using System.Windows;

namespace DanchiManager.Views;

public partial class BuildingPickWindow : Window
{
    public BuildingPickWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }
}

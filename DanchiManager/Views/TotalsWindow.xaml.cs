using System.Windows.Input;
using DanchiManager.ViewModels;

namespace DanchiManager.Views;

public partial class TotalsWindow
{
    public TotalsWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }

    void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is TotalsViewModel vm)
            vm.SelectBuildingCommand.Execute(null);
    }
}

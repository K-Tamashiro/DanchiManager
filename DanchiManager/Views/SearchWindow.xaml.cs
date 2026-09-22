using System.Windows;
using System.Windows.Input;
using DanchiManager.ViewModels;

namespace DanchiManager.Views;

public partial class SearchWindow : Window
{
    public SearchWindow()
    {
        InitializeComponent();
        DialogHook.Attach(this);
    }

    void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SearchViewModel vm)
            vm.ChooseCommand.Execute(null);
    }
}

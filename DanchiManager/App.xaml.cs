using System.Windows;
using DanchiManager.Services;
using DanchiManager.ViewModels;

namespace DanchiManager;

public partial class App : Application
{
    MainViewModel? _viewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = PathService.LoadSettings();
        var db = DatabaseService.Open(PathService.ResolveDbPath(settings));
        var dialogs = new DialogService();
        var vm = _viewModel = new MainViewModel(db, dialogs);
        var win = new MainWindow { DataContext = vm };
        dialogs.Owner = win;
        MainWindow = win;
        win.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _viewModel?.Dispose();
        base.OnExit(e);
    }
}

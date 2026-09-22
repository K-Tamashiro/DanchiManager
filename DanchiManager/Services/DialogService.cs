using System.Windows;
using DanchiManager.ViewModels;
using DanchiManager.Views;
using Microsoft.Win32;

namespace DanchiManager.Services;

public interface IDialogService
{
    Window? Owner { get; set; }
    bool Confirm(string message, string title = "確認");
    void Info(string message, string title = "団地管理");
    string? Prompt(string message, string title = "入力");
    bool? ShowSettings(SettingsViewModel vm);
    bool? ShowRoom(RoomEditorViewModel vm);
    bool? ShowBuilding(BuildingEditorViewModel vm);
    bool? ShowParking(ParkingViewModel vm);
    bool? ShowParkingPick(ParkingPickViewModel vm);
    bool? ShowFees(FeesViewModel vm);
    bool? ShowTotals(TotalsViewModel vm);
    bool? ShowSearch(SearchViewModel vm);
    bool? ShowBuildingSheet(BuildingSheetViewModel vm);
    bool? ShowBuildingPick(BuildingPickViewModel vm);
    bool? ShowEnvelopes(EnvelopePreviewViewModel vm);
    bool? ShowFeeList(FeeListPreviewViewModel vm);
    string? PickOpenDb();
    string? PickSaveFile(string filter, string defaultName);
}

public sealed class DialogService : IDialogService
{
    public Window? Owner { get; set; }

    public bool Confirm(string message, string title = "確認") =>
        MessageBox.Show(Owner, message, title, MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;

    public void Info(string message, string title = "団地管理") =>
        MessageBox.Show(Owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    public string? Prompt(string message, string title = "入力")
    {
        var dlg = new PromptWindow(message, title) { Owner = Owner };
        return dlg.ShowDialog() == true ? dlg.Value : null;
    }

    public bool? ShowSettings(SettingsViewModel vm) => Show(new SettingsWindow { DataContext = vm });
    public bool? ShowRoom(RoomEditorViewModel vm) => Show(new RoomEditorWindow { DataContext = vm });
    public bool? ShowBuilding(BuildingEditorViewModel vm) => Show(new BuildingEditorWindow { DataContext = vm });
    public bool? ShowParking(ParkingViewModel vm) => Show(new ParkingWindow { DataContext = vm });
    public bool? ShowParkingPick(ParkingPickViewModel vm) => Show(new ParkingPickWindow { DataContext = vm });
    public bool? ShowFees(FeesViewModel vm) => Show(new FeesWindow { DataContext = vm });
    public bool? ShowTotals(TotalsViewModel vm) => Show(new TotalsWindow { DataContext = vm });
    public bool? ShowSearch(SearchViewModel vm) => Show(new SearchWindow { DataContext = vm });
    public bool? ShowBuildingSheet(BuildingSheetViewModel vm) => Show(new BuildingSheetWindow { DataContext = vm });
    public bool? ShowBuildingPick(BuildingPickViewModel vm) => Show(new BuildingPickWindow { DataContext = vm });
    public bool? ShowEnvelopes(EnvelopePreviewViewModel vm) => Show(new EnvelopePreviewWindow { DataContext = vm });
    public bool? ShowFeeList(FeeListPreviewViewModel vm) => Show(new FeeListPreviewWindow { DataContext = vm });

    bool? Show(Window w)
    {
        w.Owner = Owner;
        w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        return w.ShowDialog();
    }

    public string? PickOpenDb()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "SQLite データベース (*.db)|*.db|すべてのファイル (*.*)|*.*",
            Title = "データベースを開く",
        };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }

    public string? PickSaveFile(string filter, string defaultName)
    {
        var dlg = new SaveFileDialog { Filter = filter, FileName = defaultName };
        return dlg.ShowDialog(Owner) == true ? dlg.FileName : null;
    }
}

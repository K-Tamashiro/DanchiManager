using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class FeesViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly IDialogService _dialogs;
    readonly PrintService _print;
    readonly RoomRecord? _envelopeRoom;

    [ObservableProperty] private FeeYear _fee;
    [ObservableProperty] private string _title = "";

    public FeesViewModel(DatabaseService db, IDialogService dialogs, PrintService print, FeeYear fee, RoomRecord? envelopeRoom = null)
    {
        _db = db;
        _dialogs = dialogs;
        _print = print;
        _envelopeRoom = envelopeRoom?.Clone();
        Fee = fee;
        Title = $"令和{fee.Year:00}年度 会費集金情報";
        foreach (var m in Fee.Months)
            m.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalText));
    }

    public string TotalText => Fee.Total.ToString("#,##0");

    [RelayCommand]
    private void PrevYear()
    {
        SaveSilent();
        LoadYear(Fee.Year - 1);
    }

    [RelayCommand]
    private void NextYear()
    {
        SaveSilent();
        LoadYear(Fee.Year + 1);
    }

    [RelayCommand]
    private void ThisYear()
    {
        SaveSilent();
        LoadYear(AppConstants.FiscalWarekiYear());
    }

    void LoadYear(int year)
    {
        Fee = _db.LoadFees(Fee.BuildingName, Fee.RoomNo, year, Fee.Name, Fee.SortOrder);
        Title = $"令和{year:00}年度 会費集金情報";
        foreach (var m in Fee.Months)
            m.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(TotalText));
    }

    [RelayCommand]
    private void ToggleAll()
    {
        var any = Fee.Months.Any(m => m.Amount > 0);
        foreach (var m in Fee.Months)
            m.Amount = any ? 0 : AppConstants.DefaultFee;
        OnPropertyChanged(nameof(TotalText));
    }

    [RelayCommand]
    private void Save()
    {
        SaveSilent();
        RequestClose?.Invoke(this, true);
    }

    void SaveSilent() => _db.SaveFees(Fee);

    [RelayCommand]
    private void PrintEnvelope()
    {
        SaveSilent();
        var vm = new EnvelopePreviewViewModel(_db, _print, Fee.BuildingName, Fee.Year, Fee.RoomNo, _envelopeRoom);
        _dialogs.ShowEnvelopes(vm);
    }

    [RelayCommand] private void Cancel() => RequestClose?.Invoke(this, false);

    public event EventHandler<bool>? RequestClose;
}

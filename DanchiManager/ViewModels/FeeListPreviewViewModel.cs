using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public sealed class FeeListRow
{
    public required string Building { get; init; }
    public required string RoomNo { get; init; }
    public required string Name { get; init; }
    public int Total { get; init; }
    public bool Member { get; init; }
    public string MemberMark => Member ? "○" : "";
}

public partial class FeeListPreviewViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly PrintService _print;
    readonly IDialogService _dialogs;
    readonly string? _currentBuilding;

    public ObservableCollection<FeeListRow> Rows { get; } = [];
    [ObservableProperty] private int _year;
    [ObservableProperty] private int _sum;
    public string Title => $"令和{Year}年度 会費確認";

    public FeeListPreviewViewModel(DatabaseService db, PrintService print, IDialogService dialogs, string? currentBuilding)
    {
        _db = db;
        _print = print;
        _dialogs = dialogs;
        _currentBuilding = currentBuilding;
        Year = AppConstants.FiscalWarekiYear();
    }

    partial void OnYearChanged(int value) => Reload();

    void Reload()
    {
        Rows.Clear();
        Sum = 0;
        foreach (var r in _db.FeeList(Year))
        {
            Rows.Add(new FeeListRow
            {
                Building = r.Building,
                RoomNo = r.RoomNo,
                Name = r.Name,
                Total = r.Total,
                Member = r.Member,
            });
            Sum += r.Total;
        }
        OnPropertyChanged(nameof(Title));
    }

    [RelayCommand] private void PrevYear() => Year--;
    [RelayCommand] private void NextYear() => Year++;
    [RelayCommand] private void ThisYear() => Year = AppConstants.FiscalWarekiYear();

    [RelayCommand]
    private void Print()
    {
        if (Rows.Count == 0) return;
        _print.PrintFeeList(Year, Rows.Select(r => (r.Building, r.RoomNo, r.Name, r.Total, r.Member, 0)));
    }

    [RelayCommand]
    private void PrintEnvelopes()
    {
        var vm = new EnvelopePreviewViewModel(_db, _print, _currentBuilding, Year);
        _dialogs.ShowEnvelopes(vm);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

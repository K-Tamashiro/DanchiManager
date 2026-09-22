using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public sealed class TotalsRow
{
    public required string Name { get; init; }
    public BuildingStats Stats { get; init; }
}

public partial class TotalsViewModel : ObservableObject
{
    readonly PrintService _print;

    public ObservableCollection<TotalsRow> Rows { get; } = [];
    [ObservableProperty] private BuildingStats _all;
    [ObservableProperty] private string _title = "全棟集計";
    [ObservableProperty] private int _parkingOccupied;

    public TotalsViewModel(DatabaseService db, PrintService print, int parkingOccupied)
    {
        _print = print;
        ParkingOccupied = parkingOccupied;
        foreach (var b in db.LoadBuildings())
            Rows.Add(new TotalsRow { Name = b.Name, Stats = db.Stats(b.Name) });
        All = db.Stats(null);
    }

    [RelayCommand]
    private void Print()
    {
        _print.PrintTotals("錦林住宅 全棟集計", Rows.Select(r => (r.Name, r.Stats)), All);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

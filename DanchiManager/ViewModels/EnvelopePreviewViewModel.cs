using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class EnvelopePreviewViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly PrintService _print;
    readonly string? _currentBuilding;

    public ObservableCollection<RoomRecord> Recipients { get; } = [];
    public int Year { get; }
    public string Title => $"令和{Year}年度 会費封筒";
    public bool CanPrint => Count > 0;

    [ObservableProperty] private bool _allBuildings;
    [ObservableProperty] private int _count;

    public EnvelopePreviewViewModel(DatabaseService db, PrintService print, string? currentBuilding)
    {
        _db = db;
        _print = print;
        _currentBuilding = currentBuilding;
        Year = AppConstants.FiscalWarekiYear();
        AllBuildings = string.IsNullOrEmpty(currentBuilding);
        Rebuild();
    }

    partial void OnAllBuildingsChanged(bool value) => Rebuild();

    void Rebuild()
    {
        Recipients.Clear();
        var source = AllBuildings || string.IsNullOrEmpty(_currentBuilding)
            ? _db.LoadRooms()
            : _db.LoadRooms(_currentBuilding);
        foreach (var r in source
                     .Where(x => x.Vacancy == VacancyFlag.Occupied && x.Member)
                     .OrderBy(x => x.SortOrder)
                     .ThenBy(x => x.RoomNo))
            Recipients.Add(r);
        Count = Recipients.Count;
        OnPropertyChanged(nameof(CanPrint));
    }

    [RelayCommand]
    private void Print()
    {
        if (Recipients.Count == 0) return;
        _print.PrintEnvelopes(Year, Recipients);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

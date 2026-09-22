using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class BuildingSheetViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly PrintService _print;
    readonly IDialogService _dialogs;
    readonly List<Building> _buildings;

    public ObservableCollection<RoomRecord> Rooms { get; } = [];

    [ObservableProperty] private Building? _selectedBuilding;
    [ObservableProperty] private string _title = "棟情報確認";
    [ObservableProperty] private BuildingStats _stats;

    public BuildingSheetViewModel(DatabaseService db, PrintService print, IEnumerable<Building> buildings, Building? current, IDialogService dialogs)
    {
        _db = db;
        _print = print;
        _dialogs = dialogs;
        _buildings = buildings.ToList();
        SelectedBuilding = current ?? _buildings.FirstOrDefault();
    }

    partial void OnSelectedBuildingChanged(Building? value) => Load();

    void Load()
    {
        Rooms.Clear();
        var b = SelectedBuilding;
        if (b is null)
        {
            Title = "棟情報確認";
            Stats = default;
            return;
        }
        foreach (var r in _db.LoadRooms(b.Name).OrderBy(x => x.RoomNo))
            Rooms.Add(r);
        Stats = _db.Stats(b.Name);
        Title = $"{b.Name}  棟情報確認";
    }

    [RelayCommand]
    private void PrevBuilding()
    {
        if (SelectedBuilding is null) return;
        var i = _buildings.IndexOf(SelectedBuilding);
        if (i > 0) SelectedBuilding = _buildings[i - 1];
    }

    [RelayCommand]
    private void NextBuilding()
    {
        if (SelectedBuilding is null) return;
        var i = _buildings.IndexOf(SelectedBuilding);
        if (i >= 0 && i < _buildings.Count - 1) SelectedBuilding = _buildings[i + 1];
    }

    [RelayCommand]
    private void PrintBuilding()
    {
        if (SelectedBuilding is null) return;
        _print.PrintBuilding($"{SelectedBuilding.Name}  (全 {Rooms.Count} 戸)", Rooms, Stats, includePhone: true);
    }

    [RelayCommand]
    private void PrintAllBuildings()
    {
        var pick = new BuildingPickViewModel(_buildings);
        if (_dialogs.ShowBuildingPick(pick) != true) return;
        var selected = pick.SelectedBuildings;
        if (selected.Count == 0) return;
        var reports = selected.Select(building =>
        {
            var rooms = _db.LoadRooms(building.Name);
            return ($"{building.Name}  (全 {rooms.Count} 戸)", rooms, _db.Stats(building.Name));
        });
        _print.PrintBuildings(reports);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

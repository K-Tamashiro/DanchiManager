using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class BuildingEditorViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly IDialogService _dialogs;
    readonly string _originalName;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private bool _skip4 = true;
    [ObservableProperty] private int _levels = 3;
    [ObservableProperty] private int _roomsPerFloor = 6;
    [ObservableProperty] private string _customLayout = "";

    public string BuildingId { get; }
    public bool IsNew { get; }

    public BuildingEditorViewModel(DatabaseService db, IDialogService dialogs, Building? source)
    {
        _db = db;
        _dialogs = dialogs;
        if (source is null)
        {
            IsNew = true;
            BuildingId = Guid.NewGuid().ToString("N");
            _originalName = "";
            Name = "新しい棟";
            SortOrder = 99;
            RebuildLayout();
        }
        else
        {
            BuildingId = source.Id;
            _originalName = source.Name;
            Name = source.Name;
            SortOrder = source.SortOrder;
            Skip4 = source.Skip4;
            Levels = source.Floors.Count == 0 ? 1 : source.Floors.Max(f => f.Level);
            RoomsPerFloor = source.Floors.FirstOrDefault()?.Rooms.Count ?? 6;
            CustomLayout = string.Join("\n", source.Floors.Select(f => string.Join(",", f.Rooms)));
        }
    }

    public ObservableCollection<string> PreviewLines { get; } = [];

    [RelayCommand]
    private void RebuildLayout()
    {
        var lines = new List<string>();
        for (var lv = 1; lv <= Levels; lv++)
        {
            var rooms = new List<string>();
            var n = 1;
            while (rooms.Count < RoomsPerFloor)
            {
                if (Skip4 && n % 10 == 4) { n++; continue; }
                rooms.Add($"{lv * 100 + n}");
                n++;
            }
            lines.Add(string.Join(",", rooms));
        }
        CustomLayout = string.Join("\n", lines);
        RefreshPreview();
    }

    [RelayCommand]
    private void RefreshPreview()
    {
        PreviewLines.Clear();
        foreach (var line in CustomLayout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            PreviewLines.Add(line);
    }

    List<FloorDef> ParseFloors()
    {
        var floors = new List<FloorDef>();
        foreach (var line in CustomLayout.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var rooms = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
            if (rooms.Count == 0) continue;
            var level = floors.Count + 1;
            if (int.TryParse(rooms[0], out var n) && n >= 100)
                level = Math.Max(1, n / 100);
            floors.Add(new FloorDef { Level = level, Rooms = rooms });
        }
        return floors;
    }

    public Building? Result { get; private set; }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            _dialogs.Info("棟名を入力してください。");
            return;
        }
        var floors = ParseFloors();
        if (floors.Count < Levels && Levels >= 1)
        {
            RebuildLayout();
            floors = ParseFloors();
        }
        if (floors.Count == 0)
        {
            _dialogs.Info("部屋番号を1つ以上入力してください。");
            return;
        }
        Result = new Building
        {
            Id = BuildingId,
            Name = Name.Trim(),
            SortOrder = SortOrder,
            Skip4 = Skip4,
            Floors = floors,
        };
        _db.SaveBuilding(Result, string.IsNullOrEmpty(_originalName) ? null : _originalName);
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void Delete()
    {
        if (IsNew) { RequestClose?.Invoke(this, false); return; }
        if (!_dialogs.Confirm($"{_originalName} を削除します。部屋データも消えます。よろしいですか。", "棟の削除"))
            return;
        _db.DeleteBuilding(BuildingId, _originalName);
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand] private void Cancel() => RequestClose?.Invoke(this, false);

    public event EventHandler<bool>? RequestClose;
}

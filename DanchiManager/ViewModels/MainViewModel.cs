using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    DatabaseService _db;
    readonly IDialogService _dialogs;
    readonly PrintService _print = new();
    readonly AppSettings _settings;

    [ObservableProperty] private Building? _selectedBuilding;
    [ObservableProperty] private bool _locked = true;
    [ObservableProperty] private BuildingStats _currentStats;
    [ObservableProperty] private BuildingStats _allStats;
    [ObservableProperty] private int _parkingOccupied;
    [ObservableProperty] private string _dbPathDisplay = "";
    [ObservableProperty] private double _uiFontSize = 16;

    public ObservableCollection<Building> Buildings { get; } = [];
    public ObservableCollection<RoomCardViewModel> Slots { get; } = [];
    public ObservableCollection<FloorRowViewModel> FloorRows { get; } = [];

    public string Title => _settings.WindowTitle;
    public bool IsFontSmall => Math.Abs(UiFontSize - 14) < 0.1;
    public bool IsFontMedium => Math.Abs(UiFontSize - 16) < 0.1;
    public bool IsFontLarge => Math.Abs(UiFontSize - 18) < 0.1;
    public double FloorRowHeight => UiFontSize * 14.2;

    public MainViewModel(DatabaseService db, IDialogService dialogs)
    {
        _db = db;
        _dialogs = dialogs;
        _settings = PathService.LoadSettings();
        UiFontSize = NormalizeFont(_settings.UiFontSize);
        UiSettings.Current.FontSize = UiFontSize;
        DbPathDisplay = db.FilePath;
        Reload(_settings.LastBuildingName);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_settings, _dialogs);
        if (_dialogs.ShowSettings(vm) != true) return;
        var previous = (_settings.WindowTitle, _settings.EnvelopeTitle, _settings.EnvelopeStartMonth);
        _settings.WindowTitle = vm.WindowTitle.Trim();
        _settings.EnvelopeTitle = vm.EnvelopeTitle.Trim();
        _settings.EnvelopeStartMonth = vm.EnvelopeStartMonth;
        try
        {
            PathService.SaveSettings(_settings);
        }
        catch (Exception ex)
        {
            (_settings.WindowTitle, _settings.EnvelopeTitle, _settings.EnvelopeStartMonth) = previous;
            _dialogs.Info($"設定を保存できませんでした。\n{ex.Message}");
            return;
        }
        OnPropertyChanged(nameof(Title));
    }
    static double NormalizeFont(int size) => size switch
    {
        12 => 14,
        14 => 14,
        18 => 18,
        16 => 16,
        _ => 16,
    };

    partial void OnUiFontSizeChanged(double value)
    {
        OnPropertyChanged(nameof(IsFontSmall));
        OnPropertyChanged(nameof(IsFontMedium));
        OnPropertyChanged(nameof(IsFontLarge));
        OnPropertyChanged(nameof(FloorRowHeight));
        UiSettings.Current.FontSize = value;
        _settings.UiFontSize = (int)Math.Round(value);
        PathService.SaveSettings(_settings);
    }

    public void Reload(string? selectName)
    {
        var keep = selectName ?? SelectedBuilding?.Name ?? _settings.LastBuildingName;
        Buildings.Clear();
        foreach (var b in _db.LoadBuildings()) Buildings.Add(b);
        var selected = Buildings.FirstOrDefault(b => b.Name == keep) ?? Buildings.FirstOrDefault();
        if (SelectedBuilding == selected)
            RebuildSlots();
        else
            SelectedBuilding = selected;
    }

    partial void OnSelectedBuildingChanged(Building? value)
    {
        if (!string.IsNullOrWhiteSpace(value?.Name) && _settings.LastBuildingName != value.Name)
        {
            _settings.LastBuildingName = value.Name;
            PathService.SaveSettings(_settings);
        }
        RebuildSlots();
    }

    void RebuildSlots()
    {
        Slots.Clear();
        FloorRows.Clear();
        var b = SelectedBuilding;
        var rooms = b is null ? [] : _db.LoadRooms(b.Name);
        var map = rooms.ToDictionary(r => r.RoomNo, r => r);
        var floors = (b?.Floors ?? [])
            .Where(f => f.Rooms.Count > 0)
            .OrderByDescending(f => f.Level)
            .ToList();
        var cols = floors.Count == 0 ? 1 : Math.Max(1, floors.Max(f => f.Rooms.Count));
        foreach (var floor in floors)
        {
            var row = new FloorRowViewModel { Level = floor.Level };
            for (var col = 0; col < cols; col++)
            {
                var card = new RoomCardViewModel();
                if (col < floor.Rooms.Count)
                {
                    var no = floor.Rooms[col];
                    if (!map.TryGetValue(no, out var rec))
                        rec = RoomRecord.Empty(b!.Name, no, b.SortOrder);
                    card.Room = rec;
                    card.IsPlaceholder = false;
                }
                else
                {
                    card.IsPlaceholder = true;
                }
                row.Cards.Add(card);
                Slots.Add(card);
            }
            FloorRows.Add(row);
        }
        RefreshStats();
    }

    void RefreshStats()
    {
        CurrentStats = SelectedBuilding is null ? default : _db.Stats(SelectedBuilding.Name);
        AllStats = _db.Stats(null);
        ParkingOccupied = _db.LoadLots().Sum(l => l.OccupiedCount);
    }

    [RelayCommand]
    private void OpenRoom(RoomCardViewModel? card)
    {
        if (card?.Room is null || card.IsPlaceholder || SelectedBuilding is null) return;
        foreach (var slot in Slots) slot.IsSelected = false;
        card.IsSelected = true;
        var live = _db.LoadRoom(card.Room.BuildingName, card.Room.RoomNo) ?? card.Room;
        var vm = new RoomEditorViewModel(_db, _dialogs, live, RebuildSlots);
        _dialogs.ShowRoom(vm);
        RebuildSlots();
    }

    [RelayCommand]
    private void PrevBuilding()
    {
        if (SelectedBuilding is null) return;
        var i = Buildings.IndexOf(SelectedBuilding);
        if (i > 0) SelectedBuilding = Buildings[i - 1];
    }

    [RelayCommand]
    private void NextBuilding()
    {
        if (SelectedBuilding is null) return;
        var i = Buildings.IndexOf(SelectedBuilding);
        if (i >= 0 && i < Buildings.Count - 1) SelectedBuilding = Buildings[i + 1];
    }

    [RelayCommand]
    private void ToggleLock()
    {
        if (!Locked)
        {
            Locked = true;
            return;
        }
        var pw = _dialogs.Prompt("管理者パスワードを入力してください", "パスワード");
        if (pw == AppConstants.UnlockPassword) Locked = false;
        else if (pw is not null) _dialogs.Info("パスワードが違います。");
    }

    [RelayCommand]
    private void EditBuilding()
    {
        if (Locked)
        {
            _dialogs.Info("ロックを解除してください。");
            return;
        }
        var vm = new BuildingEditorViewModel(_db, _dialogs, SelectedBuilding);
        if (_dialogs.ShowBuilding(vm) == true)
            Reload(vm.Result?.Name ?? SelectedBuilding?.Name);
    }

    [RelayCommand]
    private void NewBuilding()
    {
        if (Locked) { _dialogs.Info("ロックを解除してください。"); return; }
        var vm = new BuildingEditorViewModel(_db, _dialogs, null);
        if (_dialogs.ShowBuilding(vm) == true)
            Reload(vm.Result?.Name);
    }

    [RelayCommand]
    private void OpenParking()
    {
        var vm = new ParkingViewModel(_db, _dialogs, _print);
        _dialogs.ShowParking(vm);
        RefreshStats();
    }

    [RelayCommand]
    private void OpenTotals()
    {
        var vm = new TotalsViewModel(_db, _print, ParkingOccupied, name =>
        {
            var b = Buildings.FirstOrDefault(x => x.Name == name);
            if (b is not null) SelectedBuilding = b;
        });
        _dialogs.ShowTotals(vm);
    }

    [RelayCommand]
    private void PrintBuildingDetail()
    {
        if (SelectedBuilding is null) return;
        var vm = new BuildingSheetViewModel(_db, _print, Buildings, SelectedBuilding, _dialogs);
        _dialogs.ShowBuildingSheet(vm);
    }

    [RelayCommand]
    private void PrintFeeList()
    {
        var vm = new FeeListPreviewViewModel(_db, _print, _dialogs, SelectedBuilding?.Name);
        _dialogs.ShowFeeList(vm);
    }

    [RelayCommand]
    private void Search()
    {
        var vm = new SearchViewModel(_db.LoadRooms());
        if (_dialogs.ShowSearch(vm) == true && vm.Selected is not null)
        {
            var hit = vm.Selected;
            SelectedBuilding = Buildings.FirstOrDefault(b => b.Name == hit.BuildingName) ?? SelectedBuilding;
            var card = Slots.FirstOrDefault(s => s.Room?.RoomNo == hit.RoomNo);
            OpenRoom(card);
        }
    }

    [RelayCommand]
    private void ChangeDatabase()
    {
        if (Locked) { _dialogs.Info("ロックを解除してください。"); return; }
        var path = _dialogs.PickOpenDb();
        if (path is null) return;
        var nextDb = DatabaseService.Open(path);
        var previousDb = _db;
        _db = nextDb;
        previousDb.Dispose();
        _settings.UseCustomPath = true;
        _settings.DatabasePath = path;
        PathService.SaveSettings(_settings);
        DbPathDisplay = path;
        Reload(null);
    }

    [RelayCommand]
    private void Backup()
    {
        var dest = _dialogs.PickSaveFile("SQLite データベース (*.db)|*.db", $"danchi-{DateTime.Now:yyyyMMdd}.db");
        if (dest is null) return;
        _db.BackupTo(dest);
        _dialogs.Info("バックアップしました。");
    }

    [RelayCommand]
    private void ExportCsv()
    {
        var dest = _dialogs.PickSaveFile("CSV (*.csv)|*.csv", "danchi.csv");
        if (dest is null) return;
        var rooms = _db.LoadRooms();
        using var sw = new StreamWriter(dest, false, new System.Text.UTF8Encoding(true));
        sw.WriteLine("棟,部屋番号,名前,フリガナ,性別,人数,バイク,自転車,電話,空きフラグ,備考,更新日,入居順,会員");
        foreach (var r in rooms)
        {
            sw.WriteLine(string.Join(",",
                Csv(r.BuildingName), Csv(r.RoomNo), Csv(r.Name), Csv(r.Furigana), (int)r.Gender,
                r.People, r.Bikes, r.Bicycles, Csv(r.Phone), (int)r.Vacancy, Csv(r.Note),
                Csv(r.UpdatedAt), r.SortOrder, r.Member ? "True" : "False"));
        }
        _dialogs.Info("CSV を書き出しました。");
    }

    static string Csv(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    public void Dispose() => _db.Dispose();

    [RelayCommand] private void SetFontSmall() => UiFontSize = 14;
    [RelayCommand] private void SetFontMedium() => UiFontSize = 16;
    [RelayCommand] private void SetFontLarge() => UiFontSize = 18;

    [RelayCommand]
    private void Exit()
    {
        if (_dialogs.Confirm("終了しますか？", "団地管理"))
            System.Windows.Application.Current.Shutdown();
    }
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class EnvelopeRecipient : ObservableObject
{
    public required RoomRecord Room { get; init; }
    [ObservableProperty] private bool _isSelected = true;

    public string BuildingName => Room.BuildingName;
    public string RoomNo => Room.RoomNo;
    public string Name => Room.Name;
    public string Phone => Room.Phone;
}

public partial class EnvelopePreviewViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly PrintService _print;
    readonly string? _currentBuilding;
    string? _focusRoomNo;
    readonly RoomRecord? _envelopeRoom;

    public ObservableCollection<EnvelopeRecipient> Recipients { get; } = [];
    public int Year { get; }
    public string Title => $"令和{Year}年度 会費封筒";
    public bool CanPrint => Count > 0;

    [ObservableProperty] private bool _allBuildings;
    [ObservableProperty] private int _count;

    public EnvelopePreviewViewModel(DatabaseService db, PrintService print, string? currentBuilding, int year, string? focusRoomNo = null, RoomRecord? envelopeRoom = null)
    {
        _db = db;
        _print = print;
        _currentBuilding = currentBuilding;
        Year = year;
        _focusRoomNo = focusRoomNo;
        _envelopeRoom = envelopeRoom?.Clone();
        _allBuildings = string.IsNullOrEmpty(currentBuilding);
        Rebuild();
    }

    partial void OnAllBuildingsChanged(bool value) => Rebuild();

    void Rebuild()
    {
        Recipients.Clear();
        IReadOnlyList<RoomRecord> source = AllBuildings
            ? _db.LoadRooms()
            : string.IsNullOrEmpty(_currentBuilding) ? [] : _db.LoadRooms(_currentBuilding);
        foreach (var r in source
                     .Where(x => x.Vacancy == VacancyFlag.Occupied && x.Member)
                     .Select(WithDraft)
                     .OrderBy(x => x.SortOrder)
                     .ThenBy(x => x.RoomNo))
        {
            var item = new EnvelopeRecipient { Room = r, IsSelected = true };
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(EnvelopeRecipient.IsSelected))
                    RefreshCount();
            };
            Recipients.Add(item);
        }
        if (_focusRoomNo is not null)
        {
            ApplyFocus(_focusRoomNo);
            _focusRoomNo = null;
        }
        else
        {
            RefreshCount();
        }
    }

    void ApplyFocus(string roomNo)
    {
        EnvelopeRecipient? hit = null;
        foreach (var r in Recipients)
        {
            r.IsSelected = r.RoomNo == roomNo && r.BuildingName == _currentBuilding;
            if (r.IsSelected) hit = r;
        }
        if (hit is null && !string.IsNullOrEmpty(_currentBuilding))
        {
            var rec = _envelopeRoom is { } draft && draft.BuildingName == _currentBuilding && draft.RoomNo == roomNo
                ? draft.Clone() : _db.LoadRoom(_currentBuilding, roomNo);
            if (rec is not null)
            {
                var item = new EnvelopeRecipient { Room = rec, IsSelected = true };
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(EnvelopeRecipient.IsSelected))
                        RefreshCount();
                };
                Recipients.Insert(0, item);
            }
        }
        RefreshCount();
    }

    RoomRecord WithDraft(RoomRecord room) =>
        _envelopeRoom is { } draft && draft.BuildingName == room.BuildingName && draft.RoomNo == room.RoomNo
            ? draft.Clone() : room;

    void RefreshCount()
    {
        Count = Recipients.Count(x => x.IsSelected);
        OnPropertyChanged(nameof(CanPrint));
    }

    [RelayCommand]
    private void CheckAll()
    {
        foreach (var r in Recipients) r.IsSelected = true;
        RefreshCount();
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var r in Recipients) r.IsSelected = false;
        RefreshCount();
    }

    [RelayCommand]
    private void Print()
    {
        var list = Recipients.Where(x => x.IsSelected).Select(x => x.Room).ToList();
        if (list.Count == 0) return;
        _print.PrintEnvelopes(Year, list);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class ParkingViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly IDialogService _dialogs;
    readonly PrintService _print;

    public ObservableCollection<ParkingLot> Lots { get; } = [];

    [ObservableProperty] private ParkingLot? _selectedLot;
    [ObservableProperty] private ParkingSlot? _selectedSlot;

    public ParkingViewModel(DatabaseService db, IDialogService dialogs, PrintService print)
    {
        _db = db;
        _dialogs = dialogs;
        _print = print;
        foreach (var lot in db.LoadLots()) Lots.Add(lot);
        SelectedLot = Lots.FirstOrDefault();
    }

    partial void OnSelectedLotChanged(ParkingLot? value) => SelectedSlot = value?.Slots.FirstOrDefault();

    public int OccupiedTotal => Lots.Sum(l => l.OccupiedCount);

    [RelayCommand]
    private void AddLot()
    {
        var lot = new ParkingLot
        {
            Name = $"第{Lots.Count + 1}駐車場",
            Fee = 5000,
        };
        for (var i = 1; i <= 10; i++)
            lot.Slots.Add(new ParkingSlot { No = i, Occupant = "空き" });
        _db.SaveLot(lot);
        Lots.Add(lot);
        SelectedLot = lot;
        OnPropertyChanged(nameof(OccupiedTotal));
    }

    [RelayCommand]
    private void DeleteLot()
    {
        if (SelectedLot is null) return;
        if (!_dialogs.Confirm($"{SelectedLot.Name} を削除します。よろしいですか。", "駐車場の削除"))
            return;
        _db.DeleteLot(SelectedLot.Id);
        Lots.Remove(SelectedLot);
        SelectedLot = Lots.FirstOrDefault();
        OnPropertyChanged(nameof(OccupiedTotal));
    }

    [RelayCommand]
    private void UpdateLot()
    {
        if (SelectedLot is null) return;
        _db.SaveLot(SelectedLot);
        OnPropertyChanged(nameof(OccupiedTotal));
        _dialogs.Info("駐車場を更新しました。");
    }

    [RelayCommand]
    private void AddSlot()
    {
        if (SelectedLot is null) return;
        SelectedLot.Slots.Add(new ParkingSlot { Occupant = "空き" });
        Renumber();
        SelectedSlot = SelectedLot.Slots[^1];
        OnPropertyChanged(nameof(OccupiedTotal));
    }

    [RelayCommand]
    private void DeleteSlot()
    {
        if (SelectedLot is null || SelectedSlot is null) return;
        if (!SelectedSlot.IsVacant &&
            !_dialogs.Confirm($"{SelectedSlot.No} 番（{SelectedSlot.Occupant}）を削除します。よろしいですか。", "駐車枠の削除"))
            return;
        SelectedLot.Slots.Remove(SelectedSlot);
        Renumber();
        SelectedSlot = SelectedLot.Slots.LastOrDefault();
        OnPropertyChanged(nameof(OccupiedTotal));
    }

    void Renumber()
    {
        if (SelectedLot is null) return;
        var n = 1;
        foreach (var slot in SelectedLot.Slots)
            slot.No = n++;
    }

    [RelayCommand]
    private void Save()
    {
        foreach (var lot in Lots) _db.SaveLot(lot);
        OnPropertyChanged(nameof(OccupiedTotal));
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand]
    private void Print()
    {
        if (Lots.Count == 0) return;
        var pick = new ParkingPickViewModel(Lots);
        if (_dialogs.ShowParkingPick(pick) != true) return;
        var selected = pick.SelectedLots;
        if (selected.Count == 0) return;
        _print.PrintParking(selected);
    }

    [RelayCommand] private void Close() => RequestClose?.Invoke(this, true);

    public event EventHandler<bool>? RequestClose;
}

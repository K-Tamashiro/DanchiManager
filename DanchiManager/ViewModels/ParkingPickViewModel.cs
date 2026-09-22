using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;

namespace DanchiManager.ViewModels;

public partial class ParkingPickItem : ObservableObject
{
    public required ParkingLot Lot { get; init; }
    [ObservableProperty] private bool _isSelected = true;
    public string Name => Lot.Name;
}

public partial class ParkingPickViewModel : ObservableObject
{
    public ObservableCollection<ParkingPickItem> Items { get; } = [];
    public string Title => "印刷する駐車場を選ぶ";
    [ObservableProperty] private int _count;

    public ParkingPickViewModel(IEnumerable<ParkingLot> lots)
    {
        foreach (var lot in lots)
        {
            var item = new ParkingPickItem { Lot = lot, IsSelected = true };
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ParkingPickItem.IsSelected))
                    RefreshCount();
            };
            Items.Add(item);
        }
        RefreshCount();
    }

    public IReadOnlyList<ParkingLot> SelectedLots =>
        Items.Where(x => x.IsSelected).Select(x => x.Lot).ToList();

    void RefreshCount() => Count = Items.Count(x => x.IsSelected);

    [RelayCommand]
    private void CheckAll()
    {
        foreach (var i in Items) i.IsSelected = true;
        RefreshCount();
    }

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var i in Items) i.IsSelected = false;
        RefreshCount();
    }

    [RelayCommand]
    private void Confirm()
    {
        if (Count == 0) return;
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand] private void Cancel() => RequestClose?.Invoke(this, false);

    public event EventHandler<bool>? RequestClose;
}

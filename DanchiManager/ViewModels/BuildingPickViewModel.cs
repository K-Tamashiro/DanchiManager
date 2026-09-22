using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;

namespace DanchiManager.ViewModels;

public partial class BuildingPickItem : ObservableObject
{
    public required Building Building { get; init; }
    [ObservableProperty] private bool _isSelected = true;
    public string Name => Building.Name;
}

public partial class BuildingPickViewModel : ObservableObject
{
    public ObservableCollection<BuildingPickItem> Items { get; } = [];
    public string Title => "印刷する棟を選ぶ";
    [ObservableProperty] private int _count;

    public BuildingPickViewModel(IEnumerable<Building> buildings)
    {
        foreach (var b in buildings)
        {
            var item = new BuildingPickItem { Building = b, IsSelected = true };
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(BuildingPickItem.IsSelected))
                    RefreshCount();
            };
            Items.Add(item);
        }
        RefreshCount();
    }

    public IReadOnlyList<Building> SelectedBuildings =>
        Items.Where(x => x.IsSelected).Select(x => x.Building).ToList();

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

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;

namespace DanchiManager.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    readonly IReadOnlyList<RoomRecord> _all;

    [ObservableProperty] private string _query = "";
    public ObservableCollection<RoomRecord> Hits { get; } = [];

    [ObservableProperty] private RoomRecord? _selected;

    public SearchViewModel(IEnumerable<RoomRecord> rooms) => _all = rooms.ToList();

    partial void OnQueryChanged(string value) => Run();

    [RelayCommand]
    private void Run()
    {
        Hits.Clear();
        var q = Query.Trim();
        if (q.Length == 0) return;
        foreach (var r in _all.Where(x =>
                     x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.Furigana.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.RoomNo.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.Phone.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                     x.BuildingName.Contains(q, StringComparison.OrdinalIgnoreCase)))
            Hits.Add(r);
    }

    [RelayCommand]
    private void Choose()
    {
        if (Selected is not null)
            RequestClose?.Invoke(this, true);
    }

    [RelayCommand] private void Cancel() => RequestClose?.Invoke(this, false);

    public event EventHandler<bool>? RequestClose;
}

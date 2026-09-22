using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.Models;

public partial class Building : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private bool _skip4 = true;
    [ObservableProperty] private List<FloorDef> _floors = [];

    public IEnumerable<string> AllRoomNos => Floors.SelectMany(f => f.Rooms);
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.Models;

public partial class ParkingSlot : ObservableObject
{
    [ObservableProperty] private int _no;
    [ObservableProperty] private string _occupant = "空き";
    [ObservableProperty] private string _carNo = "";
    [ObservableProperty] private bool _isInvalid;

    public bool IsDisabled => IsInvalid;
    public bool IsVacant => !IsInvalid && (string.IsNullOrWhiteSpace(Occupant) || Occupant is "空き" or "利用不可");

    partial void OnIsInvalidChanged(bool value)
    {
        if (value)
        {
            if (string.IsNullOrWhiteSpace(Occupant) || Occupant == "空き")
                Occupant = "利用不可";
        }
        else if (Occupant == "利用不可")
        {
            Occupant = "空き";
        }
        OnPropertyChanged(nameof(IsDisabled));
        OnPropertyChanged(nameof(IsVacant));
    }
}

public partial class ParkingLot : ObservableObject
{
    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _fee = 5000;
    public System.Collections.ObjectModel.ObservableCollection<ParkingSlot> Slots { get; } = [];

    public int OccupiedCount => Slots.Count(s => !s.IsInvalid && !s.IsVacant);

    public bool Normalize()
    {
        var changed = false;
        foreach (var s in Slots)
        {
            if (s.No == 0 || s.Occupant == "利用不可")
            {
                if (!s.IsInvalid)
                {
                    s.IsInvalid = true;
                    changed = true;
                }
            }
        }
        var n = 1;
        foreach (var s in Slots)
        {
            if (s.No != n)
            {
                s.No = n;
                changed = true;
            }
            n++;
        }
        return changed;
    }
}

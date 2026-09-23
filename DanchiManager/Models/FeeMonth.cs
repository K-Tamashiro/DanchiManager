using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.Models;

public partial class FeeMonth : ObservableObject
{
    [ObservableProperty] private int _month;
    [ObservableProperty] private int _amount;
    public int Standard { get; set; } = AppConstants.DefaultFee;

    public int CalendarMonth => (Month + 3) % 12 + 1;
    public string Label => $"{CalendarMonth}月";

    public int FeeAmount
    {
        get => Amount > 0 ? Amount : Standard;
        set => Amount = value < 0 ? 0 : value;
    }

    public bool Paid
    {
        get => Amount > 0;
        set => Amount = value ? (Amount > 0 ? Amount : Standard) : 0;
    }

    partial void OnAmountChanged(int value)
    {
        OnPropertyChanged(nameof(Paid));
        OnPropertyChanged(nameof(FeeAmount));
    }
}

public partial class FeeYear : ObservableObject
{
    [ObservableProperty] private string _buildingName = "";
    [ObservableProperty] private string _roomNo = "";
    [ObservableProperty] private int _year;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _sortOrder;
    public ObservableCollection<FeeMonth> Months { get; } = [];

    public int Total => Months.Sum(m => m.Amount);
}

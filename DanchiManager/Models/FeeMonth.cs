using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.Models;

public partial class FeeMonth : ObservableObject
{
    [ObservableProperty] private int _month;
    [ObservableProperty] private int _amount;

    public string Label => Month switch
    {
        0 => "4月",
        1 => "5月",
        2 => "6月",
        3 => "7月",
        4 => "8月",
        5 => "9月",
        6 => "10月",
        7 => "11月",
        8 => "12月",
        9 => "1月",
        10 => "2月",
        11 => "3月",
        _ => $"{Month}",
    };

    public bool Paid
    {
        get => Amount > 0;
        set
        {
            Amount = value ? (Amount > 0 ? Amount : AppConstants.DefaultFee) : 0;
        }
    }

    partial void OnAmountChanged(int value) => OnPropertyChanged(nameof(Paid));
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

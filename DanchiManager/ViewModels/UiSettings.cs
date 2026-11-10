using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.ViewModels;

public partial class UiSettings : ObservableObject
{
    public static UiSettings Current { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ListRowHeight))]
    private double _fontSize = 16;

    public double ListRowHeight => FontSize + 16;
}

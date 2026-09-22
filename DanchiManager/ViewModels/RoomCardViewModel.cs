using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DanchiManager.Models;

namespace DanchiManager.ViewModels;

public sealed class FloorRowViewModel
{
    public int Level { get; init; }
    public ObservableCollection<RoomCardViewModel> Cards { get; } = [];
}

public partial class RoomCardViewModel : ObservableObject
{
    [ObservableProperty] private bool _isPlaceholder;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private RoomRecord? _room;

    public bool HasRoom => Room is not null && !IsPlaceholder;
    public bool IsVacant => Room?.Vacancy == VacancyFlag.Vacant;
    public bool IsHospital => Room?.Vacancy == VacancyFlag.Hospital;
    public bool ShowDetails => HasRoom && !IsVacant;

    partial void OnRoomChanged(RoomRecord? value) => RaiseLooks();
    partial void OnIsPlaceholderChanged(bool value) => RaiseLooks();

    void RaiseLooks()
    {
        OnPropertyChanged(nameof(HasRoom));
        OnPropertyChanged(nameof(IsVacant));
        OnPropertyChanged(nameof(IsHospital));
        OnPropertyChanged(nameof(ShowDetails));
    }

}

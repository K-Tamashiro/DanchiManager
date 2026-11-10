using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DanchiManager.Models;

namespace DanchiManager.ViewModels;

public sealed class FloorRowViewModel
{
    public ObservableCollection<RoomCardViewModel> Cards { get; } = [];
}

public partial class RoomCardViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRoom))]
    [NotifyPropertyChangedFor(nameof(IsVacant))]
    [NotifyPropertyChangedFor(nameof(IsHospital))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    private bool _isPlaceholder;
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRoom))]
    [NotifyPropertyChangedFor(nameof(IsVacant))]
    [NotifyPropertyChangedFor(nameof(IsHospital))]
    [NotifyPropertyChangedFor(nameof(ShowDetails))]
    private RoomRecord? _room;

    public bool HasRoom => Room is not null && !IsPlaceholder;
    public bool IsVacant => Room?.Vacancy == VacancyFlag.Vacant;
    public bool IsHospital => Room?.Vacancy == VacancyFlag.Hospital;
    public bool ShowDetails => HasRoom && !IsVacant;
}

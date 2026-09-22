using CommunityToolkit.Mvvm.ComponentModel;

namespace DanchiManager.Models;

public partial class RoomRecord : ObservableObject
{
    [ObservableProperty] private string _buildingName = "";
    [ObservableProperty] private string _roomNo = "";
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _furigana = "";
    [ObservableProperty] private GenderFlag _gender = GenderFlag.Unset;
    [ObservableProperty] private int _people;
    [ObservableProperty] private int _bikes;
    [ObservableProperty] private int _bicycles;
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private VacancyFlag _vacancy = VacancyFlag.Vacant;
    [ObservableProperty] private string _note = "";
    [ObservableProperty] private string _updatedAt = "";
    [ObservableProperty] private int _sortOrder;
    [ObservableProperty] private bool _member;

    public RoomRecord Clone() => new()
    {
        BuildingName = BuildingName,
        RoomNo = RoomNo,
        Name = Name,
        Furigana = Furigana,
        Gender = Gender,
        People = People,
        Bikes = Bikes,
        Bicycles = Bicycles,
        Phone = Phone,
        Vacancy = Vacancy,
        Note = Note,
        UpdatedAt = UpdatedAt,
        SortOrder = SortOrder,
        Member = Member,
    };

    public static RoomRecord Empty(string buildingName, string roomNo, int sortOrder) => new()
    {
        BuildingName = buildingName,
        RoomNo = roomNo,
        Vacancy = VacancyFlag.Vacant,
        Gender = GenderFlag.Unset,
        SortOrder = sortOrder,
        UpdatedAt = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
    };
}

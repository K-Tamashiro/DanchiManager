using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class RoomEditorViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly IDialogService _dialogs;
    readonly Action _onSaved;

    [ObservableProperty] private RoomRecord _draft;
    [ObservableProperty] private string _buildingTitle = "";
    [ObservableProperty] private int _feeYear = AppConstants.FiscalWarekiYear();

    public string PhonePrefix1 { get; }
    public string PhonePrefix2 { get; }
    public string PhonePrefix3 { get; }
    public string PhonePrefix4 { get; }
    public string PhonePrefix5 { get; }
    public bool HasPhonePrefix1 => PhonePrefix1.Length > 0;
    public bool HasPhonePrefix2 => PhonePrefix2.Length > 0;
    public bool HasPhonePrefix3 => PhonePrefix3.Length > 0;
    public bool HasPhonePrefix4 => PhonePrefix4.Length > 0;
    public bool HasPhonePrefix5 => PhonePrefix5.Length > 0;
    public IReadOnlyList<string> NoteButtons { get; }

    public RoomEditorViewModel(DatabaseService db, IDialogService dialogs, RoomRecord source, Action onSaved)
    {
        _db = db;
        _dialogs = dialogs;
        _onSaved = onSaved;
        Draft = source.Clone();
        BuildingTitle = $"{source.BuildingName}  {source.RoomNo} 号室";
        if (Draft.Vacancy == VacancyFlag.Vacant && string.IsNullOrWhiteSpace(Draft.Name))
            Draft.Gender = GenderFlag.Unset;
        var settings = PathService.LoadSettings();
        PhonePrefix1 = Clip(settings.PhonePrefix1);
        PhonePrefix2 = Clip(settings.PhonePrefix2);
        PhonePrefix3 = Clip(settings.PhonePrefix3);
        PhonePrefix4 = Clip(settings.PhonePrefix4);
        PhonePrefix5 = Clip(settings.PhonePrefix5);
        NoteButtons =
        [
            Clip(settings.NoteButton1), Clip(settings.NoteButton2), Clip(settings.NoteButton3),
            Clip(settings.NoteButton4), Clip(settings.NoteButton5), Clip(settings.NoteButton6),
            Clip(settings.NoteButton7), Clip(settings.NoteButton8), Clip(settings.NoteButton9),
            Clip(settings.NoteButton10),
        ];
        NoteButtons = NoteButtons.Where(s => s.Length > 0).ToArray();
    }

    static string Clip(string? value)
    {
        var s = (value ?? "").Trim();
        return s.Length <= 10 ? s : s[..10];
    }

    public void ApplyPhonePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return;
        var phone = Draft.Phone ?? "";
        foreach (var known in new[] { PhonePrefix1, PhonePrefix2, PhonePrefix3, PhonePrefix4, PhonePrefix5 })
        {
            if (known.Length > 0 && phone.StartsWith(known, StringComparison.Ordinal))
            {
                Draft.Phone = prefix + phone[known.Length..];
                return;
            }
        }
        Draft.Phone = phone.Length == 0 ? prefix : phone + prefix;
    }

    public VacancyFlag[] Vacancies { get; } = [VacancyFlag.Vacant, VacancyFlag.Hospital, VacancyFlag.Occupied];
    public GenderFlag[] Genders { get; } = [GenderFlag.Male, GenderFlag.Female, GenderFlag.Unset];

    [RelayCommand]
    private void Save()
    {
        if (!string.IsNullOrWhiteSpace(Draft.Name) && Draft.Vacancy == VacancyFlag.Vacant)
        {
            _dialogs.Info("部屋の状態が空室ですが通常に変更して登録します。");
            Draft.Vacancy = VacancyFlag.Occupied;
        }
        if (Draft.Vacancy != VacancyFlag.Vacant && Draft.Gender == GenderFlag.Unset)
        {
            if (!_dialogs.Confirm("性別が設定されていませんがこのまま登録しますか。", "性別確認"))
                return;
        }
        _db.UpsertRoom(Draft);
        _onSaved();
        CloseOk();
    }

    [RelayCommand]
    private void Clear()
    {
        if (!_dialogs.Confirm($"{Draft.BuildingName}の部屋番号【{Draft.RoomNo}】の情報を削除します。\n\nよろしいですか。", "部屋情報の削除"))
            return;
        _db.ClearRoom(Draft.BuildingName, Draft.RoomNo, Draft.SortOrder, Draft.Note);
        _onSaved();
        CloseOk();
    }

    [RelayCommand] private void Cancel() => Close(false);

    [RelayCommand]
    private void AppendNote(string? text)
    {
        if (string.IsNullOrEmpty(text)) return;
        Draft.Note += text is "の息子" or "の娘" ? Draft.Name + text : text;
    }

    [RelayCommand]
    private void OpenFees()
    {
        var fee = _db.LoadFees(Draft.BuildingName, Draft.RoomNo, FeeYear, Draft.Name, Draft.SortOrder);
        var vm = new FeesViewModel(_db, _dialogs, new PrintService(), fee, Draft);
        _dialogs.ShowFees(vm);
    }

    public event EventHandler<bool>? RequestClose;
    void CloseOk() => Close(true);
    void Close(bool ok) => RequestClose?.Invoke(this, ok);

    public void AppendFurigana(string reading)
    {
        Draft.Furigana += reading;
    }
}

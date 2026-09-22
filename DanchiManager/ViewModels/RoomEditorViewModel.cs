using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class RoomEditorViewModel : ObservableObject
{
    readonly DatabaseService _db;
    readonly IDialogService _dialogs;

    [ObservableProperty] private RoomRecord _draft;
    [ObservableProperty] private string _buildingTitle = "";
    [ObservableProperty] private int _feeYear = AppConstants.FiscalWarekiYear();

    public RoomEditorViewModel(DatabaseService db, IDialogService dialogs, RoomRecord source)
    {
        _db = db;
        _dialogs = dialogs;
        Draft = source.Clone();
        BuildingTitle = $"{source.BuildingName}  {source.RoomNo} 号室";
        if (Draft.Vacancy == VacancyFlag.Vacant && string.IsNullOrWhiteSpace(Draft.Name))
            Draft.Gender = GenderFlag.Unset;
    }

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
        CloseOk();
    }

    [RelayCommand]
    private void Clear()
    {
        if (!_dialogs.Confirm($"{Draft.BuildingName}の部屋番号【{Draft.RoomNo}】の情報を削除します。\n\nよろしいですか。", "部屋情報の削除"))
            return;
        _db.ClearRoom(Draft.BuildingName, Draft.RoomNo, Draft.SortOrder, Draft.Note);
        CloseOk();
    }

    [RelayCommand] private void Cancel() => Close(false);

    [RelayCommand] private void AppendNote(string text) => Draft.Note += text;
    [RelayCommand] private void NoteSon() => Draft.Note += Draft.Name + "の息子";
    [RelayCommand] private void NoteDaughter() => Draft.Note += Draft.Name + "の娘";
    [RelayCommand] private void ClearNote() => Draft.Note = "";

    [RelayCommand]
    private void OpenFees()
    {
        var fee = _db.LoadFees(Draft.BuildingName, Draft.RoomNo, FeeYear, Draft.Name, Draft.SortOrder);
        var vm = new FeesViewModel(_db, fee);
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

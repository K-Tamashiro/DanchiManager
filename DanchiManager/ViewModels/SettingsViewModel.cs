using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DanchiManager.Models;
using DanchiManager.Services;

namespace DanchiManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    readonly IDialogService _dialogs;
    [ObservableProperty] private string _windowTitle;
    [ObservableProperty] private string _envelopeTitle;
    [ObservableProperty] private int _envelopeStartMonth;
    [ObservableProperty] private int _monthlyFee;
    [ObservableProperty] private string _phonePrefix1;
    [ObservableProperty] private string _phonePrefix2;
    [ObservableProperty] private string _phonePrefix3;
    [ObservableProperty] private string _phonePrefix4;
    [ObservableProperty] private string _phonePrefix5;
    [ObservableProperty] private string _noteButton1;
    [ObservableProperty] private string _noteButton2;
    [ObservableProperty] private string _noteButton3;
    [ObservableProperty] private string _noteButton4;
    [ObservableProperty] private string _noteButton5;
    [ObservableProperty] private string _noteButton6;
    [ObservableProperty] private string _noteButton7;
    [ObservableProperty] private string _noteButton8;
    [ObservableProperty] private string _noteButton9;
    [ObservableProperty] private string _noteButton10;
    public int[] Months { get; } = Enumerable.Range(1, 12).ToArray();

    public SettingsViewModel(AppSettings settings, IDialogService dialogs)
    {
        _dialogs = dialogs;
        _windowTitle = settings.WindowTitle;
        _envelopeTitle = settings.EnvelopeTitle;
        _envelopeStartMonth = settings.EnvelopeStartMonth is >= 1 and <= 12 ? settings.EnvelopeStartMonth : 4;
        _monthlyFee = settings.MonthlyFee >= 0 ? settings.MonthlyFee : 0;
        _phonePrefix1 = settings.PhonePrefix1 ?? "";
        _phonePrefix2 = settings.PhonePrefix2 ?? "";
        _phonePrefix3 = settings.PhonePrefix3 ?? "";
        _phonePrefix4 = settings.PhonePrefix4 ?? "";
        _phonePrefix5 = settings.PhonePrefix5 ?? "";
        _noteButton1 = settings.NoteButton1 ?? "";
        _noteButton2 = settings.NoteButton2 ?? "";
        _noteButton3 = settings.NoteButton3 ?? "";
        _noteButton4 = settings.NoteButton4 ?? "";
        _noteButton5 = settings.NoteButton5 ?? "";
        _noteButton6 = settings.NoteButton6 ?? "";
        _noteButton7 = settings.NoteButton7 ?? "";
        _noteButton8 = settings.NoteButton8 ?? "";
        _noteButton9 = settings.NoteButton9 ?? "";
        _noteButton10 = settings.NoteButton10 ?? "";
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(WindowTitle) || string.IsNullOrWhiteSpace(EnvelopeTitle))
        {
            _dialogs.Info("タイトルを入力してください。");
            return;
        }
        if (EnvelopeStartMonth is < 1 or > 12) return;
        RequestClose?.Invoke(this, true);
    }

    [RelayCommand] private void Cancel() => RequestClose?.Invoke(this, false);
    public event EventHandler<bool>? RequestClose;
}

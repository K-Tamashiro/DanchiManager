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
    public int[] Months { get; } = Enumerable.Range(1, 12).ToArray();

    public SettingsViewModel(AppSettings settings, IDialogService dialogs)
    {
        _dialogs = dialogs;
        _windowTitle = settings.WindowTitle;
        _envelopeTitle = settings.EnvelopeTitle;
        _envelopeStartMonth = settings.EnvelopeStartMonth is >= 1 and <= 12 ? settings.EnvelopeStartMonth : 4;
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

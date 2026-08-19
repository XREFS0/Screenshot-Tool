using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IHotkeyService _hotkeyService;

    [ObservableProperty]
    private AppSettings _settings;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsService settingsService, IHotkeyService hotkeyService)
    {
        _settingsService = settingsService;
        _hotkeyService = hotkeyService;
        _settings = _settingsService.Settings;
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _settingsService.SaveSettingsAsync();
        _hotkeyService.RegisterHotkeys(Settings);
        StatusMessage = "Settings saved successfully!";
    }

    [RelayCommand]
    private void ResetDefaults()
    {
        _settingsService.ResetToDefaults();
        Settings = _settingsService.Settings;
        StatusMessage = "Reset to default settings";
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog();
        dialog.Description = "Select Screenshot Save Folder";
        dialog.UseDescriptionForTitle = true;
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            Settings.SaveLocation = dialog.SelectedPath;
            OnPropertyChanged(nameof(Settings));
        }
    }
}

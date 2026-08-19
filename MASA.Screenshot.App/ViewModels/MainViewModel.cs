using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;
using MASA.Screenshot.Infrastructure.Logging;

namespace MASA.Screenshot.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ICaptureService _captureService;
    private readonly IImageProcessingService _imageProcessor;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly IHistoryService _historyService;
    private readonly IHotkeyService _hotkeyService;

    [ObservableProperty]
    private string _statusText = "Ready to capture";

    [ObservableProperty]
    private CaptureMode _currentCaptureMode = CaptureMode.SelectedArea;

    [ObservableProperty]
    private int _delaySeconds = 0;

    [ObservableProperty]
    private bool _includeCursor = false;

    [ObservableProperty]
    private bool _includeShadow = true;

    [ObservableProperty]
    private bool _isCountdownActive = false;

    [ObservableProperty]
    private int _countdownValue = 0;

    [ObservableProperty]
    private ObservableCollection<HistoryItem> _historyItems = new();

    public event Action<Bitmap>? OpenOverlayRequested;
    public event Action<Bitmap>? OpenEditorRequested;
    public event Action? OpenSettingsRequested;
    public event Action? OpenHistoryRequested;

    public MainViewModel(
        ICaptureService captureService,
        IImageProcessingService imageProcessor,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        IHistoryService historyService,
        IHotkeyService hotkeyService)
    {
        _captureService = captureService;
        _imageProcessor = imageProcessor;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _historyService = historyService;
        _hotkeyService = hotkeyService;

        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
    }

    public async Task InitializeAsync()
    {
        await _settingsService.LoadSettingsAsync();
        DelaySeconds = _settingsService.Settings.DelaySeconds;
        IncludeCursor = _settingsService.Settings.IncludeCursor;
        IncludeShadow = _settingsService.Settings.IncludeWindowShadow;
        CurrentCaptureMode = _settingsService.Settings.DefaultCaptureMode;

        await _historyService.InitializeAsync();
        RefreshHistoryList();
    }

    public void RefreshHistoryList()
    {
        HistoryItems.Clear();
        foreach (var item in _historyService.Items)
        {
            HistoryItems.Add(item);
        }
    }

    [RelayCommand]
    public async Task StartCaptureAsync(string modeName)
    {
        if (Enum.TryParse<CaptureMode>(modeName, true, out var mode))
        {
            CurrentCaptureMode = mode;
        }

        if (DelaySeconds > 0)
        {
            await RunCountdownAsync(DelaySeconds);
        }

        await ExecuteCaptureAsync(CurrentCaptureMode);
    }

    private async Task RunCountdownAsync(int seconds)
    {
        IsCountdownActive = true;
        CountdownValue = seconds;
        while (CountdownValue > 0)
        {
            StatusText = $"Capturing in {CountdownValue}...";
            await Task.Delay(1000);
            CountdownValue--;
        }
        IsCountdownActive = false;
    }

    public async Task ExecuteCaptureAsync(CaptureMode mode)
    {
        StatusText = "Capturing...";
        try
        {
            if (mode == CaptureMode.SelectedArea)
            {
                // Capture entire virtual screen as background for selection overlay
                var fullVirtual = await _captureService.CaptureFullScreenAsync(IncludeCursor);
                if (fullVirtual != null)
                {
                    OpenOverlayRequested?.Invoke(fullVirtual);
                    StatusText = "Select area on screen";
                }
            }
            else
            {
                var bmp = await _captureService.CaptureAsync(mode, _settingsService.Settings.DefaultMonitorTarget, IncludeCursor, IncludeShadow);
                if (bmp != null)
                {
                    await ProcessCapturedResultAsync(bmp, mode);
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Capture execution failed", ex);
            StatusText = "Capture failed. Please retry.";
        }
    }

    public async Task ProcessCapturedResultAsync(Bitmap bitmap, CaptureMode mode)
    {
        try
        {
            if (_settingsService.Settings.CopyToClipboardOnCapture)
            {
                await _clipboardService.CopyImageAsync(bitmap);
            }

            if (_settingsService.Settings.AutoSave && !_settingsService.Settings.PrivacyMode)
            {
                string path = _settingsService.GenerateUniqueScreenshotPath(_settingsService.Settings.DefaultFormat);
                await _imageProcessor.SaveToFileAsync(bitmap, path, _settingsService.Settings.DefaultFormat, _settingsService.Settings.JpegQuality);
            }

            if (!_settingsService.Settings.PrivacyMode)
            {
                await _historyService.AddItemAsync(bitmap, mode, "Screen");
                RefreshHistoryList();
            }

            if (_settingsService.Settings.OpenEditorAfterCapture)
            {
                OpenEditorRequested?.Invoke(bitmap);
            }
            else
            {
                StatusText = "Screenshot captured & copied to clipboard!";
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Process capture result failed", ex);
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    [RelayCommand]
    private void OpenHistory()
    {
        OpenHistoryRequested?.Invoke();
    }

    private void OnHotkeyPressed(object? sender, CaptureMode mode)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(async () =>
        {
            await ExecuteCaptureAsync(mode);
        });
    }
}

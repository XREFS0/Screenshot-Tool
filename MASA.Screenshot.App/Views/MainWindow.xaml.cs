using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using MASA.Screenshot.App.ViewModels;
using MASA.Screenshot.Core.Interfaces;

namespace MASA.Screenshot.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ICaptureService _captureService;
    private readonly IHotkeyService _hotkeyService;
    private readonly ISettingsService _settingsService;
    private readonly OverlayWindow _overlayWindow;
    private readonly EditorWindow _editorWindow;
    private readonly HistoryWindow _historyWindow;
    private readonly SettingsWindow _settingsWindow;

    public MainWindow(
        MainViewModel viewModel,
        ICaptureService captureService,
        IHotkeyService hotkeyService,
        ISettingsService settingsService,
        OverlayWindow overlayWindow,
        EditorWindow editorWindow,
        HistoryWindow historyWindow,
        SettingsWindow settingsWindow)
    {
        _viewModel = viewModel;
        _captureService = captureService;
        _hotkeyService = hotkeyService;
        _settingsService = settingsService;
        _overlayWindow = overlayWindow;
        _editorWindow = editorWindow;
        _historyWindow = historyWindow;
        _settingsWindow = settingsWindow;

        DataContext = _viewModel;
        InitializeComponent();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;

        _viewModel.OpenOverlayRequested += OnOpenOverlayRequested;
        _viewModel.OpenEditorRequested += OnOpenEditorRequested;
        _viewModel.OpenSettingsRequested += () => _settingsWindow.Show();
        _viewModel.OpenHistoryRequested += () => _historyWindow.Show();

        _overlayWindow.AreaCaptured += async (bmp) =>
        {
            await _viewModel.ProcessCapturedResultAsync(bmp, Core.Enums.CaptureMode.SelectedArea);
        };

        _overlayWindow.EditCapturedArea += (bmp) =>
        {
            OnOpenEditorRequested(bmp);
        };

        _historyWindow.OpenInEditorRequested += (bmp) =>
        {
            OnOpenEditorRequested(bmp);
        };
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        _hotkeyService.Initialize(helper.Handle);
        await _viewModel.InitializeAsync();
        _hotkeyService.RegisterHotkeys(_settingsService.Settings);
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_settingsService.Settings.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
        else
        {
            _hotkeyService.UnregisterHotkeys();
        }
    }

    private void OnOpenOverlayRequested(Bitmap virtualScreen)
    {
        var bounds = _captureService.GetVirtualScreenBounds();
        _overlayWindow.Setup(virtualScreen, bounds);
        _overlayWindow.Show();
        _overlayWindow.Activate();
    }

    private void OnOpenEditorRequested(Bitmap bitmap)
    {
        _editorWindow.LoadBitmap(bitmap);
        _editorWindow.Show();
        _editorWindow.Activate();
    }
}

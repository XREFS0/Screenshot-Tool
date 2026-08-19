using System.Drawing;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MASA.Screenshot.App.ViewModels;
using MASA.Screenshot.App.Views;
using MASA.Screenshot.Application.Services;
using MASA.Screenshot.Core.Constants;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Infrastructure.Clipboard;
using MASA.Screenshot.Infrastructure.Imaging;
using MASA.Screenshot.Infrastructure.Logging;
using MASA.Screenshot.Infrastructure.Storage;
using MASA.Screenshot.Infrastructure.Windows;

namespace MASA.Screenshot.App;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    public IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Single Instance Check
        _mutex = new Mutex(true, AppConstants.MutexName, out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("MASA Screenshot is already running in the background.", AppConstants.AppName, MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        SetupTrayIcon();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        var settingsService = ServiceProvider.GetRequiredService<ISettingsService>();

        if (!settingsService.Settings.StartMinimized)
        {
            mainWindow.Show();
        }

        AppLogger.LogInfo("MASA Screenshot application started successfully.");
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core & Infrastructure Singletons
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        services.AddSingleton<ICaptureService, CaptureService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IHistoryService, HistoryService>();
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<ISmartRedactionService, SmartRedactionService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<EditorViewModel>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Views / Windows
        services.AddSingleton<MainWindow>();
        services.AddSingleton<OverlayWindow>();
        services.AddSingleton<EditorWindow>();
        services.AddSingleton<HistoryWindow>();
        services.AddSingleton<SettingsWindow>();
    }

    private void SetupTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = AppConstants.AppName,
            Visible = true
        };

        var contextMenu = new System.Windows.Forms.ContextMenuStrip();
        contextMenu.Items.Add("Take Screenshot (Area)", null, async (s, e) =>
        {
            var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
            await mainVm.StartCaptureAsync("SelectedArea");
        });
        contextMenu.Items.Add("Capture Full Screen", null, async (s, e) =>
        {
            var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
            await mainVm.StartCaptureAsync("FullScreen");
        });
        contextMenu.Items.Add("Capture Active Window", null, async (s, e) =>
        {
            var mainVm = ServiceProvider.GetRequiredService<MainViewModel>();
            await mainVm.StartCaptureAsync("ActiveWindow");
        });
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Open History", null, (s, e) =>
        {
            var historyWin = ServiceProvider.GetRequiredService<HistoryWindow>();
            historyWin.Show();
            historyWin.Activate();
        });
        contextMenu.Items.Add("Settings", null, (s, e) =>
        {
            var settingsWin = ServiceProvider.GetRequiredService<SettingsWindow>();
            settingsWin.Show();
            settingsWin.Activate();
        });
        contextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, (s, e) =>
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            Shutdown();
        });

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) =>
        {
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _notifyIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

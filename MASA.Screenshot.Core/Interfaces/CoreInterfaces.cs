using System.Drawing;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.Core.Interfaces;

public interface ICaptureService
{
    Task<Bitmap?> CaptureAsync(CaptureMode mode, MonitorTarget monitorTarget = MonitorTarget.CurrentMonitor, bool includeCursor = false, bool includeShadow = true);
    Task<Bitmap?> CaptureRegionAsync(Rectangle virtualScreenRegion, bool includeCursor = false);
    Task<Bitmap?> CaptureWindowAsync(IntPtr hWnd, bool includeShadow = true);
    Task<Bitmap?> CaptureFullScreenAsync(bool includeCursor = false);
    Task<Bitmap?> CaptureMonitorAsync(int monitorIndex, bool includeCursor = false);
    IReadOnlyList<MonitorInfo> GetAllMonitors();
    MonitorInfo GetCurrentMonitor(Point screenPoint);
    Rectangle GetVirtualScreenBounds();
}

public interface IImageProcessingService
{
    Bitmap Crop(Bitmap source, Rectangle cropRect);
    Bitmap Resize(Bitmap source, int newWidth, int newHeight, bool preserveAspect = true);
    Bitmap Rotate(Bitmap source, RotateFlipType rotateFlipType);
    Bitmap ApplyBlur(Bitmap source, Rectangle region, int radius = 15);
    Bitmap ApplyPixelate(Bitmap source, Rectangle region, int pixelSize = 16);
    ColorInfo GetPixelColor(Bitmap source, int x, int y);
    byte[] EncodeToBytes(Bitmap source, ImageFormatType format, int quality = 90);
    Task SaveToFileAsync(Bitmap source, string filePath, ImageFormatType format, int quality = 90);
    Bitmap CreateThumbnail(Bitmap source, int maxWidth = 240, int maxHeight = 160);
}

public interface IAnnotationItem
{
    string Id { get; }
    AnnotationToolType ToolType { get; }
    void Render(Graphics g, float scale = 1.0f);
}

public interface IClipboardService
{
    Task CopyImageAsync(Bitmap image);
    Task CopyTextAsync(string text);
    Bitmap? GetImageFromClipboard();
}

public interface ISettingsService
{
    AppSettings Settings { get; }
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    void ResetToDefaults();
    string GetEffectiveSaveDirectory();
    string GenerateUniqueScreenshotPath(ImageFormatType format);
}

public interface IHistoryService
{
    IReadOnlyList<HistoryItem> Items { get; }
    Task InitializeAsync();
    Task<HistoryItem?> AddItemAsync(Bitmap image, CaptureMode mode, string monitorName);
    Task DeleteItemAsync(string id);
    Task ClearAllAsync();
    Task<Bitmap?> LoadImageAsync(string filePath);
}

public interface IHotkeyService
{
    event EventHandler<CaptureMode>? HotkeyPressed;
    bool IsPaused { get; set; }
    void Initialize(IntPtr windowHandle);
    void RegisterHotkeys(AppSettings settings);
    void UnregisterHotkeys();
}

public interface ISmartRedactionService
{
    Task<IReadOnlyList<RedactionZone>> DetectSensitiveZonesAsync(Bitmap bitmap);
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void ApplyTheme(ThemeMode theme);
}

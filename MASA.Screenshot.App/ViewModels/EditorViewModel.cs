using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MASA.Screenshot.App.Converters;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;
using MASA.Screenshot.Infrastructure.Logging;

namespace MASA.Screenshot.App.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    private readonly IImageProcessingService _imageProcessor;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;
    private readonly IHistoryService _historyService;
    private readonly ISmartRedactionService _smartRedactionService;

    [ObservableProperty]
    private BitmapSource? _currentImageSource;

    [ObservableProperty]
    private Bitmap? _currentBitmap;

    [ObservableProperty]
    private int _imageWidth;

    [ObservableProperty]
    private int _imageHeight;

    [ObservableProperty]
    private string _imageDetails = string.Empty;

    [ObservableProperty]
    private AnnotationToolType _selectedTool = AnnotationToolType.Pen;

    [ObservableProperty]
    private string _selectedColorHex = "#FF007ACC";

    [ObservableProperty]
    private double _penThickness = 3.0;

    [ObservableProperty]
    private string _colorInfoHex = "#000000";

    [ObservableProperty]
    private string _colorInfoRgb = "RGB(0, 0, 0)";

    [ObservableProperty]
    private string _colorInfoHsl = "HSL(0°, 0%, 0%)";

    [ObservableProperty]
    private bool _isColorPickerVisible = false;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public event Action? RequestUndo;
    public event Action? RequestRedo;
    public event Action? RequestClear;
    public event Func<Bitmap>? RequestFlatten;

    public EditorViewModel(
        IImageProcessingService imageProcessor,
        IClipboardService clipboardService,
        ISettingsService settingsService,
        IHistoryService historyService,
        ISmartRedactionService smartRedactionService)
    {
        _imageProcessor = imageProcessor;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
        _historyService = historyService;
        _smartRedactionService = smartRedactionService;
    }

    public void LoadBitmap(Bitmap bitmap)
    {
        CurrentBitmap?.Dispose();
        CurrentBitmap = (Bitmap)bitmap.Clone();
        ImageWidth = bitmap.Width;
        ImageHeight = bitmap.Height;
        ImageDetails = $"{ImageWidth} × {ImageHeight} px";
        CurrentImageSource = CurrentBitmap.ToBitmapSource();
        StatusMessage = "Image loaded";
    }

    [RelayCommand]
    private void SelectTool(string toolName)
    {
        if (Enum.TryParse<AnnotationToolType>(toolName, true, out var tool))
        {
            SelectedTool = tool;
            StatusMessage = $"Tool: {tool}";
        }
    }

    [RelayCommand]
    private async Task CopyToClipboardAsync()
    {
        var bmp = RequestFlatten?.Invoke() ?? CurrentBitmap;
        if (bmp != null)
        {
            await _clipboardService.CopyImageAsync(bmp);
            StatusMessage = "Image copied to clipboard!";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var bmp = RequestFlatten?.Invoke() ?? CurrentBitmap;
        if (bmp != null)
        {
            string savePath = _settingsService.GenerateUniqueScreenshotPath(_settingsService.Settings.DefaultFormat);
            await _imageProcessor.SaveToFileAsync(bmp, savePath, _settingsService.Settings.DefaultFormat, _settingsService.Settings.JpegQuality);
            await _historyService.AddItemAsync(bmp, CaptureMode.SelectedArea, "Editor");
            StatusMessage = $"Saved to {Path.GetFileName(savePath)}";
        }
    }

    [RelayCommand]
    private void RotateRight()
    {
        if (CurrentBitmap == null) return;
        var rotated = _imageProcessor.Rotate(CurrentBitmap, RotateFlipType.Rotate90FlipNone);
        LoadBitmap(rotated);
        rotated.Dispose();
        StatusMessage = "Rotated 90° clockwise";
    }

    [RelayCommand]
    private void RotateLeft()
    {
        if (CurrentBitmap == null) return;
        var rotated = _imageProcessor.Rotate(CurrentBitmap, RotateFlipType.Rotate270FlipNone);
        LoadBitmap(rotated);
        rotated.Dispose();
        StatusMessage = "Rotated 90° counter-clockwise";
    }

    [RelayCommand]
    private void FlipHorizontal()
    {
        if (CurrentBitmap == null) return;
        var flipped = _imageProcessor.Rotate(CurrentBitmap, RotateFlipType.RotateNoneFlipX);
        LoadBitmap(flipped);
        flipped.Dispose();
        StatusMessage = "Flipped horizontally";
    }

    [RelayCommand]
    private void FlipVertical()
    {
        if (CurrentBitmap == null) return;
        var flipped = _imageProcessor.Rotate(CurrentBitmap, RotateFlipType.RotateNoneFlipY);
        LoadBitmap(flipped);
        flipped.Dispose();
        StatusMessage = "Flipped vertically";
    }

    [RelayCommand]
    private void Undo()
    {
        RequestUndo?.Invoke();
        StatusMessage = "Undo performed";
    }

    [RelayCommand]
    private void Redo()
    {
        RequestRedo?.Invoke();
        StatusMessage = "Redo performed";
    }

    [RelayCommand]
    private void ClearAll()
    {
        RequestClear?.Invoke();
        StatusMessage = "Annotations cleared";
    }

    [RelayCommand]
    private async Task SmartRedactAsync()
    {
        if (CurrentBitmap == null) return;
        IsBusy = true;
        StatusMessage = "Scanning for sensitive zones...";
        try
        {
            var zones = await _smartRedactionService.DetectSensitiveZonesAsync(CurrentBitmap);
            if (zones.Count > 0)
            {
                var redacted = (Bitmap)CurrentBitmap.Clone();
                foreach (var z in zones)
                {
                    var rect = new Rectangle(z.X, z.Y, z.Width, z.Height);
                    var blurred = _imageProcessor.ApplyBlur(redacted, rect, 18);
                    redacted.Dispose();
                    redacted = blurred;
                }
                LoadBitmap(redacted);
                redacted.Dispose();
                StatusMessage = $"Smart Redact applied to {zones.Count} regions";
            }
            else
            {
                StatusMessage = "No sensitive patterns identified";
            }
        }
        catch (Exception ex)
        {
            AppLogger.LogError("SmartRedact failed", ex);
            StatusMessage = "Smart Redact failed";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CopyHexAsync()
    {
        await _clipboardService.CopyTextAsync(ColorInfoHex);
        StatusMessage = $"Copied {ColorInfoHex} to clipboard";
    }

    public void UpdateHoverColor(int x, int y)
    {
        if (CurrentBitmap == null) return;
        var color = _imageProcessor.GetPixelColor(CurrentBitmap, x, y);
        ColorInfoHex = color.Hex;
        ColorInfoRgb = color.RgbString;
        ColorInfoHsl = color.HslString;
    }
}

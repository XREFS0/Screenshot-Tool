using System.Drawing;
using System.IO;
using System.Text.Json;
using MASA.Screenshot.Core.Constants;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.Infrastructure.Storage;

public sealed class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppConstants.CompanyName, AppConstants.AppName);
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }
        _settingsPath = Path.Combine(appData, AppConstants.SettingsFileName);
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                string json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                }
            }
        }
        catch
        {
            Settings = new AppSettings();
        }

        if (string.IsNullOrWhiteSpace(Settings.SaveLocation))
        {
            Settings.SaveLocation = GetDefaultScreenshotsDirectory();
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(Settings, options);
            await File.WriteAllTextAsync(_settingsPath, json);
        }
        catch
        {
            // Suppress error
        }
    }

    public void ResetToDefaults()
    {
        Settings = new AppSettings
        {
            SaveLocation = GetDefaultScreenshotsDirectory()
        };
    }

    public string GetEffectiveSaveDirectory()
    {
        if (!string.IsNullOrWhiteSpace(Settings.SaveLocation) && Directory.Exists(Settings.SaveLocation))
        {
            return Settings.SaveLocation;
        }

        string defaultDir = GetDefaultScreenshotsDirectory();
        if (!Directory.Exists(defaultDir))
        {
            Directory.CreateDirectory(defaultDir);
        }
        return defaultDir;
    }

    public string GenerateUniqueScreenshotPath(ImageFormatType format)
    {
        string dir = GetEffectiveSaveDirectory();
        string ext = format switch
        {
            ImageFormatType.Png => "png",
            ImageFormatType.Jpeg => "jpg",
            ImageFormatType.Webp => "webp",
            ImageFormatType.Bmp => "bmp",
            _ => "png"
        };

        string baseName = $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{ext}";
        string fullPath = Path.Combine(dir, baseName);
        int counter = 1;
        while (File.Exists(fullPath))
        {
            fullPath = Path.Combine(dir, $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_({counter}).{ext}");
            counter++;
        }
        return fullPath;
    }

    private static string GetDefaultScreenshotsDirectory()
    {
        string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return Path.Combine(pictures, AppConstants.DefaultScreenshotsFolder);
    }
}

public sealed class HistoryService : IHistoryService
{
    private readonly ISettingsService _settingsService;
    private readonly IImageProcessingService _imageProcessor;
    private readonly string _historyJsonPath;
    private readonly string _thumbnailsFolder;
    private readonly List<HistoryItem> _items = new();

    public IReadOnlyList<HistoryItem> Items => _items.AsReadOnly();

    public HistoryService(ISettingsService settingsService, IImageProcessingService imageProcessor)
    {
        _settingsService = settingsService;
        _imageProcessor = imageProcessor;

        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppConstants.CompanyName, AppConstants.AppName);
        _thumbnailsFolder = Path.Combine(appData, "Thumbnails");
        _historyJsonPath = Path.Combine(appData, AppConstants.HistoryFileName);

        if (!Directory.Exists(_thumbnailsFolder))
        {
            Directory.CreateDirectory(_thumbnailsFolder);
        }
    }

    public async Task InitializeAsync()
    {
        if (_settingsService.Settings.PrivacyMode)
        {
            _items.Clear();
            return;
        }

        try
        {
            if (File.Exists(_historyJsonPath))
            {
                string json = await File.ReadAllTextAsync(_historyJsonPath);
                var items = JsonSerializer.Deserialize<List<HistoryItem>>(json);
                if (items != null)
                {
                    _items.Clear();
                    _items.AddRange(items.Where(i => File.Exists(i.FilePath)));
                }
            }
        }
        catch
        {
            _items.Clear();
        }
    }

    public async Task<HistoryItem?> AddItemAsync(Bitmap image, CaptureMode mode, string monitorName)
    {
        if (_settingsService.Settings.PrivacyMode)
        {
            return null;
        }

        try
        {
            string savePath = _settingsService.GenerateUniqueScreenshotPath(_settingsService.Settings.DefaultFormat);
            await _imageProcessor.SaveToFileAsync(image, savePath, _settingsService.Settings.DefaultFormat, _settingsService.Settings.JpegQuality);

            string thumbName = $"{Guid.NewGuid():N}.png";
            string thumbPath = Path.Combine(_thumbnailsFolder, thumbName);
            using (var thumb = _imageProcessor.CreateThumbnail(image, 240, 160))
            {
                await _imageProcessor.SaveToFileAsync(thumb, thumbPath, ImageFormatType.Png);
            }

            var fi = new FileInfo(savePath);
            var item = new HistoryItem
            {
                FilePath = savePath,
                ThumbnailPath = thumbPath,
                CapturedAt = DateTime.Now,
                Width = image.Width,
                Height = image.Height,
                FileSizeBytes = fi.Exists ? fi.Length : 0,
                Format = _settingsService.Settings.DefaultFormat,
                CaptureMode = mode,
                MonitorName = monitorName
            };

            _items.Insert(0, item);
            await PersistHistoryAsync();
            return item;
        }
        catch
        {
            return null;
        }
    }

    public async Task DeleteItemAsync(string id)
    {
        var item = _items.FirstOrDefault(i => i.Id == id);
        if (item != null)
        {
            _items.Remove(item);
            try
            {
                if (File.Exists(item.ThumbnailPath)) File.Delete(item.ThumbnailPath);
            }
            catch { }

            await PersistHistoryAsync();
        }
    }

    public async Task ClearAllAsync()
    {
        _items.Clear();
        try
        {
            if (Directory.Exists(_thumbnailsFolder))
            {
                foreach (var f in Directory.GetFiles(_thumbnailsFolder))
                {
                    try { File.Delete(f); } catch { }
                }
            }
            if (File.Exists(_historyJsonPath))
            {
                File.Delete(_historyJsonPath);
            }
        }
        catch { }
        await Task.CompletedTask;
    }

    public async Task<Bitmap?> LoadImageAsync(string filePath)
    {
        return await Task.Run<Bitmap?>(() =>
        {
            if (!File.Exists(filePath)) return null;
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return new Bitmap(fs);
            }
            catch
            {
                return null;
            }
        });
    }

    private async Task PersistHistoryAsync()
    {
        if (_settingsService.Settings.PrivacyMode) return;
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_items, options);
            await File.WriteAllTextAsync(_historyJsonPath, json);
        }
        catch { }
    }
}

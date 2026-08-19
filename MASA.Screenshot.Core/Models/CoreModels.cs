using System.Text.Json.Serialization;
using MASA.Screenshot.Core.Enums;

namespace MASA.Screenshot.Core.Models;

public sealed class AppSettings
{
    // General
    public bool StartWithWindows { get; set; } = false;
    public bool StartMinimized { get; set; } = false;
    public bool MinimizeToTray { get; set; } = true;
    public bool PrivacyMode { get; set; } = false;

    // Capture Defaults
    public CaptureMode DefaultCaptureMode { get; set; } = CaptureMode.SelectedArea;
    public int DelaySeconds { get; set; } = 0;
    public bool IncludeCursor { get; set; } = false;
    public bool IncludeWindowShadow { get; set; } = true;
    public MonitorTarget DefaultMonitorTarget { get; set; } = MonitorTarget.CurrentMonitor;
    public int FixedRegionWidth { get; set; } = 800;
    public int FixedRegionHeight { get; set; } = 600;

    // Output & Auto Save
    public ImageFormatType DefaultFormat { get; set; } = ImageFormatType.Png;
    public string SaveLocation { get; set; } = string.Empty;
    public int JpegQuality { get; set; } = 92;
    public int WebpQuality { get; set; } = 90;
    public bool AutoSave { get; set; } = false;
    public bool CopyToClipboardOnCapture { get; set; } = true;
    public bool OpenEditorAfterCapture { get; set; } = false;
    public bool ShowQuickToolbar { get; set; } = true;

    // Editor & Annotation Defaults
    public double DefaultPenThickness { get; set; } = 3.0;
    public double DefaultHighlighterThickness { get; set; } = 18.0;
    public string DefaultPrimaryColor { get; set; } = "#FF007ACC";
    public string DefaultFontFamily { get; set; } = "Segoe UI";
    public double DefaultFontSize { get; set; } = 18.0;
    public int DefaultBlurStrength { get; set; } = 15;
    public int DefaultPixelateBlockSize { get; set; } = 16;

    // Hotkeys
    public string HotkeyAreaCapture { get; set; } = "PrintScreen";
    public string HotkeyFullScreen { get; set; } = "Ctrl+PrintScreen";
    public string HotkeyActiveWindow { get; set; } = "Alt+PrintScreen";
    public string HotkeyQuickCapture { get; set; } = "Win+Shift+S";
    public bool HotkeysEnabled { get; set; } = true;

    // Appearance
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public bool CompactMode { get; set; } = false;
}

public sealed class MonitorInfo
{
    public int Index { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public double ScaleFactor { get; set; } = 1.0;
    public int DpiX { get; set; } = 96;
    public int DpiY { get; set; } = 96;
    public bool IsPrimary { get; set; }

    public int Right => X + Width;
    public int Bottom => Y + Height;
}

public sealed class HistoryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string FilePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; } = DateTime.Now;
    public int Width { get; set; }
    public int Height { get; set; }
    public long FileSizeBytes { get; set; }
    public ImageFormatType Format { get; set; }
    public CaptureMode CaptureMode { get; set; }
    public string MonitorName { get; set; } = string.Empty;

    [JsonIgnore]
    public string FormattedTime => CapturedAt.ToString("yyyy-MM-dd HH:mm:ss");
    [JsonIgnore]
    public string ResolutionText => $"{Width} × {Height} px";
    [JsonIgnore]
    public string FileSizeFormatted
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F1} MB";
        }
    }
}

public sealed class ColorInfo
{
    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; } = 255;

    public string Hex => $"#{A:X2}{R:X2}{G:X2}{B:X2}";
    public string HexRgb => $"#{R:X2}{G:X2}{B:X2}";
    public string RgbString => $"RGB({R}, {G}, {B})";
    public string HslString
    {
        get
        {
            ColorHelper.RgbToHsl(R, G, B, out double h, out double s, out double l);
            return $"HSL({Math.Round(h)}°, {Math.Round(s * 100)}%, {Math.Round(l * 100)}%)";
        }
    }
}

public static class ColorHelper
{
    public static void RgbToHsl(byte r, byte g, byte b, out double h, out double s, out double l)
    {
        double rd = r / 255.0;
        double gd = g / 255.0;
        double bd = b / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));

        l = (max + min) / 2.0;

        if (Math.Abs(max - min) < 0.00001)
        {
            h = 0;
            s = 0;
        }
        else
        {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);

            if (Math.Abs(max - rd) < 0.00001)
                h = (gd - bd) / d + (gd < bd ? 6 : 0);
            else if (Math.Abs(max - gd) < 0.00001)
                h = (bd - rd) / d + 2;
            else
                h = (rd - gd) / d + 4;

            h *= 60.0;
        }
    }
}

public sealed class RedactionZone
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string MatchReason { get; set; } = string.Empty;
}

namespace MASA.Screenshot.Core.Enums;

public enum CaptureMode
{
    SelectedArea,
    FullScreen,
    ActiveWindow,
    MultiMonitor,
    FixedRegion
}

public enum MonitorTarget
{
    CurrentMonitor,
    AllMonitors,
    PrimaryMonitor,
    Monitor1,
    Monitor2,
    Monitor3
}

public enum ImageFormatType
{
    Png,
    Jpeg,
    Webp,
    Bmp
}

public enum AnnotationToolType
{
    Select,
    Pen,
    Highlighter,
    Arrow,
    Rectangle,
    Ellipse,
    Line,
    Text,
    Blur,
    Pixelate,
    SmartRedact,
    StepNumber,
    Eraser
}

public enum AspectRatioPreset
{
    Free,
    Ratio1x1,
    Ratio4x3,
    Ratio16x9,
    Ratio16x10,
    Fixed800x600,
    Fixed1920x1080,
    Custom
}

public enum ThemeMode
{
    System,
    Dark,
    Light
}

public enum DelayTimerOption
{
    None = 0,
    ThreeSeconds = 3,
    FiveSeconds = 5,
    TenSeconds = 10,
    Custom = 99
}

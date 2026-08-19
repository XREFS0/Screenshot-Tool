namespace MASA.Screenshot.Core.Constants;

public static class AppConstants
{
    public const string AppName = "MASA Screenshot";
    public const string AppVersion = "1.0.0";
    public const string CompanyName = "XREFS0";
    public const string SettingsFileName = "settings.json";
    public const string HistoryFileName = "history.json";
    public const string DefaultScreenshotsFolder = "MASA Screenshots";
    public const string MutexName = "XREFS0_Screenshot_SingleInstance_Mutex";
}

public static class HotkeyConstants
{
    public const int MOD_NONE = 0x0000;
    public const int MOD_ALT = 0x0001;
    public const int MOD_CONTROL = 0x0002;
    public const int MOD_SHIFT = 0x0004;
    public const int MOD_WIN = 0x0008;
    public const int MOD_NOREPEAT = 0x4000;

    public const int WM_HOTKEY = 0x0312;

    public const int HOTKEY_ID_AREA = 9001;
    public const int HOTKEY_ID_FULLSCREEN = 9002;
    public const int HOTKEY_ID_ACTIVE_WINDOW = 9003;
    public const int HOTKEY_ID_QUICK_CAPTURE = 9004;
}

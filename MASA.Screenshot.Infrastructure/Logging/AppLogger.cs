using System.IO;

namespace MASA.Screenshot.Infrastructure.Logging;

public static class AppLogger
{
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static AppLogger()
    {
        string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MASA", "MASA Screenshot");
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }
        LogFilePath = Path.Combine(appData, "app.log");
    }

    public static void LogInfo(string message)
    {
        Write("INFO", message);
    }

    public static void LogWarning(string message)
    {
        Write("WARN", message);
    }

    public static void LogError(string message, Exception? ex = null)
    {
        string text = ex != null ? $"{message} | Ex: {ex.Message}" : message;
        Write("ERROR", text);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (LockObj)
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(LogFilePath, line);
            }
        }
        catch { }
    }
}

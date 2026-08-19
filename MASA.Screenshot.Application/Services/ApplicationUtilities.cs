using System.Drawing;
using System.Text.RegularExpressions;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.Application.Services;

public static class CropCalculator
{
    public static Rectangle CalculateCropBounds(int originalWidth, int originalHeight, Rectangle requestedCrop)
    {
        int x = Math.Max(0, requestedCrop.X);
        int y = Math.Max(0, requestedCrop.Y);
        int right = Math.Min(originalWidth, requestedCrop.Right);
        int bottom = Math.Min(originalHeight, requestedCrop.Bottom);

        int width = Math.Max(1, right - x);
        int height = Math.Max(1, bottom - y);

        return new Rectangle(x, y, width, height);
    }

    public static Rectangle CalculateAspectRatioCrop(int currentWidth, int currentHeight, double targetRatio)
    {
        if (targetRatio <= 0) return new Rectangle(0, 0, currentWidth, currentHeight);

        double currentRatio = (double)currentWidth / currentHeight;
        int newWidth = currentWidth;
        int newHeight = currentHeight;

        if (currentRatio > targetRatio)
        {
            // Current is wider, crop width
            newWidth = (int)Math.Round(currentHeight * targetRatio);
        }
        else
        {
            // Current is taller, crop height
            newHeight = (int)Math.Round(currentWidth / targetRatio);
        }

        int x = (currentWidth - newWidth) / 2;
        int y = (currentHeight - newHeight) / 2;

        return new Rectangle(x, y, newWidth, newHeight);
    }
}

public static class FilenameGenerator
{
    public static string GenerateFilename(DateTime timestamp, string extension, string prefix = "Screenshot_")
    {
        string ext = extension.TrimStart('.');
        return $"{prefix}{timestamp:yyyy-MM-dd_HH-mm-ss}.{ext}";
    }

    public static string EnsureUniqueFilePath(string directory, string baseFilename)
    {
        string fullPath = Path.Combine(directory, baseFilename);
        if (!File.Exists(fullPath)) return fullPath;

        string nameWithoutExt = Path.GetFileNameWithoutExtension(baseFilename);
        string ext = Path.GetExtension(baseFilename);
        int counter = 1;

        while (File.Exists(fullPath))
        {
            string newName = $"{nameWithoutExt} ({counter}){ext}";
            fullPath = Path.Combine(directory, newName);
            counter++;
        }

        return fullPath;
    }
}

public static class DpiCoordinateTransformer
{
    public static Point VirtualToDpiScaled(Point virtualPoint, double scaleFactor)
    {
        return new Point((int)Math.Round(virtualPoint.X * scaleFactor), (int)Math.Round(virtualPoint.Y * scaleFactor));
    }

    public static Rectangle VirtualToDpiScaled(Rectangle virtualRect, double scaleFactor)
    {
        return new Rectangle(
            (int)Math.Round(virtualRect.X * scaleFactor),
            (int)Math.Round(virtualRect.Y * scaleFactor),
            (int)Math.Round(virtualRect.Width * scaleFactor),
            (int)Math.Round(virtualRect.Height * scaleFactor)
        );
    }

    public static Rectangle NormalizeSelection(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int width = Math.Abs(p1.X - p2.X);
        int height = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, width, height);
    }
}

public static class SensitiveDataDetector
{
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(?:\+?\d{1,3}[-.\s]?)?(?:\(?\d{2,4}\)?[-.\s]?)?\d{3,4}[-.\s]?\d{3,4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"\b(?:\d{4}[ -]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex IpRegex = new(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);

    public static bool ContainsSensitivePattern(string text, out string matchReason)
    {
        if (EmailRegex.IsMatch(text))
        {
            matchReason = "Email Address";
            return true;
        }
        if (CreditCardRegex.IsMatch(text))
        {
            matchReason = "Credit Card / Financial Number";
            return true;
        }
        if (PhoneRegex.IsMatch(text))
        {
            matchReason = "Phone Number";
            return true;
        }
        if (IpRegex.IsMatch(text))
        {
            matchReason = "IP Address";
            return true;
        }

        matchReason = string.Empty;
        return false;
    }
}

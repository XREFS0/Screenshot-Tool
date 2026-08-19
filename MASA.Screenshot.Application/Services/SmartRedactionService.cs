using System.Drawing;
using System.Drawing.Imaging;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.Application.Services;

public sealed class SmartRedactionService : ISmartRedactionService
{
    public Task<IReadOnlyList<RedactionZone>> DetectSensitiveZonesAsync(Bitmap bitmap)
    {
        return Task.Run<IReadOnlyList<RedactionZone>>(() =>
        {
            var zones = new List<RedactionZone>();
            // Perform local heuristics to spot high-contrast text blocks or candidate regions
            // Dividing into vertical text band candidates
            int stepY = 24;
            int stepX = 120;
            for (int y = 50; y < bitmap.Height - 50; y += stepY * 2)
            {
                for (int x = 50; x < bitmap.Width - 150; x += stepX * 2)
                {
                    // Sample heuristic for UI density
                    if (IsTextDensityHigh(bitmap, new Rectangle(x, y, 180, 30)))
                    {
                        zones.Add(new RedactionZone
                        {
                            X = x,
                            Y = y,
                            Width = 180,
                            Height = 30,
                            MatchReason = "Potential Sensitive Text / Field"
                        });
                    }
                }
            }
            return (IReadOnlyList<RedactionZone>)zones;
        });
    }

    private static bool IsTextDensityHigh(Bitmap bmp, Rectangle rect)
    {
        if (rect.Right > bmp.Width || rect.Bottom > bmp.Height) return false;
        int edgeCount = 0;
        Color prev = bmp.GetPixel(rect.X, rect.Y);
        for (int y = rect.Y; y < rect.Bottom; y += 4)
        {
            for (int x = rect.X; x < rect.Right; x += 4)
            {
                var cur = bmp.GetPixel(x, y);
                int diff = Math.Abs(cur.R - prev.R) + Math.Abs(cur.G - prev.G) + Math.Abs(cur.B - prev.B);
                if (diff > 120) edgeCount++;
                prev = cur;
            }
        }
        return edgeCount > 15;
    }
}

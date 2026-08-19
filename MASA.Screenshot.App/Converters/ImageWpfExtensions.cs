using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace MASA.Screenshot.App.Converters;

public static class ImageWpfExtensions
{
    public static BitmapSource ToBitmapSource(this Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static Bitmap ToBitmap(this BitmapSource bitmapSource)
    {
        using var outStream = new MemoryStream();
        BitmapEncoder enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bitmapSource));
        enc.Save(outStream);
        return new Bitmap(outStream);
    }
}

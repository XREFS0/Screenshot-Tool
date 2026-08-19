using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using MASA.Screenshot.Core.Interfaces;

namespace MASA.Screenshot.Infrastructure.Clipboard;

public sealed class ClipboardService : IClipboardService
{
    public async Task CopyImageAsync(Bitmap image)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                using var ms = new MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                System.Windows.Clipboard.SetImage(bitmapImage);
            }
            catch
            {
                // Fallback using GDI bitmap handle
                try
                {
                    var hBitmap = image.GetHbitmap();
                    var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        hBitmap,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                    source.Freeze();
                    System.Windows.Clipboard.SetImage(source);
                    MASA.Screenshot.Infrastructure.Windows.NativeMethods.DeleteObject(hBitmap);
                }
                catch { }
            }
        });
    }

    public async Task CopyTextAsync(string text)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch { }
        });
    }

    public Bitmap? GetImageFromClipboard()
    {
        Bitmap? result = null;
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            try
            {
                if (System.Windows.Clipboard.ContainsImage())
                {
                    var source = System.Windows.Clipboard.GetImage();
                    if (source != null)
                    {
                        using var ms = new MemoryStream();
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(source));
                        encoder.Save(ms);
                        result = new Bitmap(ms);
                    }
                }
            }
            catch { }
        });
        return result;
    }
}

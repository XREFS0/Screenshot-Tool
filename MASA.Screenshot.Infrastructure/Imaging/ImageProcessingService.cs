using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;

namespace MASA.Screenshot.Infrastructure.Imaging;

public sealed class ImageProcessingService : IImageProcessingService
{
    public Bitmap Crop(Bitmap source, Rectangle cropRect)
    {
        var validRect = new Rectangle(
            Math.Max(0, cropRect.X),
            Math.Max(0, cropRect.Y),
            Math.Min(source.Width - Math.Max(0, cropRect.X), cropRect.Width),
            Math.Min(source.Height - Math.Max(0, cropRect.Y), cropRect.Height)
        );

        if (validRect.Width <= 0 || validRect.Height <= 0)
        {
            return new Bitmap(source);
        }

        var cropped = new Bitmap(validRect.Width, validRect.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(cropped))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.DrawImage(source, new Rectangle(0, 0, cropped.Width, cropped.Height), validRect, GraphicsUnit.Pixel);
        }
        return cropped;
    }

    public Bitmap Resize(Bitmap source, int newWidth, int newHeight, bool preserveAspect = true)
    {
        if (newWidth <= 0) newWidth = 1;
        if (newHeight <= 0) newHeight = 1;

        int finalWidth = newWidth;
        int finalHeight = newHeight;

        if (preserveAspect)
        {
            double ratioX = (double)newWidth / source.Width;
            double ratioY = (double)newHeight / source.Height;
            double ratio = Math.Min(ratioX, ratioY);
            finalWidth = Math.Max(1, (int)(source.Width * ratio));
            finalHeight = Math.Max(1, (int)(source.Height * ratio));
        }

        var resized = new Bitmap(finalWidth, finalHeight, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(resized))
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(source, 0, 0, finalWidth, finalHeight);
        }
        return resized;
    }

    public Bitmap Rotate(Bitmap source, RotateFlipType rotateFlipType)
    {
        var clone = (Bitmap)source.Clone();
        clone.RotateFlip(rotateFlipType);
        return clone;
    }

    public unsafe Bitmap ApplyBlur(Bitmap source, Rectangle region, int radius = 15)
    {
        var result = (Bitmap)source.Clone();
        var validRect = Rectangle.Intersect(new Rectangle(0, 0, source.Width, source.Height), region);
        if (validRect.Width <= 1 || validRect.Height <= 1 || radius <= 0) return result;

        // Perform fast box blur on validRect
        var crop = Crop(source, validRect);
        int w = crop.Width;
        int h = crop.Height;

        BitmapData data = crop.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        byte* ptr = (byte*)data.Scan0.ToPointer();
        int stride = data.Stride;

        int passes = 3;
        for (int pass = 0; pass < passes; pass++)
        {
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int rSum = 0, gSum = 0, bSum = 0, count = 0;
                    for (int k = -radius; k <= radius; k += Math.Max(1, radius / 3))
                    {
                        int nx = Math.Clamp(x + k, 0, w - 1);
                        int offset = y * stride + nx * 4;
                        bSum += ptr[offset];
                        gSum += ptr[offset + 1];
                        rSum += ptr[offset + 2];
                        count++;
                    }
                    int currentOffset = y * stride + x * 4;
                    ptr[currentOffset] = (byte)(bSum / count);
                    ptr[currentOffset + 1] = (byte)(gSum / count);
                    ptr[currentOffset + 2] = (byte)(rSum / count);
                }
            }
        }

        crop.UnlockBits(data);

        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(crop, validRect.X, validRect.Y);
        }
        crop.Dispose();
        return result;
    }

    public unsafe Bitmap ApplyPixelate(Bitmap source, Rectangle region, int pixelSize = 16)
    {
        var result = (Bitmap)source.Clone();
        var validRect = Rectangle.Intersect(new Rectangle(0, 0, source.Width, source.Height), region);
        if (validRect.Width <= 1 || validRect.Height <= 1) return result;

        pixelSize = Math.Max(2, pixelSize);
        var crop = Crop(source, validRect);
        int w = crop.Width;
        int h = crop.Height;

        BitmapData data = crop.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
        byte* ptr = (byte*)data.Scan0.ToPointer();
        int stride = data.Stride;

        for (int y = 0; y < h; y += pixelSize)
        {
            for (int x = 0; x < w; x += pixelSize)
            {
                int rSum = 0, gSum = 0, bSum = 0, aSum = 0, count = 0;
                int blockW = Math.Min(pixelSize, w - x);
                int blockH = Math.Min(pixelSize, h - y);

                for (int by = 0; by < blockH; by++)
                {
                    for (int bx = 0; bx < blockW; bx++)
                    {
                        int offset = (y + by) * stride + (x + bx) * 4;
                        bSum += ptr[offset];
                        gSum += ptr[offset + 1];
                        rSum += ptr[offset + 2];
                        aSum += ptr[offset + 3];
                        count++;
                    }
                }

                byte avgB = (byte)(bSum / count);
                byte avgG = (byte)(gSum / count);
                byte avgR = (byte)(rSum / count);
                byte avgA = (byte)(aSum / count);

                for (int by = 0; by < blockH; by++)
                {
                    for (int bx = 0; bx < blockW; bx++)
                    {
                        int offset = (y + by) * stride + (x + bx) * 4;
                        ptr[offset] = avgB;
                        ptr[offset + 1] = avgG;
                        ptr[offset + 2] = avgR;
                        ptr[offset + 3] = avgA;
                    }
                }
            }
        }

        crop.UnlockBits(data);

        using (var g = Graphics.FromImage(result))
        {
            g.DrawImage(crop, validRect.X, validRect.Y);
        }
        crop.Dispose();
        return result;
    }

    public ColorInfo GetPixelColor(Bitmap source, int x, int y)
    {
        if (x < 0 || x >= source.Width || y < 0 || y >= source.Height)
        {
            return new ColorInfo { R = 0, G = 0, B = 0, A = 255 };
        }
        var c = source.GetPixel(x, y);
        return new ColorInfo { R = c.R, G = c.G, B = c.B, A = c.A };
    }

    public byte[] EncodeToBytes(Bitmap source, ImageFormatType format, int quality = 90)
    {
        using var ms = new MemoryStream();
        switch (format)
        {
            case ImageFormatType.Png:
                source.Save(ms, ImageFormat.Png);
                break;
            case ImageFormatType.Bmp:
                source.Save(ms, ImageFormat.Bmp);
                break;
            case ImageFormatType.Jpeg:
            case ImageFormatType.Webp:
            default:
                var encoder = GetEncoder(ImageFormat.Jpeg);
                if (encoder != null)
                {
                    var encParams = new EncoderParameters(1);
                    encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)Math.Clamp(quality, 1, 100));
                    source.Save(ms, encoder, encParams);
                }
                else
                {
                    source.Save(ms, ImageFormat.Jpeg);
                }
                break;
        }
        return ms.ToArray();
    }

    public async Task SaveToFileAsync(Bitmap source, string filePath, ImageFormatType format, int quality = 90)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        byte[] bytes = EncodeToBytes(source, format, quality);
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    public Bitmap CreateThumbnail(Bitmap source, int maxWidth = 240, int maxHeight = 160)
    {
        return Resize(source, maxWidth, maxHeight, preserveAspect: true);
    }

    private static ImageCodecInfo? GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageEncoders();
        return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
    }
}

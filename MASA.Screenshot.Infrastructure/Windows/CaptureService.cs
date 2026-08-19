using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using MASA.Screenshot.Core.Models;
using MASA.Screenshot.Infrastructure.Windows;

namespace MASA.Screenshot.Infrastructure.Windows;

public sealed class CaptureService : ICaptureService
{
    public Task<Bitmap?> CaptureAsync(CaptureMode mode, MonitorTarget monitorTarget = MonitorTarget.CurrentMonitor, bool includeCursor = false, bool includeShadow = true)
    {
        return Task.Run<Bitmap?>(() =>
        {
            switch (mode)
            {
                case CaptureMode.FullScreen:
                    return CaptureFullScreenInternal(includeCursor);

                case CaptureMode.ActiveWindow:
                    var fg = NativeMethods.GetForegroundWindow();
                    if (fg != IntPtr.Zero)
                    {
                        return CaptureWindowInternal(fg, includeShadow, includeCursor);
                    }
                    return CaptureFullScreenInternal(includeCursor);

                case CaptureMode.MultiMonitor:
                    return CaptureMultiMonitorInternal(monitorTarget, includeCursor);

                case CaptureMode.FixedRegion:
                case CaptureMode.SelectedArea:
                default:
                    return CaptureFullScreenInternal(includeCursor);
            }
        });
    }

    public Task<Bitmap?> CaptureRegionAsync(Rectangle virtualScreenRegion, bool includeCursor = false)
    {
        return Task.Run<Bitmap?>(() =>
        {
            if (virtualScreenRegion.Width <= 0 || virtualScreenRegion.Height <= 0) return null;

            var bmp = new Bitmap(virtualScreenRegion.Width, virtualScreenRegion.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(virtualScreenRegion.X, virtualScreenRegion.Y, 0, 0, virtualScreenRegion.Size, CopyPixelOperation.SourceCopy);
                if (includeCursor)
                {
                    DrawCursorOnGraphics(g, virtualScreenRegion.X, virtualScreenRegion.Y);
                }
            }
            return bmp;
        });
    }

    public Task<Bitmap?> CaptureWindowAsync(IntPtr hWnd, bool includeShadow = true)
    {
        return Task.Run<Bitmap?>(() => CaptureWindowInternal(hWnd, includeShadow, false));
    }

    public Task<Bitmap?> CaptureFullScreenAsync(bool includeCursor = false)
    {
        return Task.Run<Bitmap?>(() => CaptureFullScreenInternal(includeCursor));
    }

    public Task<Bitmap?> CaptureMonitorAsync(int monitorIndex, bool includeCursor = false)
    {
        return Task.Run<Bitmap?>(() =>
        {
            var monitors = GetAllMonitors();
            if (monitorIndex < 0 || monitorIndex >= monitors.Count)
            {
                return CaptureFullScreenInternal(includeCursor);
            }
            var target = monitors[monitorIndex];
            var rect = new Rectangle(target.X, target.Y, target.Width, target.Height);
            return CaptureRegionInternal(rect, includeCursor);
        });
    }

    public IReadOnlyList<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
        {
            var mi = new NativeMethods.MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
            if (NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            {
                uint dpiX = 96, dpiY = 96;
                try
                {
                    NativeMethods.GetDpiForMonitor(hMonitor, 0, out dpiX, out dpiY);
                }
                catch
                {
                    dpiX = 96;
                    dpiY = 96;
                }

                double scale = (dpiX > 0) ? (dpiX / 96.0) : 1.0;

                monitors.Add(new MonitorInfo
                {
                    Index = index++,
                    DeviceName = mi.szDevice,
                    FriendlyName = $"Display {index} ({(mi.rcMonitor.Right - mi.rcMonitor.Left)}x{(mi.rcMonitor.Bottom - mi.rcMonitor.Top)})",
                    X = mi.rcMonitor.Left,
                    Y = mi.rcMonitor.Top,
                    Width = mi.rcMonitor.Right - mi.rcMonitor.Left,
                    Height = mi.rcMonitor.Bottom - mi.rcMonitor.Top,
                    DpiX = (int)dpiX,
                    DpiY = (int)dpiY,
                    ScaleFactor = scale,
                    IsPrimary = (mi.dwFlags & 1) != 0
                });
            }
            return true;
        }, IntPtr.Zero);

        if (monitors.Count == 0)
        {
            monitors.Add(new MonitorInfo
            {
                Index = 0,
                DeviceName = "DISPLAY_PRIMARY",
                FriendlyName = "Display 1",
                X = 0,
                Y = 0,
                Width = NativeMethods.GetSystemMetrics(0),
                Height = NativeMethods.GetSystemMetrics(1),
                ScaleFactor = 1.0,
                IsPrimary = true
            });
        }

        return monitors;
    }

    public MonitorInfo GetCurrentMonitor(Point screenPoint)
    {
        var monitors = GetAllMonitors();
        foreach (var m in monitors)
        {
            if (screenPoint.X >= m.X && screenPoint.X < m.Right &&
                screenPoint.Y >= m.Y && screenPoint.Y < m.Bottom)
            {
                return m;
            }
        }
        return monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
    }

    public Rectangle GetVirtualScreenBounds()
    {
        int x = NativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
        int y = NativeMethods.GetSystemMetrics(NativeMethods.SM_YVIRTUALSCREEN);
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);

        if (width <= 0 || height <= 0)
        {
            width = NativeMethods.GetSystemMetrics(0);
            height = NativeMethods.GetSystemMetrics(1);
            x = 0;
            y = 0;
        }

        return new Rectangle(x, y, width, height);
    }

    private Bitmap CaptureFullScreenInternal(bool includeCursor)
    {
        var bounds = GetVirtualScreenBounds();
        return CaptureRegionInternal(bounds, includeCursor);
    }

    private Bitmap CaptureRegionInternal(Rectangle region, bool includeCursor)
    {
        var bmp = new Bitmap(region.Width, region.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(region.X, region.Y, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
            if (includeCursor)
            {
                DrawCursorOnGraphics(g, region.X, region.Y);
            }
        }
        return bmp;
    }

    private Bitmap? CaptureWindowInternal(IntPtr hWnd, bool includeShadow, bool includeCursor)
    {
        if (hWnd == IntPtr.Zero || !NativeMethods.IsWindowVisible(hWnd) || NativeMethods.IsIconic(hWnd))
        {
            return null;
        }

        NativeMethods.RECT rect = default;
        int result = -1;
        if (!includeShadow)
        {
            result = NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS, out rect, Marshal.SizeOf(typeof(NativeMethods.RECT)));
        }

        if (includeShadow || result != 0)
        {
            NativeMethods.GetWindowRect(hWnd, out rect);
        }

        int width = rect.Width;
        int height = rect.Height;

        if (width <= 0 || height <= 0) return null;

        var region = new Rectangle(rect.Left, rect.Top, width, height);
        return CaptureRegionInternal(region, includeCursor);
    }

    private Bitmap? CaptureMultiMonitorInternal(MonitorTarget target, bool includeCursor)
    {
        var monitors = GetAllMonitors();
        if (monitors.Count == 0) return CaptureFullScreenInternal(includeCursor);

        switch (target)
        {
            case MonitorTarget.AllMonitors:
                return CaptureFullScreenInternal(includeCursor);

            case MonitorTarget.PrimaryMonitor:
                var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
                return CaptureRegionInternal(new Rectangle(primary.X, primary.Y, primary.Width, primary.Height), includeCursor);

            case MonitorTarget.Monitor1:
                return CaptureMonitorByIndex(0, includeCursor);
            case MonitorTarget.Monitor2:
                return CaptureMonitorByIndex(1, includeCursor);
            case MonitorTarget.Monitor3:
                return CaptureMonitorByIndex(2, includeCursor);

            case MonitorTarget.CurrentMonitor:
            default:
                NativeMethods.GetCursorPos(out var pt);
                var cur = GetCurrentMonitor(new Point(pt.X, pt.Y));
                return CaptureRegionInternal(new Rectangle(cur.X, cur.Y, cur.Width, cur.Height), includeCursor);
        }
    }

    private Bitmap CaptureMonitorByIndex(int idx, bool includeCursor)
    {
        var monitors = GetAllMonitors();
        if (idx < monitors.Count)
        {
            var m = monitors[idx];
            return CaptureRegionInternal(new Rectangle(m.X, m.Y, m.Width, m.Height), includeCursor);
        }
        return CaptureFullScreenInternal(includeCursor);
    }

    private void DrawCursorOnGraphics(Graphics g, int offsetX, int offsetY)
    {
        try
        {
            var ci = new NativeMethods.CURSORINFO();
            ci.cbSize = Marshal.SizeOf(typeof(NativeMethods.CURSORINFO));

            if (NativeMethods.GetCursorInfo(out ci) && ci.flags == NativeMethods.CURSOR_SHOWING)
            {
                var hdc = g.GetHdc();
                try
                {
                    if (NativeMethods.GetIconInfo(ci.hCursor, out var ii))
                    {
                        int cursorX = ci.ptScreenPos.X - offsetX - ii.xHotspot;
                        int cursorY = ci.ptScreenPos.Y - offsetY - ii.yHotspot;

                        NativeMethods.DrawIconEx(hdc, cursorX, cursorY, ci.hCursor, 0, 0, 0, IntPtr.Zero, NativeMethods.DI_NORMAL);

                        if (ii.hbmColor != IntPtr.Zero) NativeMethods.DeleteObject(ii.hbmColor);
                        if (ii.hbmMask != IntPtr.Zero) NativeMethods.DeleteObject(ii.hbmMask);
                    }
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }
        }
        catch
        {
            // Ignore cursor capture draw errors
        }
    }
}

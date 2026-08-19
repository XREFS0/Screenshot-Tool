using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MASA.Screenshot.App.ViewModels;
using MASA.Screenshot.App.Views;
using MASA.Screenshot.Application.Services;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Infrastructure.Clipboard;
using MASA.Screenshot.Infrastructure.Imaging;
using MASA.Screenshot.Infrastructure.Logging;
using MASA.Screenshot.Infrastructure.Storage;
using MASA.Screenshot.Infrastructure.Windows;
using Xunit;

namespace MASA.Screenshot.Tests;

public class WindowScreenshotCaptureRunner
{
    private static void RenderWindowToPng(Window window, string outputPath, double width, double height)
    {
        window.Width = width;
        window.Height = height;
        window.ShowActivated = false;
        window.Show();

        // Process message loop to let WPF measure, arrange and render fully
        var frame = new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);

        window.UpdateLayout();

        var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(window);

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
        {
            encoder.Save(fs);
        }

        window.Hide();
    }

    [Fact]
    public void GenerateActualAppScreenshots()
    {
        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new System.Windows.Application
                    {
                        ShutdownMode = ShutdownMode.OnExplicitShutdown
                    };
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/MASA.Screenshot.App;component/Themes/ThemeResources.xaml", UriKind.RelativeOrAbsolute)
                    };
                    app.Resources.MergedDictionaries.Add(dict);
                }

                string outputDir = @"c:\Users\masa\Desktop\New folder\Screenshots";
                Directory.CreateDirectory(outputDir);

                // Setup services
                var settingsService = new SettingsService();
                var imgProcessor = new ImageProcessingService();
                var captureService = new CaptureService();
                var clipboardService = new ClipboardService();
                var historyService = new HistoryService(settingsService, imgProcessor);
                var hotkeyService = new HotkeyService();
                var smartRedactService = new SmartRedactionService();

                // 1. SettingsWindow Screenshot
                var settingsVm = new SettingsViewModel(settingsService, hotkeyService);
                var settingsWindow = new SettingsWindow(settingsVm);
                RenderWindowToPng(settingsWindow, Path.Combine(outputDir, "02_SettingsWindow.png"), 680, 620);

                // 2. EditorWindow Screenshot
                var editorVm = new EditorViewModel(imgProcessor, clipboardService, settingsService, historyService, smartRedactService);
                var editorWindow = new EditorWindow(editorVm, imgProcessor);

                // 3. HistoryWindow Screenshot (with sample data)
                using (var sampleBmp = new System.Drawing.Bitmap(600, 400))
                {
                    using (var g = System.Drawing.Graphics.FromImage(sampleBmp))
                    {
                        g.Clear(System.Drawing.Color.FromArgb(30, 30, 30));
                        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 212));
                        g.FillRectangle(brush, 50, 50, 200, 100);
                        using var font = new System.Drawing.Font("Segoe UI", 16);
                        using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
                        g.DrawString("MASA Screenshot Sample", font, textBrush, 60, 80);
                    }
                    historyService.AddItemAsync(sampleBmp, CaptureMode.SelectedArea, "Desktop Region").GetAwaiter().GetResult();
                }

                var historyVm = new HistoryViewModel(historyService, clipboardService, imgProcessor);
                historyVm.LoadItems();
                var historyWindow = new HistoryWindow(historyVm);
                RenderWindowToPng(historyWindow, Path.Combine(outputDir, "03_HistoryWindow.png"), 950, 650);

                // 4. OverlayWindow
                var overlayWindow = new OverlayWindow(imgProcessor);

                // 5. MainWindow Screenshot
                var mainVm = new MainViewModel(captureService, imgProcessor, clipboardService, settingsService, historyService, hotkeyService);
                var mainWindow = new MainWindow(mainVm, captureService, hotkeyService, settingsService, overlayWindow, editorWindow, historyWindow, settingsWindow);
                RenderWindowToPng(mainWindow, Path.Combine(outputDir, "01_MainWindow_Dashboard.png"), 720, 540);

                // 6. EditorWindow with canvas content
                using (var canvasBmp = new System.Drawing.Bitmap(800, 500))
                {
                    using (var g = System.Drawing.Graphics.FromImage(canvasBmp))
                    {
                        g.Clear(System.Drawing.Color.FromArgb(28, 28, 30));
                        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 120, 212), 2);
                        g.DrawRectangle(pen, 40, 40, 720, 420);
                        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45, 45, 48));
                        g.FillRoundedRectangle(brush, 60, 60, 680, 120, 10);
                        using var font = new System.Drawing.Font("Segoe UI", 18, System.Drawing.FontStyle.Bold);
                        using var textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
                        g.DrawString("MASA Screenshot Canvas Annotation Suite", font, textBrush, 80, 100);
                    }
                    editorWindow.LoadBitmap(canvasBmp);
                }
                RenderWindowToPng(editorWindow, Path.Combine(outputDir, "04_EditorWindow_Annotator.png"), 1200, 780);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                throw;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }
}

public static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this System.Drawing.Graphics g, System.Drawing.Brush brush, int x, int y, int width, int height, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(x, y, radius, radius, 180, 90);
        path.AddArc(x + width - radius, y, radius, radius, 270, 90);
        path.AddArc(x + width - radius, y + height - radius, radius, radius, 0, 90);
        path.AddArc(x, y + height - radius, radius, radius, 90, 90);
        path.CloseFigure();
        g.FillPath(brush, path);
    }
}

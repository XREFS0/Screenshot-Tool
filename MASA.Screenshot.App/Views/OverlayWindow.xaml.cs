using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MASA.Screenshot.App.Converters;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using Point = System.Windows.Point;

namespace MASA.Screenshot.App.Views;

public partial class OverlayWindow : Window
{
    private Bitmap? _virtualScreenBitmap;
    private readonly IImageProcessingService _imageProcessor;
    private Point _startPoint;
    private Point _currentPoint;
    private bool _isSelecting;
    private System.Drawing.Rectangle _selectedRect;

    public event Action<Bitmap>? AreaCaptured;
    public event Action<Bitmap>? EditCapturedArea;

    public OverlayWindow(IImageProcessingService imageProcessor)
    {
        _imageProcessor = imageProcessor;
        InitializeComponent();
    }

    public void Setup(Bitmap virtualScreen, System.Drawing.Rectangle virtualBounds)
    {
        _virtualScreenBitmap?.Dispose();
        _virtualScreenBitmap = (Bitmap)virtualScreen.Clone();

        Left = virtualBounds.X;
        Top = virtualBounds.Y;
        Width = virtualBounds.Width;
        Height = virtualBounds.Height;

        BackgroundImage.Source = _virtualScreenBitmap.ToBitmapSource();
        SelectionBorder.Visibility = Visibility.Collapsed;
        SelectionToolbar.Visibility = Visibility.Collapsed;
        MagnifierHud.Visibility = Visibility.Visible;
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _startPoint = e.GetPosition(this);
            _isSelecting = true;
            SelectionToolbar.Visibility = Visibility.Collapsed;
            SelectionBorder.Visibility = Visibility.Visible;
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            CloseOverlay();
        }
    }

    private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        _currentPoint = e.GetPosition(this);

        // Update Magnifier HUD Position & Preview
        UpdateMagnifier(_currentPoint);

        if (_isSelecting)
        {
            double x = Math.Min(_startPoint.X, _currentPoint.X);
            double y = Math.Min(_startPoint.Y, _currentPoint.Y);
            double w = Math.Abs(_currentPoint.X - _startPoint.X);
            double h = Math.Abs(_currentPoint.Y - _startPoint.Y);

            SelectionBorder.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionBorder, x);
            Canvas.SetTop(SelectionBorder, y);
            SelectionBorder.Width = w;
            SelectionBorder.Height = h;

            DimensionText.Text = $"{(int)w} × {(int)h} px";
            _selectedRect = new System.Drawing.Rectangle((int)x, (int)y, (int)w, (int)h);
        }
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isSelecting)
        {
            _isSelecting = false;
            if (_selectedRect.Width > 5 && _selectedRect.Height > 5)
            {
                // Position floating quick toolbar
                double tbX = Math.Min(Width - 280, Math.Max(10, Canvas.GetLeft(SelectionBorder) + _selectedRect.Width - 260));
                double tbY = Math.Min(Height - 60, Canvas.GetTop(SelectionBorder) + _selectedRect.Height + 10);
                Canvas.SetLeft(SelectionToolbar, tbX);
                Canvas.SetTop(SelectionToolbar, tbY);
                SelectionToolbar.Visibility = Visibility.Visible;
            }
            else
            {
                SelectionBorder.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void UpdateMagnifier(Point mousePos)
    {
        if (_virtualScreenBitmap == null) return;

        double hudX = mousePos.X + 25;
        double hudY = mousePos.Y + 25;

        if (hudX + 160 > Width) hudX = mousePos.X - 175;
        if (hudY + 160 > Height) hudY = mousePos.Y - 175;

        MagnifierHud.Margin = new Thickness(hudX, hudY, 0, 0);

        int sampleSize = 30;
        int srcX = (int)mousePos.X - sampleSize / 2;
        int srcY = (int)mousePos.Y - sampleSize / 2;
        var sampleRect = new System.Drawing.Rectangle(srcX, srcY, sampleSize, sampleSize);

        using var zoom = _imageProcessor.Crop(_virtualScreenBitmap, sampleRect);
        MagnifierImage.Source = zoom.ToBitmapSource();

        var pixelColor = _imageProcessor.GetPixelColor(_virtualScreenBitmap, (int)mousePos.X, (int)mousePos.Y);
        MagnifierColorText.Text = $"{pixelColor.Hex} | ({(int)mousePos.X}, {(int)mousePos.Y})";
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (_virtualScreenBitmap != null && _selectedRect.Width > 0 && _selectedRect.Height > 0)
        {
            var cropped = _imageProcessor.Crop(_virtualScreenBitmap, _selectedRect);
            AreaCaptured?.Invoke(cropped);
        }
        CloseOverlay();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_virtualScreenBitmap != null && _selectedRect.Width > 0 && _selectedRect.Height > 0)
        {
            var cropped = _imageProcessor.Crop(_virtualScreenBitmap, _selectedRect);
            AreaCaptured?.Invoke(cropped);
        }
        CloseOverlay();
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_virtualScreenBitmap != null && _selectedRect.Width > 0 && _selectedRect.Height > 0)
        {
            var cropped = _imageProcessor.Crop(_virtualScreenBitmap, _selectedRect);
            EditCapturedArea?.Invoke(cropped);
        }
        CloseOverlay();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CloseOverlay();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseOverlay();
        }
        else if (e.Key == Key.Enter && _selectedRect.Width > 5)
        {
            CopyButton_Click(sender, e);
        }
    }

    private void CloseOverlay()
    {
        Hide();
        _virtualScreenBitmap?.Dispose();
        _virtualScreenBitmap = null;
    }
}

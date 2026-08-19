using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MASA.Screenshot.Application.Annotation;
using MASA.Screenshot.App.Converters;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;
using Color = System.Drawing.Color;
using Point = System.Windows.Point;

namespace MASA.Screenshot.App.Controls;

public sealed class AnnotationCanvas : Canvas
{
    private Bitmap? _sourceBitmap;
    private Bitmap? _renderBitmap;
    private readonly List<BaseAnnotation> _annotations = new();
    private readonly Stack<BaseAnnotation> _undoStack = new();
    private readonly Stack<BaseAnnotation> _redoStack = new();

    private BaseAnnotation? _currentAnnotation;
    private Point _startPoint;
    private int _stepCounter = 1;

    public AnnotationToolType ActiveTool { get; set; } = AnnotationToolType.Select;
    public Color ActiveColor { get; set; } = Color.FromArgb(255, 0, 122, 204);
    public float ActiveThickness { get; set; } = 3.0f;
    public string ActiveFontFamily { get; set; } = "Segoe UI";
    public float ActiveFontSize { get; set; } = 18.0f;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }

    public event EventHandler? CanvasModified;
    public event EventHandler<System.Drawing.Point>? MouseHoverPixel;

    public AnnotationCanvas()
    {
        ClipToBounds = true;
        Focusable = true;
    }

    public void SetImage(Bitmap bitmap)
    {
        _sourceBitmap?.Dispose();
        _renderBitmap?.Dispose();
        _sourceBitmap = (Bitmap)bitmap.Clone();
        _renderBitmap = (Bitmap)bitmap.Clone();
        _annotations.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _stepCounter = 1;

        Width = _sourceBitmap.Width;
        Height = _sourceBitmap.Height;

        RebuildRender();
    }

    public Bitmap? GetSourceBitmap() => _sourceBitmap;

    public Bitmap GetFlattenedBitmap(IImageProcessingService imageProcessor)
    {
        if (_sourceBitmap == null) return new Bitmap(1, 1);
        var result = (Bitmap)_sourceBitmap.Clone();

        // Apply any blur / pixelate operations directly on the bitmap
        foreach (var ann in _annotations)
        {
            if (ann is BlurRegionAnnotation blur)
            {
                var rect = new System.Drawing.Rectangle((int)blur.Bounds.X, (int)blur.Bounds.Y, (int)blur.Bounds.Width, (int)blur.Bounds.Height);
                var blurred = imageProcessor.ApplyBlur(result, rect, blur.BlurRadius);
                result.Dispose();
                result = blurred;
            }
            else if (ann is PixelateRegionAnnotation pix)
            {
                var rect = new System.Drawing.Rectangle((int)pix.Bounds.X, (int)pix.Bounds.Y, (int)pix.Bounds.Width, (int)pix.Bounds.Height);
                var pixeled = imageProcessor.ApplyPixelate(result, rect, pix.BlockSize);
                result.Dispose();
                result = pixeled;
            }
        }

        using (var g = Graphics.FromImage(result))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            foreach (var ann in _annotations)
            {
                if (ann is not BlurRegionAnnotation && ann is not PixelateRegionAnnotation)
                {
                    ann.Render(g, 1.0f);
                }
            }
        }

        return result;
    }

    public void Undo()
    {
        if (_annotations.Count > 0)
        {
            var last = _annotations[^1];
            _annotations.RemoveAt(_annotations.Count - 1);
            _redoStack.Push(last);
            RebuildRender();
            CanvasModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Redo()
    {
        if (_redoStack.Count > 0)
        {
            var item = _redoStack.Pop();
            _annotations.Add(item);
            RebuildRender();
            CanvasModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ClearAnnotations()
    {
        _annotations.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _stepCounter = 1;
        RebuildRender();
        CanvasModified?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);
        MouseHoverPixel?.Invoke(this, new System.Drawing.Point((int)pos.X, (int)pos.Y));

        if (e.LeftButton == MouseButtonState.Pressed && _currentAnnotation != null)
        {
            float curX = (float)pos.X;
            float curY = (float)pos.Y;

            switch (_currentAnnotation)
            {
                case PenAnnotation pen:
                    pen.Points.Add(new PointF(curX, curY));
                    break;

                case HighlighterAnnotation hl:
                    hl.Points.Add(new PointF(curX, curY));
                    break;

                case LineAnnotation line:
                    line.End = new PointF(curX, curY);
                    break;

                case ArrowAnnotation arrow:
                    arrow.End = new PointF(curX, curY);
                    break;

                case RectangleAnnotation rectAnn:
                    rectAnn.Bounds = CalculateBounds(_startPoint, pos);
                    break;

                case EllipseAnnotation ellAnn:
                    ellAnn.Bounds = CalculateBounds(_startPoint, pos);
                    break;

                case BlurRegionAnnotation blur:
                    blur.Bounds = CalculateBounds(_startPoint, pos);
                    break;

                case PixelateRegionAnnotation pix:
                    pix.Bounds = CalculateBounds(_startPoint, pos);
                    break;
            }

            InvalidateVisual();
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.LeftButton != MouseButtonState.Pressed || _sourceBitmap == null) return;

        _startPoint = e.GetPosition(this);
        float sx = (float)_startPoint.X;
        float sy = (float)_startPoint.Y;

        switch (ActiveTool)
        {
            case AnnotationToolType.Pen:
                var pen = new PenAnnotation { Color = ActiveColor, Thickness = ActiveThickness };
                pen.Points.Add(new PointF(sx, sy));
                _currentAnnotation = pen;
                break;

            case AnnotationToolType.Highlighter:
                var hl = new HighlighterAnnotation { Color = ActiveColor, Thickness = ActiveThickness * 4.0f };
                hl.Points.Add(new PointF(sx, sy));
                _currentAnnotation = hl;
                break;

            case AnnotationToolType.Line:
                _currentAnnotation = new LineAnnotation
                {
                    Color = ActiveColor,
                    Thickness = ActiveThickness,
                    Start = new PointF(sx, sy),
                    End = new PointF(sx, sy)
                };
                break;

            case AnnotationToolType.Arrow:
                _currentAnnotation = new ArrowAnnotation
                {
                    Color = ActiveColor,
                    Thickness = ActiveThickness,
                    Start = new PointF(sx, sy),
                    End = new PointF(sx, sy)
                };
                break;

            case AnnotationToolType.Rectangle:
                _currentAnnotation = new RectangleAnnotation
                {
                    Color = ActiveColor,
                    Thickness = ActiveThickness,
                    Bounds = new RectangleF(sx, sy, 0, 0)
                };
                break;

            case AnnotationToolType.Ellipse:
                _currentAnnotation = new EllipseAnnotation
                {
                    Color = ActiveColor,
                    Thickness = ActiveThickness,
                    Bounds = new RectangleF(sx, sy, 0, 0)
                };
                break;

            case AnnotationToolType.StepNumber:
                var step = new StepNumberAnnotation
                {
                    Color = ActiveColor,
                    Position = new PointF(sx, sy),
                    StepIndex = _stepCounter++
                };
                _annotations.Add(step);
                _redoStack.Clear();
                RebuildRender();
                CanvasModified?.Invoke(this, EventArgs.Empty);
                break;

            case AnnotationToolType.Blur:
                _currentAnnotation = new BlurRegionAnnotation
                {
                    Bounds = new RectangleF(sx, sy, 0, 0),
                    BlurRadius = 16
                };
                break;

            case AnnotationToolType.Pixelate:
                _currentAnnotation = new PixelateRegionAnnotation
                {
                    Bounds = new RectangleF(sx, sy, 0, 0),
                    BlockSize = 16
                };
                break;

            case AnnotationToolType.Text:
                // Create inline prompt or direct text
                var textAnn = new TextAnnotation
                {
                    Color = ActiveColor,
                    Position = new PointF(sx, sy),
                    FontFamily = ActiveFontFamily,
                    FontSize = ActiveFontSize,
                    IsBold = IsBold,
                    IsItalic = IsItalic,
                    Text = "Annotation Text"
                };
                _annotations.Add(textAnn);
                _redoStack.Clear();
                RebuildRender();
                CanvasModified?.Invoke(this, EventArgs.Empty);
                break;
        }

        CaptureMouse();
        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (_currentAnnotation != null)
        {
            _annotations.Add(_currentAnnotation);
            _currentAnnotation = null;
            _redoStack.Clear();
            RebuildRender();
            CanvasModified?.Invoke(this, EventArgs.Empty);
        }
        ReleaseMouseCapture();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (_renderBitmap != null)
        {
            var src = _renderBitmap.ToBitmapSource();
            dc.DrawImage(src, new Rect(0, 0, ActualWidth, ActualHeight));
        }

        // Draw active in-progress annotation preview
        if (_currentAnnotation != null)
        {
            // Transient preview
            var pen = new System.Windows.Media.Pen(new SolidColorBrush(System.Windows.Media.Color.FromArgb(_currentAnnotation.Color.A, _currentAnnotation.Color.R, _currentAnnotation.Color.G, _currentAnnotation.Color.B)), _currentAnnotation.Thickness);
            if (_currentAnnotation is RectangleAnnotation r)
            {
                dc.DrawRectangle(null, pen, new Rect(r.Bounds.X, r.Bounds.Y, r.Bounds.Width, r.Bounds.Height));
            }
            else if (_currentAnnotation is EllipseAnnotation el)
            {
                dc.DrawEllipse(null, pen, new Point(el.Bounds.X + el.Bounds.Width / 2, el.Bounds.Y + el.Bounds.Height / 2), el.Bounds.Width / 2, el.Bounds.Height / 2);
            }
            else if (_currentAnnotation is ArrowAnnotation ar)
            {
                dc.DrawLine(pen, new Point(ar.Start.X, ar.Start.Y), new Point(ar.End.X, ar.End.Y));
            }
            else if (_currentAnnotation is LineAnnotation ln)
            {
                dc.DrawLine(pen, new Point(ln.Start.X, ln.Start.Y), new Point(ln.End.X, ln.End.Y));
            }
            else if (_currentAnnotation is BlurRegionAnnotation br)
            {
                var dashPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.DodgerBlue, 1.5) { DashStyle = DashStyles.Dash };
                dc.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 30, 144, 255)), dashPen, new Rect(br.Bounds.X, br.Bounds.Y, br.Bounds.Width, br.Bounds.Height));
            }
            else if (_currentAnnotation is PixelateRegionAnnotation px)
            {
                var dashPen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.Orange, 1.5) { DashStyle = DashStyles.Dash };
                dc.DrawRectangle(new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 255, 140, 0)), dashPen, new Rect(px.Bounds.X, px.Bounds.Y, px.Bounds.Width, px.Bounds.Height));
            }
        }
    }

    private void RebuildRender()
    {
        if (_sourceBitmap == null) return;
        _renderBitmap?.Dispose();
        _renderBitmap = (Bitmap)_sourceBitmap.Clone();

        using (var g = Graphics.FromImage(_renderBitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            foreach (var ann in _annotations)
            {
                ann.Render(g, 1.0f);
            }
        }

        InvalidateVisual();
    }

    private static RectangleF CalculateBounds(Point p1, Point p2)
    {
        float x = (float)Math.Min(p1.X, p2.X);
        float y = (float)Math.Min(p1.Y, p2.Y);
        float w = (float)Math.Abs(p1.X - p2.X);
        float h = (float)Math.Abs(p1.Y - p2.Y);
        return new RectangleF(x, y, w, h);
    }
}

using System.Drawing;
using System.Drawing.Drawing2D;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Interfaces;

namespace MASA.Screenshot.Application.Annotation;

public abstract class BaseAnnotation : IAnnotationItem
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public abstract AnnotationToolType ToolType { get; }
    public Color Color { get; set; } = Color.FromArgb(255, 0, 122, 204);
    public float Thickness { get; set; } = 3.0f;
    public abstract void Render(Graphics g, float scale = 1.0f);
}

public sealed class StrokePoint
{
    public float X { get; set; }
    public float Y { get; set; }
}

public sealed class PenAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Pen;
    public List<PointF> Points { get; set; } = new();

    public override void Render(Graphics g, float scale = 1.0f)
    {
        if (Points.Count < 2) return;
        using var pen = new Pen(Color, Thickness * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var scaledPoints = Points.Select(p => new PointF(p.X * scale, p.Y * scale)).ToArray();
        g.DrawCurve(pen, scaledPoints, 0.5f);
    }
}

public sealed class HighlighterAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Highlighter;
    public List<PointF> Points { get; set; } = new();

    public override void Render(Graphics g, float scale = 1.0f)
    {
        if (Points.Count < 2) return;
        var transparentColor = Color.FromArgb(100, Color.R, Color.G, Color.B);
        using var pen = new Pen(transparentColor, Thickness * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
            LineJoin = LineJoin.Round
        };

        var scaledPoints = Points.Select(p => new PointF(p.X * scale, p.Y * scale)).ToArray();
        g.DrawLines(pen, scaledPoints);
    }
}

public sealed class LineAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Line;
    public PointF Start { get; set; }
    public PointF End { get; set; }

    public override void Render(Graphics g, float scale = 1.0f)
    {
        using var pen = new Pen(Color, Thickness * scale)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round
        };
        g.DrawLine(pen, Start.X * scale, Start.Y * scale, End.X * scale, End.Y * scale);
    }
}

public sealed class ArrowAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Arrow;
    public PointF Start { get; set; }
    public PointF End { get; set; }

    public override void Render(Graphics g, float scale = 1.0f)
    {
        using var cap = new AdjustableArrowCap(5 * scale, 5 * scale, true);
        using var pen = new Pen(Color, Thickness * scale)
        {
            CustomEndCap = cap,
            StartCap = LineCap.Round
        };
        g.DrawLine(pen, Start.X * scale, Start.Y * scale, End.X * scale, End.Y * scale);
    }
}

public sealed class RectangleAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Rectangle;
    public RectangleF Bounds { get; set; }
    public bool IsFilled { get; set; }
    public Color FillColor { get; set; } = Color.Transparent;

    public override void Render(Graphics g, float scale = 1.0f)
    {
        var rect = new RectangleF(Bounds.X * scale, Bounds.Y * scale, Bounds.Width * scale, Bounds.Height * scale);
        if (IsFilled && FillColor != Color.Transparent)
        {
            using var brush = new SolidBrush(FillColor);
            g.FillRectangle(brush, rect);
        }
        using var pen = new Pen(Color, Thickness * scale);
        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
    }
}

public sealed class EllipseAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Ellipse;
    public RectangleF Bounds { get; set; }
    public bool IsFilled { get; set; }
    public Color FillColor { get; set; } = Color.Transparent;

    public override void Render(Graphics g, float scale = 1.0f)
    {
        var rect = new RectangleF(Bounds.X * scale, Bounds.Y * scale, Bounds.Width * scale, Bounds.Height * scale);
        if (IsFilled && FillColor != Color.Transparent)
        {
            using var brush = new SolidBrush(FillColor);
            g.FillEllipse(brush, rect);
        }
        using var pen = new Pen(Color, Thickness * scale);
        g.DrawEllipse(pen, rect);
    }
}

public sealed class TextAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Text;
    public PointF Position { get; set; }
    public string Text { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Segoe UI";
    public float FontSize { get; set; } = 18.0f;
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public Color BackgroundColor { get; set; } = Color.FromArgb(180, 0, 0, 0);

    public override void Render(Graphics g, float scale = 1.0f)
    {
        if (string.IsNullOrWhiteSpace(Text)) return;
        var style = FontStyle.Regular;
        if (IsBold) style |= FontStyle.Bold;
        if (IsItalic) style |= FontStyle.Italic;

        using var font = new Font(FontFamily, FontSize * scale, style, GraphicsUnit.Pixel);
        var size = g.MeasureString(Text, font);
        var rect = new RectangleF(Position.X * scale, Position.Y * scale, size.Width + 12 * scale, size.Height + 8 * scale);

        if (BackgroundColor != Color.Transparent)
        {
            using var bgBrush = new SolidBrush(BackgroundColor);
            g.FillRectangle(bgBrush, rect);
            using var borderPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1.0f * scale);
            g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        using var brush = new SolidBrush(Color);
        g.DrawString(Text, font, brush, Position.X * scale + 6 * scale, Position.Y * scale + 4 * scale);
    }
}

public sealed class StepNumberAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.StepNumber;
    public PointF Position { get; set; }
    public int StepIndex { get; set; } = 1;
    public float Radius { get; set; } = 14.0f;

    public override void Render(Graphics g, float scale = 1.0f)
    {
        float r = Radius * scale;
        var center = new PointF(Position.X * scale, Position.Y * scale);
        var rect = new RectangleF(center.X - r, center.Y - r, r * 2, r * 2);

        using var brush = new SolidBrush(Color);
        g.FillEllipse(brush, rect);

        using var pen = new Pen(Color.White, 2.0f * scale);
        g.DrawEllipse(pen, rect);

        using var font = new Font("Segoe UI", r * 1.05f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var stringFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(StepIndex.ToString(), font, textBrush, rect, stringFormat);
    }
}

public sealed class BlurRegionAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Blur;
    public RectangleF Bounds { get; set; }
    public int BlurRadius { get; set; } = 15;

    public override void Render(Graphics g, float scale = 1.0f)
    {
        // Visual indicator border when rendering on canvas preview
        using var pen = new Pen(Color.FromArgb(120, 0, 150, 255), 1.5f * scale)
        {
            DashStyle = DashStyle.Dash
        };
        g.DrawRectangle(pen, Bounds.X * scale, Bounds.Y * scale, Bounds.Width * scale, Bounds.Height * scale);
    }
}

public sealed class PixelateRegionAnnotation : BaseAnnotation
{
    public override AnnotationToolType ToolType => AnnotationToolType.Pixelate;
    public RectangleF Bounds { get; set; }
    public int BlockSize { get; set; } = 16;

    public override void Render(Graphics g, float scale = 1.0f)
    {
        using var pen = new Pen(Color.FromArgb(120, 255, 120, 0), 1.5f * scale)
        {
            DashStyle = DashStyle.Dash
        };
        g.DrawRectangle(pen, Bounds.X * scale, Bounds.Y * scale, Bounds.Width * scale, Bounds.Height * scale);
    }
}

using System.Drawing;
using FluentAssertions;
using MASA.Screenshot.Application.Services;
using MASA.Screenshot.Core.Enums;
using MASA.Screenshot.Core.Models;
using Xunit;

namespace MASA.Screenshot.Tests;

public class ScreenshotEngineTests
{
    [Fact]
    public void FilenameGenerator_GeneratesCorrectFormat()
    {
        var dt = new DateTime(2026, 8, 19, 16, 30, 42);
        string filename = FilenameGenerator.GenerateFilename(dt, "png");
        filename.Should().Be("Screenshot_2026-08-19_16-30-42.png");
    }

    [Fact]
    public void CropCalculator_NormalizesBoundsProperly()
    {
        var orig = new Rectangle(0, 0, 1920, 1080);
        var requested = new Rectangle(-50, -50, 400, 300);
        var calculated = CropCalculator.CalculateCropBounds(orig.Width, orig.Height, requested);

        calculated.X.Should().Be(0);
        calculated.Y.Should().Be(0);
        calculated.Width.Should().Be(350);
        calculated.Height.Should().Be(250);
    }

    [Fact]
    public void AspectRatioCrop_Calculates16x9Correctly()
    {
        int width = 1920;
        int height = 1200; // 16:10
        double targetRatio = 16.0 / 9.0;

        var crop = CropCalculator.CalculateAspectRatioCrop(width, height, targetRatio);
        crop.Width.Should().Be(1920);
        crop.Height.Should().Be(1080);
        crop.Y.Should().Be(60);
    }

    [Fact]
    public void ColorHelper_ConvertsRgbToHslAccurately()
    {
        // Pure Red
        ColorHelper.RgbToHsl(255, 0, 0, out double h, out double s, out double l);
        h.Should().BeApproximately(0.0, 0.1);
        s.Should().BeApproximately(1.0, 0.1);
        l.Should().BeApproximately(0.5, 0.1);

        // Pure Blue
        ColorHelper.RgbToHsl(0, 0, 255, out h, out s, out l);
        h.Should().BeApproximately(240.0, 0.1);
        s.Should().BeApproximately(1.0, 0.1);
        l.Should().BeApproximately(0.5, 0.1);
    }

    [Fact]
    public void SensitiveDataDetector_IdentifiesEmailsAndPhoneNumbers()
    {
        SensitiveDataDetector.ContainsSensitivePattern("Contact user@example.com for info", out string reason1).Should().BeTrue();
        reason1.Should().Be("Email Address");

        SensitiveDataDetector.ContainsSensitivePattern("Call +1-555-123-4567 now", out string reason2).Should().BeTrue();
        reason2.Should().Be("Phone Number");

        SensitiveDataDetector.ContainsSensitivePattern("Just plain text with no tokens", out string reason3).Should().BeFalse();
        reason3.Should().BeEmpty();
    }

    [Fact]
    public void DpiCoordinateTransformer_ScalesVirtualCoordinates()
    {
        var virtualRect = new Rectangle(100, 200, 300, 400);
        double scale = 1.5; // 150% scaling

        var scaled = DpiCoordinateTransformer.VirtualToDpiScaled(virtualRect, scale);
        scaled.X.Should().Be(150);
        scaled.Y.Should().Be(300);
        scaled.Width.Should().Be(450);
        scaled.Height.Should().Be(600);
    }
}

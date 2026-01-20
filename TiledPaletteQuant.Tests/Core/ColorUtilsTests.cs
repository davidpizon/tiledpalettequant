using FluentAssertions;
using TiledPaletteQuant.Core;
using Xunit;

namespace TiledPaletteQuant.Tests.Core;

public class ColorUtilsTests
{
    [Fact]
    public void ColorDistance_ShouldCalculateWeightedDistance()
    {
        // Arrange
        var color1 = new double[] { 100, 150, 200 };
        var color2 = new double[] { 120, 140, 190 };

        // Act
        double distance = ColorUtils.ColorDistance(color1, color2);

        // Assert
        // Formula: 2*(ΔR)² + 4*(ΔG)² + (ΔB)²
        // = 2*(20)² + 4*(10)² + (10)²
        // = 2*400 + 4*100 + 100
        // = 800 + 400 + 100 = 1300
        distance.Should().Be(1300);
    }

    [Theory]
    [InlineData(0, 5, 0)]
    [InlineData(255, 5, 255)]
    [InlineData(128, 5, 132)] // 128/8.22581=15.56 -> round=16 -> 16*8.22581=131.61 -> round=132
    [InlineData(100, 8, 100)]
    [InlineData(127, 2, 85)]
    public void ToNbit_ShouldQuantizeCorrectly(double value, int n, double expected)
    {
        // Act
        double result = ColorUtils.ToNbit(value, n);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToNbitColor_ShouldQuantizeAllChannels()
    {
        // Arrange
        var color = new double[] { 100, 150, 200 };

        // Act
        ColorUtils.ToNbitColor(color, 5);

        // Assert - Using 5-bit quantization (alpha=8.22581)
        // 100/8.22581≈12.16->12->98.7->99
        // 150/8.22581≈18.24->18->148.06->148
        // 200/8.22581≈24.32->24->197.42->197
        color[0].Should().Be(99);
        color[1].Should().Be(148);
        color[2].Should().Be(197);
    }

    [Fact]
    public void ToLinear_ShouldSquareValue()
    {
        // Arrange
        double value = 10;

        // Act
        double result = ColorUtils.ToLinear(value);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void ToSrgb_ShouldTakeSquareRoot()
    {
        // Arrange
        double value = 100;

        // Act
        double result = ColorUtils.ToSrgb(value);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void ToLinearColor_ShouldConvertAllChannels()
    {
        // Arrange
        var color = new double[] { 10, 20, 30 };

        // Act
        ColorUtils.ToLinearColor(color);

        // Assert
        color[0].Should().Be(100);
        color[1].Should().Be(400);
        color[2].Should().Be(900);
    }

    [Fact]
    public void ToSrgbColor_ShouldConvertAllChannels()
    {
        // Arrange
        var color = new double[] { 100, 400, 900 };

        // Act
        ColorUtils.ToSrgbColor(color);

        // Assert
        color[0].Should().Be(10);
        color[1].Should().Be(20);
        color[2].Should().Be(30);
    }

    [Fact]
    public void Brightness_ShouldCalculateWeightedBrightness()
    {
        // Arrange
        var color = new double[] { 100, 100, 100 };

        // Act
        double brightness = ColorUtils.Brightness(color);

        // Assert
        // brightness = 0.299*100² + 0.587*100² + 0.114*100²
        // = (0.299 + 0.587 + 0.114) * 10000 = 1.0 * 10000
        brightness.Should().Be(10000);
    }

    [Fact]
    public void CloneColor_ShouldCreateDeepCopy()
    {
        // Arrange
        var color = new double[] { 100, 150, 200 };

        // Act
        var clone = ColorUtils.CloneColor(color);
        clone[0] = 50;

        // Assert
        clone.Should().NotBeSameAs(color);
        color[0].Should().Be(100);
        clone[0].Should().Be(50);
    }

    [Fact]
    public void CopyColor_ShouldCopyValues()
    {
        // Arrange
        var dest = new double[] { 0, 0, 0 };
        var src = new double[] { 100, 150, 200 };

        // Act
        ColorUtils.CopyColor(dest, src);

        // Assert
        dest.Should().Equal(new double[] { 100, 150, 200 });
    }

    [Fact]
    public void AddColor_ShouldAddInPlace()
    {
        // Arrange
        var color1 = new double[] { 100, 150, 200 };
        var color2 = new double[] { 50, 25, 10 };

        // Act
        ColorUtils.AddColor(color1, color2);

        // Assert
        color1.Should().Equal(new double[] { 150, 175, 210 });
    }

    [Fact]
    public void SubtractColor_ShouldSubtractInPlace()
    {
        // Arrange
        var color1 = new double[] { 100, 150, 200 };
        var color2 = new double[] { 50, 25, 10 };

        // Act
        ColorUtils.SubtractColor(color1, color2);

        // Assert
        color1.Should().Equal(new double[] { 50, 125, 190 });
    }

    [Fact]
    public void ScaleColor_ShouldScaleInPlace()
    {
        // Arrange
        var color = new double[] { 100, 150, 200 };

        // Act
        ColorUtils.ScaleColor(color, 0.5);

        // Assert
        color.Should().Equal(new double[] { 50, 75, 100 });
    }

    [Fact]
    public void ClampColor_ShouldClampToRange()
    {
        // Arrange
        var color = new double[] { -10, 150, 300 };

        // Act
        ColorUtils.ClampColor(color, 0, 255);

        // Assert
        color.Should().Equal(new double[] { 0, 150, 255 });
    }

    [Fact]
    public void EqualColors_ShouldReturnTrueForEqualColors()
    {
        // Arrange
        var color1 = new double[] { 100, 150, 200 };
        var color2 = new double[] { 100, 150, 200 };

        // Act
        bool result = ColorUtils.EqualColors(color1, color2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EqualColors_ShouldReturnFalseForDifferentColors()
    {
        // Arrange
        var color1 = new double[] { 100, 150, 200 };
        var color2 = new double[] { 100, 150, 201 };

        // Act
        bool result = ColorUtils.EqualColors(color1, color2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void MoveColorCloser_ShouldInterpolateColors()
    {
        // Arrange
        var color = new double[] { 0, 0, 0 };
        var target = new double[] { 100, 100, 100 };

        // Act
        ColorUtils.MoveColorCloser(color, target, 0.5);

        // Assert
        // color = (1-0.5)*0 + 0.5*100 = 50
        color.Should().Equal(new double[] { 50, 50, 50 });
    }
}

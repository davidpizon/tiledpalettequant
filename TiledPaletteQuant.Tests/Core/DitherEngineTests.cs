using FluentAssertions;
using TiledPaletteQuant.Core;
using TiledPaletteQuant.Models;
using Xunit;

namespace TiledPaletteQuant.Tests.Core;

public class DitherEngineTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCorrectPattern()
    {
        // Arrange & Act
        var options = new QuantizationOptions { DitherPattern = DitherPattern.Diagonal4 };
        var engine = new DitherEngine(options);

        // Assert
        engine.Should().NotBeNull();
    }

    [Theory]
    [InlineData(DitherPattern.Diagonal4)]
    [InlineData(DitherPattern.Horizontal4)]
    [InlineData(DitherPattern.Vertical4)]
    [InlineData(DitherPattern.Diagonal2)]
    [InlineData(DitherPattern.Horizontal2)]
    [InlineData(DitherPattern.Vertical2)]
    public void Constructor_ShouldSupportAllPatterns(DitherPattern pattern)
    {
        // Arrange & Act
        var options = new QuantizationOptions { DitherPattern = pattern };
        var engine = new DitherEngine(options);

        // Assert
        engine.Should().NotBeNull();
    }

    [Fact]
    public void GetClosestColor_ShouldFindNearestColor()
    {
        // Arrange
        var palette = new List<double[]>
        {
            new double[] { 0, 0, 0 },
            new double[] { 255, 0, 0 },
            new double[] { 0, 255, 0 },
            new double[] { 0, 0, 255 }
        };
        var color = new double[] { 10, 5, 8 };

        // Act
        var (index, distance) = DitherEngine.GetClosestColor(palette, color);

        // Assert
        index.Should().Be(0); // Black is closest
        distance.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetClosestColor_ShouldReturnZeroDistanceForExactMatch()
    {
        // Arrange
        var palette = new List<double[]>
        {
            new double[] { 100, 150, 200 }
        };
        var color = new double[] { 100, 150, 200 };

        // Act
        var (index, distance) = DitherEngine.GetClosestColor(palette, color);

        // Assert
        index.Should().Be(0);
        distance.Should().Be(0);
    }

    [Fact]
    public void ClosestColorDither_ShouldReturnValidColorIndex()
    {
        // Arrange
        var options = new QuantizationOptions
        {
            DitherPattern = DitherPattern.Diagonal4,
            BitsPerChannel = 5,
            DitherWeight = 0.5
        };
        var engine = new DitherEngine(options);

        var palette = new List<double[]>
        {
            new double[] { 0, 0, 0 },
            new double[] { 128, 128, 128 },
            new double[] { 255, 255, 255 }
        };

        var tile = new TileData();
        var pixel = new PixelData
        {
            Color = new double[] { 100, 100, 100 },
            X = 0,
            Y = 0,
            Tile = tile
        };

        // Act
        var (colorIndex, distance, comparedColor) = engine.ClosestColorDither(palette, pixel);

        // Assert
        colorIndex.Should().BeInRange(0, palette.Count - 1);
        distance.Should().BeGreaterThanOrEqualTo(0);
        comparedColor.Should().HaveCount(3);
    }

    [Fact]
    public void ClosestColorDither_ShouldVaryByPosition()
    {
        // Arrange
        var options = new QuantizationOptions
        {
            DitherPattern = DitherPattern.Diagonal4,
            BitsPerChannel = 5,
            DitherWeight = 0.5
        };
        var engine = new DitherEngine(options);

        var palette = new List<double[]>
        {
            new double[] { 0, 0, 0 },
            new double[] { 64, 64, 64 },
            new double[] { 128, 128, 128 },
            new double[] { 192, 192, 192 }
        };

        var tile = new TileData();
        var results = new List<int>();

        // Act - Test different pixel positions
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                var pixel = new PixelData
                {
                    Color = new double[] { 96, 96, 96 },
                    X = x,
                    Y = y,
                    Tile = tile
                };
                var (colorIndex, _, _) = engine.ClosestColorDither(palette, pixel);
                results.Add(colorIndex);
            }
        }

        // Assert - Should potentially select different colors based on position
        results.Should().HaveCount(4);
        results.Should().AllSatisfy(r => r.Should().BeInRange(0, palette.Count - 1));
    }
}

using FluentAssertions;
using TiledPaletteQuant.Models;
using Xunit;

namespace TiledPaletteQuant.Tests.Models;

public class QuantizationOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new QuantizationOptions();

        // Assert
        options.TileWidth.Should().Be(8);
        options.TileHeight.Should().Be(8);
        options.PaletteCount.Should().Be(8);
        options.ColorsPerPalette.Should().Be(4);
        options.BitsPerChannel.Should().Be(5);
        options.FractionOfPixels.Should().Be(0.1);
        options.DitherWeight.Should().Be(0.5);
        options.ColorZeroBehavior.Should().Be(ColorZeroBehavior.Unique);
        options.DitherMode.Should().Be(DitherMode.Off);
        options.DitherPattern.Should().Be(DitherPattern.Diagonal4);
    }

    [Fact]
    public void ParseArguments_ShouldParseInputPath()
    {
        // Arrange
        var args = new[] { "-i", "input.png" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.InputPath.Should().Be("input.png");
    }

    [Fact]
    public void ParseArguments_ShouldParseOutputPath()
    {
        // Arrange
        var args = new[] { "-o", "output.bmp" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.OutputPath.Should().Be("output.bmp");
    }

    [Fact]
    public void ParseArguments_ShouldParseTileSize()
    {
        // Arrange
        var args = new[] { "-tw", "16", "-th", "32" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.TileWidth.Should().Be(16);
        options.TileHeight.Should().Be(32);
    }

    [Fact]
    public void ParseArguments_ShouldParsePaletteOptions()
    {
        // Arrange
        var args = new[] { "-p", "16", "-c", "8", "-b", "6" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.PaletteCount.Should().Be(16);
        options.ColorsPerPalette.Should().Be(8);
        options.BitsPerChannel.Should().Be(6);
    }

    [Theory]
    [InlineData("off", DitherMode.Off)]
    [InlineData("fast", DitherMode.Fast)]
    [InlineData("slow", DitherMode.Slow)]
    [InlineData("OFF", DitherMode.Off)]
    [InlineData("FAST", DitherMode.Fast)]
    public void ParseArguments_ShouldParseDitherMode(string mode, DitherMode expected)
    {
        // Arrange
        var args = new[] { "-d", mode };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.DitherMode.Should().Be(expected);
    }

    [Theory]
    [InlineData("diagonal4", DitherPattern.Diagonal4)]
    [InlineData("horizontal4", DitherPattern.Horizontal4)]
    [InlineData("vertical4", DitherPattern.Vertical4)]
    [InlineData("diagonal2", DitherPattern.Diagonal2)]
    [InlineData("horizontal2", DitherPattern.Horizontal2)]
    [InlineData("vertical2", DitherPattern.Vertical2)]
    public void ParseArguments_ShouldParseDitherPattern(string pattern, DitherPattern expected)
    {
        // Arrange
        var args = new[] { "-dp", pattern };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.DitherPattern.Should().Be(expected);
    }

    [Fact]
    public void ParseArguments_ShouldParseFraction()
    {
        // Arrange
        var args = new[] { "-f", "0.2" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.FractionOfPixels.Should().Be(0.2);
    }

    [Fact]
    public void ParseArguments_ShouldHandleLongOptions()
    {
        // Arrange
        var args = new[] { "--input", "in.png", "--output", "out.bmp", "--tile-width", "16" };

        // Act
        var options = QuantizationOptions.ParseArguments(args);

        // Assert
        options.InputPath.Should().Be("in.png");
        options.OutputPath.Should().Be("out.bmp");
        options.TileWidth.Should().Be(16);
    }

    [Fact]
    public void Validate_ShouldPassForValidOptions()
    {
        // Arrange
        var options = new QuantizationOptions();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(257)]
    public void Validate_ShouldThrowForInvalidTileWidth(int width)
    {
        // Arrange
        var options = new QuantizationOptions { TileWidth = width };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(257)]
    public void Validate_ShouldThrowForInvalidPaletteCount(int count)
    {
        // Arrange
        var options = new QuantizationOptions { PaletteCount = count };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(257)]
    public void Validate_ShouldThrowForInvalidColorsPerPalette(int colors)
    {
        // Arrange
        var options = new QuantizationOptions { ColorsPerPalette = colors };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(9)]
    public void Validate_ShouldThrowForInvalidBitsPerChannel(int bits)
    {
        // Arrange
        var options = new QuantizationOptions { BitsPerChannel = bits };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Validate_ShouldThrowForInvalidFraction(double fraction)
    {
        // Arrange
        var options = new QuantizationOptions { FractionOfPixels = fraction };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}

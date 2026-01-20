using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TiledPaletteQuant.Core;
using TiledPaletteQuant.IO;
using TiledPaletteQuant.Models;
using Xunit;

namespace TiledPaletteQuant.Tests.Integration;

public class EndToEndTests
{
    [Fact]
    public void Quantize_ShouldProcessSimpleImage()
    {
        // Arrange
        int width = 16;
        int height = 16;
        var imageData = CreateSolidColorImage(width, height, 255, 0, 0); // Red

        var options = new QuantizationOptions
        {
            TileWidth = 8,
            TileHeight = 8,
            PaletteCount = 1,
            ColorsPerPalette = 2,
            BitsPerChannel = 5
        };

        var quantizer = new TiledPaletteQuantizer(options);

        // Act
        var result = quantizer.Quantize(imageData, width, height);

        // Assert
        result.Should().NotBeNull();
        result.ImageData.Should().HaveCount(width * height * 4);
        result.Width.Should().Be(width);
        result.Height.Should().Be(height);
        result.Palettes.Should().HaveCount(1);
        result.Palettes[0].Should().HaveCount(2);
    }

    [Fact]
    public void Quantize_WithDithering_ShouldProcessImage()
    {
        // Arrange
        int width = 16;
        int height = 16;
        var imageData = CreateGradientImage(width, height);

        var options = new QuantizationOptions
        {
            TileWidth = 8,
            TileHeight = 8,
            PaletteCount = 2,
            ColorsPerPalette = 4,
            BitsPerChannel = 5,
            DitherMode = DitherMode.Fast,
            DitherPattern = DitherPattern.Diagonal4,
            FractionOfPixels = 0.05 // Faster for tests
        };

        var quantizer = new TiledPaletteQuantizer(options);

        // Act
        var result = quantizer.Quantize(imageData, width, height);

        // Assert
        result.Should().NotBeNull();
        result.Palettes.Should().HaveCount(2);
        result.Palettes.Should().AllSatisfy(p => p.Should().HaveCount(4));
    }

    [Fact]
    public void Quantize_MultipleColors_ShouldGenerateMultiplePalettes()
    {
        // Arrange
        int width = 32;
        int height = 32;
        var imageData = CreateCheckerboardImage(width, height);

        var options = new QuantizationOptions
        {
            TileWidth = 8,
            TileHeight = 8,
            PaletteCount = 4,
            ColorsPerPalette = 4,
            BitsPerChannel = 5,
            FractionOfPixels = 0.05 // Faster for tests
        };

        var quantizer = new TiledPaletteQuantizer(options);

        // Act
        var result = quantizer.Quantize(imageData, width, height);

        // Assert
        result.Palettes.Should().HaveCount(4);
        result.Palettes.Should().AllSatisfy(p => p.Should().HaveCount(4));
    }

    [Fact]
    public void FullPipeline_LoadQuantizeSave_ShouldWork()
    {
        // Arrange
        string inputPath = Path.Combine(Path.GetTempPath(), $"input_{Guid.NewGuid()}.png");
        string outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.bmp");

        try
        {
            // Create test image
            int width = 16;
            int height = 16;
            var testImage = CreateTestImage(width, height);
            testImage.SaveAsPng(inputPath);

            var (imageData, w, h) = ImageProcessor.LoadImage(inputPath);

            var options = new QuantizationOptions
            {
                TileWidth = 8,
                TileHeight = 8,
                PaletteCount = 2,
                ColorsPerPalette = 4,
                BitsPerChannel = 5,
                FractionOfPixels = 0.05
            };

            var quantizer = new TiledPaletteQuantizer(options);

            // Act
            var result = quantizer.Quantize(imageData, w, h);
            BmpWriter.WriteBmp(outputPath, w, h, result.PaletteData!, result.ColorIndexes!);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
            var outputBytes = File.ReadAllBytes(outputPath);
            outputBytes.Should().NotBeEmpty();
            outputBytes[0].Should().Be(66); // 'B'
            outputBytes[1].Should().Be(77); // 'M'
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [Fact]
    public void Quantize_ShouldHandleSmallImages()
    {
        // Arrange
        int width = 4;
        int height = 4;
        var imageData = CreateSolidColorImage(width, height, 128, 128, 128);

        var options = new QuantizationOptions
        {
            TileWidth = 4,
            TileHeight = 4,
            PaletteCount = 1,
            ColorsPerPalette = 2,
            FractionOfPixels = 0.1
        };

        var quantizer = new TiledPaletteQuantizer(options);

        // Act
        var result = quantizer.Quantize(imageData, width, height);

        // Assert
        result.Should().NotBeNull();
        result.ImageData.Should().HaveCount(width * height * 4);
    }

    // Helper methods to create test images

    private byte[] CreateSolidColorImage(int width, int height, byte r, byte g, byte b)
    {
        var data = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            data[i * 4] = r;
            data[i * 4 + 1] = g;
            data[i * 4 + 2] = b;
            data[i * 4 + 3] = 255;
        }
        return data;
    }

    private byte[] CreateGradientImage(int width, int height)
    {
        var data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                byte value = (byte)((x * 255) / (width - 1));
                data[i] = value;
                data[i + 1] = value;
                data[i + 2] = value;
                data[i + 3] = 255;
            }
        }
        return data;
    }

    private byte[] CreateCheckerboardImage(int width, int height)
    {
        var data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                bool isWhite = ((x / 8) + (y / 8)) % 2 == 0;
                byte value = (byte)(isWhite ? 255 : 0);
                data[i] = value;
                data[i + 1] = value;
                data[i + 2] = value;
                data[i + 3] = 255;
            }
        }
        return data;
    }

    private Image<Rgba32> CreateTestImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    byte value = (byte)((x + y) * 16);
                    row[x] = new Rgba32(value, value, value);
                }
            }
        });
        return image;
    }
}

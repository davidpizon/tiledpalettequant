using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
        var image = CreateSolidColorImage(width, height, 255, 0, 0); // Red

        var options = new QuantizationOptions
        {
            TileWidth = 8,
            TileHeight = 8,
            PaletteCount = 1,
            ColorsPerPalette = 2,
            BitsPerChannel = 5
        };

        var quantizer = new TiledPaletteQuantizer();

        // Act
        var result = quantizer.Quantize(image, options);

        // Assert
        result.Should().NotBeNull();
        result.image.Should().NotBeNull();
        result.image.Width.Should().Be(width);
        result.image.Height.Should().Be(height);
        result.PaletteData.Should().HaveCount(1);
        result.PaletteData[0].Should().HaveCount(2);
    }

    [Fact]
    public void Quantize_WithDithering_ShouldProcessImage()
    {
        // Arrange
        int width = 16;
        int height = 16;
        var image = CreateGradientImage(width, height);

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

        var quantizer = new TiledPaletteQuantizer();

        // Act
        var result = quantizer.Quantize(image, options);

        // Assert
        result.Should().NotBeNull();
        result.PaletteData.Should().HaveCount(2);
        result.PaletteData.Should().AllSatisfy(p => p.Should().HaveCount(4));
    }

    [Fact]
    public void Quantize_MultipleColors_ShouldGenerateMultiplePalettes()
    {
        // Arrange
        int width = 32;
        int height = 32;
        var image = CreateCheckerboardImage(width, height);

        var options = new QuantizationOptions
        {
            TileWidth = 8,
            TileHeight = 8,
            PaletteCount = 4,
            ColorsPerPalette = 4,
            BitsPerChannel = 5,
            FractionOfPixels = 0.05 // Faster for tests
        };

        var quantizer = new TiledPaletteQuantizer();

        // Act
        var result = quantizer.Quantize(image, options);

        // Assert
        result.PaletteData.Should().HaveCount(4);
        result.PaletteData.Should().AllSatisfy(p => p.Should().HaveCount(4));
    }

    [Fact]
    public void FullPipeline_LoadQuantizeSave_ShouldWork()
    {
        // Arrange
        string inputPath = Path.Combine(Path.GetTempPath(), $"input_{Guid.NewGuid()}.png");
        string outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.png");

        try
        {
            // Create and save test image
            int width = 16;
            int height = 16;
            var testImage = CreateTestImage(width, height);
            testImage.SaveAsPng(inputPath);

            // Load image
            using var image = Image.Load<Rgba32>(inputPath);

            var options = new QuantizationOptions
            {
                TileWidth = 8,
                TileHeight = 8,
                PaletteCount = 2,
                ColorsPerPalette = 4,
                BitsPerChannel = 5,
                FractionOfPixels = 0.05
            };

            var quantizer = new TiledPaletteQuantizer();

            // Act
            var result = quantizer.Quantize(image, options);
            result.image.SaveAsPng(outputPath);

            // Assert
            File.Exists(outputPath).Should().BeTrue();
            var outputBytes = File.ReadAllBytes(outputPath);
            outputBytes.Should().NotBeEmpty();
            outputBytes[0].Should().Be(0x89); // PNG signature
            outputBytes[1].Should().Be(0x50); // 'P'
            outputBytes[2].Should().Be(0x4E); // 'N'
            outputBytes[3].Should().Be(0x47); // 'G'
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
        var image = CreateSolidColorImage(width, height, 128, 128, 128);

        var options = new QuantizationOptions
        {
            TileWidth = 4,
            TileHeight = 4,
            PaletteCount = 1,
            ColorsPerPalette = 2,
            FractionOfPixels = 0.1
        };

        var quantizer = new TiledPaletteQuantizer();

        // Act
        var result = quantizer.Quantize(image, options);

        // Assert
        result.Should().NotBeNull();
        result.image.Should().NotBeNull();
        result.image.Width.Should().Be(width);
        result.image.Height.Should().Be(height);
    }

    // Helper methods to create test images

    private Image<Rgba32> CreateSolidColorImage(int width, int height, byte r, byte g, byte b)
    {
        var image = new Image<Rgba32>(width, height);
        var color = new Rgba32(r, g, b, 255);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image[x, y] = color;
            }
        }
        
        return image;
    }

    private Image<Rgba32> CreateGradientImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte value = (byte)((x * 255) / (width - 1));
                image[x, y] = new Rgba32(value, value, value, 255);
            }
        }
        
        return image;
    }

    private Image<Rgba32> CreateCheckerboardImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isWhite = ((x / 8) + (y / 8)) % 2 == 0;
                byte value = (byte)(isWhite ? 255 : 0);
                image[x, y] = new Rgba32(value, value, value, 255);
            }
        }
        
        return image;
    }

    private Image<Rgba32> CreateTestImage(int width, int height)
    {
        var image = new Image<Rgba32>(width, height);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte value = (byte)((x + y) * 16);
                image[x, y] = new Rgba32(value, value, value, 255);
            }
        }
        
        return image;
    }
}

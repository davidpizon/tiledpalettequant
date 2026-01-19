using FluentAssertions;
using TiledPaletteQuant.IO;
using Xunit;

namespace TiledPaletteQuant.Tests.IO;

public class BmpWriterTests
{
    [Fact]
    public void WriteBmp_ShouldCreateValidBmpFile()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.bmp");
        int width = 8;
        int height = 8;
        var paletteData = new byte[1024]; // 256 colors * 4 bytes
        var colorIndexes = new byte[width * height];

        // Set up a simple palette (first color is red)
        paletteData[0] = 0;   // B
        paletteData[1] = 0;   // G
        paletteData[2] = 255; // R
        paletteData[3] = 0;   // Reserved

        // All pixels use first color
        for (int i = 0; i < colorIndexes.Length; i++)
        {
            colorIndexes[i] = 0;
        }

        try
        {
            // Act
            BmpWriter.WriteBmp(tempFile, width, height, paletteData, colorIndexes);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var fileBytes = File.ReadAllBytes(tempFile);

            // Check BMP signature
            fileBytes[0].Should().Be(66);  // 'B'
            fileBytes[1].Should().Be(77);  // 'M'

            // Check file size in header
            int fileSize = fileBytes[2] | (fileBytes[3] << 8) | (fileBytes[4] << 16) | (fileBytes[5] << 24);
            fileSize.Should().Be(54 + 1024 + 64); // Header + palette + pixels

            // Check width and height
            int bmpWidth = fileBytes[0x12] | (fileBytes[0x13] << 8) | (fileBytes[0x14] << 16) | (fileBytes[0x15] << 24);
            int bmpHeight = fileBytes[0x16] | (fileBytes[0x17] << 8) | (fileBytes[0x18] << 16) | (fileBytes[0x19] << 24);
            bmpWidth.Should().Be(width);
            bmpHeight.Should().Be(height);

            // Check bits per pixel
            int bitsPerPixel = fileBytes[0x1C] | (fileBytes[0x1D] << 8);
            bitsPerPixel.Should().Be(8);

            // Check palette entry (red color)
            fileBytes[54].Should().Be(0);   // B
            fileBytes[55].Should().Be(0);   // G
            fileBytes[56].Should().Be(255); // R
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void WriteBmp_ShouldHandleDifferentImageSizes()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.bmp");
        int width = 16;
        int height = 16;
        int bmpWidth = (int)Math.Ceiling(width / 4.0) * 4;
        var paletteData = new byte[1024];
        var colorIndexes = new byte[bmpWidth * height];

        try
        {
            // Act
            BmpWriter.WriteBmp(tempFile, width, height, paletteData, colorIndexes);

            // Assert
            File.Exists(tempFile).Should().BeTrue();
            var fileBytes = File.ReadAllBytes(tempFile);

            int fileSize = fileBytes[2] | (fileBytes[3] << 8) | (fileBytes[4] << 16) | (fileBytes[5] << 24);
            fileSize.Should().Be(54 + 1024 + (bmpWidth * height));
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void WriteBmp_ShouldIncludeFullPalette()
    {
        // Arrange
        string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.bmp");
        int width = 4;
        int height = 4;
        var paletteData = new byte[1024];
        var colorIndexes = new byte[width * height];

        // Create a gradient palette
        for (int i = 0; i < 256; i++)
        {
            paletteData[i * 4] = (byte)i;     // B
            paletteData[i * 4 + 1] = (byte)i; // G
            paletteData[i * 4 + 2] = (byte)i; // R
            paletteData[i * 4 + 3] = 0;       // Reserved
        }

        try
        {
            // Act
            BmpWriter.WriteBmp(tempFile, width, height, paletteData, colorIndexes);

            // Assert
            var fileBytes = File.ReadAllBytes(tempFile);

            // Check that palette is included
            for (int i = 0; i < 256; i++)
            {
                fileBytes[54 + i * 4].Should().Be((byte)i);     // B
                fileBytes[54 + i * 4 + 1].Should().Be((byte)i); // G
                fileBytes[54 + i * 4 + 2].Should().Be((byte)i); // R
            }
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}

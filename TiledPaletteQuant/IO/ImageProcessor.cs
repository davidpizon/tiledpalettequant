using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TiledPaletteQuant.IO;

/// <summary>
/// Handles loading and saving images.
/// </summary>
public static class ImageProcessor
{
    /// <summary>
    /// Loads an image from a file and returns RGBA byte array.
    /// </summary>
    public static (byte[] data, int width, int height) LoadImage(string path)
    {
        using var image = Image.Load<Rgba32>(path);
        int width = image.Width;
        int height = image.Height;
        var data = new byte[width * height * 4];

        image.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < width; x++)
                {
                    var pixel = row[x];
                    int index = 4 * (x + width * y);
                    data[index] = pixel.R;
                    data[index + 1] = pixel.G;
                    data[index + 2] = pixel.B;
                    data[index + 3] = pixel.A;
                }
            }
        });

        return (data, width, height);
    }

    /// <summary>
    /// Saves RGBA image data to a PNG file.
    /// </summary>
    public static void SavePng(string path, byte[] data, int width, int height)
    {
        using var image = Image.LoadPixelData<Rgba32>(data, width, height);
        image.SaveAsPng(path);
    }

    /// <summary>
    /// Converts RGBA data to RGB by removing alpha channel.
    /// </summary>
    public static byte[] RgbaToRgb(byte[] rgba, int width, int height)
    {
        var rgb = new byte[width * height * 3];
        for (int i = 0; i < width * height; i++)
        {
            rgb[i * 3] = rgba[i * 4];
            rgb[i * 3 + 1] = rgba[i * 4 + 1];
            rgb[i * 3 + 2] = rgba[i * 4 + 2];
        }
        return rgb;
    }
}

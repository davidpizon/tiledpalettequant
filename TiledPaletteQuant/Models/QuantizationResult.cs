namespace TiledPaletteQuant.Models;

/// <summary>
/// Result of the quantization process.
/// </summary>
public class QuantizationResult
{
    /// <summary>
    /// Generated palettes. Each palette is an array of RGB colors.
    /// </summary>
    public required List<List<double[]>> Palettes { get; init; }

    /// <summary>
    /// Quantized image data in RGBA format.
    /// </summary>
    public required byte[] ImageData { get; init; }

    /// <summary>
    /// Width of the quantized image.
    /// </summary>
    public required int Width { get; init; }

    /// <summary>
    /// Height of the quantized image.
    /// </summary>
    public required int Height { get; init; }

    /// <summary>
    /// BMP palette data (256 colors * 4 bytes each in BGR0 format).
    /// </summary>
    public byte[]? PaletteData { get; init; }

    /// <summary>
    /// Color indexes for BMP format (1 byte per pixel, row-flipped).
    /// </summary>
    public byte[]? ColorIndexes { get; init; }
}

namespace TiledPaletteQuant.Models;

/// <summary>
/// Represents a tile in the image with its unique colors and pixel data.
/// </summary>
public class TileData
{
    /// <summary>
    /// Unique colors present in this tile.
    /// </summary>
    public List<double[]> Colors { get; set; } = new();

    /// <summary>
    /// Count of pixels for each color (parallel array to Colors).
    /// </summary>
    public List<int> Counts { get; set; } = new();

    /// <summary>
    /// All pixels in this tile.
    /// </summary>
    public List<PixelData> Pixels { get; set; } = new();
}

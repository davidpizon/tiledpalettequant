namespace TiledPaletteQuant.Models;

/// <summary>
/// Represents a single pixel in a tile with its color and position.
/// </summary>
public class PixelData
{
    /// <summary>
    /// The tile this pixel belongs to.
    /// </summary>
    public required TileData Tile { get; init; }

    /// <summary>
    /// RGB color values [R, G, B].
    /// </summary>
    public required double[] Color { get; init; }

    /// <summary>
    /// X coordinate in the image.
    /// </summary>
    public required int X { get; init; }

    /// <summary>
    /// Y coordinate in the image.
    /// </summary>
    public required int Y { get; init; }
}

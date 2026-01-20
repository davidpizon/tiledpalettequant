namespace TiledPaletteQuant;

/// <summary>
/// Provides dithering pattern matrices.
/// </summary>
public static class DitherPatterns
{
    /// <summary>
    /// Gets the dithering pattern matrix for the specified pattern type.
    /// </summary>
    /// <param name="pattern">The dither pattern type.</param>
    /// <returns>A 2D array representing the dither pattern matrix.</returns>
    public static int[,] GetPattern(DitherPattern pattern)
    {
        return pattern switch
        {
            DitherPattern.Diagonal4 => new int[,] { { 0, 2 }, { 3, 1 } },
            DitherPattern.Horizontal4 => new int[,] { { 0, 3 }, { 1, 2 } },
            DitherPattern.Vertical4 => new int[,] { { 0, 1 }, { 3, 2 } },
            DitherPattern.Diagonal2 => new int[,] { { 0, 1 }, { 1, 0 } },
            DitherPattern.Horizontal2 => new int[,] { { 0, 1 }, { 0, 1 } },
            DitherPattern.Vertical2 => new int[,] { { 0, 0 }, { 1, 1 } },
            _ => new int[,] { { 0, 2 }, { 3, 1 } }
        };
    }
    
    /// <summary>
    /// Gets the number of dither levels for the specified pattern.
    /// </summary>
    /// <param name="pattern">The dither pattern type.</param>
    /// <returns>The number of dither levels (2 or 4).</returns>
    public static int GetDitherPixels(DitherPattern pattern)
    {
        return pattern switch
        {
            DitherPattern.Diagonal2 => 2,
            DitherPattern.Horizontal2 => 2,
            DitherPattern.Vertical2 => 2,
            _ => 4
        };
    }
}

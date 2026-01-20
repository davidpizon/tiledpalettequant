namespace TiledPaletteQuant.Models;

/// <summary>
/// Defines how the first color (index 0) in each palette should be treated.
/// </summary>
public enum ColorZeroBehavior
{
    /// <summary>
    /// Each palette has its own unique color at index 0.
    /// </summary>
    Unique = 0,

    /// <summary>
    /// All palettes share the same color at index 0.
    /// </summary>
    Shared = 1,

    /// <summary>
    /// Color index 0 is transparent, determined by alpha channel &lt; 255.
    /// </summary>
    TransparentFromTransparent = 2,

    /// <summary>
    /// Color index 0 is transparent, determined by matching a specific RGB value.
    /// </summary>
    TransparentFromColor = 3
}

/// <summary>
/// Defines the dithering mode for quantization.
/// </summary>
public enum DitherMode
{
    /// <summary>
    /// No dithering applied.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Fast dithering with reduced iterations.
    /// </summary>
    Fast = 1,

    /// <summary>
    /// Slow, high-quality dithering with more iterations and refinement.
    /// </summary>
    Slow = 2
}

/// <summary>
/// Defines the spatial pattern for ordered dithering.
/// </summary>
public enum DitherPattern
{
    /// <summary>
    /// 4-level diagonal pattern: [[0,2], [3,1]]
    /// </summary>
    Diagonal4 = 0,

    /// <summary>
    /// 4-level horizontal pattern: [[0,3], [1,2]]
    /// </summary>
    Horizontal4 = 1,

    /// <summary>
    /// 4-level vertical pattern: [[0,1], [3,2]]
    /// </summary>
    Vertical4 = 2,

    /// <summary>
    /// 2-level diagonal pattern: [[0,1], [1,0]]
    /// </summary>
    Diagonal2 = 3,

    /// <summary>
    /// 2-level horizontal pattern: [[0,1], [0,1]]
    /// </summary>
    Horizontal2 = 4,

    /// <summary>
    /// 2-level vertical pattern: [[0,0], [1,1]]
    /// </summary>
    Vertical2 = 5
}

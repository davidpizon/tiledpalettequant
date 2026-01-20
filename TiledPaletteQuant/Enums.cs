namespace TiledPaletteQuant;

/// <summary>
/// Defines how color zero (the first color in each palette) is handled.
/// </summary>
public enum ColorZeroBehavior
{
    /// <summary>
    /// Each palette has its own unique color zero.
    /// </summary>
    Unique = 0,
    
    /// <summary>
    /// All palettes share the same color zero value.
    /// </summary>
    Shared = 1,
    
    /// <summary>
    /// Transparent pixels in the input become transparent in output.
    /// </summary>
    TransparentFromTransparent = 2,
    
    /// <summary>
    /// A specific color value becomes transparent in output.
    /// </summary>
    TransparentFromColor = 3
}

/// <summary>
/// Dithering mode for quantization.
/// </summary>
public enum DitherMode
{
    /// <summary>
    /// No dithering applied.
    /// </summary>
    Off = 0,
    
    /// <summary>
    /// Fast dithering mode.
    /// </summary>
    Fast = 1,
    
    /// <summary>
    /// Slow but higher quality dithering mode.
    /// </summary>
    Slow = 2
}

/// <summary>
/// Dithering pattern to use.
/// </summary>
public enum DitherPattern
{
    /// <summary>
    /// 4-level diagonal pattern.
    /// </summary>
    Diagonal4 = 0,
    
    /// <summary>
    /// 4-level horizontal pattern.
    /// </summary>
    Horizontal4 = 1,
    
    /// <summary>
    /// 4-level vertical pattern.
    /// </summary>
    Vertical4 = 2,
    
    /// <summary>
    /// 2-level diagonal pattern.
    /// </summary>
    Diagonal2 = 3,
    
    /// <summary>
    /// 2-level horizontal pattern.
    /// </summary>
    Horizontal2 = 4,
    
    /// <summary>
    /// 2-level vertical pattern.
    /// </summary>
    Vertical2 = 5
}

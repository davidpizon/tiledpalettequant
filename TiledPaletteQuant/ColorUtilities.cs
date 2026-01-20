namespace TiledPaletteQuant;

/// <summary>
/// Utility functions for color manipulation.
/// </summary>
public static class ColorUtilities
{
    /// <summary>
    /// Alpha values for bit depth reduction: 255 / (2^n - 1).
    /// </summary>
    private static readonly double[] AlphaValues = { 0, 255, 85, 36.42857, 17, 8.22581, 4.04762, 2.00787, 1 };
    
    /// <summary>
    /// Brightness scale for weighted brightness calculation (0.299R + 0.587G + 0.114B).
    /// </summary>
    private static readonly double[] BrightnessScale = { 0.299, 0.587, 0.114 };
    
    /// <summary>
    /// Calculates the weighted RGB distance between two colors.
    /// Uses formula: 2*(r1-r2)² + 4*(g1-g2)² + (b1-b2)².
    /// </summary>
    /// <param name="a">First color.</param>
    /// <param name="b">Second color.</param>
    /// <returns>The distance value.</returns>
    public static double ColorDistance(double[] a, double[] b)
    {
        double dr = a[0] - b[0];
        double dg = a[1] - b[1];
        double db = a[2] - b[2];
        return 2 * dr * dr + 4 * dg * dg + db * db;
    }
    
    /// <summary>
    /// Creates a deep copy of a color array.
    /// </summary>
    /// <param name="color">The color to clone.</param>
    /// <returns>A new color array with the same values.</returns>
    public static double[] CloneColor(double[] color)
    {
        return new double[] { color[0], color[1], color[2] };
    }
    
    /// <summary>
    /// Copies color values from source to destination.
    /// </summary>
    /// <param name="dest">Destination color.</param>
    /// <param name="source">Source color.</param>
    public static void CopyColor(double[] dest, double[] source)
    {
        dest[0] = source[0];
        dest[1] = source[1];
        dest[2] = source[2];
    }
    
    /// <summary>
    /// Adds c2 to c1 (c1 = c1 + c2).
    /// </summary>
    /// <param name="c1">First color (modified).</param>
    /// <param name="c2">Second color.</param>
    public static void AddColor(double[] c1, double[] c2)
    {
        c1[0] += c2[0];
        c1[1] += c2[1];
        c1[2] += c2[2];
    }
    
    /// <summary>
    /// Subtracts c2 from c1 (c1 = c1 - c2).
    /// </summary>
    /// <param name="c1">First color (modified).</param>
    /// <param name="c2">Second color.</param>
    public static void SubtractColor(double[] c1, double[] c2)
    {
        c1[0] -= c2[0];
        c1[1] -= c2[1];
        c1[2] -= c2[2];
    }
    
    /// <summary>
    /// Scales a color by a factor.
    /// </summary>
    /// <param name="color">The color to scale (modified).</param>
    /// <param name="factor">Scale factor.</param>
    public static void ScaleColor(double[] color, double factor)
    {
        color[0] *= factor;
        color[1] *= factor;
        color[2] *= factor;
    }
    
    /// <summary>
    /// Clamps each color component to the specified range.
    /// </summary>
    /// <param name="color">The color to clamp (modified).</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    public static void ClampColor(double[] color, double min, double max)
    {
        for (int i = 0; i < 3; i++)
        {
            if (color[i] < min)
                color[i] = min;
            else if (color[i] > max)
                color[i] = max;
        }
    }
    
    /// <summary>
    /// Reduces a color value to n-bit depth.
    /// </summary>
    /// <param name="value">The color value (0-255).</param>
    /// <param name="n">Number of bits.</param>
    /// <returns>Reduced color value.</returns>
    public static double ToNbit(double value, int n)
    {
        double alpha = AlphaValues[n];
        return Math.Round(Math.Round(value / alpha) * alpha);
    }
    
    /// <summary>
    /// Reduces all color components to n-bit depth.
    /// </summary>
    /// <param name="color">The color to reduce (modified).</param>
    /// <param name="n">Number of bits.</param>
    public static void ToNbitColor(double[] color, int n)
    {
        color[0] = ToNbit(color[0], n);
        color[1] = ToNbit(color[1], n);
        color[2] = ToNbit(color[2], n);
    }
    
    /// <summary>
    /// Moves a color closer to a target color by an alpha factor.
    /// </summary>
    /// <param name="color">The color to move (modified).</param>
    /// <param name="target">Target color.</param>
    /// <param name="alpha">Interpolation factor (0-1).</param>
    public static void MoveCloser(double[] color, double[] target, double alpha)
    {
        color[0] = (1 - alpha) * color[0] + alpha * target[0];
        color[1] = (1 - alpha) * color[1] + alpha * target[1];
        color[2] = (1 - alpha) * color[2] + alpha * target[2];
    }
    
    /// <summary>
    /// Converts sRGB value to linear space.
    /// </summary>
    /// <param name="x">sRGB value.</param>
    /// <returns>Linear value.</returns>
    public static double ToLinear(double x)
    {
        return x * x;
    }
    
    /// <summary>
    /// Converts linear value to sRGB space.
    /// </summary>
    /// <param name="x">Linear value.</param>
    /// <returns>sRGB value.</returns>
    public static double ToSrgb(double x)
    {
        return Math.Sqrt(x);
    }
    
    /// <summary>
    /// Converts color to linear space.
    /// </summary>
    /// <param name="color">The color to convert (modified).</param>
    public static void ToLinearColor(double[] color)
    {
        color[0] = ToLinear(color[0]);
        color[1] = ToLinear(color[1]);
        color[2] = ToLinear(color[2]);
    }
    
    /// <summary>
    /// Converts color to sRGB space.
    /// </summary>
    /// <param name="color">The color to convert (modified).</param>
    public static void ToSrgbColor(double[] color)
    {
        color[0] = ToSrgb(color[0]);
        color[1] = ToSrgb(color[1]);
        color[2] = ToSrgb(color[2]);
    }
    
    /// <summary>
    /// Calculates the weighted brightness of a color.
    /// </summary>
    /// <param name="color">The color.</param>
    /// <returns>Brightness value.</returns>
    public static double Brightness(double[] color)
    {
        double sum = 0;
        for (int i = 0; i < 3; i++)
        {
            sum += BrightnessScale[i] * ToLinear(color[i]);
        }
        return sum;
    }
    
    /// <summary>
    /// Checks if two colors are equal.
    /// </summary>
    /// <param name="c1">First color.</param>
    /// <param name="c2">Second color.</param>
    /// <returns>True if colors are equal.</returns>
    public static bool EqualColors(double[] c1, double[] c2)
    {
        return c1[0] == c2[0] && c1[1] == c2[1] && c1[2] == c2[2];
    }
}

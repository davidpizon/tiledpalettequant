namespace TiledPaletteQuant.Core;

/// <summary>
/// Utility methods for color operations and conversions.
/// </summary>
public static class ColorUtils
{
    // Brightness scale factors [R, G, B]
    private static readonly double[] BrightnessScale = new[] { 0.299, 0.587, 0.114 };

    // Alpha values for n-bit quantization: 255 / (2^n - 1)
    private static readonly double[] AlphaValues = new[]
    {
        0.0,        // n=0 (unused)
        255.0,      // n=1
        85.0,       // n=2
        36.42857,   // n=3
        17.0,       // n=4
        8.22581,    // n=5
        4.04762,    // n=6
        2.00787,    // n=7
        1.0         // n=8
    };

    /// <summary>
    /// Calculates the weighted Euclidean distance between two colors.
    /// Uses weights: R=2, G=4, B=1 (emphasizes green channel).
    /// </summary>
    public static double ColorDistance(double[] a, double[] b)
    {
        // From worker.js line 812:
        // return 2 * Math.pow((a[0] - b[0]), 2) + 4 * Math.pow((a[1] - b[1]), 2) + Math.pow((a[2] - b[2]), 2);
        double dr = a[0] - b[0];
        double dg = a[1] - b[1];
        double db = a[2] - b[2];
        return 2 * dr * dr + 4 * dg * dg + db * db;
    }

    /// <summary>
    /// Quantizes a color value to n-bit precision.
    /// </summary>
    public static double ToNbit(double value, int n)
    {
        // From worker.js lines 1211-1214
        double alpha = AlphaValues[n];
        return Math.Round(Math.Round(value / alpha) * alpha);
    }

    /// <summary>
    /// Quantizes an RGB color to n-bit precision per channel.
    /// </summary>
    public static void ToNbitColor(double[] color, int n)
    {
        for (int i = 0; i < 3; i++)
        {
            color[i] = ToNbit(color[i], n);
        }
    }

    /// <summary>
    /// Converts sRGB to linear color space (gamma expansion).
    /// </summary>
    public static double ToLinear(double x)
    {
        // From worker.js line 497-499
        return x * x;
    }

    /// <summary>
    /// Converts an sRGB color to linear color space in-place.
    /// </summary>
    public static void ToLinearColor(double[] color)
    {
        // From worker.js lines 500-503
        for (int i = 0; i < color.Length; i++)
        {
            color[i] = ToLinear(color[i]);
        }
    }

    /// <summary>
    /// Converts linear to sRGB color space (gamma compression).
    /// </summary>
    public static double ToSrgb(double x)
    {
        // From worker.js line 501-503
        return Math.Sqrt(x);
    }

    /// <summary>
    /// Converts a linear color to sRGB color space in-place.
    /// </summary>
    public static void ToSrgbColor(double[] color)
    {
        // From worker.js lines 504-507
        for (int i = 0; i < color.Length; i++)
        {
            color[i] = ToSrgb(color[i]);
        }
    }

    /// <summary>
    /// Calculates the perceived brightness of a color.
    /// </summary>
    public static double Brightness(double[] color)
    {
        // From worker.js lines 510-515
        double sum = 0;
        for (int i = 0; i < 3; i++)
        {
            sum += BrightnessScale[i] * ToLinear(color[i]);
        }
        return sum;
    }

    /// <summary>
    /// Creates a deep copy of a color array.
    /// </summary>
    public static double[] CloneColor(double[] color)
    {
        // From worker.js lines 1155-1161
        return new[] { color[0], color[1], color[2] };
    }

    /// <summary>
    /// Copies color values from source to destination.
    /// </summary>
    public static void CopyColor(double[] dest, double[] src)
    {
        // From worker.js lines 1163-1167
        for (int i = 0; i < 3; i++)
        {
            dest[i] = src[i];
        }
    }

    /// <summary>
    /// Adds color c2 to color c1 in-place.
    /// </summary>
    public static void AddColor(double[] c1, double[] c2)
    {
        // From worker.js lines 1169-1173
        for (int i = 0; i < 3; i++)
        {
            c1[i] += c2[i];
        }
    }

    /// <summary>
    /// Subtracts color c2 from color c1 in-place.
    /// </summary>
    public static void SubtractColor(double[] c1, double[] c2)
    {
        // From worker.js lines 1175-1179
        for (int i = 0; i < 3; i++)
        {
            c1[i] -= c2[i];
        }
    }

    /// <summary>
    /// Scales a color by a scalar factor in-place.
    /// </summary>
    public static void ScaleColor(double[] color, double scaleFactor)
    {
        // From worker.js lines 1181-1185
        for (int i = 0; i < 3; i++)
        {
            color[i] *= scaleFactor;
        }
    }

    /// <summary>
    /// Clamps each color channel to the specified range in-place.
    /// </summary>
    public static void ClampColor(double[] color, double minValue, double maxValue)
    {
        // From worker.js lines 1187-1197
        for (int i = 0; i < 3; i++)
        {
            if (color[i] < minValue)
            {
                color[i] = minValue;
            }
            else if (color[i] > maxValue)
            {
                color[i] = maxValue;
            }
        }
    }

    /// <summary>
    /// Checks if two colors are equal.
    /// </summary>
    public static bool EqualColors(double[] a, double[] b)
    {
        return a[0] == b[0] && a[1] == b[1] && a[2] == b[2];
    }

    /// <summary>
    /// Moves a color closer to a target pixel color by alpha amount.
    /// </summary>
    public static void MoveColorCloser(double[] color, double[] pixelColor, double alpha)
    {
        // From worker.js lines 1220-1224
        for (int i = 0; i < color.Length; i++)
        {
            color[i] = (1 - alpha) * color[i] + alpha * pixelColor[i];
        }
    }
}

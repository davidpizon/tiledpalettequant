using TiledPaletteQuant.Models;

namespace TiledPaletteQuant.Core;

/// <summary>
/// Handles dithering operations including pattern-based ordered dithering and error diffusion.
/// </summary>
public class DitherEngine
{
    private readonly QuantizationOptions _options;
    private readonly int[,] _ditherPattern;
    private readonly int _ditherPixels;

    /// <summary>
    /// Dither pattern matrices from worker.js lines 33-56.
    /// </summary>
    private static readonly Dictionary<DitherPattern, int[,]> DitherPatterns = new()
    {
        { DitherPattern.Diagonal4, new int[,] { { 0, 2 }, { 3, 1 } } },
        { DitherPattern.Horizontal4, new int[,] { { 0, 3 }, { 1, 2 } } },
        { DitherPattern.Vertical4, new int[,] { { 0, 1 }, { 3, 2 } } },
        { DitherPattern.Diagonal2, new int[,] { { 0, 1 }, { 1, 0 } } },
        { DitherPattern.Horizontal2, new int[,] { { 0, 1 }, { 0, 1 } } },
        { DitherPattern.Vertical2, new int[,] { { 0, 0 }, { 1, 1 } } }
    };

    public DitherEngine(QuantizationOptions options)
    {
        _options = options;
        _ditherPattern = DitherPatterns[options.DitherPattern];

        // Determine number of dither levels (2 or 4)
        _ditherPixels = options.DitherPattern switch
        {
            DitherPattern.Diagonal2 or DitherPattern.Horizontal2 or DitherPattern.Vertical2 => 2,
            _ => 4
        };
    }

    /// <summary>
    /// Candidate color for dithering with metadata.
    /// </summary>
    private class DitherCandidate
    {
        public int ColorIndex { get; set; }
        public double ColorDistance { get; set; }
        public double[] ComparedColor { get; set; } = new double[3];
        public double Brightness { get; set; }
    }

    /// <summary>
    /// Finds the closest color in a palette with error diffusion for dithering.
    /// From worker.js lines 768-810.
    /// </summary>
    public (int colorIndex, double distance, double[] comparedColor) ClosestColorDither(
        List<double[]> palette, PixelData pixel)
    {
        double[] error = new double[3];
        double[] linearPixel = ColorUtils.CloneColor(pixel.Color);
        ColorUtils.ToLinearColor(linearPixel);

        var candidates = new List<DitherCandidate>();
        double[] c = new double[3];
        double[] err = new double[3];
        double[] reducedColor = new double[3];

        for (int i = 0; i < _ditherPixels; i++)
        {
            ColorUtils.CopyColor(c, linearPixel);
            ColorUtils.CopyColor(err, error);
            ColorUtils.ScaleColor(err, _options.DitherWeight);
            ColorUtils.AddColor(c, err);
            ColorUtils.ClampColor(c, 0, 255 * 255);
            ColorUtils.ToSrgbColor(c);

            var (minColorIndex, minDist) = GetClosestColor(palette, c);
            var minColor = palette[minColorIndex];

            candidates.Add(new DitherCandidate
            {
                ColorIndex = minColorIndex,
                ColorDistance = minDist,
                ComparedColor = ColorUtils.CloneColor(c),
                Brightness = ColorUtils.Brightness(minColor)
            });

            ColorUtils.CopyColor(reducedColor, minColor);
            ColorUtils.ToNbitColor(reducedColor, _options.BitsPerChannel);
            ColorUtils.ToLinearColor(reducedColor);
            ColorUtils.AddColor(error, linearPixel);
            ColorUtils.SubtractColor(error, reducedColor);
        }

        // Sort candidates by brightness (bubble sort as in original)
        for (int i = 0; i < _ditherPixels - 1; i++)
        {
            for (int j = i + 1; j < _ditherPixels; j++)
            {
                if (candidates[i].Brightness > candidates[j].Brightness)
                {
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            }
        }

        // Select based on dither pattern
        int index = _ditherPattern[pixel.X & 1, pixel.Y & 1];
        return (candidates[index].ColorIndex, candidates[index].ColorDistance, candidates[index].ComparedColor);
    }

    /// <summary>
    /// Finds the closest color in a palette without dithering.
    /// </summary>
    public static (int index, double distance) GetClosestColor(List<double[]> palette, double[] color)
    {
        // From worker.js lines 755-767
        int minIndex = 0;
        double minDist = ColorUtils.ColorDistance(palette[0], color);

        for (int i = 1; i < palette.Count; i++)
        {
            double dist = ColorUtils.ColorDistance(palette[i], color);
            if (dist < minDist)
            {
                minIndex = i;
                minDist = dist;
            }
        }

        return (minIndex, minDist);
    }
}

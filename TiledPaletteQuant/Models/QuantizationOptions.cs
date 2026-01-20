namespace TiledPaletteQuant.Models;

/// <summary>
/// Configuration options for the tiled palette quantization algorithm.
/// </summary>
public class QuantizationOptions
{
    /// <summary>
    /// Width of each tile in pixels.
    /// </summary>
    public int TileWidth { get; set; } = 8;

    /// <summary>
    /// Height of each tile in pixels.
    /// </summary>
    public int TileHeight { get; set; } = 8;

    /// <summary>
    /// Number of palettes to generate.
    /// </summary>
    public int PaletteCount { get; set; } = 8;

    /// <summary>
    /// Number of colors per palette.
    /// </summary>
    public int ColorsPerPalette { get; set; } = 4;

    /// <summary>
    /// Bits per color channel for quantization (2-8).
    /// </summary>
    public int BitsPerChannel { get; set; } = 5;

    /// <summary>
    /// Fraction of pixels to use for iterative refinement (0.0-1.0).
    /// </summary>
    public double FractionOfPixels { get; set; } = 0.1;

    /// <summary>
    /// Behavior for color index 0 in palettes.
    /// </summary>
    public ColorZeroBehavior ColorZeroBehavior { get; set; } = ColorZeroBehavior.Unique;

    /// <summary>
    /// RGB value to treat as transparent when ColorZeroBehavior is TransparentFromColor.
    /// </summary>
    public double[] ColorZeroValue { get; set; } = new double[] { 0, 0, 0 };

    /// <summary>
    /// Dithering mode.
    /// </summary>
    public DitherMode DitherMode { get; set; } = DitherMode.Off;

    /// <summary>
    /// Weight for dither error diffusion (0.0-1.0).
    /// </summary>
    public double DitherWeight { get; set; } = 0.5;

    /// <summary>
    /// Spatial pattern for ordered dithering.
    /// </summary>
    public DitherPattern DitherPattern { get; set; } = DitherPattern.Diagonal4;

    /// <summary>
    /// Input image file path.
    /// </summary>
    public string? InputPath { get; set; }

    /// <summary>
    /// Output image file path.
    /// </summary>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Parses command-line arguments into QuantizationOptions.
    /// </summary>
    public static QuantizationOptions ParseArguments(string[] args)
    {
        var options = new QuantizationOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i" or "--input":
                    options.InputPath = GetNextArg(args, i, "input path");
                    i++;
                    break;
                case "-o" or "--output":
                    options.OutputPath = GetNextArg(args, i, "output path");
                    i++;
                    break;
                case "-tw" or "--tile-width":
                    options.TileWidth = int.Parse(GetNextArg(args, i, "tile width"));
                    i++;
                    break;
                case "-th" or "--tile-height":
                    options.TileHeight = int.Parse(GetNextArg(args, i, "tile height"));
                    i++;
                    break;
                case "-p" or "--palettes":
                    options.PaletteCount = int.Parse(GetNextArg(args, i, "palette count"));
                    i++;
                    break;
                case "-c" or "--colors":
                    options.ColorsPerPalette = int.Parse(GetNextArg(args, i, "colors per palette"));
                    i++;
                    break;
                case "-b" or "--bits":
                    options.BitsPerChannel = int.Parse(GetNextArg(args, i, "bits per channel"));
                    i++;
                    break;
                case "-d" or "--dither":
                    options.DitherMode = ParseDitherMode(GetNextArg(args, i, "dither mode"));
                    i++;
                    break;
                case "-dp" or "--dither-pattern":
                    options.DitherPattern = ParseDitherPattern(GetNextArg(args, i, "dither pattern"));
                    i++;
                    break;
                case "-f" or "--fraction":
                    options.FractionOfPixels = double.Parse(GetNextArg(args, i, "fraction of pixels"));
                    i++;
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Validates the options and throws an exception if invalid.
    /// </summary>
    public void Validate()
    {
        if (TileWidth < 1 || TileWidth > 256)
            throw new ArgumentException("Tile width must be between 1 and 256.");
        if (TileHeight < 1 || TileHeight > 256)
            throw new ArgumentException("Tile height must be between 1 and 256.");
        if (PaletteCount < 1 || PaletteCount > 256)
            throw new ArgumentException("Palette count must be between 1 and 256.");
        if (ColorsPerPalette < 2 || ColorsPerPalette > 256)
            throw new ArgumentException("Colors per palette must be between 2 and 256.");
        if (BitsPerChannel < 2 || BitsPerChannel > 8)
            throw new ArgumentException("Bits per channel must be between 2 and 8.");
        if (FractionOfPixels < 0.0 || FractionOfPixels > 1.0)
            throw new ArgumentException("Fraction of pixels must be between 0.0 and 1.0.");
        if (DitherWeight < 0.0 || DitherWeight > 1.0)
            throw new ArgumentException("Dither weight must be between 0.0 and 1.0.");
    }

    private static string GetNextArg(string[] args, int index, string name)
    {
        if (index + 1 >= args.Length)
            throw new ArgumentException($"Missing value for {name}");
        return args[index + 1];
    }

    private static DitherMode ParseDitherMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "off" => DitherMode.Off,
            "fast" => DitherMode.Fast,
            "slow" => DitherMode.Slow,
            _ => throw new ArgumentException($"Invalid dither mode: {value}. Use 'off', 'fast', or 'slow'.")
        };
    }

    private static DitherPattern ParseDitherPattern(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "diagonal4" => DitherPattern.Diagonal4,
            "horizontal4" => DitherPattern.Horizontal4,
            "vertical4" => DitherPattern.Vertical4,
            "diagonal2" => DitherPattern.Diagonal2,
            "horizontal2" => DitherPattern.Horizontal2,
            "vertical2" => DitherPattern.Vertical2,
            _ => throw new ArgumentException($"Invalid dither pattern: {value}.")
        };
    }
}

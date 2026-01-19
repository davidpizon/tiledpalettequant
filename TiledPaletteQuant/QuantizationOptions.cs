namespace TiledPaletteQuant;

/// <summary>
/// Configuration options for tiled palette quantization.
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
    /// Bits per color channel (affects color depth reduction).
    /// </summary>
    public int BitsPerChannel { get; set; } = 5;
    
    /// <summary>
    /// Fraction of pixels to use for iterative refinement (0.0-1.0).
    /// </summary>
    public double FractionOfPixels { get; set; } = 0.1;
    
    /// <summary>
    /// How to handle color zero in palettes.
    /// </summary>
    public ColorZeroBehavior ColorZeroBehavior { get; set; } = ColorZeroBehavior.Unique;
    
    /// <summary>
    /// The shared or transparent color value (RGB).
    /// </summary>
    public double[] ColorZeroValue { get; set; } = new double[] { 0, 0, 0 };
    
    /// <summary>
    /// Dithering mode.
    /// </summary>
    public DitherMode Dither { get; set; } = DitherMode.Off;
    
    /// <summary>
    /// Weight for dithering error diffusion (0.0-1.0).
    /// </summary>
    public double DitherWeight { get; set; } = 0.5;
    
    /// <summary>
    /// Dithering pattern to use.
    /// </summary>
    public DitherPattern DitherPattern { get; set; } = DitherPattern.Diagonal4;
    
    /// <summary>
    /// Input image path.
    /// </summary>
    public string InputPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Output image path.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Parses command-line arguments into QuantizationOptions.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Parsed options, or null if parsing failed.</returns>
    public static QuantizationOptions? ParseArguments(string[] args)
    {
        var options = new QuantizationOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "-i":
                case "--input":
                    if (i + 1 < args.Length)
                        options.InputPath = args[++i];
                    break;
                    
                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                        options.OutputPath = args[++i];
                    break;
                    
                case "-tw":
                case "--tile-width":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int tw))
                        options.TileWidth = tw;
                    break;
                    
                case "-th":
                case "--tile-height":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int th))
                        options.TileHeight = th;
                    break;
                    
                case "-p":
                case "--palettes":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int p))
                        options.PaletteCount = p;
                    break;
                    
                case "-c":
                case "--colors":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int c))
                        options.ColorsPerPalette = c;
                    break;
                    
                case "-b":
                case "--bits":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int b))
                        options.BitsPerChannel = b;
                    break;
                    
                case "-f":
                case "--fraction":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double f))
                        options.FractionOfPixels = f;
                    break;
                    
                case "-d":
                case "--dither":
                    if (i + 1 < args.Length)
                    {
                        string ditherValue = args[++i].ToLower();
                        options.Dither = ditherValue switch
                        {
                            "off" => DitherMode.Off,
                            "fast" => DitherMode.Fast,
                            "slow" => DitherMode.Slow,
                            _ => DitherMode.Off
                        };
                    }
                    break;
                    
                case "-dp":
                case "--dither-pattern":
                    if (i + 1 < args.Length)
                    {
                        string pattern = args[++i].ToLower();
                        options.DitherPattern = pattern switch
                        {
                            "diagonal4" => DitherPattern.Diagonal4,
                            "horizontal4" => DitherPattern.Horizontal4,
                            "vertical4" => DitherPattern.Vertical4,
                            "diagonal2" => DitherPattern.Diagonal2,
                            "horizontal2" => DitherPattern.Horizontal2,
                            "vertical2" => DitherPattern.Vertical2,
                            _ => DitherPattern.Diagonal4
                        };
                    }
                    break;
                    
                case "-dw":
                case "--dither-weight":
                    if (i + 1 < args.Length && double.TryParse(args[++i], out double dw))
                        options.DitherWeight = dw;
                    break;
                    
                case "-cz":
                case "--color-zero":
                    if (i + 1 < args.Length)
                    {
                        string czValue = args[++i].ToLower();
                        options.ColorZeroBehavior = czValue switch
                        {
                            "unique" => ColorZeroBehavior.Unique,
                            "shared" => ColorZeroBehavior.Shared,
                            "transparentfromtransparent" => ColorZeroBehavior.TransparentFromTransparent,
                            "transparentfromcolor" => ColorZeroBehavior.TransparentFromColor,
                            _ => ColorZeroBehavior.Unique
                        };
                    }
                    break;
                    
                case "-sc":
                case "--shared-color":
                    if (i + 1 < args.Length)
                    {
                        var color = ParseHexColor(args[++i]);
                        if (color != null)
                            options.ColorZeroValue = color;
                    }
                    break;
                    
                case "-tc":
                case "--transparent-color":
                    if (i + 1 < args.Length)
                    {
                        var color = ParseHexColor(args[++i]);
                        if (color != null)
                            options.ColorZeroValue = color;
                    }
                    break;
                    
                case "-h":
                case "--help":
                    PrintHelp();
                    return null;
            }
        }
        
        return options;
    }
    
    /// <summary>
    /// Parses a hex color string (e.g., "FF0000" or "#FF0000") to RGB values.
    /// </summary>
    private static double[]? ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            return null;
            
        try
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return new double[] { r, g, b };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Prints command-line help.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("Tiled Palette Quantization - Convert images to tiled palette format");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  TiledPaletteQuant -i <input> [-o <output>] [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -i, --input <path>            Input image path (required)");
        Console.WriteLine("  -o, --output <path>           Output image path (default: <input>_quantized.png)");
        Console.WriteLine("  -tw, --tile-width <n>         Tile width (default: 8)");
        Console.WriteLine("  -th, --tile-height <n>        Tile height (default: 8)");
        Console.WriteLine("  -p, --palettes <n>            Number of palettes (default: 8)");
        Console.WriteLine("  -c, --colors <n>              Colors per palette (default: 4)");
        Console.WriteLine("  -b, --bits <n>                Bits per channel (default: 5)");
        Console.WriteLine("  -f, --fraction <f>            Fraction of pixels for refinement (default: 0.1)");
        Console.WriteLine("  -d, --dither <mode>           Dither mode: off, fast, slow (default: off)");
        Console.WriteLine("  -dp, --dither-pattern <pat>   Dither pattern: diagonal4, horizontal4, vertical4,");
        Console.WriteLine("                                diagonal2, horizontal2, vertical2 (default: diagonal4)");
        Console.WriteLine("  -dw, --dither-weight <w>      Dither weight (default: 0.5)");
        Console.WriteLine("  -cz, --color-zero <mode>      Color zero behavior: unique, shared,");
        Console.WriteLine("                                transparentfromtransparent, transparentfromcolor");
        Console.WriteLine("                                (default: unique)");
        Console.WriteLine("  -sc, --shared-color <rgb>     Shared color in hex format (e.g., FF0000)");
        Console.WriteLine("  -tc, --transparent-color <rgb> Transparent color in hex format (e.g., FF00FF)");
        Console.WriteLine("  -h, --help                    Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  TiledPaletteQuant -i input.png -o output.png");
        Console.WriteLine("  TiledPaletteQuant -i input.png -tw 16 -th 16 -p 4 -c 16 -b 5");
        Console.WriteLine("  TiledPaletteQuant -i input.png -d slow -dp diagonal4 -dw 0.5");
    }
}

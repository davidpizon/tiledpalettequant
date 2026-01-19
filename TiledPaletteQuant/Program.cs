using TiledPaletteQuant.Core;
using TiledPaletteQuant.IO;
using TiledPaletteQuant.Models;

namespace TiledPaletteQuant;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Tiled Palette Quantization Tool");
        Console.WriteLine("================================\n");

        try
        {
            QuantizationOptions options;

            if (args.Length == 0)
            {
                // Interactive mode
                options = InteractiveMode();
            }
            else
            {
                // Command-line mode
                options = QuantizationOptions.ParseArguments(args);
            }

            if (string.IsNullOrEmpty(options.InputPath))
            {
                Console.WriteLine("Error: Input path is required.");
                PrintUsage();
                return;
            }

            if (!File.Exists(options.InputPath))
            {
                Console.WriteLine($"Error: Input file not found: {options.InputPath}");
                return;
            }

            // Validate options
            options.Validate();

            // Set default output path if not specified
            if (string.IsNullOrEmpty(options.OutputPath))
            {
                string ext = (options.PaletteCount * options.ColorsPerPalette <= 256) ? ".bmp" : ".png";
                options.OutputPath = Path.ChangeExtension(options.InputPath, "_quantized" + ext);
            }

            // Load image
            Console.WriteLine($"Loading image: {options.InputPath}");
            var (imageData, width, height) = ImageProcessor.LoadImage(options.InputPath);
            Console.WriteLine($"Image size: {width}x{height}");

            // Quantize
            var quantizer = new TiledPaletteQuantizer(options, progress =>
            {
                Console.Write($"\rProgress: {progress}%   ");
            });

            var result = quantizer.Quantize(imageData, width, height);
            Console.WriteLine("\n");

            // Save output
            int totalColors = options.PaletteCount * options.ColorsPerPalette;
            if (totalColors <= 256)
            {
                Console.WriteLine($"Saving indexed BMP: {options.OutputPath}");
                BmpWriter.WriteBmp(options.OutputPath, width, height, 
                    result.PaletteData!, result.ColorIndexes!);
            }
            else
            {
                Console.WriteLine($"Saving PNG (>256 colors): {options.OutputPath}");
                ImageProcessor.SavePng(options.OutputPath, result.ImageData, width, height);
            }

            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (args.Length > 0)
            {
                PrintUsage();
            }
        }
    }

    static QuantizationOptions InteractiveMode()
    {
        var options = new QuantizationOptions();

        Console.Write("Input image path: ");
        options.InputPath = Console.ReadLine()?.Trim();

        Console.Write("Output image path (leave empty for auto): ");
        var output = Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(output))
            options.OutputPath = output;

        Console.Write($"Tile width (default {options.TileWidth}): ");
        if (int.TryParse(Console.ReadLine(), out int tw))
            options.TileWidth = tw;

        Console.Write($"Tile height (default {options.TileHeight}): ");
        if (int.TryParse(Console.ReadLine(), out int th))
            options.TileHeight = th;

        Console.Write($"Number of palettes (default {options.PaletteCount}): ");
        if (int.TryParse(Console.ReadLine(), out int p))
            options.PaletteCount = p;

        Console.Write($"Colors per palette (default {options.ColorsPerPalette}): ");
        if (int.TryParse(Console.ReadLine(), out int c))
            options.ColorsPerPalette = c;

        Console.Write($"Bits per channel (default {options.BitsPerChannel}): ");
        if (int.TryParse(Console.ReadLine(), out int b))
            options.BitsPerChannel = b;

        Console.Write("Dither mode (off/fast/slow, default off): ");
        var dither = Console.ReadLine()?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(dither))
        {
            options.DitherMode = dither switch
            {
                "fast" => DitherMode.Fast,
                "slow" => DitherMode.Slow,
                _ => DitherMode.Off
            };
        }

        Console.WriteLine();
        return options;
    }

    static void PrintUsage()
    {
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  TiledPaletteQuant [options]");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -i, --input <path>          Input image path (required)");
        Console.WriteLine("  -o, --output <path>         Output image path");
        Console.WriteLine("  -tw, --tile-width <n>       Tile width (default: 8)");
        Console.WriteLine("  -th, --tile-height <n>      Tile height (default: 8)");
        Console.WriteLine("  -p, --palettes <n>          Number of palettes (default: 8)");
        Console.WriteLine("  -c, --colors <n>            Colors per palette (default: 4)");
        Console.WriteLine("  -b, --bits <n>              Bits per channel (default: 5)");
        Console.WriteLine("  -d, --dither <mode>         Dither mode: off, fast, slow (default: off)");
        Console.WriteLine("  -dp, --dither-pattern <p>   Pattern: diagonal4, horizontal4, etc.");
        Console.WriteLine("  -f, --fraction <f>          Fraction of pixels (default: 0.1)");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  TiledPaletteQuant -i input.png -o output.bmp");
        Console.WriteLine("  TiledPaletteQuant -i input.png -d slow -dp diagonal4");
        Console.WriteLine("  TiledPaletteQuant   (interactive mode)");
    }
}

// See https://aka.ms/new-console-template for more information
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TiledPaletteQuant;

if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
{
    QuantizationOptions.PrintHelp();
    return 0;
}

var options = QuantizationOptions.ParseArguments(args);
if (options == null)
{
    return 1;
}

if (string.IsNullOrEmpty(options.InputPath))
{
    Console.WriteLine("Error: Input path is required.");
    QuantizationOptions.PrintHelp();
    return 1;
}

if (!File.Exists(options.InputPath))
{
    Console.WriteLine($"Error: Input file not found: {options.InputPath}");
    return 1;
}

if (string.IsNullOrEmpty(options.OutputPath))
{
    string dir = Path.GetDirectoryName(options.InputPath) ?? ".";
    string filename = Path.GetFileNameWithoutExtension(options.InputPath);
    string ext = Path.GetExtension(options.InputPath);
    options.OutputPath = Path.Combine(dir, $"{filename}_quantized{ext}");
}

try
{
    Console.WriteLine($"Loading image: {options.InputPath}");
    using var image = Image.Load<Rgba32>(options.InputPath);
    
    var quantizer = new TiledPaletteQuantizer();
    
    void ProgressCallback(int progress)
    {
        Console.WriteLine($"Progress: {progress}%");
    }
    
    var (quantizedImage, palettes) = quantizer.Quantize(image, options, ProgressCallback);
    
    Console.WriteLine($"Saving quantized image: {options.OutputPath}");
    quantizedImage.Save(options.OutputPath);
    
    Console.WriteLine("Quantization complete!");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}

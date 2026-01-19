using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace TiledPaletteQuant;

/// <summary>
/// Represents a pixel with color and position information.
/// </summary>
public class Pixel
{
    /// <summary>
    /// Reference to the tile containing this pixel.
    /// </summary>
    public Tile Tile { get; set; }
    
    /// <summary>
    /// RGB color values.
    /// </summary>
    public double[] Color { get; set; }
    
    /// <summary>
    /// X coordinate in image.
    /// </summary>
    public int X { get; set; }
    
    /// <summary>
    /// Y coordinate in image.
    /// </summary>
    public int Y { get; set; }
    
    public Pixel(Tile tile, double[] color, int x, int y)
    {
        Tile = tile;
        Color = color;
        X = x;
        Y = y;
    }
}

/// <summary>
/// Represents a tile in the image with its colors and pixels.
/// </summary>
public class Tile
{
    /// <summary>
    /// Unique colors in this tile.
    /// </summary>
    public List<double[]> Colors { get; set; } = new List<double[]>();
    
    /// <summary>
    /// Count of each color in the tile.
    /// </summary>
    public List<int> Counts { get; set; } = new List<int>();
    
    /// <summary>
    /// All pixels in this tile.
    /// </summary>
    public List<Pixel> Pixels { get; set; } = new List<Pixel>();
}

/// <summary>
/// Provides efficient random shuffling for pixel selection.
/// </summary>
public class RandomShuffle
{
    private readonly int[] _values;
    private int _currentIndex;
    private readonly Random _random = new Random();
    
    /// <summary>
    /// Initializes a new RandomShuffle with n elements.
    /// </summary>
    /// <param name="n">Number of elements.</param>
    public RandomShuffle(int n)
    {
        _values = new int[n];
        for (int i = 0; i < n; i++)
        {
            _values[i] = i;
        }
        _currentIndex = n - 1;
    }
    
    /// <summary>
    /// Shuffles the internal array using Fisher-Yates algorithm.
    /// </summary>
    private void Shuffle()
    {
        for (int i = 0; i < _values.Length; i++)
        {
            int index = i + _random.Next(_values.Length - i);
            (_values[i], _values[index]) = (_values[index], _values[i]);
        }
    }
    
    /// <summary>
    /// Gets the next random index, reshuffling when needed.
    /// </summary>
    /// <returns>A random index.</returns>
    public int Next()
    {
        _currentIndex++;
        if (_currentIndex >= _values.Length)
        {
            Shuffle();
            _currentIndex = 0;
        }
        return _values[_currentIndex];
    }
}

/// <summary>
/// Candidate color structure for dithering.
/// </summary>
public struct ColorCandidate
{
    public int ColorIndex { get; set; }
    public double ColorDistance { get; set; }
    public double[] ComparedColor { get; set; }
    public double Brightness { get; set; }
}

/// <summary>
/// Main tiled palette quantization algorithm implementation.
/// </summary>
public class TiledPaletteQuantizer
{
    private QuantizationOptions _options = null!;
    private int[,] _ditherPattern = null!;
    private int _ditherPixels;
    private readonly Random _random = new Random();
    
    /// <summary>
    /// Progress callback delegate.
    /// </summary>
    /// <param name="progress">Progress percentage (0-100).</param>
    public delegate void ProgressCallback(int progress);
    
    /// <summary>
    /// Quantizes an image using tiled palette quantization.
    /// </summary>
    /// <param name="image">Input image.</param>
    /// <param name="options">Quantization options.</param>
    /// <param name="progressCallback">Optional progress callback.</param>
    /// <returns>Tuple of quantized image and generated palettes.</returns>
    public (Image<Rgba32> image, List<List<double[]>> palettes) Quantize(
        Image<Rgba32> image,
        QuantizationOptions options,
        ProgressCallback? progressCallback = null)
    {
        _options = options;
        _ditherPattern = DitherPatterns.GetPattern(options.DitherPattern);
        _ditherPixels = DitherPatterns.GetDitherPixels(options.DitherPattern);
        
        Console.WriteLine($"Tile size: {_options.TileWidth}x{_options.TileHeight}");
        Console.WriteLine($"Palettes: {_options.PaletteCount}, Colors: {_options.ColorsPerPalette}");
        
        var startTime = DateTime.Now;
        
        bool useDither = _options.Dither != DitherMode.Off;
        
        // Extract tiles from image
        var tiles = ExtractTiles(image, _options);
        
        // Extract all pixels
        var pixels = ExtractAllPixels(tiles);
        var randomShuffle = new RandomShuffle(pixels.Count);
        
        // Algorithm parameters
        double iterations = _options.FractionOfPixels * pixels.Count;
        double alpha = 0.3;
        double finalAlpha = 0.05;
        
        if (_options.Dither == DitherMode.Slow)
        {
            iterations /= 5;
            alpha = 0.1;
            finalAlpha = 0.02;
        }
        
        double minColorFactor = 0.5;
        double minPaletteFactor = 0.5;
        int replaceIterations = 10;
        bool useMin = true;
        
        int[] prog = { 25, 65, 90, 100 };
        if (_options.Dither != DitherMode.Off)
        {
            prog[3] = 94;
        }
        
        // Initial palette generation
        var palettes = ColorQuantize1Color(tiles, pixels, randomShuffle, _options);
        
        int startIndex = 2;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.Shared)
        {
            startIndex += 1;
        }
        
        int endIndex = _options.ColorsPerPalette;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor ||
            _options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent)
        {
            endIndex -= 1;
        }
        
        progressCallback?.Invoke(prog[0] / _options.PaletteCount);
        
        // Expand palettes
        for (int numColors = startIndex; numColors <= endIndex; numColors++)
        {
            ExpandPalettesByOneColor(palettes, tiles, pixels, randomShuffle, _options);
            progressCallback?.Invoke((int)((prog[0] * numColors) / _options.ColorsPerPalette));
        }
        
        // Replace weakest colors
        double minMse = MeanSquareError(palettes, tiles);
        var minPalettes = DeepClonePalettes(palettes);
        
        for (int i = 0; i < replaceIterations; i++)
        {
            palettes = ReplaceWeakestColors(palettes, tiles, minColorFactor, minPaletteFactor, true, _options, _ditherPattern, _ditherPixels);
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var nextPixel = pixels[randomShuffle.Next()];
                MovePalettesCloser(palettes, nextPixel, alpha, _options, _ditherPattern, _ditherPixels);
            }
            
            double mse = MeanSquareError(palettes, tiles);
            if (mse < minMse)
            {
                minMse = mse;
                minPalettes = DeepClonePalettes(palettes);
            }
            
            progressCallback?.Invoke((int)(prog[0] + ((prog[1] - prog[0]) * (i + 1)) / replaceIterations));
            Console.WriteLine($"MSE: {mse:F0}");
        }
        
        if (useMin)
        {
            palettes = minPalettes;
        }
        
        if (!useDither)
        {
            palettes = ReducePalettes(palettes, _options.BitsPerChannel);
        }
        
        // Final refinement
        double finalIterations = iterations * 10;
        for (int iteration = 0; iteration < finalIterations; iteration++)
        {
            var nextPixel = pixels[randomShuffle.Next()];
            MovePalettesCloser(palettes, nextPixel, finalAlpha, _options, _ditherPattern, _ditherPixels);
            
            if (iteration % iterations == 0)
            {
                progressCallback?.Invoke((int)(prog[1] + ((prog[2] - prog[1]) * iteration) / finalIterations));
            }
        }
        
        Console.WriteLine($"Normal final: {MeanSquareError(palettes, tiles):F0}");
        Console.WriteLine($"Dither final: {MeanSquareErrorDither(palettes, tiles, _options, _ditherPattern, _ditherPixels):F0}");
        
        progressCallback?.Invoke(prog[2]);
        
        if (!useDither)
        {
            palettes = ReducePalettes(palettes, _options.BitsPerChannel);
            for (int i = 0; i < 3; i++)
            {
                palettes = KMeans(palettes, tiles, _options, _ditherPattern, _ditherPixels);
                progressCallback?.Invoke((int)(prog[2] + ((prog[3] - prog[2]) * (i + 1)) / 3));
            }
        }
        
        palettes = ReducePalettes(palettes, _options.BitsPerChannel);
        palettes = SortPalettes(palettes, GetSortStartIndex(_options));
        
        var quantizedImage = QuantizeTiles(palettes, image, useDither, _options, _ditherPattern, _ditherPixels);
        
        progressCallback?.Invoke(100);
        
        var elapsed = (DateTime.Now - startTime).TotalSeconds;
        Console.WriteLine($"> MSE: {MeanSquareError(palettes, tiles):F2}");
        Console.WriteLine($"> Time: {elapsed:F2} sec");
        
        return (quantizedImage, palettes);
    }
    
    /// <summary>
    /// Extracts tiles from the input image.
    /// </summary>
    /// <param name="image">Input image.</param>
    /// <param name="options">Quantization options.</param>
    /// <returns>List of extracted tiles.</returns>
    public List<Tile> ExtractTiles(Image<Rgba32> image, QuantizationOptions options)
    {
        var tiles = new List<Tile>();
        int totalPixels = 0;
        int tileCount = 0;
        
        for (int y = 0; y < image.Height; y += options.TileHeight)
        {
            for (int x = 0; x < image.Width; x += options.TileWidth)
            {
                var tile = ExtractTile(image, x, y, options);
                if (tile.Colors.Count == 0)
                    continue;
                    
                tiles.Add(tile);
                totalPixels += tile.Pixels.Count;
                tileCount++;
            }
        }
        
        double avgPixelsPerTile = tileCount > 0 ? (double)totalPixels / tileCount : 0;
        Console.WriteLine($"Avg pixels per tile: {avgPixelsPerTile:F2}");
        
        return tiles;
    }
    
    /// <summary>
    /// Extracts a single tile from the image.
    /// </summary>
    /// <param name="image">Input image.</param>
    /// <param name="startX">Starting X coordinate.</param>
    /// <param name="startY">Starting Y coordinate.</param>
    /// <param name="options">Quantization options.</param>
    /// <returns>Extracted tile.</returns>
    public Tile ExtractTile(Image<Rgba32> image, int startX, int startY, QuantizationOptions options)
    {
        var tile = new Tile();
        int endX = Math.Min(startX + options.TileWidth, image.Width);
        int endY = Math.Min(startY + options.TileHeight, image.Height);
        
        bool useDither = options.Dither != DitherMode.Off;
        
        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var pixel = image[x, y];
                var color = new double[] { pixel.R, pixel.G, pixel.B };
                
                // Apply color reduction if not using dithering
                if (!useDither)
                {
                    ColorUtilities.ToNbitColor(color, options.BitsPerChannel);
                }
                
                // Skip transparent pixels
                if (IsPixelTransparent(pixel, color, options))
                    continue;
                
                tile.Pixels.Add(new Pixel(tile, color, x, y));
                
                // Track unique colors
                int colorIndex = tile.Colors.FindIndex(c => ColorUtilities.EqualColors(c, color));
                if (colorIndex >= 0)
                {
                    tile.Counts[colorIndex]++;
                }
                else
                {
                    tile.Colors.Add(color);
                    tile.Counts.Add(1);
                }
            }
        }
        
        return tile;
    }
    
    private bool IsPixelTransparent(Rgba32 pixel, double[] color, QuantizationOptions options)
    {
        if (options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent &&
            pixel.A < 255)
        {
            return true;
        }
        
        if (options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor &&
            ColorUtilities.EqualColors(color, options.ColorZeroValue))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Extracts all pixels from tiles into a single list.
    /// </summary>
    /// <param name="tiles">List of tiles.</param>
    /// <returns>List of all pixels.</returns>
    public List<Pixel> ExtractAllPixels(List<Tile> tiles)
    {
        var pixels = new List<Pixel>();
        foreach (var tile in tiles)
        {
            foreach (var pixel in tile.Pixels)
            {
                pixels.Add(new Pixel(pixel.Tile, ColorUtilities.CloneColor(pixel.Color), pixel.X, pixel.Y));
            }
        }
        return pixels;
    }
    
    /// <summary>
    /// Initial palette generation with 1 color per palette.
    /// </summary>
    public List<List<double[]>> ColorQuantize1Color(
        List<Tile> tiles,
        List<Pixel> pixels,
        RandomShuffle randomShuffle,
        QuantizationOptions options)
    {
        double iterations = options.FractionOfPixels * pixels.Count;
        double alpha = 0.3;
        
        if (options.Dither == DitherMode.Slow)
        {
            iterations /= 5;
            alpha = 0.1;
        }
        
        // Calculate average color
        var avgColor = new double[] { 0, 0, 0 };
        foreach (var pixel in pixels)
        {
            ColorUtilities.AddColor(avgColor, pixel.Color);
        }
        ColorUtilities.ScaleColor(avgColor, 1.0 / pixels.Count);
        
        var palettes = new List<List<double[]>> { new List<double[]> { avgColor } };
        
        if (options.ColorZeroBehavior == ColorZeroBehavior.Shared)
        {
            palettes[0].Add(avgColor);
            palettes[0][0] = ColorUtilities.CloneColor(options.ColorZeroValue);
        }
        
        int splitIndex = 0;
        for (int numPalettes = 2; numPalettes <= options.PaletteCount; numPalettes++)
        {
            palettes.Add(DeepClonePalette(palettes[splitIndex]));
            
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var nextPixel = pixels[randomShuffle.Next()];
                MovePalettesCloser(palettes, nextPixel, alpha, options, _ditherPattern, _ditherPixels);
            }
            
            var paletteDistance = new double[numPalettes];
            foreach (var tile in tiles)
            {
                var (palIndex, distance) = ClosestPaletteDistance(palettes, tile);
                paletteDistance[palIndex] += distance;
            }
            splitIndex = MaxIndex(paletteDistance);
        }
        
        return palettes;
    }
    
    /// <summary>
    /// Expands each palette by one color.
    /// </summary>
    public void ExpandPalettesByOneColor(
        List<List<double[]>> palettes,
        List<Tile> tiles,
        List<Pixel> pixels,
        RandomShuffle randomShuffle,
        QuantizationOptions options)
    {
        double iterations = options.FractionOfPixels * pixels.Count;
        double alpha = 0.3;
        
        if (options.Dither == DitherMode.Slow)
        {
            iterations /= 5;
            alpha = 0.1;
        }
        
        int numColors = palettes[0].Count + 1;
        var splitIndexes = new int[palettes.Count];
        
        if (numColors > 2)
        {
            var totalColorDistances = new List<double[]>();
            for (int i = 0; i < palettes.Count; i++)
            {
                totalColorDistances.Add(new double[numColors]);
            }
            
            foreach (var tile in tiles)
            {
                int closestPaletteIndex = GetClosestPaletteIndex(palettes, tile);
                var palette = palettes[closestPaletteIndex];
                
                for (int i = 0; i < tile.Colors.Count; i++)
                {
                    var (minIndex, minDist) = GetClosestColor(palette, tile.Colors[i]);
                    totalColorDistances[closestPaletteIndex][minIndex] += tile.Counts[i] * minDist;
                }
            }
            
            for (int i = 0; i < palettes.Count; i++)
            {
                splitIndexes[i] = MaxIndex(totalColorDistances[i]);
            }
        }
        
        for (int i = 0; i < palettes.Count; i++)
        {
            var colors = palettes[i];
            int splitIndex = splitIndexes[i];
            colors.Add(ColorUtilities.CloneColor(colors[splitIndex]));
        }
        
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            var nextPixel = pixels[randomShuffle.Next()];
            MovePalettesCloser(palettes, nextPixel, alpha, options, _ditherPattern, _ditherPixels);
        }
    }
    
    /// <summary>
    /// Moves palettes closer to a pixel color.
    /// </summary>
    public void MovePalettesCloser(
        List<List<double[]>> palettes,
        Pixel pixel,
        double alpha,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        int sharedColorIndex = -1;
        if (options.ColorZeroBehavior == ColorZeroBehavior.Shared)
        {
            sharedColorIndex = 0;
        }
        
        int closestPaletteIndex;
        int closestColorIndex;
        double[] targetColor;
        
        if (options.Dither == DitherMode.Slow)
        {
            closestPaletteIndex = GetClosestPaletteIndexDither(palettes, pixel.Tile, options, ditherPattern, ditherPixels);
            (closestColorIndex, _, targetColor) = GetClosestColorDither(palettes[closestPaletteIndex], pixel, options, ditherPattern, ditherPixels);
        }
        else
        {
            closestPaletteIndex = GetClosestPaletteIndex(palettes, pixel.Tile);
            (closestColorIndex, _) = GetClosestColor(palettes[closestPaletteIndex], pixel.Color);
            targetColor = pixel.Color;
        }
        
        if (closestColorIndex != sharedColorIndex)
        {
            ColorUtilities.MoveCloser(palettes[closestPaletteIndex][closestColorIndex], targetColor, alpha);
        }
    }
    
    /// <summary>
    /// Finds the closest color in a palette to a target color.
    /// </summary>
    public (int index, double distance) GetClosestColor(List<double[]> palette, double[] color)
    {
        int minIndex = palette.Count - 1;
        double minDist = ColorUtilities.ColorDistance(palette[minIndex], color);
        
        for (int i = palette.Count - 2; i >= 0; i--)
        {
            double dist = ColorUtilities.ColorDistance(palette[i], color);
            if (dist < minDist)
            {
                minIndex = i;
                minDist = dist;
            }
        }
        
        return (minIndex, minDist);
    }
    
    /// <summary>
    /// Finds the closest color using dithering.
    /// </summary>
    public (int index, double distance, double[] comparedColor) GetClosestColorDither(
        List<double[]> palette,
        Pixel pixel,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        var error = new double[] { 0, 0, 0 };
        var linearPixel = ColorUtilities.CloneColor(pixel.Color);
        ColorUtilities.ToLinearColor(linearPixel);
        
        var candidates = new List<ColorCandidate>();
        var c = new double[3];
        var err = new double[3];
        var reducedColor = new double[3];
        
        for (int i = 0; i < ditherPixels; i++)
        {
            ColorUtilities.CopyColor(c, linearPixel);
            ColorUtilities.CopyColor(err, error);
            ColorUtilities.ScaleColor(err, options.DitherWeight);
            ColorUtilities.AddColor(c, err);
            ColorUtilities.ClampColor(c, 0, 255 * 255);
            ColorUtilities.ToSrgbColor(c);
            
            var (minColorIndex, minDist) = GetClosestColor(palette, c);
            var minColor = palette[minColorIndex];
            
            candidates.Add(new ColorCandidate
            {
                ColorIndex = minColorIndex,
                ColorDistance = minDist,
                ComparedColor = ColorUtilities.CloneColor(c),
                Brightness = ColorUtilities.Brightness(minColor)
            });
            
            ColorUtilities.CopyColor(reducedColor, minColor);
            ColorUtilities.ToNbitColor(reducedColor, options.BitsPerChannel);
            ColorUtilities.ToLinearColor(reducedColor);
            ColorUtilities.AddColor(error, linearPixel);
            ColorUtilities.SubtractColor(error, reducedColor);
        }
        
        // Sort candidates by brightness
        for (int i = 0; i < ditherPixels - 1; i++)
        {
            for (int j = i + 1; j < ditherPixels; j++)
            {
                if (candidates[i].Brightness > candidates[j].Brightness)
                {
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }
            }
        }
        
        int index = ditherPattern[pixel.X & 1, pixel.Y & 1];
        return (candidates[index].ColorIndex, candidates[index].ColorDistance, candidates[index].ComparedColor);
    }
    
    /// <summary>
    /// Finds the closest palette index for a tile.
    /// </summary>
    public int GetClosestPaletteIndex(List<List<double[]>> palettes, Tile tile)
    {
        if (palettes.Count == 1)
            return 0;
            
        var distances = palettes.Select(palette => PaletteDistance(palette, tile)).ToList();
        return MinIndex(distances);
    }
    
    /// <summary>
    /// Finds the closest palette index using dithering.
    /// </summary>
    public int GetClosestPaletteIndexDither(
        List<List<double[]>> palettes,
        Tile tile,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        if (palettes.Count == 1)
            return 0;
            
        var distances = palettes.Select(palette => PaletteDistanceDither(palette, tile, options, ditherPattern, ditherPixels)).ToList();
        return MinIndex(distances);
    }
    
    /// <summary>
    /// Calculates distance between a palette and a tile.
    /// </summary>
    public double PaletteDistance(List<double[]> palette, Tile tile)
    {
        double sum = 0;
        for (int i = 0; i < tile.Colors.Count; i++)
        {
            var (_, minDist) = GetClosestColor(palette, tile.Colors[i]);
            sum += tile.Counts[i] * minDist;
        }
        return sum;
    }
    
    /// <summary>
    /// Calculates distance using dithering.
    /// </summary>
    public double PaletteDistanceDither(
        List<double[]> palette,
        Tile tile,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        double sum = 0;
        foreach (var pixel in tile.Pixels)
        {
            var (_, minDist, _) = GetClosestColorDither(palette, pixel, options, ditherPattern, ditherPixels);
            sum += minDist;
        }
        return sum;
    }
    
    /// <summary>
    /// Finds the closest palette and its distance.
    /// </summary>
    public (int index, double distance) ClosestPaletteDistance(List<List<double[]>> palettes, Tile tile)
    {
        var distances = palettes.Select(palette => PaletteDistance(palette, tile)).ToList();
        int index = MinIndex(distances);
        return (index, distances[index]);
    }
    
    /// <summary>
    /// Finds the closest palette and its distance using dithering.
    /// </summary>
    public (int index, double distance) ClosestPaletteDistanceDither(
        List<List<double[]>> palettes,
        Tile tile,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        var distances = palettes.Select(palette => PaletteDistanceDither(palette, tile, options, ditherPattern, ditherPixels)).ToList();
        int index = MinIndex(distances);
        return (index, distances[index]);
    }
    
    /// <summary>
    /// Calculates mean square error for palettes and tiles.
    /// </summary>
    public double MeanSquareError(List<List<double[]>> palettes, List<Tile> tiles)
    {
        double totalDistance = 0;
        int count = 0;
        
        foreach (var tile in tiles)
        {
            int palIndex = GetClosestPaletteIndex(palettes, tile);
            for (int i = 0; i < tile.Colors.Count; i++)
            {
                var (_, minDistance) = GetClosestColor(palettes[palIndex], tile.Colors[i]);
                totalDistance += minDistance * tile.Counts[i];
                count += tile.Counts[i];
            }
        }
        
        return count > 0 ? totalDistance / count : 0;
    }
    
    /// <summary>
    /// Calculates mean square error using dithering.
    /// </summary>
    public double MeanSquareErrorDither(
        List<List<double[]>> palettes,
        List<Tile> tiles,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        double totalDistance = 0;
        int count = 0;
        
        foreach (var tile in tiles)
        {
            int palIndex = GetClosestPaletteIndexDither(palettes, tile, options, ditherPattern, ditherPixels);
            foreach (var pixel in tile.Pixels)
            {
                var (_, minDistance, _) = GetClosestColorDither(palettes[palIndex], pixel, options, ditherPattern, ditherPixels);
                totalDistance += minDistance;
                count += 1;
            }
        }
        
        return count > 0 ? totalDistance / count : 0;
    }
    
    /// <summary>
    /// Replaces weakest colors in palettes.
    /// </summary>
    public List<List<double[]>> ReplaceWeakestColors(
        List<List<double[]>> palettes,
        List<Tile> tiles,
        double minColorFactor,
        double minPaletteFactor,
        bool replacePalettes,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        bool useSlowDither = options.Dither == DitherMode.Slow;
        
        var closestPaletteIndex = new int[tiles.Count];
        int maxPaletteIndex = 0;
        int minPaletteIndex = 0;
        var totalPaletteMse = new double[palettes.Count];
        var removedPaletteMse = new double[palettes.Count];
        
        if (palettes.Count > 1)
        {
            for (int j = 0; j < tiles.Count; j++)
            {
                var tile = tiles[j];
                int index;
                double minDistance;
                
                if (useSlowDither)
                {
                    (index, minDistance) = ClosestPaletteDistanceDither(palettes, tile, options, ditherPattern, ditherPixels);
                }
                else
                {
                    (index, minDistance) = ClosestPaletteDistance(palettes, tile);
                }
                
                totalPaletteMse[index] += minDistance;
                closestPaletteIndex[j] = index;
                
                var remainingPalettes = new List<List<double[]>>();
                for (int i = 0; i < palettes.Count; i++)
                {
                    if (i != index)
                    {
                        remainingPalettes.Add(palettes[i]);
                    }
                }
                
                if (remainingPalettes.Count > 0)
                {
                    double minDistance2;
                    if (useSlowDither)
                    {
                        (_, minDistance2) = ClosestPaletteDistanceDither(remainingPalettes, tile, options, ditherPattern, ditherPixels);
                    }
                    else
                    {
                        (_, minDistance2) = ClosestPaletteDistance(remainingPalettes, tile);
                    }
                    removedPaletteMse[index] += minDistance2;
                }
            }
            
            maxPaletteIndex = MaxIndex(totalPaletteMse);
            minPaletteIndex = MinIndex(removedPaletteMse);
        }
        
        var result = new List<List<double[]>>();
        
        if (palettes[0].Count > 1)
        {
            var totalColorMse = new List<double[]>();
            var secondColorMse = new List<double[]>();
            
            for (int j = 0; j < palettes.Count; j++)
            {
                totalColorMse.Add(new double[palettes[j].Count]);
                secondColorMse.Add(new double[palettes[j].Count]);
            }
            
            for (int j = 0; j < tiles.Count; j++)
            {
                var tile = tiles[j];
                int minPalIndex = closestPaletteIndex[j];
                var pal = palettes[minPalIndex];
                
                if (useSlowDither)
                {
                    foreach (var pixel in tile.Pixels)
                    {
                        var (minColorIndex, minDist, _) = GetClosestColorDither(pal, pixel, options, ditherPattern, ditherPixels);
                        totalColorMse[minPalIndex][minColorIndex] += minDist;
                        
                        var remainingColors = new List<double[]>();
                        for (int i = 0; i < pal.Count; i++)
                        {
                            if (i != minColorIndex)
                            {
                                remainingColors.Add(pal[i]);
                            }
                        }
                        
                        var (_, secondDist, _) = GetClosestColorDither(remainingColors, pixel, options, ditherPattern, ditherPixels);
                        secondColorMse[minPalIndex][minColorIndex] += secondDist;
                    }
                }
                else
                {
                    for (int i = 0; i < tile.Colors.Count; i++)
                    {
                        var color = tile.Colors[i];
                        var (minColorIndex, minDist) = GetClosestColor(pal, color);
                        totalColorMse[minPalIndex][minColorIndex] += minDist * tile.Counts[i];
                        
                        var remainingColors = new List<double[]>();
                        for (int k = 0; k < pal.Count; k++)
                        {
                            if (k != minColorIndex)
                            {
                                remainingColors.Add(pal[k]);
                            }
                        }
                        
                        var (_, secondDist) = GetClosestColor(remainingColors, color);
                        secondColorMse[minPalIndex][minColorIndex] += secondDist * tile.Counts[i];
                    }
                }
            }
            
            int sharedColorIndex = -1;
            if (options.ColorZeroBehavior == ColorZeroBehavior.Shared)
            {
                sharedColorIndex = 0;
            }
            
            for (int palIndex = 0; palIndex < palettes.Count; palIndex++)
            {
                int maxColorIndex = MaxIndex(totalColorMse[palIndex]);
                int minColorIndex = MinIndex(secondColorMse[palIndex]);
                
                bool shouldReplaceMinColor = minColorIndex != maxColorIndex &&
                    minColorIndex != sharedColorIndex &&
                    secondColorMse[palIndex][minColorIndex] < minColorFactor * totalColorMse[palIndex][maxColorIndex];
                
                var colors = new List<double[]>();
                for (int i = 0; i < palettes[palIndex].Count; i++)
                {
                    if (i == minColorIndex && shouldReplaceMinColor)
                    {
                        Console.WriteLine($"Replaced color in palette {palIndex}");
                        colors.Add(ColorUtilities.CloneColor(palettes[palIndex][maxColorIndex]));
                    }
                    else
                    {
                        colors.Add(ColorUtilities.CloneColor(palettes[palIndex][i]));
                    }
                }
                result.Add(colors);
            }
        }
        else
        {
            for (int palIndex = 0; palIndex < palettes.Count; palIndex++)
            {
                var colors = new List<double[]>();
                for (int i = 0; i < palettes[palIndex].Count; i++)
                {
                    colors.Add(ColorUtilities.CloneColor(palettes[palIndex][i]));
                }
                result.Add(colors);
            }
        }
        
        if (replacePalettes &&
            minPaletteIndex != maxPaletteIndex &&
            removedPaletteMse[minPaletteIndex] < minPaletteFactor * totalPaletteMse[maxPaletteIndex])
        {
            Console.WriteLine($"Replaced palette {minPaletteIndex}");
            result[minPaletteIndex].Clear();
            foreach (var color in result[maxPaletteIndex])
            {
                result[minPaletteIndex].Add(ColorUtilities.CloneColor(color));
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Performs k-means clustering on palettes.
    /// </summary>
    public List<List<double[]>> KMeans(
        List<List<double[]>> palettes,
        List<Tile> tiles,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        var counts = new List<int[]>();
        var sumColors = new List<double[][]>();
        
        for (int i = 0; i < palettes.Count; i++)
        {
            var c = new int[palettes[i].Count];
            var colors = new double[palettes[i].Count][];
            for (int j = 0; j < palettes[i].Count; j++)
            {
                c[j] = 0;
                colors[j] = new double[] { 0, 0, 0 };
            }
            counts.Add(c);
            sumColors.Add(colors);
        }
        
        foreach (var tile in tiles)
        {
            if (options.Dither == DitherMode.Slow)
            {
                int palIndex = GetClosestPaletteIndexDither(palettes, tile, options, ditherPattern, ditherPixels);
                foreach (var pixel in tile.Pixels)
                {
                    var (colIndex, _, _) = GetClosestColorDither(palettes[palIndex], pixel, options, ditherPattern, ditherPixels);
                    counts[palIndex][colIndex] += 1;
                    ColorUtilities.AddColor(sumColors[palIndex][colIndex], pixel.Color);
                }
            }
            else
            {
                int palIndex = GetClosestPaletteIndex(palettes, tile);
                for (int i = 0; i < tile.Colors.Count; i++)
                {
                    var (colIndex, _) = GetClosestColor(palettes[palIndex], tile.Colors[i]);
                    counts[palIndex][colIndex] += tile.Counts[i];
                    var color = ColorUtilities.CloneColor(tile.Colors[i]);
                    ColorUtilities.ScaleColor(color, tile.Counts[i]);
                    ColorUtilities.AddColor(sumColors[palIndex][colIndex], color);
                }
            }
        }
        
        int sharedColorIndex = -1;
        if (options.ColorZeroBehavior == ColorZeroBehavior.Shared)
        {
            sharedColorIndex = 0;
        }
        
        for (int i = 0; i < sumColors.Count; i++)
        {
            for (int j = 0; j < sumColors[i].Length; j++)
            {
                if (counts[i][j] == 0 || j == sharedColorIndex)
                {
                    sumColors[i][j] = ColorUtilities.CloneColor(palettes[i][j]);
                }
                else
                {
                    ColorUtilities.ScaleColor(sumColors[i][j], 1.0 / counts[i][j]);
                }
            }
        }
        
        return sumColors.Select(p => p.ToList()).ToList();
    }
    
    /// <summary>
    /// Applies quantization to tiles and generates output image.
    /// </summary>
    public Image<Rgba32> QuantizeTiles(
        List<List<double[]>> palettes,
        Image<Rgba32> image,
        bool useDither,
        QuantizationOptions options,
        int[,] ditherPattern,
        int ditherPixels)
    {
        var quantizedImage = new Image<Rgba32>(image.Width, image.Height);
        
        var reducedPalettes = DeepClonePalettes(palettes);
        foreach (var pal in reducedPalettes)
        {
            foreach (var color in pal)
            {
                ColorUtilities.ToNbitColor(color, options.BitsPerChannel);
            }
        }
        
        var transparentColor = ColorUtilities.CloneColor(options.ColorZeroValue);
        if (useDither)
        {
            ColorUtilities.ToNbitColor(transparentColor, options.BitsPerChannel);
        }
        
        for (int startY = 0; startY < image.Height; startY += options.TileHeight)
        {
            for (int startX = 0; startX < image.Width; startX += options.TileWidth)
            {
                var tile = ExtractTile(image, startX, startY, options);
                var palette = reducedPalettes[0];
                int closestPaletteIndex = 0;
                
                if (tile.Colors.Count > 0)
                {
                    if (useDither)
                    {
                        closestPaletteIndex = GetClosestPaletteIndexDither(reducedPalettes, tile, options, ditherPattern, ditherPixels);
                    }
                    else
                    {
                        closestPaletteIndex = GetClosestPaletteIndex(reducedPalettes, tile);
                    }
                    palette = reducedPalettes[closestPaletteIndex];
                }
                
                int endX = Math.Min(startX + options.TileWidth, image.Width);
                int endY = Math.Min(startY + options.TileHeight, image.Height);
                
                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        var pixel = image[x, y];
                        var color = new double[] { pixel.R, pixel.G, pixel.B };
                        
                        if (!useDither)
                        {
                            ColorUtilities.ToNbitColor(color, options.BitsPerChannel);
                        }
                        
                        bool isTransparent = (options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent && pixel.A < 255) ||
                                           (options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor && ColorUtilities.EqualColors(color, transparentColor));
                        
                        if (isTransparent)
                        {
                            quantizedImage[x, y] = pixel;
                        }
                        else
                        {
                            int closestColorIndex;
                            if (useDither)
                            {
                                (closestColorIndex, _, _) = GetClosestColorDither(palette, new Pixel(tile, color, x, y), options, ditherPattern, ditherPixels);
                            }
                            else
                            {
                                (closestColorIndex, _) = GetClosestColor(palette, color);
                            }
                            
                            var paletteColor = palette[closestColorIndex];
                            quantizedImage[x, y] = new Rgba32(
                                (byte)Math.Round(paletteColor[0]),
                                (byte)Math.Round(paletteColor[1]),
                                (byte)Math.Round(paletteColor[2]),
                                255
                            );
                        }
                    }
                }
            }
        }
        
        return quantizedImage;
    }
    
    /// <summary>
    /// Sorts palettes for optimal similarity between adjacent palettes and colors.
    /// </summary>
    public List<List<double[]>> SortPalettes(List<List<double[]>> palettes, int startIndex)
    {
        const int pairIterations = 2000;
        const int tIterations = 10000;
        const int paletteIterations = 100000;
        const int upWeight = 2;
        
        int numPalettes = palettes.Count;
        int numColors = palettes[0].Count;
        
        if (numColors == 2 && startIndex == 1)
        {
            return palettes;
        }
        
        // Build palette distance matrix and color index mapping
        // Arrays sized with +2 to accommodate sentinel values at boundaries for the traveling salesman algorithm
        var paletteDist = new double[numPalettes + 2, numPalettes + 2];
        var colorIndex = new int[numPalettes][,];
        
        for (int i = 0; i < numPalettes; i++)
        {
            colorIndex[i] = new int[numPalettes, numColors];
            for (int j = 0; j < numPalettes; j++)
            {
                for (int k = 0; k < numColors; k++)
                {
                    colorIndex[i][j, k] = k;
                }
            }
        }
        
        // Calculate distances between palettes
        for (int p1 = 0; p1 < numPalettes - 1; p1++)
        {
            for (int p2 = p1 + 1; p2 < numPalettes; p2++)
            {
                var index = colorIndex[p1];
                
                for (int iteration = 0; iteration < pairIterations; iteration++)
                {
                    // Need at least 2 colors in the range to swap
                    if (numColors - startIndex - 1 < 1)
                        break;
                        
                    int i1 = startIndex + _random.Next(numColors - startIndex - 1);
                    int i2 = i1 + 1 + _random.Next(numColors - i1 - 1);
                    
                    if (_random.NextDouble() < 0.5)
                    {
                        (i1, i2) = (i2, i1);
                    }
                    
                    var p1i1 = palettes[p1][i1];
                    var p1i2 = palettes[p1][i2];
                    var p2i1 = palettes[p2][index[p2, i1]];
                    var p2i2 = palettes[p2][index[p2, i2]];
                    
                    double straightDist = ColorUtilities.ColorDistance(p1i1, p2i1) + ColorUtilities.ColorDistance(p1i2, p2i2);
                    double swappedDist = ColorUtilities.ColorDistance(p1i1, p2i2) + ColorUtilities.ColorDistance(p1i2, p2i1);
                    
                    if (swappedDist < straightDist)
                    {
                        (index[p2, i1], index[p2, i2]) = (index[p2, i2], index[p2, i1]);
                    }
                }
                
                double sum = 0;
                for (int i = 0; i < numColors; i++)
                {
                    sum += ColorUtilities.ColorDistance(palettes[p1][i], palettes[p2][index[p2, i]]);
                }
                paletteDist[p1 + 1, p2 + 1] = sum;
                paletteDist[p2 + 1, p1 + 1] = sum;
            }
        }
        
        // Build reverse index
        for (int p1 = 1; p1 < numPalettes; p1++)
        {
            for (int p2 = 0; p2 < p1; p2++)
            {
                var index = colorIndex[p2];
                var revIndex = colorIndex[p1];
                for (int i = 0; i < numColors; i++)
                {
                    for (int j = 0; j < numColors; j++)
                    {
                        if (index[p1, j] == i)
                        {
                            revIndex[p2, i] = j;
                            break;
                        }
                    }
                }
            }
        }
        
        // Optimize palette order
        var palIndex = Enumerable.Range(0, numPalettes + 2).ToArray();
        
        if (numPalettes > 2)
        {
            for (int iteration = 0; iteration < paletteIterations; iteration++)
            {
                int index1 = Math.Max(1, _random.Next(numPalettes));
                if (numPalettes - index1 < 1)
                    continue;
                int index2 = Math.Min(numPalettes - 1, index1 + 1 + _random.Next(numPalettes - index1));
                
                int i1b = palIndex[index1 - 1];
                int i1 = palIndex[index1];
                int i2 = palIndex[index2];
                int i2b = palIndex[index2 + 1];
                
                double straightDist = paletteDist[i1b, i1] + paletteDist[i2, i2b];
                double swappedDist = paletteDist[i1b, i2] + paletteDist[i1, i2b];
                
                if (swappedDist < straightDist)
                {
                    Reverse(palIndex, index1, index2);
                }
            }
        }
        
        // Optimize first palette color order
        var pal1 = palettes[palIndex[1] - 1];
        // Array sized with +2 to accommodate sentinel values at boundaries
        var p1Index = Enumerable.Range(0, numColors + 2).ToArray();
        var p1Dist = new double[numColors + 2, numColors + 2];
        
        for (int i = 1; i <= numColors; i++)
        {
            for (int j = 1; j <= numColors; j++)
            {
                p1Dist[i, j] = ColorUtilities.ColorDistance(pal1[i - 1], pal1[j - 1]);
            }
        }
        
        if (numColors > 2)
        {
            for (int iteration = 0; iteration < paletteIterations; iteration++)
            {
                int index1 = Math.Max(1 + startIndex, (int)Math.Floor(_random.NextDouble() * numColors));
                int index2 = Math.Min(numColors, index1 + 1 + (int)Math.Floor(_random.NextDouble() * numColors));
                
                int i1b = p1Index[index1 - 1];
                int i1 = p1Index[index1];
                int i2 = p1Index[index2];
                int i2b = p1Index[index2 + 1];
                
                double straightDist = p1Dist[i1b, i1] + p1Dist[i2, i2b];
                double swappedDist = p1Dist[i1b, i2] + p1Dist[i1, i2b];
                
                if (swappedDist < straightDist)
                {
                    Reverse(p1Index, index1, index2);
                }
            }
        }
        
        // Build final palette index mapping
        var pIndex = new int[numPalettes, numColors];
        for (int i = 0; i < numColors; i++)
        {
            pIndex[0, i] = p1Index[i + 1] - 1;
        }
        
        for (int i = 1; i < numPalettes; i++)
        {
            for (int j = 0; j < numColors; j++)
            {
                int p1 = palIndex[i] - 1;
                int p2 = palIndex[i + 1] - 1;
                pIndex[i, j] = colorIndex[p1][p2, pIndex[i - 1, j]];
            }
        }
        
        // Optimize color order in subsequent palettes
        if (numColors >= 4)
        {
            for (int i = 1; i < numPalettes; i++)
            {
                int p1 = palIndex[i] - 1;
                int p2 = palIndex[i + 1] - 1;
                int iteration = 0;
                
                while (iteration < tIterations)
                {
                    int index1 = Math.Max(startIndex, _random.Next(numColors));
                    int index2 = Math.Max(startIndex, _random.Next(numColors));
                    
                    if (index1 == index2)
                        continue;
                    
                    int up1 = pIndex[i - 1, index1];
                    int idx1 = pIndex[i, index1];
                    int left1 = index1 > 0 ? pIndex[i, index1 - 1] : -1;
                    int right1 = index1 < numColors - 1 ? pIndex[i, index1 + 1] : numColors;
                    
                    int up2 = pIndex[i - 1, index2];
                    int idx2 = pIndex[i, index2];
                    int left2 = index2 > 0 ? pIndex[i, index2 - 1] : -1;
                    int right2 = index2 < numColors - 1 ? pIndex[i, index2 + 1] : numColors;
                    
                    double straightDist = upWeight * ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p1][up1]);
                    if (left1 >= 0)
                        straightDist += ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p2][left1]);
                    if (right1 < numColors)
                        straightDist += ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p2][right1]);
                    
                    straightDist += upWeight * ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p1][up2]);
                    if (left2 >= 0)
                        straightDist += ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p2][left2]);
                    if (right2 < numColors)
                        straightDist += ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p2][right2]);
                    
                    double swappedDist = upWeight * ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p1][up1]);
                    if (left1 >= 0)
                        swappedDist += ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p2][left1]);
                    if (right1 < numColors)
                        swappedDist += ColorUtilities.ColorDistance(palettes[p2][idx2], palettes[p2][right1]);
                    
                    swappedDist += upWeight * ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p1][up2]);
                    if (left2 >= 0)
                        swappedDist += ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p2][left2]);
                    if (right2 < numColors)
                        swappedDist += ColorUtilities.ColorDistance(palettes[p2][idx1], palettes[p2][right2]);
                    
                    if (swappedDist < straightDist)
                    {
                        (pIndex[i, index1], pIndex[i, index2]) = (pIndex[i, index2], pIndex[i, index1]);
                    }
                    
                    iteration++;
                }
            }
        }
        
        // Build final sorted palettes
        var pals = new List<List<double[]>>();
        for (int i = 0; i < numPalettes; i++)
        {
            int p2 = palIndex[i + 1] - 1;
            var pal = new List<double[]>();
            for (int j = 0; j < numColors; j++)
            {
                pal.Add(palettes[p2][pIndex[i, j]]);
            }
            pals.Add(pal);
        }
        
        return pals;
    }
    
    /// <summary>
    /// Reduces palette colors to specified bit depth.
    /// </summary>
    public List<List<double[]>> ReducePalettes(List<List<double[]>> palettes, int bitsPerChannel)
    {
        var result = new List<List<double[]>>();
        foreach (var palette in palettes)
        {
            var pal = new List<double[]>();
            foreach (var color in palette)
            {
                var col = ColorUtilities.CloneColor(color);
                ColorUtilities.ToNbitColor(col, bitsPerChannel);
                pal.Add(col);
            }
            result.Add(pal);
        }
        return result;
    }
    
    /// <summary>
    /// Finds the index of the maximum value in an array.
    /// </summary>
    public int MaxIndex(double[] values)
    {
        int maxI = 0;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > values[maxI])
            {
                maxI = i;
            }
        }
        return maxI;
    }
    
    /// <summary>
    /// Finds the index of the maximum value in a list.
    /// </summary>
    public int MaxIndex(List<double> values)
    {
        int maxI = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] > values[maxI])
            {
                maxI = i;
            }
        }
        return maxI;
    }
    
    /// <summary>
    /// Finds the index of the minimum value in an array.
    /// </summary>
    public int MinIndex(double[] values)
    {
        int minI = 0;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] < values[minI])
            {
                minI = i;
            }
        }
        return minI;
    }
    
    /// <summary>
    /// Finds the index of the minimum value in a list.
    /// </summary>
    public int MinIndex(List<double> values)
    {
        int minI = 0;
        for (int i = 1; i < values.Count; i++)
        {
            if (values[i] < values[minI])
            {
                minI = i;
            }
        }
        return minI;
    }
    
    private void Reverse(int[] array, int left, int right)
    {
        double middle = (left + right) / 2.0;
        while (left < middle)
        {
            (array[left], array[right]) = (array[right], array[left]);
            left++;
            right--;
        }
    }
    
    private List<double[]> DeepClonePalette(List<double[]> palette)
    {
        return palette.Select(color => ColorUtilities.CloneColor(color)).ToList();
    }
    
    private List<List<double[]>> DeepClonePalettes(List<List<double[]>> palettes)
    {
        return palettes.Select(palette => DeepClonePalette(palette)).ToList();
    }
    
    private int GetSortStartIndex(QuantizationOptions options)
    {
        int startIndex = 0;
        if (options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor ||
            options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent)
        {
            startIndex = 1;
        }
        if (options.ColorZeroBehavior == ColorZeroBehavior.Shared)
        {
            startIndex = 1;
        }
        return startIndex;
    }
}

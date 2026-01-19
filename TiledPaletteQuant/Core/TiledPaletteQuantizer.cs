using TiledPaletteQuant.Models;

namespace TiledPaletteQuant.Core;

/// <summary>
/// Main tiled palette quantization algorithm.
/// Ports the quantizeImage function from worker.js lines 141-282.
/// </summary>
public class TiledPaletteQuantizer
{
    private readonly QuantizationOptions _options;
    private readonly Action<int>? _progressCallback;

    public TiledPaletteQuantizer(QuantizationOptions options, Action<int>? progressCallback = null)
    {
        _options = options;
        _progressCallback = progressCallback;
    }

    /// <summary>
    /// Quantizes an image using tiled palette quantization.
    /// </summary>
    public QuantizationResult Quantize(byte[] imageData, int width, int height)
    {
        Console.WriteLine($"Quantizing {width}x{height} image...");
        var startTime = DateTime.Now;

        // Step 1: Reduce image to n-bit (or keep full color for dither)
        var reducedImageData = ReduceImageData(imageData, width, height);

        // Step 2: Extract tiles
        var tiles = ExtractTiles(reducedImageData, width, height);
        Console.WriteLine($"Extracted {tiles.Count} tiles");

        // Step 3: Extract all pixels from tiles
        var pixels = ExtractAllPixels(tiles);
        Console.WriteLine($"Total pixels: {pixels.Count}");

        var randomShuffle = new RandomShuffle(pixels.Count);

        // Step 4: Set up algorithm parameters
        bool useDither = _options.DitherMode != DitherMode.Off;
        double iterations = _options.FractionOfPixels * pixels.Count;
        double alpha = 0.3;
        double finalAlpha = 0.05;

        if (_options.DitherMode == DitherMode.Slow)
        {
            iterations /= 5;
            alpha = 0.1;
            finalAlpha = 0.02;
        }

        const double minColorFactor = 0.5;
        const double minPaletteFactor = 0.5;
        const int replaceIterations = 10;

        int[] prog = useDither ? new[] { 25, 65, 90, 94 } : new[] { 25, 65, 90, 100 };

        // Step 5: Generate initial single-color palette
        var palettes = ColorQuantize1Color(tiles, pixels, randomShuffle);

        int startIndex = 2;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.Shared)
            startIndex = 3;

        int endIndex = _options.ColorsPerPalette;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor ||
            _options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent)
            endIndex -= 1;

        UpdateProgress(prog[0] / _options.PaletteCount);

        // Step 6: Expand palettes by adding colors one at a time
        for (int numColors = startIndex; numColors <= endIndex; numColors++)
        {
            ExpandPalettesByOneColor(palettes, tiles, pixels, randomShuffle);
            UpdateProgress((prog[0] * numColors) / _options.ColorsPerPalette);
        }

        // Step 7: Replace weakest colors iteratively
        var minMse = MeanSquareError(palettes, tiles);
        var minPalettes = DeepClonePalettes(palettes);

        for (int i = 0; i < replaceIterations; i++)
        {
            palettes = ReplaceWeakestColors(palettes, tiles, minColorFactor, minPaletteFactor, true);

            for (int iteration = 0; iteration < (int)iterations; iteration++)
            {
                var nextPixel = pixels[randomShuffle.Next()];
                MovePalettesCloser(palettes, nextPixel, alpha);
            }

            double mse = MeanSquareError(palettes, tiles);
            if (mse < minMse)
            {
                minMse = mse;
                minPalettes = DeepClonePalettes(palettes);
            }

            UpdateProgress(prog[0] + ((prog[1] - prog[0]) * (i + 1)) / replaceIterations);
            Console.WriteLine($"MSE: {mse:F0}");
        }

        palettes = minPalettes;

        // Step 8: Reduce palettes to n-bit if not using dither
        if (!useDither)
            palettes = ReducePalettes(palettes, _options.BitsPerChannel);

        // Step 9: Final refinement iterations
        int finalIterations = (int)(iterations * 10);
        int nextUpdate = (int)iterations;

        for (int iteration = 0; iteration < finalIterations; iteration++)
        {
            var nextPixel = pixels[randomShuffle.Next()];
            MovePalettesCloser(palettes, nextPixel, finalAlpha);

            if (iteration >= nextUpdate)
            {
                nextUpdate += (int)iterations;
                UpdateProgress(prog[1] + ((prog[2] - prog[1]) * iteration) / finalIterations);
            }
        }

        Console.WriteLine($"Normal final MSE: {MeanSquareError(palettes, tiles):F0}");

        UpdateProgress(prog[2]);

        // Step 10: K-means clustering (if no dither)
        if (!useDither)
        {
            palettes = ReducePalettes(palettes, _options.BitsPerChannel);

            for (int i = 0; i < 3; i++)
            {
                palettes = KMeans(palettes, tiles);
                UpdateProgress(prog[2] + ((prog[3] - prog[2]) * (i + 1)) / 3);
            }
        }

        // Step 11: Final palette reduction and sorting
        palettes = ReducePalettes(palettes, _options.BitsPerChannel);
        palettes = PaletteSorter.SortPalettes(palettes, startIndex);

        // Step 12: Generate final quantized image
        var result = QuantizeTiles(palettes, reducedImageData, width, height, useDither);

        Console.WriteLine($"Final MSE: {MeanSquareError(palettes, tiles):F2}");
        Console.WriteLine($"Time: {(DateTime.Now - startTime).TotalSeconds:F2} sec");

        UpdateProgress(100);

        return result;
    }

    private byte[] ReduceImageData(byte[] imageData, int width, int height)
    {
        bool useDither = _options.DitherMode != DitherMode.Off;
        var reducedData = new byte[imageData.Length];

        if (useDither)
        {
            Array.Copy(imageData, reducedData, imageData.Length);
        }
        else
        {
            for (int i = 0; i < imageData.Length; i++)
            {
                reducedData[i] = (byte)ColorUtils.ToNbit(imageData[i], _options.BitsPerChannel);
            }
        }

        return reducedData;
    }

    private List<TileData> ExtractTiles(byte[] imageData, int width, int height)
    {
        var tiles = new List<TileData>();

        for (int startY = 0; startY < height; startY += _options.TileHeight)
        {
            for (int startX = 0; startX < width; startX += _options.TileWidth)
            {
                var tile = ExtractTile(imageData, width, height, startX, startY);
                if (tile.Colors.Count > 0)
                {
                    tiles.Add(tile);
                }
            }
        }

        return tiles;
    }

    private TileData ExtractTile(byte[] imageData, int width, int height, int startX, int startY)
    {
        var tile = new TileData();
        int endX = Math.Min(startX + _options.TileWidth, width);
        int endY = Math.Min(startY + _options.TileHeight, height);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                int index = 4 * (x + width * y);
                var color = new[] { (double)imageData[index], (double)imageData[index + 1], (double)imageData[index + 2] };

                // Skip transparent pixels
                if (IsPixelTransparent(imageData, index, color))
                    continue;

                var pixel = new PixelData { Tile = tile, Color = color, X = x, Y = y };
                tile.Pixels.Add(pixel);

                // Add to color histogram
                int colorIndex = tile.Colors.FindIndex(c => ColorUtils.EqualColors(c, color));
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

    private bool IsPixelTransparent(byte[] imageData, int index, double[] color)
    {
        if (_options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent &&
            imageData[index + 3] < 255)
            return true;

        if (_options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor &&
            ColorUtils.EqualColors(color, _options.ColorZeroValue))
            return true;

        return false;
    }

    private List<PixelData> ExtractAllPixels(List<TileData> tiles)
    {
        var pixels = new List<PixelData>();
        foreach (var tile in tiles)
        {
            pixels.AddRange(tile.Pixels);
        }
        return pixels;
    }

    private List<List<double[]>> ColorQuantize1Color(List<TileData> tiles, List<PixelData> pixels, RandomShuffle randomShuffle)
    {
        // From worker.js lines 1093-1153: Start with 1 color palette and expand
        double iterations = _options.FractionOfPixels * pixels.Count;
        if (_options.DitherMode == DitherMode.Slow)
            iterations /= 5;

        const double alpha = 0.3;

        // Calculate average color
        double[] avgColor = new[] { 0.0, 0.0, 0.0 };
        foreach (var pixel in pixels)
        {
            for (int i = 0; i < 3; i++)
                avgColor[i] += pixel.Color[i];
        }
        for (int i = 0; i < 3; i++)
            avgColor[i] /= pixels.Count;

        // Create initial palette with average color
        var palette = new List<double[]> { avgColor };

        // Expand to desired number of colors
        int targetColors = _options.ColorsPerPalette;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromColor ||
            _options.ColorZeroBehavior == ColorZeroBehavior.TransparentFromTransparent)
            targetColors--;

        while (palette.Count < targetColors)
        {
            // Run iterations to converge current palette
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var nextPixel = pixels[randomShuffle.Next()];
                var (closestIndex, _) = DitherEngine.GetClosestColor(palette, nextPixel.Color);
                ColorUtils.MoveColorCloser(palette[closestIndex], nextPixel.Color, alpha);
            }

            // Find color with maximum error and split it
            var errors = new double[palette.Count];
            foreach (var pixel in pixels)
            {
                var (closestIndex, dist) = DitherEngine.GetClosestColor(palette, pixel.Color);
                errors[closestIndex] += dist;
            }

            int maxErrorIndex = 0;
            for (int i = 1; i < errors.Length; i++)
            {
                if (errors[i] > errors[maxErrorIndex])
                    maxErrorIndex = i;
            }

            // Duplicate the color with max error
            palette.Add(ColorUtils.CloneColor(palette[maxErrorIndex]));
        }

        // Create palettes (initially all the same)
        var palettes = new List<List<double[]>>();
        for (int i = 0; i < _options.PaletteCount; i++)
        {
            var pal = new List<double[]>();
            foreach (var color in palette)
            {
                pal.Add(ColorUtils.CloneColor(color));
            }
            palettes.Add(pal);
        }

        return palettes;
    }

    private void ExpandPalettesByOneColor(List<List<double[]>> palettes, List<TileData> tiles, 
        List<PixelData> pixels, RandomShuffle randomShuffle)
    {
        // From worker.js lines 203-222
        if (palettes.Count == 1)
            return;

        double iterations = _options.FractionOfPixels * pixels.Count;
        if (_options.DitherMode == DitherMode.Slow)
            iterations /= 5;

        double alpha = 0.3;
        if (_options.DitherMode == DitherMode.Slow)
            alpha = 0.1;

        // Find palette with maximum distance
        var paletteDistances = new double[palettes.Count];
        foreach (var tile in tiles)
        {
            for (int i = 0; i < palettes.Count; i++)
            {
                paletteDistances[i] += PaletteDistance(palettes[i], tile);
            }
        }

        int maxPaletteIndex = 0;
        for (int i = 1; i < paletteDistances.Length; i++)
        {
            if (paletteDistances[i] > paletteDistances[maxPaletteIndex])
                maxPaletteIndex = i;
        }

        // Duplicate the palette with max distance
        var newPalette = new List<double[]>();
        foreach (var color in palettes[maxPaletteIndex])
        {
            newPalette.Add(ColorUtils.CloneColor(color));
        }
        palettes.Add(newPalette);

        // Converge the new palette configuration
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            var nextPixel = pixels[randomShuffle.Next()];
            MovePalettesCloser(palettes, nextPixel, alpha);
        }
    }

    private List<List<double[]>> ReplaceWeakestColors(List<List<double[]>> palettes, List<TileData> tiles,
        double minColorFactor, double minPaletteFactor, bool replacePalettes)
    {
        // From worker.js lines 529-653
        bool useSlowDither = _options.DitherMode == DitherMode.Slow;
        
        // Find closest palette for each tile
        var closestPaletteIndex = new int[tiles.Count];
        var totalPaletteMse = new double[palettes.Count];
        var removedPaletteMse = new double[palettes.Count];

        if (palettes.Count > 1)
        {
            for (int j = 0; j < tiles.Count; j++)
            {
                var tile = tiles[j];
                var (index, minDistance) = ClosestPaletteDistance(palettes, tile, useSlowDither);
                totalPaletteMse[index] += minDistance;
                closestPaletteIndex[j] = index;

                // Calculate distance with palette removed
                var remainingPalettes = new List<List<double[]>>();
                for (int i = 0; i < palettes.Count; i++)
                {
                    if (i != index)
                        remainingPalettes.Add(palettes[i]);
                }

                if (remainingPalettes.Count > 0)
                {
                    var (_, minDistance2) = ClosestPaletteDistance(remainingPalettes, tile, useSlowDither);
                    removedPaletteMse[index] += minDistance2;
                }
            }
        }

        // Find color usage within palettes
        var totalColorMse = new List<double[]>();
        var secondColorMse = new List<double[]>();

        foreach (var palette in palettes)
        {
            totalColorMse.Add(new double[palette.Count]);
            secondColorMse.Add(new double[palette.Count]);
        }

        for (int j = 0; j < tiles.Count; j++)
        {
            var tile = tiles[j];
            int minPaletteIndex = closestPaletteIndex[j];
            var pal = palettes[minPaletteIndex];

            if (useSlowDither)
            {
                var ditherEngine = new DitherEngine(_options);
                foreach (var pixel in tile.Pixels)
                {
                    var (minColorIndex, minDist, _) = ditherEngine.ClosestColorDither(pal, pixel);
                    totalColorMse[minPaletteIndex][minColorIndex] += minDist;

                    var remainingColors = new List<double[]>();
                    for (int i = 0; i < pal.Count; i++)
                    {
                        if (i != minColorIndex)
                            remainingColors.Add(pal[i]);
                    }

                    var (_, secondDist, _) = ditherEngine.ClosestColorDither(remainingColors, pixel);
                    secondColorMse[minPaletteIndex][minColorIndex] += secondDist;
                }
            }
            else
            {
                for (int i = 0; i < tile.Colors.Count; i++)
                {
                    var color = tile.Colors[i];
                    var (minColorIndex, minDist) = DitherEngine.GetClosestColor(pal, color);
                    totalColorMse[minPaletteIndex][minColorIndex] += minDist * tile.Counts[i];

                    var remainingColors = new List<double[]>();
                    for (int k = 0; k < pal.Count; k++)
                    {
                        if (k != minColorIndex)
                            remainingColors.Add(pal[k]);
                    }

                    var (_, secondDist) = DitherEngine.GetClosestColor(remainingColors, color);
                    secondColorMse[minPaletteIndex][minColorIndex] += secondDist * tile.Counts[i];
                }
            }
        }

        // Replace weak colors
        var result = new List<List<double[]>>();
        int sharedColorIndex = _options.ColorZeroBehavior == ColorZeroBehavior.Shared ? 0 : -1;

        for (int palIndex = 0; palIndex < palettes.Count; palIndex++)
        {
            var maxColorIndex = MaxIndex(totalColorMse[palIndex]);
            var minColorIndex = MinIndex(secondColorMse[palIndex]);

            var newPalette = new List<double[]>();
            foreach (var color in palettes[palIndex])
            {
                newPalette.Add(ColorUtils.CloneColor(color));
            }

            if (palettes[palIndex].Count > 1 &&
                totalColorMse[palIndex][minColorIndex] < minColorFactor * totalColorMse[palIndex][maxColorIndex] &&
                minColorIndex != sharedColorIndex)
            {
                newPalette[minColorIndex] = ColorUtils.CloneColor(palettes[palIndex][maxColorIndex]);
            }

            result.Add(newPalette);
        }

        return result;
    }

    private List<List<double[]>> KMeans(List<List<double[]>> palettes, List<TileData> tiles)
    {
        // From worker.js lines 655-703
        var colorSums = new List<double[][]>();
        var colorCounts = new List<int[]>();

        foreach (var palette in palettes)
        {
            var sums = new double[palette.Count][];
            var counts = new int[palette.Count];
            for (int i = 0; i < palette.Count; i++)
            {
                sums[i] = new double[3];
            }
            colorSums.Add(sums);
            colorCounts.Add(counts);
        }

        // Accumulate pixel assignments
        foreach (var tile in tiles)
        {
            int paletteIndex = GetClosestPaletteIndex(palettes, tile);
            var palette = palettes[paletteIndex];

            for (int i = 0; i < tile.Colors.Count; i++)
            {
                var color = tile.Colors[i];
                var (colorIndex, _) = DitherEngine.GetClosestColor(palette, color);

                for (int j = 0; j < 3; j++)
                {
                    colorSums[paletteIndex][colorIndex][j] += color[j] * tile.Counts[i];
                }
                colorCounts[paletteIndex][colorIndex] += tile.Counts[i];
            }
        }

        // Compute new centroids
        var result = new List<List<double[]>>();
        for (int i = 0; i < palettes.Count; i++)
        {
            var newPalette = new List<double[]>();
            for (int j = 0; j < palettes[i].Count; j++)
            {
                if (colorCounts[i][j] > 0)
                {
                    var newColor = new double[3];
                    for (int k = 0; k < 3; k++)
                    {
                        newColor[k] = colorSums[i][j][k] / colorCounts[i][j];
                    }
                    newPalette.Add(newColor);
                }
                else
                {
                    newPalette.Add(ColorUtils.CloneColor(palettes[i][j]));
                }
            }
            result.Add(newPalette);
        }

        return result;
    }

    private void MovePalettesCloser(List<List<double[]>> palettes, PixelData pixel, double alpha)
    {
        // Find closest palette and color
        var closestPaletteIndex = 0;
        var closestColorIndex = 0;
        var minDistance = double.MaxValue;

        for (int i = 0; i < palettes.Count; i++)
        {
            var (colorIndex, dist) = DitherEngine.GetClosestColor(palettes[i], pixel.Color);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPaletteIndex = i;
                closestColorIndex = colorIndex;
            }
        }

        // Move the closest color closer to the pixel
        ColorUtils.MoveColorCloser(palettes[closestPaletteIndex][closestColorIndex], pixel.Color, alpha);
    }

    private QuantizationResult QuantizeTiles(List<List<double[]>> palettes, byte[] imageData, 
        int width, int height, bool useDither)
    {
        // From worker.js lines 977-1092
        var reducedPalettes = ReducePalettes(palettes, _options.BitsPerChannel);
        
        int adjustedIndex = 0;
        if (_options.ColorZeroBehavior == ColorZeroBehavior.Shared)
            adjustedIndex = 1;

        var colorZero = ColorUtils.CloneColor(_options.ColorZeroValue);
        ColorUtils.ToNbitColor(colorZero, _options.BitsPerChannel);

        int bmpWidth = (int)Math.Ceiling(width / 4.0) * 4;
        var quantizedData = new byte[imageData.Length];
        var paletteData = new byte[1024];
        var colorIndexes = new byte[bmpWidth * height];

        // Build BMP palette data (BGR format, 4 bytes per color)
        if (_options.PaletteCount * _options.ColorsPerPalette <= 256)
        {
            int i = 0;
            foreach (var pal in reducedPalettes)
            {
                if (adjustedIndex == 1)
                {
                    paletteData[i] = (byte)colorZero[2];     // B
                    paletteData[i + 1] = (byte)colorZero[1]; // G
                    paletteData[i + 2] = (byte)colorZero[0]; // R
                    i += 4;
                }

                foreach (var color in pal)
                {
                    paletteData[i] = (byte)color[2];     // B
                    paletteData[i + 1] = (byte)color[1]; // G
                    paletteData[i + 2] = (byte)color[0]; // R
                    i += 4;
                }
            }
        }

        // Quantize each tile
        var ditherEngine = useDither ? new DitherEngine(_options) : null;

        for (int startY = 0; startY < height; startY += _options.TileHeight)
        {
            for (int startX = 0; startX < width; startX += _options.TileWidth)
            {
                var tile = ExtractTile(imageData, width, height, startX, startY);
                
                int closestPaletteIndex = 0;
                if (tile.Colors.Count > 0)
                {
                    closestPaletteIndex = useDither 
                        ? GetClosestPaletteIndexDither(reducedPalettes, tile)
                        : GetClosestPaletteIndex(reducedPalettes, tile);
                }

                var palette = reducedPalettes[closestPaletteIndex];
                int endX = Math.Min(startX + _options.TileWidth, width);
                int endY = Math.Min(startY + _options.TileHeight, height);

                for (int y = startY; y < endY; y++)
                {
                    for (int x = startX; x < endX; x++)
                    {
                        int index = 4 * (x + width * y);
                        int bmpIndex = x + bmpWidth * (height - 1 - y); // Row flip for BMP

                        var color = new[] { (double)imageData[index], (double)imageData[index + 1], (double)imageData[index + 2] };

                        if (IsPixelTransparent(imageData, index, color))
                        {
                            quantizedData[index] = imageData[index];
                            quantizedData[index + 1] = imageData[index + 1];
                            quantizedData[index + 2] = imageData[index + 2];
                            quantizedData[index + 3] = imageData[index + 3];
                            colorIndexes[bmpIndex] = (byte)(closestPaletteIndex * _options.ColorsPerPalette);
                        }
                        else
                        {
                            int closestColorIndex;
                            if (useDither)
                            {
                                var pixel = new PixelData { Color = color, X = x, Y = y, Tile = tile };
                                (closestColorIndex, _, _) = ditherEngine!.ClosestColorDither(palette, pixel);
                            }
                            else
                            {
                                (closestColorIndex, _) = DitherEngine.GetClosestColor(palette, color);
                            }

                            var paletteColor = palette[closestColorIndex];
                            quantizedData[index] = (byte)paletteColor[0];
                            quantizedData[index + 1] = (byte)paletteColor[1];
                            quantizedData[index + 2] = (byte)paletteColor[2];
                            quantizedData[index + 3] = 255;

                            colorIndexes[bmpIndex] = (byte)(closestPaletteIndex * _options.ColorsPerPalette + 
                                                           closestColorIndex + adjustedIndex);
                        }
                    }
                }
            }
        }

        return new QuantizationResult
        {
            Palettes = reducedPalettes,
            ImageData = quantizedData,
            Width = width,
            Height = height,
            PaletteData = paletteData,
            ColorIndexes = colorIndexes
        };
    }

    // Helper methods

    private List<List<double[]>> ReducePalettes(List<List<double[]>> palettes, int bitsPerChannel)
    {
        var result = new List<List<double[]>>();
        foreach (var palette in palettes)
        {
            var pal = new List<double[]>();
            foreach (var color in palette)
            {
                var col = ColorUtils.CloneColor(color);
                ColorUtils.ToNbitColor(col, bitsPerChannel);
                pal.Add(col);
            }
            result.Add(pal);
        }
        return result;
    }

    private List<List<double[]>> DeepClonePalettes(List<List<double[]>> palettes)
    {
        var result = new List<List<double[]>>();
        foreach (var palette in palettes)
        {
            var pal = new List<double[]>();
            foreach (var color in palette)
            {
                pal.Add(ColorUtils.CloneColor(color));
            }
            result.Add(pal);
        }
        return result;
    }

    private double MeanSquareError(List<List<double[]>> palettes, List<TileData> tiles)
    {
        double sum = 0;
        foreach (var tile in tiles)
        {
            int paletteIndex = GetClosestPaletteIndex(palettes, tile);
            sum += PaletteDistance(palettes[paletteIndex], tile);
        }
        return sum;
    }

    private double PaletteDistance(List<double[]> palette, TileData tile)
    {
        double sum = 0;
        for (int i = 0; i < tile.Colors.Count; i++)
        {
            var (_, minDist) = DitherEngine.GetClosestColor(palette, tile.Colors[i]);
            sum += tile.Counts[i] * minDist;
        }
        return sum;
    }

    private int GetClosestPaletteIndex(List<List<double[]>> palettes, TileData tile)
    {
        if (palettes.Count == 1)
            return 0;

        int minIndex = 0;
        double minDistance = PaletteDistance(palettes[0], tile);

        for (int i = 1; i < palettes.Count; i++)
        {
            double dist = PaletteDistance(palettes[i], tile);
            if (dist < minDistance)
            {
                minIndex = i;
                minDistance = dist;
            }
        }

        return minIndex;
    }

    private int GetClosestPaletteIndexDither(List<List<double[]>> palettes, TileData tile)
    {
        if (palettes.Count == 1)
            return 0;

        var ditherEngine = new DitherEngine(_options);
        int minIndex = 0;
        double minDistance = PaletteDistanceDither(palettes[0], tile, ditherEngine);

        for (int i = 1; i < palettes.Count; i++)
        {
            double dist = PaletteDistanceDither(palettes[i], tile, ditherEngine);
            if (dist < minDistance)
            {
                minIndex = i;
                minDistance = dist;
            }
        }

        return minIndex;
    }

    private double PaletteDistanceDither(List<double[]> palette, TileData tile, DitherEngine ditherEngine)
    {
        double sum = 0;
        foreach (var pixel in tile.Pixels)
        {
            var (_, minDist, _) = ditherEngine.ClosestColorDither(palette, pixel);
            sum += minDist;
        }
        return sum;
    }

    private (int index, double distance) ClosestPaletteDistance(List<List<double[]>> palettes, 
        TileData tile, bool useDither)
    {
        if (useDither)
        {
            var ditherEngine = new DitherEngine(_options);
            var distances = new double[palettes.Count];
            for (int i = 0; i < palettes.Count; i++)
            {
                distances[i] = PaletteDistanceDither(palettes[i], tile, ditherEngine);
            }
            int index = MinIndex(distances);
            return (index, distances[index]);
        }
        else
        {
            var distances = new double[palettes.Count];
            for (int i = 0; i < palettes.Count; i++)
            {
                distances[i] = PaletteDistance(palettes[i], tile);
            }
            int index = MinIndex(distances);
            return (index, distances[index]);
        }
    }

    private int MaxIndex(double[] values)
    {
        int maxIndex = 0;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] > values[maxIndex])
                maxIndex = i;
        }
        return maxIndex;
    }

    private int MinIndex(double[] values)
    {
        int minIndex = 0;
        for (int i = 1; i < values.Length; i++)
        {
            if (values[i] < values[minIndex])
                minIndex = i;
        }
        return minIndex;
    }

    private void UpdateProgress(int progress)
    {
        _progressCallback?.Invoke(progress);
    }
}

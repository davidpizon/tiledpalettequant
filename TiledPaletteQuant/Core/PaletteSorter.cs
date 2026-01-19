namespace TiledPaletteQuant.Core;

/// <summary>
/// Optimizes palette ordering for visual coherence using pairwise distance optimization.
/// From worker.js lines 296-475.
/// </summary>
public static class PaletteSorter
{
    /// <summary>
    /// Sorts palettes and their colors to minimize visual discontinuities.
    /// </summary>
    public static List<List<double[]>> SortPalettes(List<List<double[]>> palettes, int startIndex)
    {
        const int pairIterations = 2000;
        const int tIterations = 10000;
        const int paletteIterations = 100000;
        const int upWeight = 2;

        int numPalettes = palettes.Count;
        int numColors = palettes[0].Count;

        // Early return for simple case
        if (numColors == 2 && startIndex == 1)
        {
            return palettes;
        }

        var random = new Random();

        // paletteDist[i+1][j+1] stores distance between palette i and palette j
        var paletteDist = CreateMatrix(numPalettes + 2, numPalettes + 2);

        // colorIndex[p1][p2][i] stores the index of the closest color in p2 from color index i in p1
        var colorIndex = new int[numPalettes][][];
        for (int i = 0; i < numPalettes; i++)
        {
            colorIndex[i] = new int[numPalettes][];
            for (int j = 0; j < numPalettes; j++)
            {
                colorIndex[i][j] = new int[numColors];
                for (int k = 0; k < numColors; k++)
                {
                    colorIndex[i][j][k] = k;
                }
            }
        }

        // Optimize pairwise color mappings between palettes
        for (int p1 = 0; p1 < numPalettes - 1; p1++)
        {
            for (int p2 = p1 + 1; p2 < numPalettes; p2++)
            {
                var index = colorIndex[p1][p2];

                for (int iteration = 0; iteration < pairIterations; iteration++)
                {
                    int i1 = startIndex + random.Next(numColors - startIndex - 1);
                    int i2 = i1 + 1 + random.Next(numColors - i1 - 1);

                    if (random.NextDouble() < 0.5)
                    {
                        (i1, i2) = (i2, i1);
                    }

                    var p1i1 = palettes[p1][i1];
                    var p1i2 = palettes[p1][i2];
                    var p2i1 = palettes[p2][index[i1]];
                    var p2i2 = palettes[p2][index[i2]];

                    double straightDist = ColorUtils.ColorDistance(p1i1, p2i1) + ColorUtils.ColorDistance(p1i2, p2i2);
                    double swappedDist = ColorUtils.ColorDistance(p1i1, p2i2) + ColorUtils.ColorDistance(p1i2, p2i1);

                    if (swappedDist < straightDist)
                    {
                        (index[i1], index[i2]) = (index[i2], index[i1]);
                    }
                }

                // Compute total distance between palettes
                double sum = 0;
                for (int i = 0; i < numColors; i++)
                {
                    var p1i = palettes[p1][i];
                    var p2i = palettes[p2][index[i]];
                    sum += ColorUtils.ColorDistance(p1i, p2i);
                }

                paletteDist[p1 + 1][p2 + 1] = sum;
                paletteDist[p2 + 1][p1 + 1] = sum;
            }
        }

        // Build reverse color indices
        for (int p1 = 1; p1 < numPalettes; p1++)
        {
            for (int p2 = 0; p2 < p1; p2++)
            {
                var index = colorIndex[p2][p1];
                var revIndex = colorIndex[p1][p2];

                for (int i = 0; i < numColors; i++)
                {
                    revIndex[i] = Array.IndexOf(index, i);
                }
            }
        }

        // Optimize palette ordering
        var palIndex = new int[numPalettes + 2];
        for (int i = 0; i < numPalettes + 2; i++)
        {
            palIndex[i] = i;
        }

        if (numPalettes > 2)
        {
            for (int iteration = 0; iteration < paletteIterations; iteration++)
            {
                int index1 = Math.Max(1, random.Next(numPalettes));
                int index2 = Math.Min(numPalettes, index1 + 1 + random.Next(numPalettes));

                int i1b = palIndex[index1 - 1];
                int i1 = palIndex[index1];
                int i2 = palIndex[index2];
                int i2b = palIndex[index2 + 1];

                double straightDist = paletteDist[i1b][i1] + paletteDist[i2][i2b];
                double swappedDist = paletteDist[i1b][i2] + paletteDist[i1][i2b];

                if (swappedDist < straightDist)
                {
                    Reverse(palIndex, index1, index2);
                }
            }
        }

        // Optimize color ordering within first palette
        var pal1 = palettes[palIndex[1] - 1];
        var p1Index = new int[numColors + 2];
        for (int i = 0; i < numColors + 2; i++)
        {
            p1Index[i] = i;
        }

        var p1Dist = CreateMatrix(numColors + 2, numColors + 2);
        for (int i = 1; i <= numColors; i++)
        {
            for (int j = 1; j <= numColors; j++)
            {
                p1Dist[i][j] = ColorUtils.ColorDistance(pal1[i - 1], pal1[j - 1]);
            }
        }

        if (numColors > 2)
        {
            for (int iteration = 0; iteration < paletteIterations; iteration++)
            {
                int index1 = Math.Max(1 + startIndex, random.Next(numColors));
                int index2 = Math.Min(numColors, index1 + 1 + random.Next(numColors));

                int i1b = p1Index[index1 - 1];
                int i1 = p1Index[index1];
                int i2 = p1Index[index2];
                int i2b = p1Index[index2 + 1];

                double straightDist = p1Dist[i1b][i1] + p1Dist[i2][i2b];
                double swappedDist = p1Dist[i1b][i2] + p1Dist[i1][i2b];

                if (swappedDist < straightDist)
                {
                    Reverse(p1Index, index1, index2);
                }
            }
        }

        // Build final color index mapping
        var pIndex = new int[numPalettes][];
        for (int i = 0; i < numPalettes; i++)
        {
            pIndex[i] = new int[numColors];
        }

        for (int i = 0; i < numColors; i++)
        {
            pIndex[0][i] = p1Index[i + 1] - 1;
        }

        for (int i = 1; i < numPalettes; i++)
        {
            for (int j = 0; j < numColors; j++)
            {
                int p1 = palIndex[i] - 1;
                int p2 = palIndex[i + 1] - 1;
                pIndex[i][j] = colorIndex[p1][p2][pIndex[i - 1][j]];
            }
        }

        // T-junction optimization (minimize discontinuities across palette transitions)
        if (numColors >= 4)
        {
            for (int i = 1; i < numPalettes; i++)
            {
                int p1 = palIndex[i] - 1;
                int p2 = palIndex[i + 1] - 1;
                int iteration = 0;

                while (iteration < tIterations)
                {
                    int index1 = Math.Max(startIndex, random.Next(numColors));
                    int index2 = Math.Max(startIndex, random.Next(numColors));

                    if (index1 == index2)
                        continue;

                    int up1 = pIndex[i - 1][index1];
                    int i1 = pIndex[i][index1];
                    int left1 = pIndex[i][index1 - 1];
                    int right1 = pIndex[i][index1 + 1];

                    int up2 = pIndex[i - 1][index2];
                    int i2 = pIndex[i][index2];
                    int left2 = pIndex[i][index2 - 1];
                    int right2 = pIndex[i][index2 + 1];

                    double straightDist = upWeight * ColorUtils.ColorDistance(palettes[p2][i1], palettes[p1][up1]);
                    if (left1 >= 0)
                        straightDist += ColorUtils.ColorDistance(palettes[p2][i1], palettes[p2][left1]);
                    if (right1 < numColors)
                        straightDist += ColorUtils.ColorDistance(palettes[p2][i1], palettes[p2][right1]);

                    straightDist += upWeight * ColorUtils.ColorDistance(palettes[p2][i2], palettes[p1][up2]);
                    if (left2 >= 0)
                        straightDist += ColorUtils.ColorDistance(palettes[p2][i2], palettes[p2][left2]);
                    if (right2 < numColors)
                        straightDist += ColorUtils.ColorDistance(palettes[p2][i2], palettes[p2][right2]);

                    double swappedDist = upWeight * ColorUtils.ColorDistance(palettes[p2][i2], palettes[p1][up1]);
                    if (left1 >= 0)
                        swappedDist += ColorUtils.ColorDistance(palettes[p2][i2], palettes[p2][left1]);
                    if (right1 < numColors)
                        swappedDist += ColorUtils.ColorDistance(palettes[p2][i2], palettes[p2][right1]);

                    swappedDist += upWeight * ColorUtils.ColorDistance(palettes[p2][i1], palettes[p1][up2]);
                    if (left2 >= 0)
                        swappedDist += ColorUtils.ColorDistance(palettes[p2][i1], palettes[p2][left2]);
                    if (right2 < numColors)
                        swappedDist += ColorUtils.ColorDistance(palettes[p2][i1], palettes[p2][right2]);

                    if (swappedDist < straightDist)
                    {
                        (pIndex[i][index1], pIndex[i][index2]) = (pIndex[i][index2], pIndex[i][index1]);
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
                pal.Add(palettes[p2][pIndex[i][j]]);
            }

            pals.Add(pal);
        }

        return pals;
    }

    private static double[][] CreateMatrix(int rows, int cols)
    {
        var matrix = new double[rows][];
        for (int i = 0; i < rows; i++)
        {
            matrix[i] = new double[cols];
        }
        return matrix;
    }

    private static void Reverse(int[] array, int start, int end)
    {
        while (start < end)
        {
            (array[start], array[end]) = (array[end], array[start]);
            start++;
            end--;
        }
    }
}

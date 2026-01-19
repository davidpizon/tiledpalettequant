namespace TiledPaletteQuant.IO;

/// <summary>
/// Writes 8-bit indexed color BMP files.
/// From script.js lines 274-321.
/// </summary>
public static class BmpWriter
{
    /// <summary>
    /// Creates a BMP file from palette data and color indexes.
    /// </summary>
    public static void WriteBmp(string path, int width, int height, byte[] paletteData, byte[] colorIndexes)
    {
        int bmpFileSize = 54 + paletteData.Length + colorIndexes.Length;
        var bmpData = new byte[bmpFileSize];

        // BMP Header (14 bytes)
        bmpData[0] = 66;  // 'B'
        bmpData[1] = 77;  // 'M'
        Write32Le(bmpData, 2, bmpFileSize);  // File size
        Write32Le(bmpData, 6, 0);            // Reserved
        Write32Le(bmpData, 0x0A, 54 + paletteData.Length); // Pixel data offset

        // DIB Header (40 bytes)
        Write32Le(bmpData, 0x0E, 40);        // DIB header size
        Write32Le(bmpData, 0x12, width);     // Width
        Write32Le(bmpData, 0x16, height);    // Height
        Write16Le(bmpData, 0x1A, 1);         // Planes
        Write16Le(bmpData, 0x1C, 8);         // Bits per pixel
        Write32Le(bmpData, 0x1E, 0);         // Compression (none)
        Write32Le(bmpData, 0x22, colorIndexes.Length); // Image size
        Write32Le(bmpData, 0x26, 2835);      // X pixels per meter
        Write32Le(bmpData, 0x2A, 2835);      // Y pixels per meter
        Write32Le(bmpData, 0x2E, 256);       // Colors in palette
        Write32Le(bmpData, 0x32, 0);         // Important colors

        // Copy palette data (1024 bytes - 256 colors * 4 bytes each)
        for (int i = 0; i < paletteData.Length; i++)
        {
            bmpData[i + 54] = paletteData[i];
        }

        // Copy pixel data
        int imageDataAddress = 54 + paletteData.Length;
        for (int i = 0; i < colorIndexes.Length; i++)
        {
            bmpData[i + imageDataAddress] = colorIndexes[i];
        }

        File.WriteAllBytes(path, bmpData);
    }

    private static void Write32Le(byte[] buffer, int index, int value)
    {
        buffer[index] = (byte)(value % 256);
        value /= 256;
        buffer[index + 1] = (byte)(value % 256);
        value /= 256;
        buffer[index + 2] = (byte)(value % 256);
        value /= 256;
        buffer[index + 3] = (byte)(value % 256);
    }

    private static void Write16Le(byte[] buffer, int index, int value)
    {
        buffer[index] = (byte)(value % 256);
        value /= 256;
        buffer[index + 1] = (byte)(value % 256);
    }
}

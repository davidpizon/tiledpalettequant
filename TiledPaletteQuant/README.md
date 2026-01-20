# Tiled Palette Quantization Tool (C#)

C# console application port of the JavaScript tiled palette quantization tool.

## Features

- **Tile-based palette quantization**: Divides images into tiles and assigns optimal color palettes per tile
- **Multiple dithering modes**: Off, Fast, and Slow dithering with 6 pattern options
- **K-means palette optimization**: Refines palettes for better color accuracy
- **BMP indexed color output**: Generates 8-bit indexed BMPs for ≤256 total colors
- **PNG output**: For images requiring more than 256 colors
- **Command-line and interactive modes**: Flexible usage options
- **Cross-platform**: Works on Windows, Linux, and macOS

## Requirements

- .NET 8.0 SDK or later

## Installation

```bash
git clone https://github.com/davidpizon/tiledpalettequant.git
cd tiledpalettequant
dotnet build
```

## Usage

### Interactive Mode

Run without arguments for an interactive prompt:

```bash
dotnet run --project TiledPaletteQuant
```

### Command Line Mode

```bash
# Basic usage
dotnet run --project TiledPaletteQuant -- -i input.png -o output.bmp

# Custom tile and palette settings
dotnet run --project TiledPaletteQuant -- -i input.png -o output.bmp -tw 8 -th 8 -p 8 -c 4

# With dithering
dotnet run --project TiledPaletteQuant -- -i input.png -d slow -dp diagonal4 -f 0.2

# High quality settings
dotnet run --project TiledPaletteQuant -- -i input.png -d slow -b 6 -p 16 -c 8
```

## Command-Line Options

| Option | Long Form | Description | Default |
|--------|-----------|-------------|---------|
| `-i` | `--input` | Input image path (required) | - |
| `-o` | `--output` | Output image path | Auto-generated |
| `-tw` | `--tile-width` | Tile width in pixels | 8 |
| `-th` | `--tile-height` | Tile height in pixels | 8 |
| `-p` | `--palettes` | Number of palettes | 8 |
| `-c` | `--colors` | Colors per palette | 4 |
| `-b` | `--bits` | Bits per channel (2-8) | 5 |
| `-d` | `--dither` | Dither mode: off, fast, slow | off |
| `-dp` | `--dither-pattern` | Pattern: diagonal4, horizontal4, vertical4, diagonal2, horizontal2, vertical2 | diagonal4 |
| `-f` | `--fraction` | Fraction of pixels for refinement (0.0-1.0) | 0.1 |

## Algorithm Overview

The quantization algorithm:

1. **Image Reduction**: Optionally reduces color precision to n-bit per channel
2. **Tile Extraction**: Divides image into tiles and builds color histograms
3. **Initial Palette Generation**: Creates base palettes using iterative color splitting
4. **Palette Expansion**: Progressively increases palette count with error-based optimization
5. **Weak Color Replacement**: Replaces underutilized colors (10 iterations)
6. **Final Refinement**: Intensive optimization pass (10x iterations)
7. **K-means Clustering**: Refines palettes using k-means (3 iterations, non-dither only)
8. **Palette Sorting**: Optimizes palette order for visual coherence
9. **Final Quantization**: Applies palettes to generate output image

### Key Parameters

- **Color Distance**: Weighted Euclidean with weights R=2, G=4, B=1
- **Alpha Values**: Initial=0.3, Final=0.05 (or 0.1/0.02 for slow dither)
- **Iterations**: Based on `fractionOfPixels × totalPixels`

## Examples

### Game Boy-style (4 colors, no dither)
```bash
dotnet run --project TiledPaletteQuant -- -i photo.png -p 1 -c 4 -b 8
```

### NES-style (8x8 tiles, 4 palettes)
```bash
dotnet run --project TiledPaletteQuant -- -i sprite.png -tw 8 -th 8 -p 4 -c 4
```

### High-quality with dithering
```bash
dotnet run --project TiledPaletteQuant -- -i landscape.png -d slow -dp diagonal4 -p 16 -c 16
```

## Testing

Run the test suite:

```bash
dotnet test
```

For verbose output:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Project Structure

```
TiledPaletteQuant/
├── TiledPaletteQuant/           # Main console application
│   ├── Core/                    # Core quantization algorithms
│   │   ├── ColorUtils.cs        # Color operations and conversions
│   │   ├── DitherEngine.cs      # Dithering with error diffusion
│   │   ├── PaletteSorter.cs     # Palette optimization
│   │   ├── RandomShuffle.cs     # Fisher-Yates shuffling
│   │   └── TiledPaletteQuantizer.cs  # Main algorithm
│   ├── Models/                  # Data models
│   │   ├── Enums.cs            # ColorZeroBehavior, DitherMode, DitherPattern
│   │   ├── QuantizationOptions.cs   # Configuration with CLI parsing
│   │   ├── QuantizationResult.cs    # Output data
│   │   ├── TileData.cs         # Tile information
│   │   └── PixelData.cs        # Pixel information
│   ├── IO/                     # Input/Output operations
│   │   ├── ImageProcessor.cs   # Image loading and saving
│   │   └── BmpWriter.cs        # BMP file generation
│   └── Program.cs              # Entry point
└── TiledPaletteQuant.Tests/    # Test project
    ├── Core/                   # Core algorithm tests
    ├── Models/                 # Model tests
    ├── IO/                     # I/O tests
    └── Integration/            # End-to-end tests
```

## Algorithm Fidelity

This C# implementation faithfully ports the JavaScript algorithm from [rilden/tiledpalettequant](https://github.com/rilden/tiledpalettequant), maintaining:

- Exact color distance formula: `2×(ΔR)² + 4×(ΔG)² + (ΔB)²`
- Alpha quantization values: `[0, 255, 85, 36.42857, 17, 8.22581, 4.04762, 2.00787, 1]`
- Iteration counts and convergence parameters
- Dither pattern matrices
- Palette sorting optimization steps

## Contributing

Contributions are welcome! Please ensure:

- All tests pass
- Code follows .NET conventions
- XML documentation is provided for public APIs
- Changes maintain algorithm fidelity

## License

See LICENSE file for details.

## Acknowledgments

Based on the JavaScript implementation by rilden:
https://github.com/rilden/tiledpalettequant

## Author

Ported to C# by davidpizon

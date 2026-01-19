# Tiled Palette Quantization

A C# console application for converting images to tiled palette format. This tool applies color quantization using a tiled approach where the image is divided into tiles, and each tile is assigned one of several optimized palettes.

## Features

- **Tiled Palette Quantization**: Divide images into tiles and assign optimal palettes
- **Multiple Dithering Modes**: Off, Fast, and Slow dithering support
- **Flexible Configuration**: Customizable tile sizes, palette counts, and color depths
- **Color Zero Handling**: Multiple modes for transparent or shared colors
- **K-means Optimization**: Iterative palette refinement for best quality
- **Command-line Interface**: Easy to use with comprehensive options

## Installation

### Prerequisites
- .NET 8.0 or higher

### Build from Source
```bash
cd TiledPaletteQuant
dotnet build -c Release
```

## Usage

### Basic Usage
```bash
dotnet run -- -i input.png -o output.png
```

### With Custom Settings
```bash
dotnet run -- -i input.png -tw 16 -th 16 -p 4 -c 16 -b 5
```

### With Dithering
```bash
dotnet run -- -i input.png -d slow -dp diagonal4 -dw 0.5
```

## Command-line Options

| Option | Long Form | Description | Default |
|--------|-----------|-------------|---------|
| `-i` | `--input` | Input image path (required) | - |
| `-o` | `--output` | Output image path | `<input>_quantized.png` |
| `-tw` | `--tile-width` | Tile width in pixels | 8 |
| `-th` | `--tile-height` | Tile height in pixels | 8 |
| `-p` | `--palettes` | Number of palettes | 8 |
| `-c` | `--colors` | Colors per palette | 4 |
| `-b` | `--bits` | Bits per channel (1-8) | 5 |
| `-f` | `--fraction` | Fraction of pixels for refinement (0.0-1.0) | 0.1 |
| `-d` | `--dither` | Dither mode: off, fast, slow | off |
| `-dp` | `--dither-pattern` | Pattern: diagonal4, horizontal4, vertical4, diagonal2, horizontal2, vertical2 | diagonal4 |
| `-dw` | `--dither-weight` | Dither weight (0.0-1.0) | 0.5 |
| `-cz` | `--color-zero` | Behavior: unique, shared, transparentfromtransparent, transparentfromcolor | unique |
| `-sc` | `--shared-color` | Shared color in hex (e.g., FF0000) | 000000 |
| `-tc` | `--transparent-color` | Transparent color in hex (e.g., FF00FF) | 000000 |
| `-h` | `--help` | Show help message | - |

## Algorithm Overview

The tiled palette quantization algorithm works as follows:

1. **Tile Extraction**: The input image is divided into rectangular tiles
2. **Initial Palette Generation**: Create initial palettes using color clustering
3. **Palette Expansion**: Gradually expand palettes to the target color count
4. **Iterative Refinement**: Move palette colors closer to pixel colors using a k-means-like approach
5. **Weak Color Replacement**: Replace underutilized colors with better candidates
6. **K-means Optimization**: Final refinement using k-means clustering
7. **Palette Sorting**: Organize palettes for smooth transitions (traveling salesman-style)
8. **Quantization**: Apply the optimized palettes to generate the output image
9. **Optional Dithering**: Apply error diffusion for smoother gradients

### Color Distance

The algorithm uses a weighted RGB distance formula that gives more importance to green:
```
distance = 2*(r1-r2)² + 4*(g1-g2)² + (b1-b2)²
```

### Dithering

- **Fast Dither**: Quick dithering using pattern-based color selection
- **Slow Dither**: Higher quality dithering with error diffusion and brightness sorting

## Examples

### Retro Game Graphics
```bash
dotnet run -- -i sprite.png -tw 8 -th 8 -p 8 -c 4 -b 5
```

### High Quality with Dithering
```bash
dotnet run -- -i photo.png -tw 16 -th 16 -p 4 -c 16 -d slow
```

### Transparent Background
```bash
dotnet run -- -i logo.png -cz transparentfromtransparent
```

## Technical Details

- **Language**: C# (.NET 8.0+)
- **Image Library**: SixLabors.ImageSharp
- **Color Space**: sRGB with linear interpolation for calculations
- **Optimization**: k-means clustering with iterative refinement

## Credits

This C# implementation is based on the original JavaScript web application by [rilden](https://github.com/rilden).

Original project: [Tiled Palette Quantization](https://github.com/rilden/tiledpalettequant)

## License

MIT License - see LICENSE file for details.

Copyright (c) 2022 rilden (original JavaScript implementation)

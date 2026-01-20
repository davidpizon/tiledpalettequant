using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiledPaletteQuant
{
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
}

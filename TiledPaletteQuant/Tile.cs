using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiledPaletteQuant
{
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

}

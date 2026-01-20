using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiledPaletteQuant
{


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
}

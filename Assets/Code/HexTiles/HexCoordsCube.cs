using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexTiles
{
    /// <summary>
    /// Cube coordinates for a hex tile.
    /// </summary>
    public struct HexCoordsCube
    {
        public int X;
        public int Y;
        public int Z;

        /// <summary>
        /// Convert back to axial coordinates
        /// </summary>
        public HexCoords ToAxial()
        {
            return new HexCoords(X, Z);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexTiles
{
    /// <summary>
    /// Coordinates of the hex tile (longitude, latitude, elevation)
    /// </summary>
    [Serializable]
    public class HexCoords : Object
    {
        public int col = 0;

        public int row = 0;

        public float elevation = 0f;
    }
}

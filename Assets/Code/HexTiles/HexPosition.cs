using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexTiles
{
    /// <summary>
    /// Necessary information for positioning a hex tile.
    /// </summary>
    [Serializable]
    public struct HexPosition
    {
        public HexPosition(HexCoords coordinates, float elevation)
        {
            Coordinates = coordinates;
            Elevation = elevation;
        }

        public HexCoords Coordinates;
        public float Elevation;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    /// <summary>
    /// Necessary information for positioning a hex tile.
    /// </summary>
    [Serializable]
    public class HexPosition
    {
        public HexPosition()
        {
            Coordinates = new HexCoords(0, 0);
            Elevation = 0f;
        }

        public HexPosition(HexCoords coordinates, float elevation)
        {
            Coordinates = coordinates;
            Elevation = elevation;
        }

        public HexCoords Coordinates;
        public float Elevation;

        /// <summary>
        /// Returns the position of this tile (relative to tile [0, 0] being at 
        /// position [0, 0, 0]), assuming the tiles are of equal size of the width provided.
        /// </summary>
        public Vector3 GetPositionVector(float hexWidth)
        {
            var x = hexWidth/2f * 3f/2f * Coordinates.Q;
            var z = hexWidth/2f * Mathf.Sqrt(3f) * (Coordinates.R + Coordinates.Q / 2f);
            var y = Elevation;
            return new Vector3(x, y, z);
        }
    }
}

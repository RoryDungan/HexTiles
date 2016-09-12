using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    /// <summary>
    /// Data structure with all the information about a single tile on the tile map.
    /// </summary>
    [Serializable]
    public class HexTileData
    {
        public HexTileData(HexPosition position, float diameter, Material material)
        {
            Position = position;
            Diameter = diameter;
            Material = material;
        }

        /// <summary>
        /// Position (coordinates and elevation) of the tile.
        /// </summary>
        public HexPosition Position { get; private set; }

        /// <summary>
        /// Width of the tile (assuming it is a flat topped hex)
        /// </summary>
        public float Diameter { get; private set; }

        /// <summary>
        /// Material applied to the tile.
        /// </summary>
        public Material Material { get; private set; }
    }
}

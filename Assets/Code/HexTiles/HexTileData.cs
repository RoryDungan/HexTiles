using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    [Serializable]
    public class HexTileData
    {
        public HexTileData(HexPosition position, float diameter, Material material)
        {
            Position = position;
            Diameter = diameter;
            Material = material;
        }

        public HexPosition Position { get; private set; }

        public float Diameter { get; private set; }

        public Material Material { get; private set; }
    }
}

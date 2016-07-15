using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    class HexMetrics
    {
        /// <summary>
        /// sqrt(3)/2
        /// The ratio of a flat-topped hexagon's height to its width.
        /// </summary>
        public static readonly float hexHeightToWidth = 0.86602540378f;

        /// <summary>
        /// Returns the 6 vertices of a hex of a certain size.
        /// </summary>
        public static Vector3[] GetHexVertices(float size)
        {
            var vertices = new Vector3[6];

            vertices[0].Set(-0.5f * size, 0f, 0f);
            vertices[1].Set(-0.25f * size, 0f, 0.5f * hexHeightToWidth * size);
            vertices[2].Set(0.25f * size, 0f, 0.5f * hexHeightToWidth * size);
            vertices[3].Set(0.5f * size, 0f, 0f);
            vertices[4].Set(0.25f * size, 0f, -0.5f * hexHeightToWidth * size);
            vertices[5].Set(-0.25f * size, 0f, -0.5f * hexHeightToWidth * size);

            return vertices;
        }

        /// <summary>
        /// Array of possible locations for adjacent hexes.
        /// </summary>
        public static readonly HexCoords[] AdjacentHexes = new HexCoords[]
        {
            new HexCoords(-1,  1),
            new HexCoords( 0,  1),
            new HexCoords( 1,  0),
            new HexCoords( 1, -1),
            new HexCoords( 0, -1),
            new HexCoords(-1,  0)
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexTiles
{
    /// <summary>
    /// Side piece for the hex tile.
    /// </summary>
    [Serializable]
    internal struct SidePiece
    {
        public int direction;
        public float elevationDelta;
    }
}

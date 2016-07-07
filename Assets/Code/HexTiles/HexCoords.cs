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
        public HexCoords(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q = 0;

        public int R = 0;

        public float Elevation = 0f;

        public static HexCoords operator+(HexCoords a, HexCoords b)
        {
            return new HexCoords(a.Q + b.Q, a.R + b.R);
        }

        public static HexCoords operator-(HexCoords a, HexCoords b)
        {
            return new HexCoords(a.Q - b.Q, a.R - b.R);
        }
        // override object.GetHashCode
        // Taken from http://stackoverflow.com/questions/5221396/what-is-an-appropriate-gethashcode-algorithm-for-a-2d-point-struct-avoiding
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                var hash = 17;
                hash = hash * 23 + Q.GetHashCode();
                hash = hash * 23 + R.GetHashCode();
                return hash;
            }
        }

        // override object.Equals
        public override bool Equals (object obj)
        {
            //
            // See the full list of guidelines at
            //   http://go.microsoft.com/fwlink/?LinkID=85237
            // and also the guidance for operator== at
            //   http://go.microsoft.com/fwlink/?LinkId=85238
            //
            
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var hexCoords = (HexCoords)obj;
            return hexCoords.Q == Q && hexCoords.R == R;
        }
    }
}

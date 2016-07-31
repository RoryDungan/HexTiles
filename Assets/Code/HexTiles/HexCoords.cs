using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    /// <summary>
    /// Coordinates of the hex tile (longitude, latitude, elevation)
    /// </summary>
    [Serializable]
    public class HexCoords : System.Object
    {
        public HexCoords(int q, int r)
        {
            Q = q;
            R = r;
        }

        public int Q = 0;

        public int R = 0;


        public static HexCoords operator+(HexCoords a, HexCoords b)
        {
            return new HexCoords(a.Q + b.Q, a.R + b.R);
        }

        public static HexCoords operator-(HexCoords a, HexCoords b)
        {
            return new HexCoords(a.Q - b.Q, a.R - b.R);
        }

        /// <summary>
        /// Return the value of this position in odd-q offset coordinates.
        /// </summary>
        public Vector2 ToOffset()
        {
            return new Vector2(Q, R + (Q - (Q&1)) / 2f);
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

        public override string ToString()
        {
            return string.Format("({0}, {1})", Q, R);
        }
    }
}

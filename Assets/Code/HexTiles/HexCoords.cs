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
        /// Get the distance between this and another hex.
        /// </summary>
        public int Distance(HexCoords other)
        {
            return (Math.Abs(this.Q - other.Q)
                + Math.Abs(this.Q + this.R - other.Q - other.R)
                + Math.Abs(this.R - other.R)) / 2;
        }

        /// <summary>
        /// Return whether or not this tile is within the squarish bounds of two other points.
        /// </summary>
        public bool IsWithinBounds(HexCoords a, HexCoords b)
        {
            return Q >= a.Q && Q < b.Q && R >= a.R && R < b.R;
        }

        /// <summary>
        /// Get an array of hexes within a certain range of this one.
        /// </summary>
        public HexCoords[] CoordinateRange(int range)
        {
            if (range < 0)
            {
                throw new ArgumentOutOfRangeException("range", "Range must be at least 1");
            }

            if (range == 0)
            {
                return new HexCoords[] { this };
            }

            var results = new List<HexCoords>();
            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = Math.Max(-range, -dx-range); dy <= Math.Min(range, -dx+range); dy++)
                {
                    var dz = -dx-dy;
                    results.Add(this + new HexCoords(dx, dz));
                }
            }

            return results.ToArray();
            
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
        public override bool Equals(object obj)
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

        public static bool operator ==(HexCoords a, HexCoords b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Q == b.Q && a.R == b.R;
        }

        public static bool operator !=(HexCoords a, HexCoords b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", Q, R);
        }
    }
}

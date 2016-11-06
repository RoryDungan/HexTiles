using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    public static class Utils
    {
        /// <summary>
        /// Returns the modulo of two floats. Needed because C#'s '%' operator
        /// actually returns the remainder of integer division as opposed to a modulo.
        /// See http://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain 
        /// </summary>
        public static float Mod(float a, float b)
        {
            return (a % b + b) % b;
        }
    }
}

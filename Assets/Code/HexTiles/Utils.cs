using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

        /// <summary>
        /// Destroy an object, recording an undo action if we're in the editor.
        /// </summary>
        /// <param name="objectToDestroy"></param>
        public static void Destroy(UnityEngine.Object objectToDestroy)
        {
#if UNITY_EDITOR
            Undo.DestroyObjectImmediate(objectToDestroy);
#else
            UnityEngine.Object.Destroy(objectToDestroy);
#endif
        }
    }
}

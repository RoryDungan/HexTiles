using UnityEngine;
using System.Collections;
using UnityEditor;

namespace HexTiles.Editor
{
    public class HexTileCounter : ScriptableObject
    {
        [MenuItem("Tools/Count hex tiles")]
        static void CountTilesClicked()
        {
            var regularTileCount = GameObject.FindObjectsOfType<HexTile>().Length;
            var chunkCount = GameObject.FindObjectsOfType<HexChunk>().Length;

            var message = string.Format("Individual tiles: {0}\nChunks: {1}", regularTileCount, chunkCount);

            EditorUtility.DisplayDialog("Hex tile count", message, "Ok");
        }
    }
}

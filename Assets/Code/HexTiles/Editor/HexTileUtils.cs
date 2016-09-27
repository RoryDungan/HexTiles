using UnityEngine;
using System.Collections;
using UnityEditor;

namespace HexTiles.Editor
{
    public class HexTileUtils : ScriptableObject
    {
        [MenuItem("Tools/Count hex tiles")]
        static void CountTilesClicked()
        {
            var regularTileCount = GameObject.FindObjectsOfType<HexTile>().Length;
            var chunkCount = GameObject.FindObjectsOfType<HexChunk>().Length;

            var message = string.Format("Individual tiles: {0}\nChunks: {1}", regularTileCount, chunkCount);

            EditorUtility.DisplayDialog("Hex tile count", message, "Ok");
        }

        [MenuItem("Tools/Regenerate all hex tiles in all HexTileMaps")]
        static void RegenerateAllHexTiles()
        {
            foreach (var map in GameObject.FindObjectsOfType<HexTileMap>())
            {
                map.RegenerateAllTiles();
            }

            EditorUtility.DisplayDialog("Finished regenerating tiles.", "Finished regenerating meshes for all hex tiles in all HexTileMaps.", "Ok");
        }
    }
}

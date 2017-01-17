using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

namespace HexTiles.Editor
{
    public class HexTileUtils : ScriptableObject
    {
        [MenuItem("Hex tiles/Count hex tiles", false, 0)]
        static void CountTilesClicked()
        {
            var chunks = GameObject.FindObjectsOfType<HexChunk>();
            var chunkCount = chunks.Length;
            var regularTileCount = chunks.SelectMany(chunk => chunk.Tiles).Count();

            var message = string.Format("Individual tiles: {0}\nChunks: {1}", regularTileCount, chunkCount);

            EditorUtility.DisplayDialog("Hex tile count", message, "Ok");
        }

        [MenuItem("Hex tiles/Regenerate all hex tiles in all HexTileMaps", false, 1)]
        static void RegenerateAllHexTiles()
        {
            foreach (var map in GameObject.FindObjectsOfType<HexTileMap>())
            {
                map.RegenerateAllTiles();
                MarkSceneDirty(map.gameObject); 
            }

            EditorUtility.DisplayDialog("Finished regenerating tiles.", "Finished regenerating meshes for all hex tiles in all HexTileMaps.", "Ok");
        }

        [MenuItem("GameObject/Hex tile map", false, 10)]
        static void CreateNewHexTileMap(MenuCommand menuCommand)
        {
            var go = new GameObject("Hex tile map");
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            go.AddComponent(typeof(HexTileMap));

            Undo.RegisterCreatedObjectUndo(go, "Created new hex tile map");

            Selection.activeGameObject = go;
        }

        /// <summary>
        /// Mark the scene for a specified object dirty
        /// </summary>
        private static void MarkSceneDirty(GameObject obj)
        {
#if UNITY_5_3_OR_NEWER
            EditorSceneManager.MarkSceneDirty(obj.scene);
#else
            EditorUtility.SetDirty(obj);
#endif
       }
    }
}

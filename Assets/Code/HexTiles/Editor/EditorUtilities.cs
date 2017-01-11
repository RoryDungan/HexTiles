using UnityEngine;
using UnityEditor;

namespace HexTiles.Editor
{
    internal static class EditorUtilities
    {
        /// <summary>
        /// Show a UI with some information about the selected tool.
        /// </summary>
        internal static void ShowHelpBox(string toolName, string description)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(toolName);
            GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }
    }
}
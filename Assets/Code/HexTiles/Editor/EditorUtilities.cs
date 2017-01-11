using UnityEngine;
using UnityEditor;
using System;

namespace HexTiles.Editor
{
    /// <summary>
    /// Collection of useful pieces of code for displaying things in 
    /// the inspector.
    /// </summary>
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

        /// <summary>
        /// Displays an int field that we can't edit with a label next
        /// to it.
        /// </summary>
        internal static void ShowReadonlyIntField(string name, int value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(EditorGUIUtility.labelWidth));
            GUI.enabled = false;
            EditorGUILayout.IntField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays a float field that we can't editor with a label next
        /// to it.
        /// </summary>
        internal static void ShowReadonlyFloatField(string name, float value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(name, GUILayout.Width(EditorGUIUtility.labelWidth));
            GUI.enabled = false;
            EditorGUILayout.FloatField(value);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Return the point we would hit at the specified height for the specified mouse position.
        /// </summary>
        internal static Nullable<Vector3> GetWorldPositionForMouse(Vector2 mousePosition, float placementHeight)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0, placementHeight, 0));

            var distance = 0f;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }

            return null;
        }
    }
}
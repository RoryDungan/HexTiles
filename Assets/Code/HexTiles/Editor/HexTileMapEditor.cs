using UnityEngine;
using System.Collections;
using UnityEditor;
using System;

namespace HexTiles.Editor
{
    /// <summary>
    /// Editor for hex tile maps. Contains code for interacting with and painting tiles
    /// in the editor.
    /// </summary>
    [CustomEditor(typeof(HexTileMap))]
    public class HexTileMapEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Struct containg normal and selected icons for tool buttons.
        /// </summary>
        private struct ButtonIcon
        {
            public Texture2D NormalIcon;

            public Texture2D SelectedIcon;
        }

        private ButtonIcon[] toolIcons = {};

        /// <summary>
        /// Index of the currently selected tool.
        /// </summary>
        private int selectedToolIndex = 0;

        private static int hexTileEditorHash = "HexTileEditor".GetHashCode();

        /// <summary>
        /// Height to place new tiles at.
        /// </summary>
        private float placementHeight = 0f;

        public HexTileMapEditor()
        {
            toolIcons = new ButtonIcon[] {
                new ButtonIcon{ NormalIcon = LoadImage("mouse-pointer_44"), SelectedIcon = LoadImage("mouse-pointer_44_selected") },
                new ButtonIcon{ NormalIcon = LoadImage("paint-brush_44"), SelectedIcon = LoadImage("paint-brush_44_selected") },
                new ButtonIcon{ NormalIcon = LoadImage("eraser_44"), SelectedIcon = LoadImage("eraser_44_selected") },
                new ButtonIcon{ NormalIcon = LoadImage("cog_44"), SelectedIcon = LoadImage("cog_44_selected") },
            };
        }

        void OnSceneGUI()
        {
            var hexMap = (HexTileMap)target;

            var controlId = GUIUtility.GetControlID(hexTileEditorHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (Event.current.button == 0)
                    {
                        var position = GetWorldPositionForMouseClick(Event.current.mousePosition);
                        hexMap.AddHexTile(hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault()));
                        if (position != null)
                        {
                            hexMap.selectedTile = hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault());
                        }
                        Event.current.Use();
                    }
                    break;
                case EventType.layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        public override void OnInspectorGUI()
        {
            var toolbarContent = new GUIContent[]
            {
                new GUIContent(GetToolButtonIcon(0), "Select"),
                new GUIContent(GetToolButtonIcon(1), "Paint"),
                new GUIContent(GetToolButtonIcon(2), "Delete"),
                new GUIContent(GetToolButtonIcon(3), "Settings")
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Toolbar(selectedToolIndex, toolbarContent, "command");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Select");
            GUILayout.Label("Pick a hex tile to manually edit its properties.", EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();

            GUILayout.Label("Settings", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.Vector2Field("Coordinates", Vector2.zero);
            GUI.enabled = true;

            EditorGUILayout.FloatField("Height", 1f);

            EditorGUILayout.ObjectField("Material", null, typeof(HexTileMaterial), false);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Button("Manage materials", EditorStyles.miniButton);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Helper function to get the correct icon for a tool button.
        /// </summary>
        private Texture2D GetToolButtonIcon(int index)
        {
            return selectedToolIndex == index ? toolIcons[index].SelectedIcon : toolIcons[index].NormalIcon;
        }

        Texture2D LoadImage(string resource)
        {
            var image = Resources.Load<Texture2D>(resource);
            if (image == null)
            {
                throw new ApplicationException("Failed to load image from resource \"" + resource + "\"");
            }

            return image;
        }

        /// <summary>
        /// Return the point we would hit at the specified height for the specified mouse position.
        /// </summary>
        private Nullable<Vector3> GetWorldPositionForMouseClick(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var plane = new Plane(Vector3.up, new Vector3(0, placementHeight, 0));

            var distance = 0f;
            if (plane.Raycast(ray, out distance))
            {
                return ray.GetPoint(distance);
            }

            // Could not get ray cast point
            Debug.LogError("Could not find or place tile at the specified position");
            return null;
        }
    }
}

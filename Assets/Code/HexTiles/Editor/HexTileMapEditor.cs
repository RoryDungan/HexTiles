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
        private Texture2D selectButtonIcon;
        private Texture2D selectButtonIcon_selected;
        private Texture2D paintButtonIcon;
        private Texture2D eraseButtonIcon;
        private Texture2D settingsButtonIcon;

        private static int hexTileEditorHash = "HexTileEditor".GetHashCode();

        void OnSceneGUI()
        {
            var controlId = GUIUtility.GetControlID(hexTileEditorHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (Event.current.button == 0)
                    {
                        Debug.Log(Event.current.mousePosition);
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
            var toolIcons = new GUIContent[]
            {
                new GUIContent(selectButtonIcon_selected, "Select"),
                new GUIContent(paintButtonIcon, "Paint"),
                new GUIContent(eraseButtonIcon, "Delete"),
                new GUIContent(settingsButtonIcon, "Settings")
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Toolbar(0, toolIcons, "command");
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

        void OnEnable()
        {
            selectButtonIcon = LoadImage("mouse-pointer_44");
            selectButtonIcon_selected = LoadImage("mouse-pointer_44_selected");
            paintButtonIcon = LoadImage("paint-brush_44");
            eraseButtonIcon = LoadImage("eraser_44");
            settingsButtonIcon = LoadImage("cog_44");
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
    }
}

using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using RSG;

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
        /// States that the UI can be in.
        /// </summary>
        private static readonly string[] States = 
        {
            "Select",
            "Paint", 
            "Erase",
            "Settings"
        };

        /// <summary>
        /// Root state for state machine.
        /// </summary>
        private IState rootState;

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
            rootState = new StateMachineBuilder()
                .State("Select")
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Select", "Pick a hex tile to manually edit its properties.");

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
                    })
                .End()
                .State("Paint")
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Paint", "Click and drag to add hex tiles at the specified height.");
                    })
                .End()
                .State("Erase")
                    .Update((state, dt) => 
                    {
                        ShowHelpBox("Erase", "Click and drag on existing hex tiles to remove them.");
                    })
                .End()
                .State("Settings")
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Settings", "Configure options for the whole tile map.");
                    })
                .End()
                .Build();
            
            rootState.ChangeState("Select");
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
                case EventType.MouseMove:
                    var highlightedPosition = GetWorldPositionForMouse(Event.current.mousePosition);
                    if (highlightedPosition != null)
                    {
                        hexMap.HighlightedTile = hexMap.QuantizeVector3ToHexCoords(highlightedPosition.GetValueOrDefault());
                    }
                    Event.current.Use();
                    
                    break;
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    if (Event.current.button == 0)
                    {
                        var position = GetWorldPositionForMouse(Event.current.mousePosition);
                        hexMap.AddHexTile(hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault()));
                        if (position != null)
                        {
                            hexMap.SelectedTile = hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault());
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

            var newSelectedTool = GUILayout.Toolbar(selectedToolIndex, toolbarContent, "command");
            if (newSelectedTool != selectedToolIndex)
            {
                selectedToolIndex = newSelectedTool;
                rootState.ChangeState(States[selectedToolIndex]);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            rootState.Update(Time.deltaTime);
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
        /// Show a UI with some information about the selected tool.
        /// </summary>
        private void ShowHelpBox(string toolName, string description)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(toolName);
            GUILayout.Label(description, EditorStyles.wordWrappedMiniLabel);
            GUILayout.EndVertical();
        }

        /// <summary>
        /// Return the point we would hit at the specified height for the specified mouse position.
        /// </summary>
        private Nullable<Vector3> GetWorldPositionForMouse(Vector2 mousePosition)
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

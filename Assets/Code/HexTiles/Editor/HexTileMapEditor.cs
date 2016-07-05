using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using RSG;
using System.Linq;

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
        /// The object we're editing.
        /// </summary>
        private HexTileMap hexMap;

        public HexTileMapEditor()
        {
            rootState = new StateMachineBuilder()
                .State("Select")
                    .Enter(evt => selectedToolIndex = 0)
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Select", "Pick a hex tile to manually edit its properties.");

                        // Tile info
                        GUILayout.Label("Tile position", EditorStyles.boldLabel);

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Column", GUILayout.Width(EditorGUIUtility.labelWidth));
                        GUI.enabled = false;
                        EditorGUILayout.IntField(hexMap.SelectedTile.Q);
                        GUI.enabled = true;
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Row", GUILayout.Width(EditorGUIUtility.labelWidth));
                        GUI.enabled = false;
                        EditorGUILayout.IntField(hexMap.SelectedTile.R);
                        GUI.enabled = true;
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Elevation", GUILayout.Width(EditorGUIUtility.labelWidth));
                        GUI.enabled = false;
                        EditorGUILayout.FloatField(hexMap.SelectedTile.Elevation);
                        GUI.enabled = true;
                        EditorGUILayout.EndHorizontal();


                        // Tile settings
                        GUILayout.Label("Settings", EditorStyles.boldLabel);

                        EditorGUILayout.ObjectField("Material", null, typeof(HexTileMaterial), false);

                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        GUILayout.Button("Manage materials", EditorStyles.miniButton);
                        GUILayout.EndHorizontal();
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var tile = TryFindTileForMousePosition(eventArgs.Position);
                            if (tile != null)
                            {
                                hexMap.SelectedTile = tile;
                            }
                        }
                    })
                .End()
                .State<PaintState>("Paint")
                    .Enter(evt => selectedToolIndex = 1)
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Paint", "Click and drag to add hex tiles at the specified height.");

                        state.PaintHeight = EditorGUILayout.FloatField("Tile height", state.PaintHeight);
                    })
                    .Event("MouseMove", state =>
                    {
                        var highlightedPosition = GetWorldPositionForMouse(Event.current.mousePosition, state.PaintHeight);
                        if (highlightedPosition != null)
                        {
                            hexMap.HighlightedTile = hexMap.QuantizeVector3ToHexCoords(highlightedPosition.GetValueOrDefault());
                        }
                        Event.current.Use();
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var position = GetWorldPositionForMouse(eventArgs.Position, state.PaintHeight);
                            if (position != null)
                            {
                                // Select the tile that was clicked on.
                                hexMap.SelectedTile = hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault());
                                // Create tile
                                hexMap.CreateAndAddTile(hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault()));
                            }
                        }
                    })
                .End()
                .State("Erase")
                    .Enter(evt => selectedToolIndex = 2)
                    .Update((state, dt) => 
                    {
                        ShowHelpBox("Erase", "Click and drag on existing hex tiles to remove them.");
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var tile = TryFindTileForMousePosition(eventArgs.Position);
                            if (tile != null)
                            {
                                // Select the tile that was clicked on.
                                hexMap.SelectedTile = tile;
                                // Create tile
                                hexMap.TryRemovingTile(tile);
                            }
                        }
                    })
                .End()
                .State<SettingsState>("Settings")
                    .Enter(state => 
                    {
                        selectedToolIndex = 3;
                        state.HexSize = hexMap.hexWidth;
                    })
                    .Update((state, dt) =>
                    {
                        ShowHelpBox("Settings", "Configure options for the whole tile map.");

                        state.HexSize = EditorGUILayout.FloatField("Tile size", state.HexSize);
                        if (state.HexSize != hexMap.hexWidth)
                        {
                            state.Dirty = true;
                        }

                        if (GUILayout.Button("Clear all tiles"))
                        {
                            if (EditorUtility.DisplayDialog("Clear all tiles", 
                                "Are you sure you want to delete all tiles in this hex tile map?", 
                                "Clear", 
                                "Cancel"))
                            {
                                hexMap.ClearAllTiles();
                            }
                        }

                        EditorGUILayout.Space();

                        GUI.enabled = state.Dirty;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Apply", GUILayout.Width(160)))
                        {
                            Debug.Log("Saving settings");
                            hexMap.hexWidth = state.HexSize;
                            hexMap.RegenerateAllTiles();

                            state.Dirty = false;
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                        GUI.enabled = true;
                    })
                .End()
                .Build();
            
            rootState.ChangeState("Select");

            if (EditorGUIUtility.isProSkin)
            {
                toolIcons = new ButtonIcon[] {
                    new ButtonIcon{ NormalIcon = LoadImage("mouse-pointer_44_pro"), SelectedIcon = LoadImage("mouse-pointer_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("paint-brush_44_pro"), SelectedIcon = LoadImage("paint-brush_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("eraser_44_pro"), SelectedIcon = LoadImage("eraser_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("cog_44_pro"), SelectedIcon = LoadImage("cog_44_selected") },
                };
            }
            else
            {
                toolIcons = new ButtonIcon[] {
                    new ButtonIcon{ NormalIcon = LoadImage("mouse-pointer_44"), SelectedIcon = LoadImage("mouse-pointer_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("paint-brush_44"), SelectedIcon = LoadImage("paint-brush_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("eraser_44"), SelectedIcon = LoadImage("eraser_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("cog_44"), SelectedIcon = LoadImage("cog_44_selected") },
                };
            }
        }

        void OnSceneGUI()
        {
            var controlId = GUIUtility.GetControlID(hexTileEditorHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseMove:

                    rootState.TriggerEvent("MouseMove");
                    
                    break;
                case EventType.MouseDown:
                case EventType.MouseDrag:

                    var eventArgs = new SceneClickedEventArgs { 
                        Button = Event.current.button, 
                        Position = Event.current.mousePosition
                    };
                    rootState.TriggerEvent("SceneClicked", eventArgs);

                    // Disable the normal interaction with objects in the scene so that we 
                    // can do things with tiles.
                    if (Event.current.button == 0)
                    {
                        Repaint();
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

        void OnEnable()
        {
            hexMap = (HexTileMap)target;
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
                rootState.ChangeState(States[newSelectedTool]);
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
        private Nullable<Vector3> GetWorldPositionForMouse(Vector2 mousePosition, float placementHeight)
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

        /// <summary>
        /// Try to find a tile by raycasting from the specified mouse position. 
        /// Returns null if no tile was found.
        /// </summary>
        private HexCoords TryFindTileForMousePosition(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            return Physics.RaycastAll(ray, 1000f)
                .Where(hit => hit.collider.GetComponent<HexTile>() != null)
                .OrderBy(hit => hit.distance)
                .Select(hit => hexMap.QuantizeVector3ToHexCoords(hit.point))
                .FirstOrDefault();
        }

        /// <summary>
        /// State for when we're painting tiles.
        /// </summary>
        private class PaintState : AbstractState
        {
            public float PaintHeight;
        }

        /// <summary>
        /// State for when we're in the map settings mode.
        /// </summary>
        private class SettingsState : AbstractState
        {
            /// <summary>
            /// Whether or not a value has been changed and needs to be saved.
            /// </summary>
            public bool Dirty = false;

            public float HexSize;
        }

        /// <summary>
        /// Event args for when the user clicks in the scene. Passed on to whatever
        /// the active tool is.
        /// </summary>
        private class SceneClickedEventArgs : EventArgs
        {
            public int Button;

            public Vector2 Position;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RSG;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            "Paint tiles",
            "Material paint", 
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

        private IEnumerable<HexPosition> highlightedTiles = Enumerable.Empty<HexPosition>();
        private IEnumerable<HexPosition> nextTilePositions = Enumerable.Empty<HexPosition>();

        private AnimBool showTileCoordinateFormat;

        /// <summary>
        /// Center of the current selection.
        /// </summary>
        private HexCoords centerSelectedTileCoords;

        /// <summary>
        /// Current size of the area we want to effect by adding/removing/paining over tiles.
        /// </summary>
        int brushSize = 1;

        private static readonly string undoMessage = "Edited hex tiles";

        private void Initialise()
        {
            rootState = new StateMachineBuilder()
                .State("Select")
                    .Enter(evt => selectedToolIndex = 0)
                    .Update((state, dt) =>
                    {
                        EditorUtilities.ShowHelpBox("Select", "Pick a hex tile to manually edit its properties.");

                        HexTileData currentTile;
                        if (hexMap.SelectedTile != null && hexMap.TryGetTile(hexMap.SelectedTile, out currentTile))
                        {
                            // Tile info
                            GUILayout.Label("Tile position", EditorStyles.boldLabel);

                            EditorUtilities.ShowReadonlyIntField("Column", hexMap.SelectedTile.Q);
                            EditorUtilities.ShowReadonlyIntField("Row", hexMap.SelectedTile.R);
                            EditorUtilities.ShowReadonlyFloatField("Elevation", currentTile.Position.Elevation);

                            // Tile settings
                            GUILayout.Label("Settings", EditorStyles.boldLabel);

                            var currentMaterial = currentTile.Material;
                            var newMaterial = (Material)EditorGUILayout.ObjectField("Material", currentMaterial, typeof(Material), false);
                            if (currentMaterial != newMaterial)
                            {
                                hexMap.ReplaceMaterialOnTile(currentTile.Position.Coordinates, newMaterial);
                                MarkSceneDirty();
                            }
                        }
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var tile = TryFindTileForMousePosition(eventArgs.Position);
                            if (tile != null)
                            {
                                hexMap.SelectedTile = tile.Coordinates;
                            }
                        }
                    })
                .End()
                .State<PaintState>("Paint tiles")
                    .Enter(state => 
                    {
                        selectedToolIndex = 1;
                    })
                    .Update((state, dt) =>
                    {
                        EditorUtilities.ShowHelpBox("Paint tiles", "Click and drag to add hex tiles at the specified height.");

                        bool sceneNeedsRepaint = false;

                        var newBrushSize = EditorGUILayout.IntSlider("Brush size", brushSize, 1, 10);
                        if (newBrushSize != brushSize)
                        {
                            brushSize = newBrushSize;

                            sceneNeedsRepaint = true;
                        }

                        var paintHeight = EditorGUILayout.FloatField("Paint height", state.PaintHeight);
                        if (paintHeight != state.PaintHeight)
                        {
                            state.PaintHeight = paintHeight;
                            foreach (var tile in highlightedTiles)
                            {
                                tile.Elevation = paintHeight;
                            }

                            sceneNeedsRepaint = true;
                        }

                        var paintOffsetHeight = EditorGUILayout.FloatField("Height offset", state.PaintOffset);
                        if (paintOffsetHeight != state.PaintOffset)
                        {
                            state.PaintOffset = paintOffsetHeight;
                            foreach (var tile in nextTilePositions)
                            {
                                tile.Elevation = paintHeight + paintOffsetHeight;
                            }

                            sceneNeedsRepaint = true;
                        }

                        hexMap.CurrentMaterial = (Material)EditorGUILayout.ObjectField("Material", hexMap.CurrentMaterial, typeof(Material), false);

                        if (sceneNeedsRepaint)
                        {
                            if (centerSelectedTileCoords != null)
                            {
                                UpdateHighlightedTiles(centerSelectedTileCoords.CoordinateRange(brushSize - 1), state.PaintHeight, state.PaintOffset);
                            }
                            SceneView.RepaintAll();
                        }
                    })
                    .Event("MouseMove", state =>
                    {
                        var highlightedPosition = EditorUtilities.GetWorldPositionForMouse(Event.current.mousePosition, state.PaintHeight);
                        if (highlightedPosition != null)
                        {
                            centerSelectedTileCoords = hexMap.QuantizeVector3ToHexCoords(highlightedPosition.GetValueOrDefault());

                            UpdateHighlightedTiles(centerSelectedTileCoords.CoordinateRange(brushSize - 1), state.PaintHeight, state.PaintOffset);
                        }
                        Event.current.Use();
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var position = EditorUtilities.GetWorldPositionForMouse(eventArgs.Position, state.PaintHeight);
                            if (position != null)
                            {
                                // Select the tile that was clicked on.
                                centerSelectedTileCoords = hexMap.QuantizeVector3ToHexCoords(position.GetValueOrDefault());
                                var coords = centerSelectedTileCoords.CoordinateRange(brushSize - 1);
                                
                                foreach (var hex in coords)
                                {
                                    // Keep track of which chunks we've modified so that 
                                    // we only record undo actions for each once.
                                    var oldChunk = hexMap.FindChunkForCoordinates(hex);
                                    if (oldChunk != null && !state.ModifiedChunks.Contains(oldChunk))
                                    {
                                        RecordChunkModifiedUndo(oldChunk);
                                        state.ModifiedChunks.Add(oldChunk);
                                    }

                                    var newChunk = hexMap.FindChunkForCoordinatesAndMaterial(hex, hexMap.CurrentMaterial);
                                    if (newChunk != null && newChunk != oldChunk && !state.ModifiedChunks.Contains(newChunk))
                                    {
                                        RecordChunkModifiedUndo(newChunk);
                                        state.ModifiedChunks.Add(newChunk);
                                    }

                                    // TODO: add feature for disabling wireframe again.
                                    var paintHeight = state.PaintHeight + state.PaintOffset;
                                    
                                    var action = hexMap.CreateAndAddTile(
                                        new HexPosition(hex, paintHeight),
                                        hexMap.CurrentMaterial);
                                    
                                    if (action.Operation == ModifiedTileInfo.ChunkOperation.Added)
                                    {
                                        RecordChunkAddedUndo(action.Chunk);
                                    }
                                }

                                hexMap.SelectedTile = centerSelectedTileCoords;

                                MarkSceneDirty();
                            }
                        }
                    })
                    .Event("MouseUp", state => 
                    {
                        // Flush list of modified chunks so that they are not included in 
                        // the next undo action.
                        state.ModifiedChunks.Clear();
                    })
                    .Exit(state =>
                    {
                        highlightedTiles = Enumerable.Empty<HexPosition>();
                        nextTilePositions = Enumerable.Empty<HexPosition>();

                        hexMap.NextTilePositions = null;
                    })
                .End()
                .State<ChunkEditingState>("Material paint")
                    .Enter(state =>
                    {
                        selectedToolIndex = 2;
                    })
                    .Update((state, dt) =>
                    {
                        bool sceneNeedsRepaint = false;

                        EditorUtilities.ShowHelpBox("Material paint", "Paint over existing tiles to change their material.");

                        var newBrushSize = EditorGUILayout.IntSlider("Brush size", brushSize, 1, 10);
                        if (newBrushSize != brushSize)
                        {
                            brushSize = newBrushSize;

                            sceneNeedsRepaint = true;
                        }

                        hexMap.CurrentMaterial = (Material)EditorGUILayout.ObjectField("Material", hexMap.CurrentMaterial, typeof(Material), false);

                        EditorGUILayout.Space();

                        if (GUILayout.Button("Apply to all tiles"))
                        {
                            ApplyCurrentMaterialToAllTiles();
                            MarkSceneDirty();

                            sceneNeedsRepaint = true;
                        }

                        if (sceneNeedsRepaint)
                        {
                            SceneView.RepaintAll();
                        }
                    })
                    .Event("MouseMove", state =>
                    {
                        HighlightTilesUnderMousePosition();

                        Event.current.Use();
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            var tilePosition = TryFindTileForMousePosition(eventArgs.Position);
                            if (tilePosition != null && hexMap.ContainsTile(tilePosition.Coordinates))
                            {
                                // Select that the tile that was clicked on.
                                hexMap.SelectedTile = tilePosition.Coordinates;

                                // Change the material on the tile
                                var tilesUnderBrush = tilePosition.Coordinates.CoordinateRange(brushSize - 1)
                                    .Where(coords => hexMap.ContainsTile(coords));
                                foreach (var coords in tilesUnderBrush)
                                {
                                    ReplaceMaterialOnTile(coords, state.ModifiedChunks);
                                }
                            }

                            Event.current.Use();
                        }
                    })
                    .Event("MouseUp", state => 
                    {
                        // Flush list of modified chunks so that they are not included in 
                        // the next undo action.
                        state.ModifiedChunks.Clear();
                    })
                .End()
                .State<ChunkEditingState>("Erase")
                    .Enter(evt => selectedToolIndex = 3)
                    .Update((state, dt) => 
                    {
                        EditorUtilities.ShowHelpBox("Erase", "Click and drag on existing hex tiles to remove them.");

                        var newBrushSize = EditorGUILayout.IntSlider("Brush size", brushSize, 1, 10);
                        if (newBrushSize != brushSize)
                        {
                            brushSize = newBrushSize;

                            SceneView.RepaintAll();
                        }
                    })
                    .Event("MouseMove", state =>
                    {
                        HighlightTilesUnderMousePosition();

                        Event.current.Use();
                    })
                    .Event<SceneClickedEventArgs>("SceneClicked", (state, eventArgs) =>
                    {
                        if (eventArgs.Button == 0)
                        {
                            bool removedTile = false;

                            var centerTile = TryFindTileForMousePosition(eventArgs.Position);
                            if (centerTile != null)
                            {
                                foreach (var tile in centerTile.Coordinates.CoordinateRange(brushSize - 1))
                                {
                                    var chunk = hexMap.FindChunkForCoordinates(tile);
                                    if (chunk != null && !state.ModifiedChunks.Contains(chunk))
                                    {
                                        RecordChunkModifiedUndo(chunk);
                                        state.ModifiedChunks.Add(chunk);
                                    }

                                    // Destroy tile
                                    removedTile |= hexMap.TryRemovingTile(tile);
                                }
                            }

                            if (removedTile)
                            {
                                MarkSceneDirty();
                            }
                        }
                    })
                    .Event("MouseUp", state =>
                    {
                        // Flush list of modified chunks so that they are not included in 
                        // the next undo action.
                        state.ModifiedChunks.Clear();
                    })
                .End()
                .State<SettingsState>("Settings")
                    .Enter(state => 
                    {
                        selectedToolIndex = 4;
                        state.HexSize = hexMap.tileDiameter;
                        state.ChunkSize = hexMap.ChunkSize;

                        showTileCoordinateFormat.value = hexMap.DrawHexPositionHandles;
                    })
                    .Update((state, dt) =>
                    {
                        EditorUtilities.ShowHelpBox("Settings", "Configure options for the whole tile map.");

                        var shouldDrawPositionHandles = EditorGUILayout.Toggle("Show tile positions", hexMap.DrawHexPositionHandles);
                        if (shouldDrawPositionHandles != hexMap.DrawHexPositionHandles)
                        {
                            hexMap.DrawHexPositionHandles = shouldDrawPositionHandles;
                            SceneView.RepaintAll();
                            MarkSceneDirty();

                            showTileCoordinateFormat.target = shouldDrawPositionHandles;
                        }

                        if (EditorGUILayout.BeginFadeGroup(showTileCoordinateFormat.faded))
                        {
                            var newHandleFormat = (HexTileMap.HexCoordinateFormat)
                                EditorGUILayout.EnumPopup("Tile coordinate format", hexMap.HexPositionHandleFormat);
                            if (newHandleFormat != hexMap.HexPositionHandleFormat)
                            {
                                hexMap.HexPositionHandleFormat = newHandleFormat;
                                SceneView.RepaintAll();
                            }
                        }
                        EditorGUILayout.EndFadeGroup();

                        state.HexSize = EditorGUILayout.FloatField("Tile size", state.HexSize);
                        if (state.HexSize != hexMap.tileDiameter)
                        {
                            state.Dirty = true;
                        }

                        var newChunkSize = EditorGUILayout.IntField("Chunk size", state.ChunkSize);
                        if (state.ChunkSize != newChunkSize)
                        {
                            state.ChunkSize = newChunkSize;
                            state.Dirty = true;
                        }

                        if (GUILayout.Button("Re-generate all tile geometry"))
                        {
                            hexMap.RegenerateAllTiles();
                            MarkSceneDirty();
                        }

                        if (GUILayout.Button("Clear all tiles"))
                        {
                            if (EditorUtility.DisplayDialog("Clear all tiles", 
                                "Are you sure you want to delete all tiles in this hex tile map?", 
                                "Clear", 
                                "Cancel"))
                            {
                                hexMap.ClearAllTiles();
                                MarkSceneDirty();
                            }
                        }

                        EditorGUILayout.Space();

                        GUI.enabled = state.Dirty;
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Apply", GUILayout.Width(160)))
                        {
                            Debug.Log("Saving settings");
                            hexMap.tileDiameter = state.HexSize;
                            hexMap.ChunkSize = state.ChunkSize;
                            hexMap.RegenerateAllTiles();
                            MarkSceneDirty();

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
                    new ButtonIcon{ NormalIcon = LoadImage("add-hex_44_pro"), SelectedIcon = LoadImage("add-hex_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("paint-brush_44_pro"), SelectedIcon = LoadImage("paint-brush_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("eraser_44_pro"), SelectedIcon = LoadImage("eraser_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("cog_44_pro"), SelectedIcon = LoadImage("cog_44_selected") },
                };
            }
            else
            {
                toolIcons = new ButtonIcon[] {
                    new ButtonIcon{ NormalIcon = LoadImage("mouse-pointer_44"), SelectedIcon = LoadImage("mouse-pointer_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("add-hex_44"), SelectedIcon = LoadImage("add-hex_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("paint-brush_44"), SelectedIcon = LoadImage("paint-brush_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("eraser_44"), SelectedIcon = LoadImage("eraser_44_selected") },
                    new ButtonIcon{ NormalIcon = LoadImage("cog_44"), SelectedIcon = LoadImage("cog_44_selected") },
                };
            }
        }

        private void UpdateHighlightedTiles(IEnumerable<HexCoords> coords, float paintHeight, float paintOffset)
        {
            highlightedTiles = coords.Select(tile => new HexPosition(tile, paintHeight));
            nextTilePositions = coords.Select(tile => new HexPosition(tile, paintHeight + paintOffset));
        }

        /// <summary>
        /// Replace the material on the specified tile with the currently selected
        /// material. A HashSet of the chunks that have been modified so far in this 
        /// action must be passed in so that this can record which chunks were affected
        /// for the purposes for registering undo actions.
        /// </summary>
        private void ReplaceMaterialOnTile(HexCoords coords, HashSet<HexChunk> modifiedChunks)
        {
            var oldChunk = hexMap.FindChunkForCoordinates(coords);
            // Skip if the material is already the same.
            if (oldChunk.Material == hexMap.CurrentMaterial)
            {
                return;
            }

            if (oldChunk != null && !modifiedChunks.Contains(oldChunk))
            {
                RecordChunkModifiedUndo(oldChunk);
                modifiedChunks.Add(oldChunk);
            }
            var newChunk = hexMap.FindChunkForCoordinatesAndMaterial(coords, hexMap.CurrentMaterial);
            if (newChunk != null && newChunk != oldChunk && !modifiedChunks.Contains(newChunk))
            {
                RecordChunkModifiedUndo(newChunk);
                modifiedChunks.Add(newChunk);
            }

            var action = hexMap.ReplaceMaterialOnTile(coords, hexMap.CurrentMaterial);

            if (action.Operation == ModifiedTileInfo.ChunkOperation.Added)
            {
                RecordChunkAddedUndo(action.Chunk);
            }
        }

        /// <summary>
        /// Applies the material stored in hexMap.CurrentMaterial to all the tiles in hexMap
        /// </summary>
        private void ApplyCurrentMaterialToAllTiles()
        {
            var modifiedChunks = new HashSet<HexChunk>();

            foreach (var tile in hexMap.GetAllTiles().ToArray())
            {
                ReplaceMaterialOnTile(tile.Position.Coordinates, modifiedChunks);
            }
        }

        /// <summary>
        /// Tell Unity that a change has been made and we have to save the scene.
        /// </summary>
        private void MarkSceneDirty()
        {
#if UNITY_5_3_OR_NEWER
            // TODO: Undo.RecordObject also marks the scene dirty, so this will no longer be necessary once undo support is added.
            EditorSceneManager.MarkSceneDirty(hexMap.gameObject.scene);
#else
            EditorUtility.SetDirty(hexMap.gameObject);
#endif
        }

        /// <summary>
        /// Record an undo action for a chunk.
        /// </summary>
        private void RecordChunkModifiedUndo(HexChunk chunk)
        {
            Undo.RegisterCompleteObjectUndo(chunk, undoMessage);
            Undo.RegisterCompleteObjectUndo(chunk.MeshFilter, undoMessage);
            Undo.RegisterCompleteObjectUndo(chunk.MeshCollider, undoMessage);
        }

        /// <summary>
        /// Record that a chunk was added.
        /// </summary>
        private void RecordChunkAddedUndo(HexChunk chunk)
        {
            Undo.RegisterCreatedObjectUndo(chunk.gameObject, undoMessage);
        }

        void OnSceneGUI()
        {
            if (hexMap.DrawHexPositionHandles)
            {
                DrawHexPositionHandles();
            }

            hexMap.HighlightedTiles = highlightedTiles;
            hexMap.NextTilePositions = nextTilePositions;


            // Handle mouse input
            var controlId = GUIUtility.GetControlID(hexTileEditorHash, FocusType.Passive);
            switch (Event.current.GetTypeForControl(controlId))
            {
                case EventType.MouseMove:

                    rootState.TriggerEvent("MouseMove");
                    
                    break;
                case EventType.MouseDrag:
                case EventType.MouseDown:

                    // Don't do anything if the user alt-left clicks to rotate the camera.
                    if ((Event.current.button == 0 && Event.current.alt) || Event.current.button != 0)
                    {
                        break;
                    }

                    rootState.TriggerEvent("MouseMove");

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
                case EventType.MouseUp:
                    // Don't do anything if the user alt-left clicks to rotate the camera.
                    if ((Event.current.button == 0 && Event.current.alt) || Event.current.button != 0)
                    {
                        break;
                    }

                    rootState.TriggerEvent("MouseUp");
                    break;
                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlId);
                    break;
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }

        /// <summary>
        /// Draw handles with the position of each hex tile above that tile in the scene.
        /// </summary>
        private void DrawHexPositionHandles()
        {
            foreach (var tile in hexMap.GetAllTiles())
            {
                var position = hexMap.HexPositionToWorldPosition(tile.Position);

                // Only draw this handle if the tile is in front of the camera.
                var cameraTransform = SceneView.currentDrawingSceneView.camera.transform;
                var cameraToTile = cameraTransform.position - position;
                if (Vector3.Dot(cameraToTile, cameraTransform.forward) > 0)
                {
                    continue;
                }

                var hexCoords = hexMap.QuantizeVector3ToHexCoords(position);
                var labelText = string.Empty;
                switch (hexMap.HexPositionHandleFormat)
                {
                    case HexTileMap.HexCoordinateFormat.Axial:
                        labelText = hexCoords.ToString();
                        break;
                    case HexTileMap.HexCoordinateFormat.OffsetOddQ:
                        labelText = hexCoords.ToOffset().ToString("0");
                        break;
                    case HexTileMap.HexCoordinateFormat.WorldSpacePosition:
                        labelText = position.ToString();
                        break;
                }
                Handles.Label(position, labelText);
            }
        }

        void OnEnable()
        {
            hexMap = (HexTileMap)target;

            // Init anim bools
            showTileCoordinateFormat = new AnimBool(Repaint);

            Initialise();

            Undo.undoRedoPerformed += OnUndoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoPerformed;
        }

        public override void OnInspectorGUI()
        {
            var toolbarContent = new GUIContent[]
            {
                new GUIContent(GetToolButtonIcon(0), "Select"),
                new GUIContent(GetToolButtonIcon(1), "Paint tiles"),
                new GUIContent(GetToolButtonIcon(2), "Material paint"),
                new GUIContent(GetToolButtonIcon(3), "Delete"),
                new GUIContent(GetToolButtonIcon(4), "Settings")
            };

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var newSelectedTool = GUILayout.Toolbar(selectedToolIndex, toolbarContent, "command");
            if (newSelectedTool != selectedToolIndex)
            {
                rootState.ChangeState(States[newSelectedTool]);
                SceneView.RepaintAll();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            rootState.Update(Time.deltaTime);

            if (hexMap.UpdateTileChunks())
            {
                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Callback triggered when an undo is performed.
        /// </summary>
        private void OnUndoPerformed()
        {
            // This must be done in case the undo operation deleted a chunk
            // or added a new one. This would be more efficient if we could 
            // actually know whether the undo operation affected this object
            // rather than doing it after every undo.
            hexMap.ClearChunkCache();
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
        /// Try to find a tile by raycasting from the specified mouse position. 
        /// Returns null if no tile was found.
        /// </summary>
        private HexPosition TryFindTileForMousePosition(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            return Physics.RaycastAll(ray, 1000f)
                .Where(hit => hit.collider.GetComponent<HexChunk>() != null)
                .OrderBy(hit => hit.distance)
                .Select(hit => new HexPosition(hexMap.QuantizeVector3ToHexCoords(hit.point), hit.point.y))
                .FirstOrDefault();
        }

        /// <summary>
        /// Highlights all the tiles under the current mouse position.
        /// </summary>
        private void HighlightTilesUnderMousePosition()
        {
            var centerTile = TryFindTileForMousePosition(Event.current.mousePosition);
            if (centerTile != null)
            {
                var newHighlightedTiles = new List<HexPosition>();
                foreach (var tile in centerTile.Coordinates.CoordinateRange(brushSize - 1))
                {
                    HexTileData tileData;
                    if (hexMap.TryGetTile(tile, out tileData))
                    {
                        newHighlightedTiles.Add(new HexPosition(tile, tileData.Position.Elevation));
                    }
                }
                highlightedTiles = newHighlightedTiles;
            }
            else 
            {
                highlightedTiles = Enumerable.Empty<HexPosition>();
            }
        }

        /// <summary>
        /// State for when we're painting tiles.
        /// </summary>
        private class PaintState : ChunkEditingState
        {
            public float PaintHeight;

            public float PaintOffset;
        }

        /// <summary>
        /// Base class for states that modify tile chunks.
        /// </summary>
        private class ChunkEditingState : AbstractState
        {
            public HashSet<HexChunk> ModifiedChunks = new HashSet<HexChunk>();
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

            public int ChunkSize;

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

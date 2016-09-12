using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;

namespace HexTiles
{
    [SelectionBase]
    public class HexTileMap : MonoBehaviour
    {
        public float hexWidth = 1f;

        [SerializeField]
        private bool drawHexPositionGizmos = false;

        public int chunkSize = 5;

        /// <summary>
        /// Whether or not to draw the position of each tile on top of the tile in the editor.
        /// </summary>
        public bool DrawHexPositionHandles
        {
            get
            {
                return drawHexPositionGizmos;
            }
            set
            {
                drawHexPositionGizmos = value;
            }
        }

        [SerializeField]
        private bool drawWireframeWhenSelected = true;

        /// <summary>
        /// Whether or not to highlight all hexes in wireframe when the tile map is selected.
        /// </summary>
        public bool DrawWireframeWhenSelected
        {
            get
            {
                return drawWireframeWhenSelected;
            }
            set
            {
                drawWireframeWhenSelected = value;
            }
        }

        public enum HexCoordinateFormat
        {
            Axial,
            OffsetOddQ
        }

        /// <summary>
        /// The format to use when drawing hex position handles.
        /// </summary>
        public HexCoordinateFormat HexPositionHandleFormat { get; set; }

        /// <summary>
        /// Collection of all hex tiles that are part of this map.
        /// </summary>
        private IDictionary<HexCoords, HexTileData> Tiles
        {
            get
            {
                // Lazy init hexes hashtable
                if (tiles == null)
                {
                    // Add all the existing tiles in the scene that are part of this map
                    tiles = new Dictionary<HexCoords, HexTileData>();

                    // Get tiles from legacy, non-batched tile objects in the scene.
                    foreach (var tile in GetComponentsInChildren<HexTile>())
                    {
                        var tilePosition = QuantizeVector3ToHexCoords(tile.transform.position);
                        try
                        {
                            var newTile = new HexTileData(new HexPosition(tilePosition, tile.Elevation), hexWidth, tile.Material);
                            tiles.Add(tilePosition, newTile);
                        }
                        catch (ArgumentException)
                        {
                            Debug.LogWarning("Duplicate tile at position " + tilePosition + " found. Deleting.", this);
                            if (Application.isEditor)
                            {
                                DestroyImmediate(tile.gameObject);
                            }
                            else
                            {
                                Destroy(tile.gameObject);
                            }
                            continue;
                        }
                        tile.Diameter = hexWidth;
                    }

                    // Get tiles from batched objects
                    foreach (var tileChunk in GetComponentsInChildren<HexChunk>())
                    {
                        foreach (var tilePosition in tileChunk.Tiles)
                        {
                            tiles.Add(tilePosition.Coordinates, new HexTileData(tilePosition, tileChunk.Diameter, tileChunk.Material));
                        }
                    }
                }
                return tiles;
            }
        }
        private IDictionary<HexCoords, HexTileData> tiles;

        private IList<HexChunk> chunks;

        private IList<HexChunk> Chunks
        {
            get
            {
                if (chunks == null)
                {
                    chunks = new List<HexChunk>();

                    foreach (var chunk in GetComponentsInChildren<HexChunk>())
                    {
                        chunks.Add(chunk);
                    }
                }

                return chunks;
            }
        }

        /// <summary>
        /// The current material used for painting tiles. Serialised here so that it will be saved for convenience
        /// when we have to reload scripts.
        /// </summary>
        public Material CurrentMaterial
        {
            get
            {
                return currentMaterial;
            }
            set
            {
                currentMaterial = value;
            }
        }
        [SerializeField]
        private Material currentMaterial;

        /// <summary>
        /// Highlighted tile for editing
        /// </summary>
        public HexCoords SelectedTile
        {
            get
            {
                return selectedTile;
            }
            set
            {
                selectedTile = value;
            }
        }
        private HexCoords selectedTile;

        /// <summary>
        /// Tile that the mouse is currently hovering over.
        /// </summary>
        public IEnumerable<HexPosition> HighlightedTiles { get; set; }

        /// <summary>
        /// The position where a new tile will appear when we paint
        /// </summary>
        public IEnumerable<HexPosition> NextTilePositions { get; set; }

        void OnDrawGizmosSelected()
        {
            if (HighlightedTiles != null)
            {
                foreach (var tile in HighlightedTiles)
                {
                    DrawHexGizmo(HexPositionToWorldPosition(tile), Color.white);
                }
            }

            if (NextTilePositions != null)
            {
                foreach (var tile in NextTilePositions)
                {
                    DrawHexGizmo(HexPositionToWorldPosition(tile), Color.cyan);
                }
            }

            if (SelectedTile != null && Tiles.ContainsKey(SelectedTile))
            {
                DrawHexGizmo(HexPositionToWorldPosition(Tiles[SelectedTile].Position), Color.green);
            }
        }

        /// <summary>
        /// Draws the outline of a hex at the specified position.
        /// Can be grey or green depending on whether it's highlighted or not.
        /// </summary>
        private void DrawHexGizmo(Vector3 position, Color color)
        {
            Gizmos.color = color;

            var verts = HexMetrics.GetHexVertices(hexWidth)
                .Select(v => v + position)
                .ToArray();

            for (var i = 0; i < verts.Length; i++)
            {
                Gizmos.DrawLine(verts[i], verts[(i + 1) % verts.Length]);
            }
        }

        /// <summary>
        /// Take a vector in world space and return the closest hex coords.
        /// </summary>
        public HexCoords QuantizeVector3ToHexCoords(Vector3 vIn)
        {
            var vector = transform.InverseTransformPoint(vIn);

            var q = vector.x * 2f/3f / (hexWidth/2f);
            var r = (-vector.x / 3f + Mathf.Sqrt(3f)/3f * vector.z) / (hexWidth/2f);

            return new HexCoords (Mathf.RoundToInt(q), Mathf.RoundToInt(r));
        }

        /// <summary>
        /// Get the world space position of the specified hex coords.
        /// This uses axial coordinates for the hexes.
        /// </summary>
        public Vector3 HexPositionToWorldPosition(HexPosition position)
        {
            return transform.TransformPoint(position.GetPositionVector(hexWidth));
        }

        /// <summary>
        /// Returns the nearest hex tile position in world space 
        /// to the specified position.
        /// </summary>
        public Vector3 QuantizePositionToHexGrid(Vector3 vIn)
        {
            return HexPositionToWorldPosition(new HexPosition(QuantizeVector3ToHexCoords(vIn), vIn.y));
        }

        /// <summary>
        /// Re-position and re-generate geometry for all tiles.
        /// Needed after changing global settings that affect all the tiles
        /// such as the tile size.
        /// </summary>
        public void RegenerateAllTiles()
        {
            var tileData = new List<HexTileData>();

            foreach (var tileCoords in Tiles.Keys)
            {
                var hexTile = Tiles[tileCoords];
                tileData.Add(hexTile);
            }

            Tiles.Clear();
            Chunks.Clear();

            var childTiles = GetComponentsInChildren<HexTile>();
            var childChunks = GetComponentsInChildren<HexChunk>();
            foreach (var tile in childTiles)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(tile.gameObject);
                }
                else
                {
                    Destroy(tile.gameObject);
                }
            }
            foreach (var chunk in childChunks)
            {
                if (Application.isEditor)
                {
                    DestroyImmediate(chunk.gameObject);
                }
                else
                {
                    Destroy(chunk.gameObject);
                }
            }

            foreach (var tile in tileData)
            {
                CreateAndAddTile(tile.Position, tile.Material);
            }

            UpdateTileChunks();
        }

        /// <summary>
        /// Refresh the meshes of any chunks that have been modified.
        /// </summary>
        public void UpdateTileChunks()
        {
            foreach (var chunk in chunks)
            {
                if (chunk.Dirty)
                {
                    chunk.GenerateMesh();
                }
            }
        }

        /// <summary>
        /// Add a tile to the map. Returns the newly added hex tile.
        /// If a tile already exists at that position then that is returned instead.
        /// </summary>
        public void CreateAndAddTile(HexPosition position, Material material)
        {
            var coords = position.Coordinates;
            var elevation = position.Elevation;

            // See if there's already a tile at the specified position.
            HexTileData tile;
            if (Tiles.TryGetValue(coords, out tile))
            {
                // If a tlie at that position and that height already exists, return it.
                if (tile.Position.Elevation == elevation
                    && tile.Material == material)
                {
                    return;
                }

                // Remove the tile before adding a new one.
                TryRemovingTile(coords);
            }

            // Try to find existing chunk.
            var chunk = Chunks.Where(c => position.Coordinates.IsWithinBounds(c.upperBounds, c.lowerBounds))
                .Where(c => c.Material == material)
                .FirstOrDefault();

            // Create new chunk if necessary
            if (chunk == null)
            {
                chunk = CreateChunkForCoordinates(position.Coordinates, material);
            }

            chunk.AddTile(position);

            Tiles.Add(coords, new HexTileData(position, hexWidth, material));

            // Generate side pieces
            // Note that we also need to update all the tiles adjacent to this one so that any side pieces that could be 
            // Obscured by this one are removed.
            /*foreach (var side in HexMetrics.AdjacentHexes)
            {
                HexTileData adjacentTile;
                var adjacentTilePos = coords + side;
                if (Tiles.TryGetValue(adjacentTilePos, out adjacentTile))
                {
                    SetUpSidePiecesForTile(adjacentTilePos);
                    adjacentTile.GenerateMesh(adjacentTilePos);
                }
            }
            SetUpSidePiecesForTile(coords);
            hex.GenerateMesh(coords);*/
        }

        private HexChunk CreateChunkForCoordinates(HexCoords coordinates, Material material)
        {
            var lowerBounds = new HexCoords(RoundDownToInterval(coordinates.Q, chunkSize), RoundDownToInterval(coordinates.R, chunkSize));
            var upperBounds = new HexCoords(lowerBounds.Q + chunkSize, lowerBounds.R + chunkSize);

            return CreateNewChunk(lowerBounds, upperBounds, material);
        }

        private HexChunk CreateNewChunk(HexCoords lowerBounds, HexCoords upperBounds, Material material)
        {
            var newGameObject = new GameObject(string.Format("Chunk {0} - {1}", lowerBounds, upperBounds));
            newGameObject.transform.parent = transform;

            var hexChunk = newGameObject.AddComponent<HexChunk>();
            hexChunk.lowerBounds = lowerBounds;
            hexChunk.upperBounds = upperBounds;

            hexChunk.Material = material;
            hexChunk.Diameter = hexWidth;

            Chunks.Add(hexChunk);

            return hexChunk;
        }

        private static int RoundDownToInterval(int input, int interval)
        {
            return ((int)Math.Floor(input / (float)interval)) * interval;
        }


        // TODO: Get side pieces working again.
        //private void SetUpSidePiecesForTile(HexCoords position)
        //{
        //    HexTileData tile;
        //    if (!Tiles.TryGetValue(position, out tile))
        //    {
        //        throw new ApplicationException("Tried to set up side pieces for non-existent tile.");
        //    }

        //    foreach (var side in HexMetrics.AdjacentHexes)
        //    {
        //        HexTileData adjacentTile;
        //        if (Tiles.TryGetValue(position + side, out adjacentTile))
        //        {
        //            tile.TryRemovingSidePiece(side);
        //            if (adjacentTile.Position.Elevation < tile.Position.Elevation)
        //            {
        //                tile.AddSidePiece(side, tile.Position.Elevation - adjacentTile.Position.Elevation);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Attempt to remove the tile at the specified position.
        /// Returns true if it was removed successfully, false if no tile was found at that position.
        /// </summary>
        public bool TryRemovingTile(HexCoords position)
        {
            if (!Tiles.ContainsKey(position))
            {
                return false;
            }

            var tile = Tiles[position];
            var chunksWithTile = Chunks.Where(c => c.Tiles.Select(pos => pos.Coordinates).Contains(position));
            if (chunksWithTile == null || chunksWithTile.Count() < 1)
            {
                Debug.LogError("Tile found in internal tile collection but not in scene. Removing", this);
            }
            Tiles.Remove(position);

            foreach (var chunk in chunksWithTile)
            {
                chunk.RemoveTile(position);
            }

            return true;
        }

        private GameObject SpawnTileObject(HexPosition position)
        {
            var newObject = new GameObject("Tile [" + position.Coordinates.Q + ", " + position.Coordinates.R + "]");
            newObject.transform.parent = transform;
            newObject.transform.position = HexPositionToWorldPosition(position);

            return newObject;
        }

        /// <summary>
        /// Destroy all child tiles and clear hashtable.
        /// </summary>
        public void ClearAllTiles()
        {
            Tiles.Clear();

            // Note that we must add all children to a list first because if we
            // destroy children as we loop through them, the array we're looping 
            // through will change and we can miss some.
            var children = new List<GameObject>();
            foreach (Transform child in transform) 
            {
                children.Add(child.gameObject);
            }
            children.ForEach(child => DestroyImmediate(child));
        }

        /// <summary>
        /// Returns whether or not the specified tile exists.
        /// </summary>
        public bool ContainsTile(HexCoords tileCoords)
        {
            return Tiles[tileCoords] != null;
        }

        /// <summary>
        /// Return data about the tile at the specified position.
        /// </summary>
        public bool TryGetTile(HexCoords tileCoords, out HexTileData data)
        {
            var tile = Tiles[tileCoords];
            if (tile == null)
            {
                data = null;
                return false;
            }
            data = tile;
            return true;
        }

        /// <summary>
        /// Remove the tile at the specified coordinates and replace it with one with the specified material.
        /// </summary>
        public void ReplaceMaterialOnTile(HexCoords tileCoords, Material material)
        {
            HexTileData tile;
            if (!TryGetTile(tileCoords, out tile))
            {
                throw new ArgumentOutOfRangeException("Tried replacing material on tile but map contains no tile at position " + tileCoords);
            }

            // Early out if the material is the same.
            if (tile.Material == material)
            {
                return;
            }

            TryRemovingTile(tileCoords);

            CreateAndAddTile(tile.Position, material);
        }

        /// <summary>
        /// Returns all the tiles in this map.
        /// </summary>
        public IEnumerable<HexTileData> GetAllTiles()
        {
            return Tiles.Values;
        }
    }
}

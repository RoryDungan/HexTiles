using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace HexTiles
{
    [SelectionBase]
    public class HexTileMap : MonoBehaviour
    {
        [FormerlySerializedAs("hexWidth")]
        public float tileDiameter = 1f;

        [SerializeField]
        private bool drawHexPositionGizmos = false;

        [SerializeField]
        private int chunkSize = 10;

        public int ChunkSize
        {
            get
            {
                return chunkSize;
            }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException("value", "ChunkSize must be at least 1");
                }
                chunkSize = value;
            }
        }

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
            OffsetOddQ,
            WorldSpacePosition
        }

        /// <summary>
        /// The format to use when drawing hex position handles.
        /// </summary>
        public HexCoordinateFormat HexPositionHandleFormat { get; set; }

        /// <summary>
        /// Collection of all hex tiles that are part of this map.
        /// </summary>
        private IEnumerable<HexTileData> Tiles
        {
            get
            {
                return Chunks
                    .SelectMany(chunk => chunk
                        .Tiles
                        .Select(tile => new HexTileData(tile, chunk.TileDiameter, chunk.Material))
                    );
            }
        }

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

            if (SelectedTile != null)
            {
                var tile = FindTileForCoords(SelectedTile);
                
                if (tile != null)
                {
                    DrawHexGizmo(HexPositionToWorldPosition(tile.Position), Color.green);
                }
            }
        }

        /// <summary>
        /// Returns the tile at the specified coordinates, or null
        /// if none exists.
        /// </summary>
        private HexTileData FindTileForCoords(HexCoords coords)
        {
            return Tiles
                .Where(t => t.Position.Coordinates == coords)
                .FirstOrDefault();
        }

        /// <summary>
        /// Draws the outline of a hex at the specified position.
        /// Can be grey or green depending on whether it's highlighted or not.
        /// </summary>
        private void DrawHexGizmo(Vector3 position, Color color)
        {
            Gizmos.color = color;

            var verts = HexMetrics.GetHexVertices(tileDiameter)
                .Select(v => (transform.localToWorldMatrix * v) + (Vector4)position)
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

            var q = vector.x * 2f/3f / (tileDiameter/2f);
            var r = (-vector.x / 3f + Mathf.Sqrt(3f)/3f * vector.z) / (tileDiameter/2f);

            return new HexCoords (Mathf.RoundToInt(q), Mathf.RoundToInt(r));
        }

        /// <summary>
        /// Get the world space position of the specified hex coords.
        /// This uses axial coordinates for the hexes.
        /// </summary>
        public Vector3 HexPositionToWorldPosition(HexPosition position)
        {
            return transform.TransformPoint(position.GetPositionVector(tileDiameter));
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
        /// 
        /// Returns the chunks that were changed as a result of this operation.
        /// </summary>
        public IEnumerable<ModifiedTileInfo> RegenerateAllTiles()
        {
            var tileData = new List<HexTileData>();

            foreach (var hexTile in Tiles)
            {
                tileData.Add(hexTile);
            }

            Chunks.Clear();

            var childChunks = GetComponentsInChildren<HexChunk>();
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

            var modifiedChunks = tileData
                .Select(tile => CreateAndAddTile(tile.Position, tile.Material))
                .Distinct()
                .ToArray(); // Need to force this to evaluate now so that CreateAndAddTile actually gets called.

            UpdateTileChunks();

            return modifiedChunks;
        }

        /// <summary>
        /// Refresh the meshes of any chunks that have been modified.
        /// Returns true if any changes have been made.
        /// </summary>
        public bool UpdateTileChunks()
        {
            var updatedChunk = false;

            var oldChunks = Chunks.ToArray();
            for (int i = 0; i < oldChunks.Length; i++)
            {
                // Remove the chunk if it has no tiles in it.
                if (oldChunks[i].Tiles.Count <= 0)
                {
                    Chunks.Remove(oldChunks[i]);
                    Utils.Destroy(oldChunks[i].gameObject);
                    updatedChunk = true;
                    continue;
                }

                // Re-generate meshes.
                if (oldChunks[i].Dirty)
                {
                    oldChunks[i].GenerateMesh();
                    updatedChunk = true;
                }
            }

            return updatedChunk;
        }

        //public HexChunk FindChunkForCoordinates

        /// <summary>
        /// Add a tile to the map. Returns the chunk object containing the new tile.
        /// </summary>
        public ModifiedTileInfo CreateAndAddTile(HexPosition position, Material material)
        {
            var coords = position.Coordinates;
            var elevation = position.Elevation;

            var chunk = FindChunkForCoordinatesAndMaterial(coords, material);
            var chunkOperation = ModifiedTileInfo.ChunkOperation.Modified;

            // Create new chunk if necessary
            if (chunk == null)
            {
                chunk = CreateChunkForCoordinates(position.Coordinates, material);
                chunkOperation = ModifiedTileInfo.ChunkOperation.Added;
            }

            // See if there's already a tile at the specified position.
            var tile = FindTileForCoords(coords);
            if (tile != null)
            {
                // If a tlie at that position and that height already exists, return it.
                if (tile.Position.Elevation == elevation
                    && tile.Material == material)
                {
                    return new ModifiedTileInfo(chunk, chunkOperation);
                }

                // Remove the tile before adding a new one.
                TryRemovingTile(coords);
            }

            chunk.AddTile(position);

            // Generate side pieces
            // Note that we also need to update all the tiles adjacent to this one so that any side pieces that could be 
            // Obscured by this one are removed.
            foreach (var side in HexMetrics.AdjacentHexes)
            {
                var adjacentTilePos = coords + side;
                var adjacentTile = FindTileForCoords(adjacentTilePos);
                if (adjacentTile != null)
                {
                    var adjacentTileChunk = FindChunkForCoordinatesAndMaterial(adjacentTilePos, adjacentTile.Material);
                    SetUpSidePiecesForTile(adjacentTilePos, adjacentTileChunk);
                }
            }
            SetUpSidePiecesForTile(coords, chunk);

            return new ModifiedTileInfo(chunk, chunkOperation);
        }

        /// <summary>
        /// Add a new chunk with the specified material and bounds around the specified coordinates.
        /// </summary>
        private HexChunk CreateChunkForCoordinates(HexCoords coordinates, Material material)
        {
            var lowerBounds = new HexCoords(RoundDownToInterval(coordinates.Q, chunkSize), RoundDownToInterval(coordinates.R, chunkSize));
            var upperBounds = new HexCoords(lowerBounds.Q + chunkSize, lowerBounds.R + chunkSize);

            return CreateNewChunk(lowerBounds, upperBounds, material);
        }

        /// <summary>
        /// Returns the chunk for the tile at the specified coordinates, or 
        /// null if none exists.
        /// </summary>
        public HexChunk FindChunkForCoordinates(HexCoords coordinates)
        {
            return Chunks.Where(c => c.Tiles.Where(tile => tile.Coordinates == coordinates).Any())
                .FirstOrDefault();
        }

        /// <summary>
        /// Find a chunk with bounds that match the specified coordinates, and the specified material.
        /// Returns null if none was found.
        /// </summary>
        public HexChunk FindChunkForCoordinatesAndMaterial(HexCoords coordinates, Material material)
        {
            // Try to find existing chunk.
            var matchingChunks = Chunks.Where(c => coordinates.IsWithinBounds(c.lowerBounds, c.upperBounds))
                .Where(c => c.Material == material);

            if (matchingChunks.Count() > 1)
            {
                Debug.LogWarning("Overlapping chunks detected for coordinates " + coordinates + ". Taking first.");
            }
            return matchingChunks.FirstOrDefault();
        }

        /// <summary>
        /// Create a new chunk with the specified bounds and material.
        /// </summary>
        private HexChunk CreateNewChunk(HexCoords lowerBounds, HexCoords upperBounds, Material material)
        {
            var newGameObject = new GameObject(string.Format("Chunk {0} - {1}", lowerBounds, upperBounds));
            newGameObject.transform.parent = transform;

            var hexChunk = newGameObject.AddComponent<HexChunk>();
            hexChunk.lowerBounds = lowerBounds;
            hexChunk.upperBounds = upperBounds;

            hexChunk.Material = material;
            hexChunk.TileDiameter = tileDiameter;

            Chunks.Add(hexChunk);

            return hexChunk;
        }

        /// <summary>
        /// Round the input integer down to the nearest multiple of the supplied interval.
        /// Used to calculate which chunk a tile falls inside the bounds of.
        /// </summary>
        private static int RoundDownToInterval(int input, int interval)
        {
            return ((int)Math.Floor(input / (float)interval)) * interval;
        }


        private void SetUpSidePiecesForTile(HexCoords position, HexChunk tileChunk)
        {
            var tile = FindTileForCoords(position);
            if (tile == null)
            {
                throw new ApplicationException("Tried to set up side pieces for non-existent tile.");
            }

            foreach (var side in HexMetrics.AdjacentHexes)
            {
                var sidePosition = position + side;

                var adjacentTile = FindTileForCoords(sidePosition);
                if (adjacentTile != null)
                {
                    var chunkWithTile = FindChunkForCoordinatesAndMaterial(sidePosition, adjacentTile.Material);

                    if (chunkWithTile != null)
                    {
                        chunkWithTile.TryRemovingSidePiece(position, side);

                        if (adjacentTile.Position.Elevation < tile.Position.Elevation)
                        {
                            tileChunk.AddSidePiece(position, side, tile.Position.Elevation - adjacentTile.Position.Elevation);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to remove the tile at the specified position.
        /// Returns true if it was removed successfully, false if no tile was found at that position.
        /// </summary>
        public bool TryRemovingTile(HexCoords position)
        {
            var tile = FindTileForCoords(position);
            if (tile == null)
            {
                return false;
            }

            var chunksWithTile = Chunks.Where(c => c.Tiles.Select(pos => pos.Coordinates).Contains(position));
            if (chunksWithTile == null || chunksWithTile.Count() < 1)
            {
                Debug.LogError("Tile found in internal tile collection but not in scene. Removing", this);
            }

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
            Chunks.Clear();

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
            return FindTileForCoords(tileCoords) != null;
        }

        /// <summary>
        /// Return data about the tile at the specified position.
        /// </summary>
        public bool TryGetTile(HexCoords tileCoords, out HexTileData data)
        {
            data = FindTileForCoords(tileCoords);
            return data != null;
        }

        /// <summary>
        /// Remove the tile at the specified coordinates and replace it with one with the specified material.
        /// Returns the chunk with the tile that was modified.
        /// </summary>
        public ModifiedTileInfo ReplaceMaterialOnTile(HexCoords tileCoords, Material material)
        {
            HexTileData tile;
            if (!TryGetTile(tileCoords, out tile))
            {
                throw new ArgumentOutOfRangeException("Tried replacing material on tile but map contains no tile at position " + tileCoords);
            }

            // Early out if the material is the same.
            if (tile.Material == material)
            {
                var chunk = FindChunkForCoordinatesAndMaterial(tileCoords, material);
                return new ModifiedTileInfo(chunk, ModifiedTileInfo.ChunkOperation.Modified);
            }

            TryRemovingTile(tileCoords);

            return CreateAndAddTile(tile.Position, material);
        }

        /// <summary>
        /// Returns all the tiles in this map.
        /// </summary>
        public IEnumerable<HexTileData> GetAllTiles()
        {
            return Tiles;
        }

        /// <summary>
        /// Clears the internal cache of chunks, which will be lazily rebuilt next 
        /// time it's needed by scanning the scene hierarchy. Needed to be done after
        /// a chunk is deleted by an undo action.
        /// </summary>
        public void ClearChunkCache()
        {
            chunks = null;
        }
    }
}

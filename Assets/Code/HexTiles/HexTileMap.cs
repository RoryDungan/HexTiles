using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Collections.Generic;

namespace HexTiles
{
    public class HexTileMap : MonoBehaviour
    {
        public float hexWidth = 1f;

        [SerializeField]
        private bool drawHexPositionGizmos = false;

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
        public IHexTileCollection Tiles
        {
            get
            {
                // Lazy init hexes hashtable
                if (tiles == null)
                {
                    // Add all the existing tiles in the scene that are part of this map
                    tiles = new HexTileCollection();
                    foreach (var tile in GetComponentsInChildren<HexTile>())
                    {
                        var tilePosition = QuantizeVector3ToHexCoords(tile.transform.position);
                        try
                        {
                            tiles.Add(tilePosition, tile);
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
                }
                return tiles;
            }
        }
        private IHexTileCollection tiles;

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

            if (SelectedTile != null && Tiles[SelectedTile] != null)
            {
                DrawHexGizmo(HexPositionToWorldPosition(new HexPosition(SelectedTile, Tiles[SelectedTile].transform.position.y)), Color.green);
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
            var x = hexWidth/2f * 3f/2f * position.Coordinates.Q;
            var z = hexWidth/2f * Mathf.Sqrt(3f) * (position.Coordinates.R + position.Coordinates.Q / 2f);
            var y = position.Elevation;
            return transform.TransformPoint(new Vector3(x, y, z));
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
            foreach (var tileCoords in Tiles.Keys)
            {
                var hexTile = Tiles[tileCoords];
                hexTile.Diameter = hexWidth;
                hexTile.transform.position = HexPositionToWorldPosition(new HexPosition(tileCoords, hexTile.Elevation));
                hexTile.GenerateMesh(tileCoords);
            }
        }

        /// <summary>
        /// Add a tile to the map. Returns the newly added hex tile.
        /// If a tile already exists at that position then that is returned instead.
        /// </summary>
        public HexTile CreateAndAddTile(HexPosition position, Material material)
        {
            var coords = position.Coordinates;
            var elevation = position.Elevation;

            // See if there's already a tile at the specified position.
            if (Tiles.Contains(coords))
            {
                var tile = Tiles[coords];

                // If a tlie at that position and that height already exists, return it.
                if (tile.Elevation == elevation
                    && tile.GetComponent<MeshRenderer>().sharedMaterial == material)
                {
                    return tile;
                }

                // Remove the tile before adding a new one.
                TryRemovingTile(coords);
            }

            var obj = SpawnTileObject(position);

            var hex = obj.AddComponent<HexTile>();
            hex.Diameter = hexWidth;

            Tiles.Add(coords, hex);

            // Generate side pieces
            // Note that we also need to update all the tiles adjacent to this one so that any side pieces that could be 
            // Obscured by this one are removed.
            foreach (var side in HexMetrics.AdjacentHexes)
            {
                HexTile adjacentTile;
                var adjacentTilePos = coords + side;
                if (Tiles.TryGetValue(adjacentTilePos, out adjacentTile))
                {
                    SetUpSidePiecesForTile(adjacentTilePos);
                    adjacentTile.GenerateMesh(adjacentTilePos);
                }
            }
            SetUpSidePiecesForTile(coords);
            hex.GenerateMesh(coords);

            // Set up material
            hex.GetComponent<Renderer>().sharedMaterial = material;

            return hex;
        }

        private void SetUpSidePiecesForTile(HexCoords position)
        {
            HexTile tile;
            if (!Tiles.TryGetValue(position, out tile))
            {
                throw new ApplicationException("Tried to set up side pieces for non-existent tile.");
            }

            foreach (var side in HexMetrics.AdjacentHexes)
            {
                HexTile adjacentTile;
                if (Tiles.TryGetValue(position + side, out adjacentTile))
                {
                    tile.TryRemovingSidePiece(side);
                    if (adjacentTile.Elevation < tile.Elevation)
                    {
                        tile.AddSidePiece(side, tile.Elevation - adjacentTile.Elevation);
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
            if (!Tiles.Contains(position))
            {
                return false;
            }

            var tile = Tiles[position];
            DestroyImmediate(tile.gameObject);
            Tiles.Remove(position);

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
    }
}

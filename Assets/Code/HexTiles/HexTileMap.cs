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

        /// <summary>
        /// Collection of all hex tiles that are part of this map.
        /// </summary>
        private IHexTileCollection Tiles
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
                        tiles.Add(QuantizeVector3ToHexCoords(tile.transform.position), tile);
                    }
                }
                return tiles;
            }
        }
        private IHexTileCollection tiles;

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
        public HexCoords HighlightedTile
        {
            get
            {
                return highlightedTile;
            }
            set
            {
                highlightedTile = value;
            }
        }
        private HexCoords highlightedTile;

        void OnDrawGizmos()
        {
            if (HighlightedTile != null)
            {
                DrawHexGizmo(HexCoordsToWorldPosition(HighlightedTile), Color.grey);
            }

            if (SelectedTile != null)
            {
                DrawHexGizmo(HexCoordsToWorldPosition(SelectedTile), Color.green);
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
            var q = vIn.x * 2f/3f / (hexWidth/2f);
            var r = (-vIn.x / 3f + Mathf.Sqrt(3f)/3f * vIn.z) / (hexWidth/2f);

            return new HexCoords 
            { 
                Q = Mathf.RoundToInt(q), 
                R = Mathf.RoundToInt(r),
                Elevation = vIn.y
            };
        }

        /// <summary>
        /// Get the world space position of the specified hex coords.
        /// This uses axial coordinates for the hexes.
        /// </summary>
        public Vector3 HexCoordsToWorldPosition(HexCoords hIn)
        {
            var x = hexWidth/2f * 3f/2f * hIn.Q;
            var z = hexWidth/2f * Mathf.Sqrt(3f) * (hIn.R + hIn.Q / 2f);
            var y = hIn.Elevation;
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the nearest hex tile position in world space 
        /// to the specified position.
        /// </summary>
        public Vector3 QuantizePositionToHexGrid(Vector3 vIn)
        {
            return HexCoordsToWorldPosition(QuantizeVector3ToHexCoords(vIn));
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
                hexTile.transform.position = HexCoordsToWorldPosition(tileCoords);
                hexTile.GenerateMesh();
            }
        }

        /// <summary>
        /// Add a tile to the map. Returns the newly added hex tile.
        /// If a tile already exists at that position then that is returned instead.
        /// </summary>
        public HexTile CreateAndAddTile(HexCoords position)
        {
            // See if there's already a tile at the specified position.
            if (Tiles.Contains(position))
            {
                return Tiles[position];
            }

            var obj = SpawnTileObject(position);

            var hex = obj.AddComponent<HexTile>();
            hex.Diameter = hexWidth;
            hex.GenerateMesh();

            Tiles.Add(position, hex);

            // TODO Rory 26/06/16: Set up material.

            return hex;
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

        private GameObject SpawnTileObject(HexCoords position)
        {
            var newObject = new GameObject("Tile [" + position.Q + ", " + position.R + "]");
            newObject.transform.parent = transform;
            newObject.transform.position = HexCoordsToWorldPosition(position);

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

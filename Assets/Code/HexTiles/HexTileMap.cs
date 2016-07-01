using UnityEngine;
using System.Collections;
using System.Linq;
using System;

namespace HexTiles
{
    public class HexTileMap : MonoBehaviour
    {
        [SerializeField]
        private float hexWidth = 1f;

        /// <summary>
        /// Hashtable of all hex tiles that are part of this map.
        /// </summary>
        private Hashtable Tiles
        {
            get
            {
                // Lazy init hexes hashtable
                if (tiles == null)
                {
                    // Add all the existing tiles in the scene that are part of this map
                    tiles = new Hashtable();
                    foreach (var tile in GetComponentsInChildren<HexTile>())
                    {
                        tiles.Add(QuantizeVector3ToHexCoords(tile.transform.position), tile);
                    }
                }
                return tiles;
            }
        }
        private Hashtable tiles;

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
            DrawHexGizmo(HexCoordsToWorldPosition(SelectedTile), Color.green);

            DrawHexGizmo(HexCoordsToWorldPosition(HighlightedTile), Color.grey);
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
            var r = (-(float)vIn.x / 3f + Mathf.Sqrt(3f)/3f * (float)vIn.z) / (hexWidth/2f);

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
            var z = hexWidth/2f * Mathf.Sqrt(3f) * ((float)hIn.R + (float)hIn.Q / 2f);
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
        /// Add a tile to the map. Returns the newly added hex tile.
        /// If a tile already exists at that position then that is returned instead.
        /// </summary>
        public HexTile AddHexTile(HexCoords position)
        {
            // See if there's already a tile at the specified position.
            if (Tiles.Contains(position))
            {
                return (HexTile)Tiles[position];
            }

            var newObject = new GameObject("Tile [" + position.Q + ", " + position.R + "]");
            newObject.transform.parent = transform;
            newObject.transform.position = HexCoordsToWorldPosition(position);

            var hex = newObject.AddComponent<HexTile>();
            hex.Diameter = hexWidth;
            hex.GenerateMesh();

            Tiles.Add(position, hex);

            // TODO Rory 26/06/16: Set up material.

            return hex;
        }
    }
}

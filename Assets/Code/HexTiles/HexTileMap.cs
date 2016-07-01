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


        // Use this for initialization
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnDrawGizmos()
        {
            DrawGizmos(false);
        }

        void OnDrawGizmosSelected()
        {
            DrawGizmos(true);
        }

        void DrawGizmos(bool selected)
        {
            DrawHexGizmo(Vector3.zero, selected);
        }

        /// <summary>
        /// Draws the outline of a hex at the specified position.
        /// Can be grey or green depending on whether it's highlighted or not.
        /// </summary>
        private void DrawHexGizmo(Vector3 position, bool highlight = false)
        {
            Gizmos.color = highlight ? Color.green : Color.gray;

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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the world space position of the specified hex coords.
        /// This uses odd-q layout for the hexes.
        /// </summary>
        public Vector3 HexCoordsToWorldPosition(HexCoords hIn)
        {
            var x = hexWidth * hIn.col;
            var z = hIn.row * hexWidth * HexMetrics.hexHeightToWidth +
                hIn.row % 2 == 0 ? 0 : hexWidth * HexMetrics.hexHeightToWidth;
            var y = hIn.elevation;
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
        /// </summary>
        public HexTile AddHexTile(HexCoords position)
        {
            var newObject = new GameObject(string.Format("Tile_{0}-{1}", position.col, position.row));
            newObject.transform.parent = transform;
            newObject.transform.position = HexCoordsToWorldPosition(position);

            var hex = newObject.AddComponent<HexTile>();
            hex.Diameter = hexWidth;
            hex.GenerateMesh();

            // TODO Rory 26/06/16: Set up material.

            return hex;
        }
    }
}

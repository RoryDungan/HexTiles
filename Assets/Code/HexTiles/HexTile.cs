using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using RSG.Utils;

namespace HexTiles
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexTile : MonoBehaviour
    {
        /// <summary>
        /// Total diameter of the hex.
        /// </summary>
        private float size = 1f;

        /// <summary>
        /// Total diameter of the hex.
        /// </summary>
        public float Diameter
        {
            get
            {
                return size;
            }
            set
            {
                size = value;
            }
        }

        /// <summary>
        /// Helper for easily getting the elevation of a tile.
        /// </summary>
        public float Elevation
        {
            get
            {
                return transform.localPosition.y;
            }
        }

        [SerializeField]
        private List<SidePiece> sidePieces = new List<SidePiece>();

        /// <summary>
        /// Draw gizmos for debugging the hex vertex placement.
        /// </summary>
        [SerializeField]
        [Tooltip("Draw gizmos for debugging the hex vertex placement")]
        private bool debugDrawGizmos = false;

        /// <summary>
        /// Tileset textures are 3 hexes wide for the tops of the tiles.
        /// </summary>
        private static readonly float hexWidthUV = 1f / 3f;

        /// <summary>
        /// Get the UV coordinates of a given hex tile.
        /// </summary>
        private static Vector2 HexCoordsToUV(HexCoords hIn)
        {
            var x = hexWidthUV/2f * 3f/2f * hIn.Q;
            var y = hexWidthUV/2f * Mathf.Sqrt(3f) * (hIn.R + hIn.Q / 2f);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Create the mesh used to render the hex.
        /// </summary>
        public void GenerateMesh(HexCoords position)
        {
            var mesh = GetComponent<MeshFilter>().mesh = new Mesh();
            mesh.name = "Procedural hex tile";

            var vertices = HexMetrics.GetHexVertices(Diameter).ToList();
            var triangles = new List<int>
            {
                // Start with the preset triangles for the hex top.
                0, 1, 2,
                0, 2, 3,
                0, 3, 5,
                5, 3, 4
            };

            // UV coordinates for tops of hex tiles.
            var uvBasePos = HexCoordsToUV(position);
            var hexTopVertices = HexMetrics.GetHexVertices(hexWidthUV);
            var uv = new List<Vector2>();
            for (var i = 0; i < hexTopVertices.Length; i++)
            {
                uv.Add(new Vector2(uvBasePos.x + hexTopVertices[i].x, (uvBasePos.y + hexTopVertices[i].z) / HexMetrics.hexHeightToWidth));
            }

            foreach (var sidePiece in sidePieces)
            {
                var nextVertexIndex = vertices.Count;
                vertices.Add(new Vector3(vertices[sidePiece.direction].x, -sidePiece.elevationDelta, vertices[sidePiece.direction].z));
                vertices.Add(new Vector3(vertices[(sidePiece.direction + 1) % 6].x, -sidePiece.elevationDelta, vertices[(sidePiece.direction + 1) % 6].z));

                triangles.AddRange(new int[]{
                    sidePiece.direction, nextVertexIndex, nextVertexIndex + 1,
                    sidePiece.direction, nextVertexIndex + 1, (sidePiece.direction + 1) % 6
                });

                // TODO: add proper side piece UV coords.
                uv.Add(Vector2.zero);
                uv.Add(Vector2.zero);
            }

            var tangents = new Vector4[vertices.Count];

            for (var i = 0; i < vertices.Count; i++)
            {
                tangents[i].Set(1f, 0f, 0f, -1f);
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();

            mesh.RecalculateNormals();

            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Generates and adds a side piece to the tile.
        /// </summary>
        internal void AddSidePiece(HexCoords side, float height)
        {
            var sideIndex = Array.IndexOf(HexMetrics.AdjacentHexes, side);
            if (sideIndex < 0)
            {
                throw new ApplicationException("Hex tile " + side + " is not a valid adjacent tile.");
            }

            sidePieces.Add(new SidePiece { direction = sideIndex, elevationDelta = height });
        }

        /// <summary>
        /// Remove the specified side piece from this hex tile.
        /// </summary>
        internal void TryRemovingSidePiece(HexCoords side)
        {
            var sideIndex = Array.IndexOf(HexMetrics.AdjacentHexes, side);
            if (sideIndex < 0)
            {
                throw new ApplicationException("Hex tile " + side + " is not a valid adjacent tile.");
            }

            var sidePiecesToRemove = sidePieces
                .Where(sidePiece => sidePiece.direction == sideIndex)
                .ToArray();

            foreach (var sidePiece in sidePiecesToRemove)
            {
                sidePieces.Remove(sidePiece);
            }
        }

        /// <summary>
        /// Clear all side pieces. Note that this won't affect the actual mesh until GenerateMesh() is called.
        /// </summary>
        internal void ResetSidePieces()
        {
            sidePieces.Clear();
        }

        /// <summary>
        /// Side piece for the hex tile.
        /// </summary>
        [Serializable]
        private struct SidePiece
        {
            public int direction;
            public float elevationDelta;
        }

        void OnDrawGizmos()
        {
            if (!debugDrawGizmos)
            {
                return;
            }

            var vertices = HexMetrics.GetHexVertices(size);
            var gizmoSize = size / 10f;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(vertices[0], gizmoSize);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(vertices[1], gizmoSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(vertices[2], gizmoSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(vertices[3], gizmoSize);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(vertices[4], gizmoSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(vertices[5], gizmoSize);
        }
    }
}
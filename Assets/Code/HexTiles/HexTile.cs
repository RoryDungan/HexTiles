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
        /// Offset for the side piece textures, since they only use the lower half of the texture we apply to the mesh.
        /// </summary>
        private static readonly float sidePieceUVOffsetY = 0.5f;

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
            var triangles = new List<int>();

            // UV coordinates for tops of hex tiles.
            var uvBasePos = HexCoordsToUV(position);
            var uv = new List<Vector2>();

            // Each third tile needs a UV seam through the middle of it for textures to loop properly.
            var offsetCoords = position.ToOffset();
            if (offsetCoords.x % 2 == 0 && offsetCoords.y % 3 == 0)
            {
                vertices.Insert(4, vertices[0]);
                vertices.Insert(5, vertices[3]);

                triangles.AddRange(new int[]
                {
                    // Start with the preset triangles for the hex top.
                    0, 1, 2,
                    0, 2, 3,
                    4, 5, 7,
                    7, 5, 6
                });

                for (var i = 0; i < vertices.Count; i++)
                {
                    float uvY;
                    if (i == 5 || i == 4)
                    {
                        uvY = -0.5f;
                    }
                    else
                    {
                        var relativeVertexPosInUVSpace = vertices[i].z / Diameter * hexWidthUV;
                        uvY = -Utils.Mod(((uvBasePos.y + relativeVertexPosInUVSpace) / HexMetrics.hexHeightToWidth / 2), 0.5f);
                    }
                    uv.Add(new Vector2(
                        uvBasePos.x + vertices[i].x / Diameter * hexWidthUV,
                        uvY));
                }
            }
            else
            {
                triangles.AddRange(new int[]
                {
                    // Start with the preset triangles for the hex top.
                    0, 1, 2,
                    0, 2, 3,
                    0, 3, 5,
                    5, 3, 4
                });

                for (var i = 0; i < vertices.Count; i++)
                {
                    var relativeVertexPosInUVSpace = vertices[i].z / Diameter * hexWidthUV;
                    float uvY;
                    // Sometimes due to rounding the UV coordinate can end up on the wrong side of the texture
                    // for tiles at the edges of the texture. This will ensure that they appear on the correct
                    // side.
                    if ((offsetCoords.x % 2 != 0 && (offsetCoords.y + 1) % 3 == 0) && (i == 1 || i == 2))
                    {
                        uvY = -0.5f;
                    }
                    else
                    {
                        uvY = -Utils.Mod(((uvBasePos.y + relativeVertexPosInUVSpace) / HexMetrics.hexHeightToWidth / 2), 0.5f);
                    }
                    uv.Add(new Vector2(
                        uvBasePos.x + vertices[i].x / Diameter * hexWidthUV, 
                        uvY));
                }
            }

            var topVerts = HexMetrics.GetHexVertices(Diameter).ToList();
            foreach (var sidePiece in sidePieces)
            {
                // Nedd to add a side piece for each time the texture loops.
                var sideLoopCount = 0;
                var maxSideHeight = (Diameter / 2f * 3f); // Maximum height of a single piece before they loop
                var totalSideHeight = sidePiece.elevationDelta;
                do
                {
                    // The Y position in UV space of the bottom vertices.
                    // Dependent on the height of the side piece.
                    var currentHeightLeft = (totalSideHeight - sideLoopCount * maxSideHeight);
                    var currentPieceHeight = Mathf.Min(maxSideHeight, currentHeightLeft);
                    var startingHeight = maxSideHeight * sideLoopCount;

                    var sideIndex = sidePiece.direction;
                    var nextSideIndex = (sidePiece.direction + 1) % 6;

                    var nextVertexIndex = vertices.Count;

                    vertices.Add(new Vector3(topVerts[sideIndex].x, startingHeight, topVerts[sideIndex].z));
                    vertices.Add(new Vector3(topVerts[nextSideIndex].x, startingHeight, topVerts[nextSideIndex].z));
                    vertices.Add(new Vector3(topVerts[sideIndex].x, -(startingHeight + currentPieceHeight), topVerts[sideIndex].z));
                    vertices.Add(new Vector3(topVerts[nextSideIndex].x, -(startingHeight + currentPieceHeight), topVerts[nextSideIndex].z));

                    triangles.AddRange(new int[]{
                        nextVertexIndex, nextVertexIndex + 2, nextVertexIndex + 1,
                        nextVertexIndex + 1, nextVertexIndex + 2, nextVertexIndex + 3
                    });

                    const float maxUVHeight = 0.5f; // We're only using the top or bottom half of the bottomo half of the texture for this part of the side pieces.
                    var bottomUvY = (currentPieceHeight / maxSideHeight) * maxUVHeight;
                    // The first part of the side piece uses the top half of the texture,
                    // whild the rest use looped copies of the second half of the texture.
                    if (sideLoopCount == 0)
                    {
                        uv.Add(new Vector2(hexWidthUV / 2f * sideIndex, 0f + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * nextSideIndex, 0f + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * sideIndex, bottomUvY / sidePieceUVOffsetY + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * nextSideIndex, bottomUvY / sidePieceUVOffsetY + sidePieceUVOffsetY));
                    }
                    else
                    {
                        uv.Add(new Vector2(hexWidthUV / 2f * sideIndex, maxUVHeight / sidePieceUVOffsetY + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * nextSideIndex, maxUVHeight / sidePieceUVOffsetY + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * sideIndex, bottomUvY / sidePieceUVOffsetY + sidePieceUVOffsetY));
                        uv.Add(new Vector2(hexWidthUV / 2f * nextSideIndex, bottomUvY / sidePieceUVOffsetY + sidePieceUVOffsetY));
                    }

                    sideLoopCount++;
                }
                while (maxSideHeight * sideLoopCount < totalSideHeight);
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
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[0]), gizmoSize);
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[1]), gizmoSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[2]), gizmoSize);
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[3]), gizmoSize);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[4]), gizmoSize);
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.localToWorldMatrix.MultiplyPoint(vertices[5]), gizmoSize);
        }
    }
}
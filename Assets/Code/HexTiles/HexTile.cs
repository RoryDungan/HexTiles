using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HexTiles
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class HexTile : MonoBehaviour
    {
        /// <summary>
        /// Total diameter of the hex.
        /// </summary>
        [HideInInspector]
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
        /// sqrt(3)/2
        /// The ratio of a flat-topped hexagon's height to its width.
        /// </summary>
        public static readonly float hexHeightToWidth = 0.86602540378f;

        private IList<SidePiece> sidePieces = new List<SidePiece>();

        /// <summary>
        /// Create the mesh used to render the hex.
        /// </summary>
        [ContextMenu("Generate mesh")]
        public void GenerateMesh()
        {
            var mesh = GetComponent<MeshFilter>().mesh = new Mesh();
            mesh.name = "Procedural hex tile";

            var vertices = HexMetrics.GetHexVertices(Diameter).ToList();
            var triangles = new List<int>
            {
                // Start with the preset triangles for the hex top.
                0, 1, 5,
                1, 4, 5,
                1, 2, 4,
                2, 3, 4
            };
            
            foreach (var sidePiece in sidePieces)
            {
                var nextVertexIndex = vertices.Count;
                vertices.Add(new Vector3(vertices[sidePiece.direction].x, -sidePiece.elevationDelta, vertices[sidePiece.direction].z));
                vertices.Add(new Vector3(vertices[(sidePiece.direction + 1) % 6].x, -sidePiece.elevationDelta, vertices[(sidePiece.direction + 1) % 6].z));

                triangles.AddRange(new int[]{
                    sidePiece.direction, nextVertexIndex, nextVertexIndex + 1,
                    sidePiece.direction, nextVertexIndex + 1, (sidePiece.direction + 1) % 6
                });
            }

            var uv = new Vector2[vertices.Count];
            var tangents = new Vector4[vertices.Count];

            for (var i = 0; i < vertices.Count; i++)
            {
                tangents[i].Set(1f, 0f, 0f, -1f);
            }

            mesh.vertices = vertices.ToArray();

            mesh.triangles = triangles.ToArray();

            // TODO: Generate meshes for side pieces.

            mesh.RecalculateNormals();

            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Generates and adds a side piece to the tile.
        /// </summary>
        internal void AddSidePiece(HexCoords side, float height)
        {
            var sideIndex = (Array.IndexOf(HexMetrics.AdjacentHexes, side) + 4) % 6;
            if (sideIndex < 0)
            {
                throw new ApplicationException("Hex tile " + side + " is not a valid adjacent tile.");
            }

            sidePieces.Add(new SidePiece { direction = sideIndex, elevationDelta = height });
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
        private struct SidePiece
        {
            public int direction;
            public float elevationDelta;
        }
    }
}

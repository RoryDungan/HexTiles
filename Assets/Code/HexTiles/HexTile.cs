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

        /// <summary>
        /// Accessor for tile material for convenience.
        /// </summary>
        public Material Material
        {
            get
            {
                return GetComponent<MeshRenderer>().sharedMaterial;
            }
            set
            {
                GetComponent<MeshRenderer>().sharedMaterial = value;
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
        /// Create the mesh used to render the hex.
        /// </summary>
        public void GenerateMesh(HexCoords position)
        {
            // Initial setup for game object 
            transform.localScale = Vector3.one;
            gameObject.isStatic = true;

            var mesh = GetComponent<MeshFilter>().mesh = new Mesh();
            mesh.name = "Procedural hex tile";

            var data = HexMeshGenerator.GenerateHexMesh(position, Diameter, sidePieces);

            mesh.vertices = data.verts.ToArray();
            mesh.triangles = data.tris.ToArray();
            mesh.uv = data.uvs.ToArray();

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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    /// <summary>
    /// Chunk of several hex tiles.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    internal class HexChunk : MonoBehaviour
    {
        public HexCoords upperBounds;
        public HexCoords lowerBounds;

        /// <summary>
        /// Total diameter of the hex.
        /// </summary>
        private float size = 1f;

        private Material material;

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
        private List<SidePieceInfo> sidePieces = new List<SidePieceInfo>();

        [SerializeField]
        private List<HexPosition> tiles = new List<HexPosition>();

        internal IList<HexPosition> Tiles { get { return tiles; } }

        internal void AddTile(HexPosition position)
        {
            tiles.Add(position);

            Dirty = true;
        }

        internal void RemoveTile(HexCoords coords)
        {
            tiles.RemoveAll(tile => tile.Coordinates.Equals(coords));

            Dirty = true;
        }

        /// <summary>
        /// Whether or not we need to re-generate the mesh for this tile.
        /// </summary>
        public bool Dirty { get; private set; }

        /// <summary>
        /// Create the mesh used to render the hex.
        /// </summary>
        public void GenerateMesh()
        {
            // Initial setup for game object 
            transform.localScale = Vector3.one;
            gameObject.isStatic = true;

            var mesh = GetComponent<MeshFilter>().mesh = new Mesh();
            mesh.name = "Chunk";

            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            // UV coordinates for tops of hex tiles.
            var uv = new List<Vector2>();

            foreach (var tile in tiles)
            {
                var startingTriIndex = vertices.Count;

                var data = HexMeshGenerator.GenerateHexMesh(
                    tile.Coordinates, 
                    Diameter, 
                    sidePieces
                        .Where(sideInfo => sideInfo.hex == tile.Coordinates)
                        .Select(sideInfo => sideInfo.side)
                );

                // Transform to correct position
                vertices.AddRange(data.verts.Select(vert => vert + tile.GetPositionVector(Diameter))); 

                // Add to get correct indices
                triangles.AddRange(data.tris.Select(index => index + startingTriIndex)); 

                uv.AddRange(data.uvs);

            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();

            mesh.RecalculateNormals();

            GetComponent<MeshCollider>().sharedMesh = mesh;

            Dirty = false;
        }

        /// <summary>
        /// Generates and adds a side piece to the tile.
        /// </summary>
        internal void AddSidePiece(HexCoords tile, HexCoords side, float height)
        {
            var sideIndex = Array.IndexOf(HexMetrics.AdjacentHexes, side);
            if (sideIndex < 0)
            {
                throw new ApplicationException("Hex tile " + side + " is not a valid adjacent tile.");
            }

            sidePieces.Add(new SidePieceInfo
            {
                side = new SidePiece { direction = sideIndex, elevationDelta = height },
                hex = tile
            });

            Dirty = true;
        }

        /// <summary>
        /// Remove the specified side piece from this hex tile.
        /// </summary>
        internal void TryRemovingSidePiece(HexCoords tile, HexCoords side)
        {
            var sideIndex = Array.IndexOf(HexMetrics.AdjacentHexes, side);
            if (sideIndex < 0)
            {
                throw new ApplicationException("Hex tile " + side + " is not a valid adjacent tile.");
            }

            var sidePiecesToRemove = sidePieces
                .Where(sidePieceInfo => sidePieceInfo.hex == tile)
                .Where(sidePieceInfo => sidePieceInfo.side.direction == sideIndex)
                .ToArray();

            foreach (var sidePiece in sidePiecesToRemove)
            {
                sidePieces.Remove(sidePiece);
            }

            Dirty = true;
        }

        /// <summary>
        /// Clear all side pieces. Note that this won't affect the actual mesh until GenerateMesh() is called.
        /// </summary>
        internal void ResetSidePieces()
        {
            sidePieces.Clear();
        }

        [Serializable]
        private struct SidePieceInfo
        {
            public SidePiece side;
            public HexCoords hex;
        }
    }
}

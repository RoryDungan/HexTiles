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
    public class HexChunk : MonoBehaviour
    {
        /// <summary>
        /// Upper bounds of the range of tiles contained in this chunk.
        /// </summary>
        [HideInInspector]
        public HexCoords upperBounds;

        /// <summary>
        /// Lower bounds of the range of tiles contained in this chunk.
        /// </summary>
        [HideInInspector]
        public HexCoords lowerBounds;

        /// <summary>
        /// Width of each individual hex tile.
        /// </summary>
        [SerializeField, HideInInspector]
        private float tileDiameter = 1f;

        /// <summary>
        /// Width of each individual hex tile.
        /// </summary>
        public float TileDiameter
        {
            get
            {
                return tileDiameter;
            }
            set
            {
                tileDiameter = value;
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

        /// <summary>
        /// The mesh of this chunk.
        /// </summary>
        public MeshFilter MeshFilter 
        {
            get 
            {
                if (meshFilter == null)
                {
                    meshFilter = GetComponent<MeshFilter>();
                }
                return meshFilter;
            }
        }
        
        /// <summary>
        /// The mesh of this chunk.
        /// </summary>
        public MeshCollider MeshCollider 
        {
            get 
            {
                if (meshCollider == null)
                {
                    meshCollider = GetComponent<MeshCollider>();
                }
                return meshCollider;
            }
        }

        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        [SerializeField, HideInInspector]
        private List<SidePieceInfo> sidePieces = new List<SidePieceInfo>();

        [SerializeField, HideInInspector]
        private List<HexPosition> tiles = new List<HexPosition>();

        public IList<HexPosition> Tiles { get { return tiles; } }

        /// <summary>
        /// Add a tile to this chunk.
        /// </summary>
        internal void AddTile(HexPosition position)
        {
            tiles.Add(position);

            Dirty = true;
        }

        /// <summary>
        /// Remove a tile from this chunk.
        /// </summary>
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
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            gameObject.isStatic = true;

            var mesh = MeshFilter.mesh = new Mesh();
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
                    TileDiameter, 
                    sidePieces
                        .Where(sideInfo => sideInfo.hex == tile.Coordinates)
                        .Select(sideInfo => sideInfo.side)
                );

                // Transform to correct position
                vertices.AddRange(data.verts.Select(vert => vert + tile.GetPositionVector(TileDiameter))); 

                // Add to get correct indices
                triangles.AddRange(data.tris.Select(index => index + startingTriIndex)); 

                uv.AddRange(data.uvs);

            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uv.ToArray();

            mesh.RecalculateNormals();

            MeshCollider.sharedMesh = mesh;

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

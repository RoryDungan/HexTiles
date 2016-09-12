using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HexTiles
{
    /// <summary>
    /// Utilites for generating meshes for hex tiles.
    /// </summary>
    public static class HexMeshGenerator
    {
        /// <summary>
        /// Tileset textures are 3 hexes wide for the tops of the tiles.
        /// </summary>
        private static readonly float hexWidthUV = 1f / 3f;

        /// <summary>
        /// Side pieces start half way down the texture.
        /// </summary>
        private static readonly float sidePieceStartingUVY = 0.5f;

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
        /// Generate the mesh for the specified position with the specified side pieces.
        /// Adds the necessary vertices, UVs and tris to the supplied lists.
        /// </summary>
        internal static HexMeshData GenerateHexMesh(HexCoords position, 
            float diameter,
            IEnumerable<SidePiece> sidePieces)
        {
            var vertices = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            GenerateTop(position, vertices, tris, uvs, diameter);

            GenerateSidePieces(vertices, tris, uvs, sidePieces, diameter);

            return new HexMeshData() { verts = vertices, tris = tris, uvs = uvs };
        }

        /// <summary>
        /// Generate the top for the hex tile, assiging its vertices, triangles and UVs to the supplied lists.
        /// </summary>
        private static void GenerateTop(HexCoords position, List<Vector3> vertices, List<int> triangles, List<Vector2> uv, float diameter)
        {
            var uvBasePos = HexCoordsToUV(position);

            vertices.AddRange(HexMetrics.GetHexVertices(diameter));

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
                    if (i == 0 || i == 3)
                    {
                        uvY = 0.0f;
                    }
                    else if (i == 5 || i == 4)
                    {
                        uvY = -0.5f;
                    }
                    else
                    {
                        var relativeVertexPosInUVSpace = vertices[i].z / diameter * hexWidthUV;
                        uvY = -Utils.Mod(((uvBasePos.y + relativeVertexPosInUVSpace) / HexMetrics.hexHeightToWidth / 2), 0.5f);
                    }
                    uv.Add(new Vector2(
                        -(uvBasePos.x + vertices[i].x / diameter * hexWidthUV),
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
                    var relativeVertexPosInUVSpace = vertices[i].z / diameter * hexWidthUV;
                    float uvY;
                    // Sometimes due to rounding the UV coordinate can end up on the wrong side of the texture
                    // for tiles at the edges of the texture. This will ensure that they appear on the correct
                    // side.
                    if ((offsetCoords.x % 2 != 0 && (offsetCoords.y + 1) % 3 == 0) && (i == 1 || i == 2))
                    {
                        uvY = -0.5f;
                    }
                    else if (offsetCoords.x % 2 != 0 && offsetCoords.y % 3 == 0 && (i == 4 || i == 5))
                    {
                        uvY = 0f;
                    }
                    else
                    {
                        uvY = -Utils.Mod(((uvBasePos.y + relativeVertexPosInUVSpace) / HexMetrics.hexHeightToWidth / 2), 0.5f);
                    }
                    uv.Add(new Vector2(
                        -(uvBasePos.x + vertices[i].x / diameter * hexWidthUV),
                        uvY));
                }
            }
        }

        /// <summary>
        /// Generates all the necessary side pieces, assigning their vertices, triangles and UVs back to the supplied lists.
        /// </summary>
        private static void GenerateSidePieces(List<Vector3> vertices, List<int> triangles, List<Vector2> uv, IEnumerable<SidePiece> sidePieces, float diameter)
        {
            var topVerts = HexMetrics.GetHexVertices(diameter).ToList();
            foreach (var sidePiece in sidePieces)
            {
                // Nedd to add a side piece for each time the texture loops.
                var sideLoopCount = 0;
                var maxSideHeight = (diameter / 2f * 3f); // Maximum height of a single piece before they loop
                var totalSideHeight = sidePiece.elevationDelta;
                do
                {
                    // The Y position in UV space of the bottom vertices.
                    // Dependent on the height of the side piece.
                    var currentHeightLeft = (totalSideHeight - sideLoopCount * maxSideHeight);
                    var currentPieceHeight = Mathf.Min(maxSideHeight, currentHeightLeft);
                    var startingHeight = -maxSideHeight * sideLoopCount;

                    var sideIndex = sidePiece.direction;
                    var nextSideIndex = (sidePiece.direction + 1) % 6;

                    var nextVertexIndex = vertices.Count;

                    vertices.Add(new Vector3(topVerts[sideIndex].x, startingHeight, topVerts[sideIndex].z));
                    vertices.Add(new Vector3(topVerts[nextSideIndex].x, startingHeight, topVerts[nextSideIndex].z));
                    vertices.Add(new Vector3(topVerts[sideIndex].x, startingHeight - currentPieceHeight, topVerts[sideIndex].z));
                    vertices.Add(new Vector3(topVerts[nextSideIndex].x, startingHeight - currentPieceHeight, topVerts[nextSideIndex].z));

                    triangles.AddRange(new int[]{
                        nextVertexIndex, nextVertexIndex + 2, nextVertexIndex + 1,
                        nextVertexIndex + 1, nextVertexIndex + 2, nextVertexIndex + 3
                    });

                    // We're only using the top or bottom half of the bottom half of the texture for this part of the side pieces.
                    const float maxUVHeight = 0.25f;
                    var bottomUvY = sidePieceStartingUVY + (currentPieceHeight / maxSideHeight) * -maxUVHeight;
                    // The first part of the side piece uses the top half of the texture,
                    // whild the rest use looped copies of the second half of the texture.
                    if (sideLoopCount == 0)
                    {
                        uv.Add(new Vector2((hexWidthUV / 2f) * sideIndex, sidePieceStartingUVY));
                        uv.Add(new Vector2((hexWidthUV / 2f) * (sideIndex + 1), sidePieceStartingUVY));
                        uv.Add(new Vector2((hexWidthUV / 2f) * sideIndex, bottomUvY));
                        uv.Add(new Vector2((hexWidthUV / 2f) * (sideIndex + 1), bottomUvY));
                    }
                    else
                    {
                        uv.Add(new Vector2((hexWidthUV / 2f) * sideIndex, sidePieceStartingUVY - maxUVHeight));
                        uv.Add(new Vector2((hexWidthUV / 2f) * (sideIndex + 1), sidePieceStartingUVY - maxUVHeight));
                        uv.Add(new Vector2((hexWidthUV / 2f) * sideIndex, bottomUvY - maxUVHeight));
                        uv.Add(new Vector2((hexWidthUV / 2f) * (sideIndex + 1), bottomUvY - maxUVHeight));
                    }

                    sideLoopCount++;
                }
                while (maxSideHeight * sideLoopCount < totalSideHeight);
            }
        }

        /// <summary>
        /// Data structure containing the vertex, triangle and UV data for a hex tile.
        /// </summary>
        public struct HexMeshData
        {
            public IEnumerable<Vector3> verts;
            public IEnumerable<int> tris;
            public IEnumerable<Vector2> uvs;
        }
    }
}

using UnityEngine;
using System.Collections;
using System.Linq;

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
    }
}

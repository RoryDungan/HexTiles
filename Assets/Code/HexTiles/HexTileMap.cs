using UnityEngine;
using System.Collections;

public class HexTileMap : MonoBehaviour
{
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
        if (selected)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color = Color.gray;
        }
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 4);
    }
}

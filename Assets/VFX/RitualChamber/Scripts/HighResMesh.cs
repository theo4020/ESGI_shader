using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HighResMesh : MonoBehaviour
{
    public int   resolution = 100;
    public float size       = 4f;

    void Start()
    {
        GetComponent<MeshFilter>().mesh = GenerateDiskMesh();
    }

    Mesh GenerateDiskMesh()
    {
        float radius   = size / 2f;
        int   rings    = resolution;
        int   segments = resolution * 2;

        int totalVerts = 1 + rings * segments;
        var verts = new Vector3[totalVerts];
        var uvs   = new Vector2[totalVerts];
        var tris  = new List<int>(segments * 3 + (rings - 1) * segments * 6);

        // Centre vertex
        verts[0] = Vector3.zero;
        uvs[0]   = new Vector2(0.5f, 0.5f);

        // Rings
        for (int r = 0; r < rings; r++)
        {
            float t = (r + 1f) / rings;
            float rr = radius * t;
            for (int s = 0; s < segments; s++)
            {
                float angle = 2f * Mathf.PI * s / segments;
                float cos   = Mathf.Cos(angle);
                float sin   = Mathf.Sin(angle);
                int   idx   = 1 + r * segments + s;
                verts[idx] = new Vector3(rr * cos, 0f, rr * sin);
                uvs[idx]   = new Vector2(0.5f + 0.5f * t * cos, 0.5f + 0.5f * t * sin);
            }
        }

        // Centre fan (ring 0)
        for (int s = 0; s < segments; s++)
        {
            tris.Add(0);
            tris.Add(1 + s);
            tris.Add(1 + (s + 1) % segments);
        }

        // Ring strips
        for (int r = 0; r < rings - 1; r++)
        {
            int inner = 1 + r * segments;
            int outer = 1 + (r + 1) * segments;
            for (int s = 0; s < segments; s++)
            {
                int sn = (s + 1) % segments;
                tris.Add(inner + s);
                tris.Add(outer + s);
                tris.Add(outer + sn);

                tris.Add(inner + s);
                tris.Add(outer + sn);
                tris.Add(inner + sn);
            }
        }

        var mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices    = verts;
        mesh.uv          = uvs;
        mesh.triangles   = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}

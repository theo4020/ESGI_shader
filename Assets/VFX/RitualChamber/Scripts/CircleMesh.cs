using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CircleMesh : MonoBehaviour
{
    public int resolution = 100;
    public float radius = 2f;

    void Start()
    {
        GetComponent<MeshFilter>().mesh = GenerateMesh();
    }

    Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int rings = resolution;
        int segments = resolution;

        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> tris = new List<int>();

        // Centre
        verts.Add(Vector3.zero);
        uvs.Add(new Vector2(0.5f, 0.5f));

        // Anneaux
        for (int r = 1; r <= rings; r++)
        {
            float t = (float)r / rings;
            float currentRadius = t * radius;

            for (int s = 0; s < segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * currentRadius;
                float z = Mathf.Sin(angle) * currentRadius;
                verts.Add(new Vector3(x, 0, z));
                uvs.Add(new Vector2(x / (radius * 2) + 0.5f, z / (radius * 2) + 0.5f));
            }
        }

        // Triangles du centre vers le premier anneau
        for (int s = 0; s < segments; s++)
        {
            int cur = 1 + s;
            int next = 1 + (s + 1) % segments;
            tris.Add(0);
            tris.Add(next);
            tris.Add(cur);
        }

        // Triangles entre les anneaux
        for (int r = 0; r < rings - 1; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int cur  = 1 + r * segments + s;
                int next = 1 + r * segments + (s + 1) % segments;
                int curUp  = cur + segments;
                int nextUp = next + segments;

                tris.Add(cur);
                tris.Add(next);
                tris.Add(curUp);

                tris.Add(next);
                tris.Add(nextUp);
                tris.Add(curUp);
            }
        }

        mesh.vertices = verts.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}

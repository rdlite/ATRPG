using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGenTest : MonoBehaviour
{
    [SerializeField] private int _xSize = 10, _ySize = 10;

    private Vector3[] _vertices;
    private Mesh _mesh;

    private void Start()
    {
        Generate();
    }

    private void Generate()
    {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;
        _mesh.name = "Grid";

        _vertices = new Vector3[(_xSize + 1) * (_ySize + 1)];
        Vector2[] uvs = new Vector2[_vertices.Length];

        for (int i = 0, y = 0; y <= _ySize; y++)
        {
            for (int x = 0; x <= _xSize; i++, x++)
            {
                if (x == 5 && y == 5)
                {
                    _vertices[i] = Vector3.one * 10000;
                    continue;
                }

                _vertices[i] = new Vector3(x, y, 0);
                uvs[i] = new Vector2((float)x / _xSize, (float)y / _ySize);
            }
        }

        _mesh.vertices = _vertices;
        _mesh.uv = uvs;

        int[] triangles = new int[_xSize * _ySize * 6];

        for (int ti = 0, vi = 0, y = 0, i = 0; y < _ySize; y++, vi++)
        {
            for (int x = 0; x < _xSize; x++, ti += 6, vi++, i++)
            {
                if (_vertices[i] != null)
                {
                    triangles[ti] = vi;
                    triangles[ti + 1] = triangles[ti + 4] = vi + _xSize + 1;
                    triangles[ti + 2] = triangles[ti + 3] = vi + 1;
                    triangles[ti + 5] = vi + _xSize + 2;
                }
            }
        }

        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    private void OnDrawGizmos()
    {
        if (_vertices != null)
        {
            Gizmos.color = Color.red;

            for (int i = 0; i < _vertices.Length; i++)
            {
                Gizmos.DrawSphere(transform.position + _vertices[i], .2f);
            }
        }
    }
}
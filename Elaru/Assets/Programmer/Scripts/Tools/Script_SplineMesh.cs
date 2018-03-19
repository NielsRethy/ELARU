using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Script_SplineGenerator))]
public class Script_SplineMesh : MonoBehaviour
{
    private MeshFilter _mf;
    public float UVScale = 1f;
    public int CircleInterpolation = 10;
    public float CircleRadius = 1f;

    void Start()
    {
        _mf = GetComponent<MeshFilter>();

        GenerateMesh();
    }


    public void GenerateMesh()
    {
        var mesh = GetMesh();
        var shape = GetExtrudeShape();
        var path = GetPath();

        Extrude(mesh, shape, path);
    }

    public void ClearMesh()
    {
        _mf.sharedMesh.Clear();
    }

    private ExtrudeShape GetExtrudeShape()
    {
        //Generates point of circle
        var ci = CircleInterpolation;
        var cr = CircleRadius;
        List<Vertex> vl = new List<Vertex>();
        float u = 0;
        for (float i = 0; i < ci; ++i)
        {
            var pos = new Vector3(cr * Mathf.Cos((float)(i * (2 * Math.PI / ci))), cr * Mathf.Sin((float)(i * (2 * Math.PI / ci))), 0f);
            var norm = pos / cr;
            vl.Add(new Vertex(pos, norm, u));
            if (i < ci / 2f)
                u += 1f / ci;
            else
                u -= 1f / ci;
        }

        var vert2Ds = vl.ToArray();

        List<int> vLines = new List<int>();
        for (int i = 0; i < ci; ++i)
        {
            vLines.Add(i);
            vLines.Add((i + 1) % ci);
        }
        vLines.Reverse();
        var lines = vLines.ToArray();

        return new ExtrudeShape(vert2Ds, lines);
    }

    private OrientedPoint[] GetPath()
    {
        Vector3[] p = new Vector3[0];
        var advanced = GetComponent<Script_SplineGenerator>();
        if (advanced)
            p = advanced.SplinePoints.ToArray();

        for (int i = 0; i < p.Length; ++i)
        {
            p[i] -= transform.position;
        }

        var path = new List<OrientedPoint>();

        for (int i = 0; i < p.Length; ++i)
        {
            var point = p[i];
            Vector3 rotDir;
            if (i < p.Length - 1)
            {
                rotDir = p[i + 1] - p[i];
            }
            else
            {
                rotDir = p[i] - p[i - 1];
            }
            var rotation = Quaternion.LookRotation(rotDir, Vector3.up);
            path.Add(new OrientedPoint(point, rotation));
        }

        return path.ToArray();
    }

    private Mesh GetMesh()
    {
        if (_mf == null)
            _mf = GetComponent<MeshFilter>();

        return _mf.sharedMesh ?? (_mf.sharedMesh = new Mesh());
    }

    private void Extrude(Mesh mesh, ExtrudeShape shape, OrientedPoint[] path)
    {
        int vertsInShape = shape.vert2Ds.Length;
        int segments = path.Length - 1;
        int edgeLoops = path.Length;
        int vertCount = vertsInShape * edgeLoops;
        int triCount = shape.lines.Length * segments;
        int triIndexCount = triCount * 3;

        var triangleIndices = new int[triIndexCount];
        var vertices = new Vector3[vertCount];
        var normals = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];

        float totalLength = 0;
        float distanceCovered = 0;
        for (int i = 0; i < path.Length - 1; i++)
        {
            var d = Vector3.Distance(path[i].position, path[i + 1].position);
            totalLength += d;
        }

        for (int i = 0; i < path.Length; i++)
        {
            int offset = i * vertsInShape;
            if (i > 0)
            {
                var d = Vector3.Distance(path[i].position, path[i - 1].position);
                distanceCovered += d;
            }
            float v = distanceCovered / totalLength * UVScale;

            for (int j = 0; j < vertsInShape; j++)
            {
                int id = offset + j;
                vertices[id] = path[i].LocalToWorld(shape.vert2Ds[j].point);
                normals[id] = path[i].LocalToWorldDirection(shape.vert2Ds[j].normal);
                uvs[id] = new Vector2(shape.vert2Ds[j].uCoord, v);
            }
        }
        int ti = 0;
        for (int i = 0; i < segments; i++)
        {
            int offset = i * vertsInShape;
            for (int l = 0; l < shape.lines.Length; l += 2)
            {
                int a = offset + shape.lines[l] + vertsInShape;
                int b = offset + shape.lines[l];
                int c = offset + shape.lines[l + 1];
                int d = offset + shape.lines[l + 1] + vertsInShape;
                triangleIndices[ti] = c; ti++;
                triangleIndices[ti] = b; ti++;
                triangleIndices[ti] = a; ti++;
                triangleIndices[ti] = a; ti++;
                triangleIndices[ti] = d; ti++;
                triangleIndices[ti] = c; ti++;
            }
        }


        mesh.Clear();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangleIndices;
    }

    public struct ExtrudeShape
    {
        public Vertex[] vert2Ds;
        public int[] lines;

        public ExtrudeShape(Vertex[] vert2Ds, int[] lines)
        {
            this.vert2Ds = vert2Ds;
            this.lines = lines;
        }
    }


    public struct Vertex
    {
        public Vector3 point;
        public Vector3 normal;
        public float uCoord;


        public Vertex(Vector3 point, Vector3 normal, float uCoord)
        {
            this.point = point;
            this.normal = normal;
            this.uCoord = uCoord;
        }
    }

    public struct OrientedPoint
    {
        public Vector3 position;
        public Quaternion rotation;


        public OrientedPoint(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }


        public Vector3 LocalToWorld(Vector3 point)
        {
            return position + rotation * point;
        }


        public Vector3 WorldToLocal(Vector3 point)
        {
            return Quaternion.Inverse(rotation) * (point - position);
        }


        public Vector3 LocalToWorldDirection(Vector3 dir)
        {
            return rotation * dir;
        }
    }
}

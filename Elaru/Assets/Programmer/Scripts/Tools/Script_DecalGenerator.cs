#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class Script_DecalGenerator : MonoBehaviour
{
    public bool AutoUpdate = false;

    //Whethet to hit static objects without collider as well
    public bool HitNonColliders = false;

    //Offset to avoid possible z fighting
    public bool OverrideOffset = false;
    public float Offset = 1e-3f;

    //Mesh variables
    private MeshFilter _mf;
    private Mesh _myMesh;

    private List<Vector3> _verts = new List<Vector3>();
    private List<Vector3> _normals = new List<Vector3>();
    private List<int> _triangles = new List<int>();
    private List<Vector2> _uvs = new List<Vector2>();

    //Projection bounds (in local space)
    private static Bounds _bounds = new Bounds(Vector3.zero, Vector3.one);

    //Variables to check if decal needs to be auto updated
    private Vector3 _lastPos;
    private Quaternion _lastRotation;
    private Vector3 _lastScale;

    void Start()
    {
        //Disable script in play mode to avoid unnecessary updating
        if (Application.isPlaying)
            Destroy(this);
    }


    void Update()
    {
        //Do not automatically update when hitting without colliders (performance)
        if (HitNonColliders)
            return;

        //Check unity transform changed
        if (!transform.hasChanged)
            return;

        //Check if transform actually changed
        if (transform.position == _lastPos
            && transform.rotation == _lastRotation
            && transform.localScale == _lastScale)
            return;

        //Save new transform values for next check
        _lastPos = transform.position;
        _lastRotation = transform.rotation;
        _lastScale = transform.localScale;

        //Clear mesh when moving without auto update
        if (!AutoUpdate)
        {
            CheckMesh();
            _myMesh.Clear();
            _mf.mesh = _myMesh;
            return;
        }

        UpdateMesh();
    }

    public void UpdateMesh()
    {
        CheckMesh();

        //Check hit colliders
        var overlapObjects = GetObjectsInOverlap();

        //Check hit static objects without collider if needed
        if (HitNonColliders)
        {
            var nonColOverlaps = GetObjectsInOverlapNoCollider();
            //Combine with objects hit by collider
            overlapObjects = overlapObjects.Union(nonColOverlaps).ToArray();
        }

        //Remove self if included by accident
        if (overlapObjects.Contains(gameObject))
            overlapObjects.ToList().Remove(gameObject);

        //No Objects hit
        if (overlapObjects.Length < 1)
        {
            ClearMesh();
            return;
        }

        //Clear buffers
        _verts.Clear();
        _normals.Clear();
        _triangles.Clear();
        _uvs.Clear();

        //Calculate new mesh
        foreach (var obj in overlapObjects)
        {
            //Matrix to transform from other local to own local space
            var matrix = transform.worldToLocalMatrix * obj.transform.localToWorldMatrix;

            var mf = obj.GetComponent<MeshFilter>();
            if (mf == null)
                return;

            var mesh = mf.sharedMesh;

            for (var i = 0; i < mesh.triangles.Length; i += 3)
            {
                //Get triangle indices
                var i1 = mesh.triangles[i];
                var i2 = mesh.triangles[i + 1];
                var i3 = mesh.triangles[i + 2];

                //Transform verts to own local space
                var offset = OverrideOffset ? Offset : 1e-3f;
                var p1 = matrix.MultiplyPoint(mesh.vertices[i1] + offset * mesh.normals[i1]);
                var p2 = matrix.MultiplyPoint(mesh.vertices[i2] + offset * mesh.normals[i2]);
                var p3 = matrix.MultiplyPoint(mesh.vertices[i3] + offset * mesh.normals[i3]);

                //Check if triangle has any vertex in bounding box
                if (!(_bounds.Contains(p1) || _bounds.Contains(p2) || _bounds.Contains(p3)))
                {
                    //Check for midpoints aswell
                    var m1 = (p1 + p2) / 2f;
                    var m2 = (p1 + p3) / 2f;
                    var m3 = (p2 + p3) / 2f;
                    if (!(_bounds.Contains(m1) || _bounds.Contains(m2) || _bounds.Contains(m3)))
                    {
                        //Last check, check for bounding box intersection
                        //Create triangle bouding box
                        var center = (p1 + p2 + p3) / 3f;
                        Bounds tb = new Bounds(center, Vector3.zero);
                        tb.Encapsulate(p1);
                        tb.Encapsulate(p2);
                        tb.Encapsulate(p3);
                        if (!_bounds.Intersects(tb))
                            continue;
                    }
                }

                //Calculate local space normals
                var n1 = matrix.MultiplyPoint(mesh.normals[i1]);
                var n2 = matrix.MultiplyPoint(mesh.normals[i2]);
                var n3 = matrix.MultiplyPoint(mesh.normals[i3]);

                //Add triangle to list
                AddPoint(p1, n1);
                AddPoint(p2, n2);
                AddPoint(p3, n3);
            }
        }
        
        //Generate mesh
        if (_verts.Count < 3)
            return;

        if (_myMesh == null)
            _myMesh = new Mesh();

        _myMesh.Clear();
        _myMesh.vertices = _verts.ToArray();
        _myMesh.normals = _normals.ToArray();
        _myMesh.uv = _uvs.ToArray();
        _myMesh.triangles = _triangles.ToArray();

        _mf.sharedMesh = _myMesh;

        //Make sure decal does not cast shadows
        GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    void AddPoint(Vector3 vert, Vector3 norm)
    {
        var ind = FindVertIndex(vert);
        //New vertex
        if (ind < 0)
        {
            _verts.Add(vert);
            _normals.Add(norm);
            _uvs.Add(new Vector2(vert.x + .5f, vert.z + .5f));
            _triangles.Add(_verts.Count - 1);
        }
        else
        {
            _normals[ind] = (_normals[ind] + norm).normalized;
            _triangles.Add(ind);
        }
    }

    private int FindVertIndex(Vector3 v)
    {
        for (var i = 0; i < _verts.Count; ++i)
        {
            if ((v - _verts[i]).sqrMagnitude < .01f * .01f)
                return i;
        }
        return -1;
    }

    public void ClearMesh()
    {
        CheckMesh();
        _myMesh.Clear();
        _mf.mesh = _myMesh;
    }

    private void CheckMesh()
    {
        if (_mf == null)
            _mf = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

        if (_myMesh == null)
        {
            _myMesh = _mf.GetComponent<Mesh>();
            if (_myMesh == null)
            {
                _myMesh = new Mesh();
                _mf.mesh = _myMesh;
            }
        }
    }

    private GameObject[] GetObjectsInOverlap()
    {
        //Return game objects where collider overlap with decal bounds
        return Physics.OverlapBox(transform.position, transform.localScale / 2f, transform.rotation)
            .Select(x => x.gameObject).ToArray();
    }

    private GameObject[] GetObjectsInOverlapNoCollider()
    {
        //Calculate world space bounding box
        var ltw = transform.localToWorldMatrix;
        var center = ltw.MultiplyPoint(_bounds.center);
        var max = ltw.MultiplyPoint(_bounds.max);
        var min = ltw.MultiplyPoint(_bounds.min);
        var worldBounds = new Bounds(center, max - min);

        //Return static objects that overlap with world space bounding box
        return FindObjectsOfType<MeshRenderer>().Where(x => x.gameObject.isStatic).Where(x => x.bounds.Intersects(worldBounds))
            .Select(x => x.gameObject).ToArray();
    }

    void OnDrawGizmosSelected()
    {
        //Draw cube around projection area
        Gizmos.color = new Color(1, 0, 0, 0.5F);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 1, 1));
        Gizmos.matrix = Matrix4x4.identity;
    }
}
#endif
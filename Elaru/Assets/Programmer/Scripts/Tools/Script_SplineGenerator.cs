using System;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Struct to contain spline control point and handles
/// </summary>
[Serializable]
public struct SplineControl
{
    public SplineControl(Vector3 p, Vector3 h)
    {
        Point = p;
        Handle = h;
        var b = h - p;
        Handle2 = p + b;
    }
    public Vector3 Point;
    public Vector3 Handle;
    public Vector3 Handle2;

    public Vector3 GetHandle(int index)
    {
        return index == 0 ? Handle : Handle2;
    }

    public void MovePoints(Vector3 offset)
    {
        Point += offset;
        Handle += offset;
        Handle2 += offset;
    }
}

/// <summary>
/// Generates spline along given spline control points
/// </summary>
[ExecuteInEditMode]
public class Script_SplineGenerator : MonoBehaviour
{
    public bool RenderHandles = true;
    public List<SplineControl> ControlPoints = new List<SplineControl>();
    public int InterpolationSteps = 5;
    [HideInInspector]
    public List<Vector3> SplinePoints = new List<Vector3>();

    private Vector3 _objectOrigin;

    // Use this for initialization
    void Start()
    {
        RefreshSplinePoints();
        _objectOrigin = transform.position;
    }

    public Vector3 CalculateSplinePoint(float t, SplineControl s1, SplineControl s2)
    {
        var han1 = s1.GetHandle(1);
        var han2 = s2.GetHandle(0);
        //Formula to interpolate over bezier curve
        Vector3 result = Mathf.Pow(1 - t, 3) * s1.Point +
                         3 * Mathf.Pow(1 - t, 2) * t * han1 +
                         3 * (1 - t) * Mathf.Pow(t, 2) * han2 +
                         Mathf.Pow(t, 3) * s2.Point;
        return result;
    }

#if UNITY_EDITOR
    public void AddControlPoint()
    {
        //Add action to undo buffer
        Undo.RecordObject(this, "Added Point");
        if (ControlPoints.Count > 0)
        {
            //Add control point in the direction following the spline
            var last = ControlPoints[ControlPoints.Count - 1];
            var dir = last.Point - last.Handle;
            dir.Normalize();
            ControlPoints.Add(new SplineControl(last.Point + 3 * dir, last.Point + dir));
        }
        else
        {
            //Initial control point
            ControlPoints.Add(new SplineControl(5 * Vector3.forward, Vector3.zero));
        }
        RefreshSplinePoints();
    }
#endif

    public void RefreshSplinePoints()
    {
        //Clear current points
        SplinePoints.Clear();

        //Can only generate spline with 2 or more points
        var sm = GetComponent<Script_SplineMesh>();
        if (ControlPoints.Count < 2)
        {
            if (sm != null)
                sm.ClearMesh();
            return;
        }

        //Calculate interpolation points
        for (int i = 0; i < ControlPoints.Count - 1; ++i)
        {
            var deltaT = 1.0f / (InterpolationSteps - 1);
            //Add control point
            SplinePoints.Add(ControlPoints[i].Point);

            //Add points between this and next control point
            for (var j = 1; j < InterpolationSteps - 1; j++)
            {
                var p = CalculateSplinePoint(j * deltaT, ControlPoints[i], ControlPoints[i + 1]);
                SplinePoints.Add(p);
            }
        }

        //Add last control point
        SplinePoints.Add(ControlPoints[ControlPoints.Count - 1].Point);

        //Regenerate mesh along spline if needed
        if (sm != null)
            sm.GenerateMesh();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (ControlPoints.Count < 1)
            return;

        if (transform.hasChanged)
        {
            //Regenerate spline with object position offset
            var offset = transform.position - _objectOrigin;
            if (Math.Abs(offset.x + offset.y + offset.z) > 1e-5)
            {
                for (int i = 0; i < ControlPoints.Count; ++i)
                {
                    var copy = new SplineControl(ControlPoints[i].Point, ControlPoints[i].Handle);
                    copy.Handle2 = ControlPoints[i].Handle2;
                    copy.MovePoints(offset);
                    ControlPoints[i] = copy;
                }
                _objectOrigin = transform.position;
            }

            RefreshSplinePoints();
        }
    }
#endif

    void OnDrawGizmos()
    {
        if (ControlPoints.Count < 1 || !RenderHandles)
            return;

        //Draw ControlPoints
        foreach (var cp in ControlPoints)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cp.Point, .4f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(cp.Handle, .2f);
            Gizmos.DrawWireSphere(cp.Handle2, .2f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(cp.Point, cp.Handle);
        }

        //Draw Connection Lines
        var prevP = ControlPoints[0].Point;
        foreach (Vector3 point in SplinePoints)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(point, .1f);
            Gizmos.color = Color.white;
            Gizmos.DrawLine(prevP, point);
            prevP = point;
        }
    }
}

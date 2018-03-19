using UnityEngine;
using System.Collections.Generic;

public enum eOrientationMode { NODE = 0, TANGENT }

[AddComponentMenu("Splines/Spline Controller")]
[RequireComponent(typeof(Script_SplineInterpolator))]
public class Script_SplineController : MonoBehaviour
{
    public GameObject SplineRoot;
    public float Duration = 10;
    public eOrientationMode OrientationMode = eOrientationMode.NODE;
    public eWrapMode WrapMode = eWrapMode.ONCE;
    public bool AutoStart = true;
    public bool AutoClose = true;
    public bool HideOnExecute = true;

    private bool _start;


    Script_SplineInterpolator mSplineInterp;
    Transform[] mTransforms;

    public bool StartFollow
    {
        get { return _start; }
        set
        {
            _start = value;
            mSplineInterp.Pauze = value;
        }
    }

    void Start()
    {
        if (SplineRoot == null)
        {
            Debug.Log("No spline root controller attached, used an empty gameobject");
            SplineRoot = new GameObject();
        }

        mSplineInterp = GetComponent(typeof(Script_SplineInterpolator)) as Script_SplineInterpolator;

        mTransforms = GetTransforms();

        if (HideOnExecute)
            DisableTransforms();

        if (AutoStart)
        {
            FollowSpline();
            _start = true;
        }

        FollowSpline();
    }

    public void StartAgain()
    {
        mSplineInterp = GetComponent(typeof(Script_SplineInterpolator)) as Script_SplineInterpolator;

        mTransforms = GetTransforms();

        if (HideOnExecute)
            DisableTransforms();

        if (AutoStart)
            FollowSpline();

        if (AutoStart)
        {
            _start = true;
        }
        FollowSpline();
    }

    void SetupSplineInterpolator(Script_SplineInterpolator interp, Transform[] trans)
    {
        interp.Reset();

        float step = (AutoClose) ? Duration / trans.Length : Duration / (trans.Length - 1);

        int c;
        for (c = 0; c < trans.Length; c++)
        {
            if (OrientationMode == eOrientationMode.NODE)
            {
                interp.AddPoint(trans[c].position, trans[c].rotation, step * c, new Vector2(0, 1));
            }
            else if (OrientationMode == eOrientationMode.TANGENT)
            {
                Quaternion rot;
                if (c != trans.Length - 1)
                    rot = Quaternion.LookRotation(trans[c + 1].position - trans[c].position, trans[c].up);
                else if (AutoClose)
                    rot = Quaternion.LookRotation(trans[0].position - trans[c].position, trans[c].up);
                else
                    rot = trans[c].rotation;

                interp.AddPoint(trans[c].position, rot, step * c, new Vector2(0, 1));
            }
        }

        if (AutoClose)
            interp.SetAutoCloseMode(step * c);
    }


    /// <summary>
    /// Returns children transforms, sorted by name.
    /// </summary>
    Transform[] GetTransforms()
    {
        if (SplineRoot != null)
        {
            List<Component> components = new List<Component>(SplineRoot.GetComponentsInChildren(typeof(Transform)));
            List<Transform> transforms = components.ConvertAll(c => (Transform)c);

            transforms.Remove(SplineRoot.transform);
            transforms.Sort(delegate (Transform a, Transform b)
            {
                return a.name.CompareTo(b.name);
            });

            return transforms.ToArray();
        }

        return null;
    }

    /// <summary>
    /// Disables the spline objects, we don't need them outside design-time.
    /// </summary>
    void DisableTransforms()
    {
        if (SplineRoot != null)
            SplineRoot.SetActiveRecursively(false);
    }

    /// <summary>
    /// Starts the interpolation
    /// </summary>
    void FollowSpline()
    {
        if (mTransforms.Length > 0)
        {
            SetupSplineInterpolator(mSplineInterp, mTransforms);
            mSplineInterp.StartInterpolation(null, true, WrapMode);
        }
    }

    public void PauseSplineInterpolation(bool v)
    {
        mSplineInterp.Pauze = v;
    }
}
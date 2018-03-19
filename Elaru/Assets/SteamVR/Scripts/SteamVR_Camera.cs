//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Adds SteamVR render support to existing camera objects
//
//=============================================================================

using UnityEngine;
using System.Collections;
using System.Reflection;
//using Valve.VR;
//using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class SteamVR_Camera : MonoBehaviour
{
    [SerializeField]
    private Transform _head;
    public Transform Head { get { return _head; } }
    public Transform Offset { get { return _head; } } // legacy
    public Transform Origin { get { return _head.parent; } }

    public new Camera Camera { get; private set; }

    [SerializeField]
    private Transform _ears;
    public Transform Ears { get { return _ears; } }

    public Ray GetRay()
    {
        return new Ray(_head.position, _head.forward);
    }

    public bool wireframe = false;

    static public float SceneResolutionScale
    {
        get { return UnityEngine.XR.XRSettings.eyeTextureResolutionScale; }
        set { UnityEngine.XR.XRSettings.eyeTextureResolutionScale = value; }
    }

    #region Enable / Disable

    void OnDisable()
    {
        SteamVR_Render.Remove(this);
    }

    void OnEnable()
    {
        // Bail if no hmd is connected
        var vr = SteamVR.instance;
        if (vr == null)
        {
            if (Head != null)
            {
                Head.GetComponent<SteamVR_TrackedObject>().enabled = false;
            }

            enabled = false;
            return;
        }

        // Convert camera rig for native OpenVR integration.
        var t = transform;
        if (Head != t)
        {
            Expand();

            t.parent = Origin;

            while (Head.childCount > 0)
                Head.GetChild(0).SetParent(t);

            // Keep the head around, but parent to the camera now since it moves with the hmd
            // but existing content may still have references to this object.
            Head.parent = t;
            Head.localPosition = Vector3.zero;
            Head.localRotation = Quaternion.identity;
            Head.localScale = Vector3.one;
            Head.gameObject.SetActive(false);

            _head = t;
        }

        if (Ears == null)
        {
            var e = transform.GetComponentInChildren<SteamVR_Ears>();
            if (e != null)
                _ears = e.transform;
        }

        if (Ears != null)
            Ears.GetComponent<SteamVR_Ears>().vrcam = this;

        SteamVR_Render.Add(this);
    }

    #endregion

    #region Functionality to ensure SteamVR_Camera component is always the last component on an object

    void Awake()
    {
        Camera = GetComponent<Camera>(); // cached to avoid runtime lookup
        ForceLast();
    }

    static Hashtable values;

    public void ForceLast()
    {
        if (values != null)
        {
            // Restore values on new instance
            foreach (DictionaryEntry entry in values)
            {
                var f = entry.Key as FieldInfo;
                f.SetValue(this, entry.Value);
            }
            values = null;
        }
        else
        {
            // Make sure it's the last component
            var components = GetComponents<Component>();

            // But first make sure there aren't any other SteamVR_Cameras on this object.
            for (int i = 0; i < components.Length; i++)
            {
                var c = components[i] as SteamVR_Camera;
                if (c != null && c != this)
                {
                    DestroyImmediate(c);
                }
            }

            components = GetComponents<Component>();

            if (this != components[components.Length - 1])
            {
                // Store off values to be restored on new instance
                values = new Hashtable();
                var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                foreach (var f in fields)
                    if (f.IsPublic || f.IsDefined(typeof(SerializeField), true))
                        values[f] = f.GetValue(this);

                var go = gameObject;
                DestroyImmediate(this);
                go.AddComponent<SteamVR_Camera>().ForceLast();
            }
        }
    }

    #endregion

    #region Expand / Collapse object hierarchy

#if UNITY_EDITOR
    public bool IsExpanded { get { return Head != null && transform.parent == Head; } }
#endif
    const string eyeSuffix = " (eye)";
    const string earsSuffix = " (ears)";
    const string headSuffix = " (head)";
    const string originSuffix = " (origin)";
    public string BaseName { get { return name.EndsWith(eyeSuffix) ? name.Substring(0, name.Length - eyeSuffix.Length) : name; } }

    // Object hierarchy creation to make it easy to parent other objects appropriately,
    // otherwise this gets called on demand at runtime. Remaining initialization is
    // performed at startup, once the hmd has been identified.
    public void Expand()
    {
        var _origin = transform.parent;
        if (_origin == null)
        {
            _origin = new GameObject(name + originSuffix).transform;
            _origin.localPosition = transform.localPosition;
            _origin.localRotation = transform.localRotation;
            _origin.localScale = transform.localScale;
        }

        if (Head == null)
        {
            _head = new GameObject(name + headSuffix, typeof(SteamVR_TrackedObject)).transform;
            Head.parent = _origin;
            Head.position = transform.position;
            Head.rotation = transform.rotation;
            Head.localScale = Vector3.one;
            Head.tag = tag;
        }

        if (transform.parent != Head)
        {
            transform.parent = Head;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            while (transform.childCount > 0)
                transform.GetChild(0).parent = Head;

            var guiLayer = GetComponent<GUILayer>();
            if (guiLayer != null)
            {
                DestroyImmediate(guiLayer);
                Head.gameObject.AddComponent<GUILayer>();
            }

            var audioListener = GetComponent<AudioListener>();
            if (audioListener != null)
            {
                DestroyImmediate(audioListener);
                _ears = new GameObject(name + earsSuffix, typeof(SteamVR_Ears)).transform;
                Ears.parent = _head;
                Ears.localPosition = Vector3.zero;
                Ears.localRotation = Quaternion.identity;
                Ears.localScale = Vector3.one;
            }
        }

        if (!name.EndsWith(eyeSuffix))
            name += eyeSuffix;
    }

    public void Collapse()
    {
        transform.parent = null;

        // Move children and components from head back to camera.
        while (Head.childCount > 0)
            Head.GetChild(0).parent = transform;

        var guiLayer = Head.GetComponent<GUILayer>();
        if (guiLayer != null)
        {
            DestroyImmediate(guiLayer);
            gameObject.AddComponent<GUILayer>();
        }

        if (Ears != null)
        {
            while (Ears.childCount > 0)
                Ears.GetChild(0).parent = transform;

            DestroyImmediate(Ears.gameObject);
            _ears = null;

            gameObject.AddComponent(typeof(AudioListener));
        }

        if (Origin != null)
        {
            // If we created the origin originally, destroy it now.
            if (Origin.name.EndsWith(originSuffix))
            {
                // Reparent any children so we don't accidentally delete them.
                var _origin = Origin;
                while (_origin.childCount > 0)
                    _origin.GetChild(0).parent = _origin.parent;

                DestroyImmediate(_origin.gameObject);
            }
            else
            {
                transform.parent = Origin;
            }
        }

        DestroyImmediate(Head.gameObject);
        _head = null;

        if (name.EndsWith(eyeSuffix))
            name = name.Substring(0, name.Length - eyeSuffix.Length);
    }

    #endregion
}


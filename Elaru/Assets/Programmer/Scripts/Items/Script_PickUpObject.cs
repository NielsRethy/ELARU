using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Script_PickUpEffects))]
public class Script_PickUpObject : MonoBehaviour
{
    [Header("PickUp Settings")]
    [HideInInspector]
    public bool BeingHeld = false;
    [HideInInspector]
    public HandSide ControlHandSide = HandSide.None;

    //Object state booleans
    [SerializeField]
    private bool _holdItem = false;
    public bool HoldItem { get { return _holdItem; } private set { _holdItem = value; } }

    [HideInInspector]
    public Script_PickUpEffects ScriptPickUpEffects = null;

    public bool SnapPositionToHand = true;
    public bool SnapRotationToHand = true;

    //Actions to execute when object is grabbed or released by player (Invoked from Script_PickUp)
    public Action<GameObject> OnRelease;
    public Action<GameObject> OnGrab;

    //Component caching vars
    private Rigidbody _rigidbody = null;
    public Rigidbody Rigidbody { get { return _rigidbody ?? (_rigidbody = GetComponent<Rigidbody>()); } }

    [Header("Collision")]
    public bool NoClipWhilstHeld = false;
    [SerializeField]
    private Collider[] _noClipColliders = null;
    public Collider[] NoClipColliders { get { return _noClipColliders; } }

    private const string TagCamera = "MainCamera";
    private const string TagPlayer = "Player";

    [Header("Physics")]
    [SerializeField]
    private bool _startKinematic = false;
    [SerializeField]
    private bool _toggleKinematicWhilstHeld = false;
    public bool ToggleKinematicWhilstHeld { get { return _toggleKinematicWhilstHeld; } }

    private Collider _thisCollider = null;
    
    public bool ImpossibleToDrop = false;

    private void Start()
    {
        BeingHeld = false;

        //Cache components
        _rigidbody = GetComponent<Rigidbody>();
        if (_rigidbody == null)
            Debug.LogWarning(name + " has no Rigidbody!");

        if (_noClipColliders == null)
            _noClipColliders = new Collider[0];

        //Safety check emmisive script
        if (ScriptPickUpEffects == null)
            ScriptPickUpEffects = GetComponent<Script_PickUpEffects>();
        if (ScriptPickUpEffects == null)
            Debug.LogWarning(name + " has no PickUpEffects script!");

        //Safety check Tag
        if (!(CompareTag("PickUp") || CompareTag("Gun") || CompareTag("Sword")))
            Debug.LogWarning(name + " has the pick up script but isn't tagged as pick up!");

        //Physics
        _rigidbody.isKinematic = _startKinematic;

        _thisCollider = GetComponent<Collider>();
        if (_thisCollider == null)
            Debug.LogWarning("Failed to get collider on pick up object");
    }

    public void SwitchColliderTriggerState(bool newState)
    {
        // Turn off kinematic if necessary
        if (_toggleKinematicWhilstHeld && Rigidbody != null)
            Rigidbody.isKinematic = newState;

        ////Update trigger if necessary
        if (_noClipColliders != null)
            foreach (Collider noClip in _noClipColliders)
                if (noClip != null)
                    noClip.isTrigger = newState;

        if (ScriptPickUpEffects != null)
            ScriptPickUpEffects.BeingHeld = newState;
    }

    public void SetCollidersActive(bool state)
    {
        if (_noClipColliders != null)
            foreach (Collider noClip in _noClipColliders)
                if (noClip != null)
                    noClip.enabled = state;
    }

    private void OnCollisionEnter(Collision other)
    {
        //Ignore collisions with player or cameracollider
        if (other.collider.CompareTag(TagPlayer) || other.collider.CompareTag(TagCamera))
            if (other != null && other.collider != null)
                Physics.IgnoreCollision(other.collider, _thisCollider);
    }
}

using System.Linq;
using UnityEngine;

public enum HandSide
{
    None,
    Left,
    Right
}

public class Script_PickUp : MonoBehaviour
{
    private HandSide _hand;
    public HandSide Hand { get { return _hand; } }

    private SteamVR_TrackedObject _trackedObject = null;
    private SteamVR_Controller.Device _controllerDevice = null;
    private SteamVR_Controller.Device ControllerDevice { get { return _controllerDevice = _controllerDevice ?? _scriptLoco.GetController(Hand); } }

    private SteamVR_RenderModel[] _models = null;

    //Object tracking vars
    public GameObject SelectedObject = null;
    public GameObject HeldObject = null;
    public bool IsHoldingObject { get { return HeldObject != null; } }
    private Script_PickUpObject _heldPickUpScript = null;

    //Throw physics vars
    private bool _throwing = false;
    private Script_PickUpObject _throwPickUpScript = null;
    private FixedJoint _fixedJoint = null;

    private float _maxHoldDistance = .1f;

    private bool _usingVR;

    private Script_LocomotionBase _scriptLoco = null;
    private Script_PlayerCollisionCheck _scriptCollision = null;

    private SkinnedMeshRenderer _skin = null;

    public bool ControllersAreVisible { get; private set; }
    public bool HandsAreVisible { get; private set; }

    private Vector3 _holdOffset = Vector3.zero;
    private Script_TactileFeedback _scriptFeedback = null;
    private const ushort VibrationDuration = 1000;
    private const string TagPickUp = "PickUp";
    private const string TagGun = "Gun";
    private const string TagSword = "Sword";

    private void Start()
    {
        _usingVR = SteamVR.active;

        //Cache components
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptFeedback = Script_TactileFeedback.Instance;

        _scriptCollision = _scriptLoco.ScriptCollisionCheck;
        _trackedObject = GetComponent<SteamVR_TrackedObject>();

        _models = GetComponentsInChildren<SteamVR_RenderModel>();
        var animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.Log("Failed to get hand animator on children!");

        _skin = animator.GetComponentInChildren<SkinnedMeshRenderer>();

        _fixedJoint = GetComponent<FixedJoint>();

        _fixedJoint.connectedBody = null;

        _hand = _scriptLoco.GetHandSideFromObject(gameObject);

        HideHands(false);
        // Hide controllers on start
        ControllersAreVisible = true;
        HideControllers(true);

        var thisCollider = GetComponent<Collider>();
        var playerColliders = _scriptLoco.RootRigidbody.GetComponentsInChildren<Collider>().Where(coll => coll.CompareTag("Player")).ToArray();
        // Ignore all collisions with the player itself
        foreach (var coll in playerColliders)
            Physics.IgnoreCollision(coll, thisCollider, true);
    }

    public void HideControllers(bool state)
    {
        if (ControllersAreVisible == !state)
            return;

        if (_models != null && _models.Length > 0)
            foreach (SteamVR_RenderModel model in _models)
                model.gameObject.SetActive(!state);

        ControllersAreVisible = !state;
    }

    public void HideHands(bool state)
    {
        if (HandsAreVisible == !state)
            return;

        if (_skin != null)
            _skin.enabled = !state;

        HandsAreVisible = !state;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (SelectedObject == other || HeldObject == other)
            return;

        //Select object when entering with empty hand
        if (other.CompareTag(TagPickUp) || other.CompareTag(TagSword) || other.CompareTag(TagGun))
            Select(other);
    }

    public void Select(Collider other)
    {
        // Check if the hand is already selecting something
        if (SelectedObject != null)
        {
            // Deselect this pick up
            SelectedObject.GetComponent<Script_PickUpEffects>().Hover = false;
            Deselect();
        }

        SelectedObject = other.gameObject;

        // Haptic feedback
        if (_usingVR)
            _scriptFeedback.SendShortVib(VibrationDuration, Hand);

        //Pick ups don't push player away
        if (other.CompareTag("Player"))
            Physics.IgnoreCollision(other, GetComponent<Collider>(), true);
    }

    public void Deselect()
    {
        SelectedObject = null;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == SelectedObject)
            Deselect();
    }

    private void FixedUpdate()
    {
        // Throw the object using the controller velocity
        if (!_throwing)
            return;

        if (_throwPickUpScript == null)
            return;

        //Re-enable no clip colliders
        if (_throwPickUpScript.NoClipWhilstHeld)
            _throwPickUpScript.SetCollidersActive(true);

        //Update throw object velocity
        Transform origin = transform;
        if (_trackedObject != null)
        {
            if (_trackedObject.origin != null)
                origin = _trackedObject.origin;
            else origin = _trackedObject.transform.parent;
        }

        if (_usingVR)
        {
            _throwPickUpScript.Rigidbody.velocity = origin.TransformVector(ControllerDevice.velocity);
            _throwPickUpScript.Rigidbody.angularVelocity = origin.TransformVector(ControllerDevice.angularVelocity);
        }
        else
        {
            _throwPickUpScript.Rigidbody.velocity = origin.TransformVector(GetComponent<Rigidbody>().velocity);
            _throwPickUpScript.Rigidbody.angularVelocity = origin.TransformVector(GetComponent<Rigidbody>().angularVelocity);
        }

        _throwing = false;
    }

    private void VRUpdate()
    {
        if (_scriptLoco.GetController(Hand) == null && _usingVR)
        {
            Debug.LogWarning("Controller not initialized!");
            return;
        }

        //Check for pick up / drop
        if (SelectedObject == null && HeldObject == null)
            return;

        // Check for grip press
        if (_scriptLoco.GetPressDown(_scriptLoco.GripButton, Hand))
        {
            //Pick up the selected object
            if (_heldPickUpScript == null)
                SetPickUp();

            PickUp();
        }

        if (_heldPickUpScript == null || HeldObject == null)
            return;

        // Check for grip release
        if (_heldPickUpScript != null && !_heldPickUpScript.HoldItem && _scriptLoco.GetPressUp(_scriptLoco.GripButton, Hand))
            Drop();

        // Do a distance check to see if the object is being pushed away from the controller (eg.: against a wall)
        if (!_scriptLoco.ScriptLocomotionDash.Dashing && _heldPickUpScript != null && !_heldPickUpScript.NoClipWhilstHeld)
        {
            var distance = _heldPickUpScript.SnapPositionToHand ?
                (transform.position - HeldObject.transform.position).sqrMagnitude :
                (HeldObject.transform.position - transform.position).sqrMagnitude - _holdOffset.sqrMagnitude;

            if (distance > _maxHoldDistance * _maxHoldDistance)
                Drop();
        }

        //Move held object with player on dash
        if (_scriptLoco.ScriptLocomotionDash.Dashing && HeldObject != null)
            HeldObject.transform.position = transform.position;
    }

    private void PickUp()
    {
        if (_heldPickUpScript == null || _heldPickUpScript.Rigidbody == null)
            return;

        if (_heldPickUpScript != null)
        {
            //Drop the held item if it's a HoldItem
            if (_heldPickUpScript.BeingHeld && _heldPickUpScript.HoldItem && _heldPickUpScript.ControlHandSide == Hand)
            {
                Drop();
                return;
            }
            //Being held by other hand -> Drop from that hand
            else if (_heldPickUpScript.BeingHeld && _heldPickUpScript.ControlHandSide != Hand)
                _scriptLoco.GetPickUpFromHand(_heldPickUpScript.ControlHandSide).Drop();
        }

        //Hold selected object
        HeldObject = SelectedObject;

        //Call OnGrab actions for possible additional execution
        if (_heldPickUpScript.OnGrab != null)
            _heldPickUpScript.OnGrab.Invoke(HeldObject);

        //Update held object states
        _heldPickUpScript.ControlHandSide = Hand;

        //Set the collider of the held object to a trigger
        if (_heldPickUpScript.NoClipWhilstHeld)
            _heldPickUpScript.SwitchColliderTriggerState(true);

        //Toggle the kinematic state
        if (_heldPickUpScript.ToggleKinematicWhilstHeld)
            _heldPickUpScript.Rigidbody.isKinematic = !_heldPickUpScript.Rigidbody.isKinematic;

        _heldPickUpScript.BeingHeld = true;
        _heldPickUpScript.Rigidbody.useGravity = false;
        Deselect();

        // Set position and rotation of picked obj
        if (_heldPickUpScript.SnapPositionToHand)
            HeldObject.transform.position = transform.position;
        // Set position the object was picked up on
        else
            _holdOffset = transform.position - HeldObject.transform.position;
        // Set the rotation
        if (_heldPickUpScript.SnapRotationToHand)
            HeldObject.transform.rotation = Quaternion.LookRotation(transform.right, transform.up);

        // Connect the rigidbody to your hand
        _fixedJoint.connectedBody = _heldPickUpScript.Rigidbody;

        // Hide the controller
        HideHands(true);
    }

    private void FPSUpdate()
    {
        if (_heldPickUpScript != null)
        {
            // Check for grip press
            if (Input.GetMouseButtonDown(1))
            {
#if DEBUG
                Debug.Log("gripButton down");
#endif

                //drop held item
                if (_heldPickUpScript.HoldItem && _heldPickUpScript.BeingHeld)
                {
                    Drop();
                    return;
                }
                //set item as being held
                HeldObject = SelectedObject;
                if (!HeldObject) return;
                _heldPickUpScript.BeingHeld = true;
                if (_heldPickUpScript.OnGrab != null)
                    _heldPickUpScript.OnGrab.Invoke(HeldObject);
#if DEBUG
                Debug.Log("Deselect 2");
#endif
                Deselect();

                // Set position and rotation of picked obj
                if (_heldPickUpScript.SnapPositionToHand)
                    HeldObject.transform.position = transform.position;

                if (_heldPickUpScript.SnapRotationToHand)
                    HeldObject.transform.rotation = Quaternion.LookRotation(transform.right, transform.up);

                _fixedJoint.connectedBody = _heldPickUpScript.Rigidbody;
            }

            if (Input.GetMouseButtonUp(1))
            {
                if (!_heldPickUpScript.HoldItem)
                    Drop();
            }
        }
    }
    private void Update()
    {
        if (_usingVR)
            VRUpdate();
        else
            FPSUpdate();
    }

    public void SetPickUp(Script_PickUpObject pickUp = null)
    {
        if (pickUp == null)
        {
            _heldPickUpScript = SelectedObject.GetComponent<Script_PickUpObject>();
#if DEBUG
            if (_heldPickUpScript == null)
            {
                Debug.Log("No Pick up object script attached to selected object");
                return;
            }
#endif
        }
        else
        {
            SelectedObject = pickUp.gameObject;
            _heldPickUpScript = pickUp;
        }

        // Update pick up effect
        SetPickUpEffect(true);

        // Turn off collision with body
        foreach (var coll in _heldPickUpScript.NoClipColliders)
            if (coll != null && _scriptLoco.PlayerCollider != null)
                Physics.IgnoreCollision(_scriptLoco.PlayerCollider, coll);

        // Turn off collision with face
        _scriptCollision.IgnoreCollision(true, _heldPickUpScript.NoClipColliders);
    }

    public void Drop()
    {

        if (_heldPickUpScript != null && _heldPickUpScript.ImpossibleToDrop)
            return;
        //Debug.Log("Dropped item";
        Deselect();
        if (_heldPickUpScript == null)
            return;

        // Show controller
        HideHands(false);

        // Update body
        _heldPickUpScript.SwitchColliderTriggerState(false);
        _heldPickUpScript.BeingHeld = false;
        if (_heldPickUpScript.OnRelease != null)
            _heldPickUpScript.OnRelease.Invoke(HeldObject);
        _heldPickUpScript.ControlHandSide = HandSide.None;
        _heldPickUpScript.Rigidbody.useGravity = true;
        _fixedJoint.connectedBody = null;

        // Update pick up effect
        SetPickUpEffect(false);

        // Allow collision
        _scriptCollision.IgnoreCollision(false, _heldPickUpScript.NoClipColliders);

        //Prepare for throw of object
        _throwPickUpScript = _heldPickUpScript;
        _throwing = true;

        _heldPickUpScript = null;

        if (HeldObject != null)
        {
            var coll = HeldObject.GetComponent<Collider>();
            if (coll == null)
                return;

            // Do the on trigger enter again
            coll.enabled = false;
            coll.enabled = true;
        }

        HeldObject = null;
    }

    private void SetPickUpEffect(bool state)
    {
        if (_heldPickUpScript.ScriptPickUpEffects != null)
        {
            _heldPickUpScript.ScriptPickUpEffects.Pressed = state;
            _heldPickUpScript.ScriptPickUpEffects.BeingHeld = state;
        }
    }
}

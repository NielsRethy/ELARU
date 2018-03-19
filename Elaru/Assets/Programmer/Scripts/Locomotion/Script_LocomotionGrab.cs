using UnityEngine;
using Valve.VR;

/// <summary>
/// Script for handling the grabbing/climbing locomotion
/// </summary>
public class Script_LocomotionGrab : MonoBehaviour
{
    private Script_LocomotionBase _scriptLoco = null;

    //Left hand variables
    private Script_LocomotionGrabState _lGrabState = null;
    private Script_LocomotionGrabState _leftGrabState //Property as controllers can be enabled/disabled during gameplay
    {
        get
        {
            return _lGrabState ?? (_lGrabState = _scriptLoco.LeftControllerTrObj != null ?
                _scriptLoco.LeftControllerTrObj.GetComponent<Script_LocomotionGrabState>() : null);
        }
    }

    private Script_PickUp _lPickUp = null;
    private Script_PickUp _leftPickUp
    {
        get
        {
            return _lPickUp ?? (_lPickUp = _scriptLoco.LeftControllerTrObj != null ?
                _scriptLoco.LeftControllerTrObj.GetComponent<Script_PickUp>() : null);
        }
    }

    //Right hand variables
    private Script_LocomotionGrabState _rGrabState = null;
    private Script_LocomotionGrabState _rightGrabState
    {
        get
        {
            return _rGrabState ?? (_rGrabState = _scriptLoco.RightControllerTrObj != null ?
                _scriptLoco.RightControllerTrObj.GetComponent<Script_LocomotionGrabState>() : null);
        }
    }

    private Script_PickUp _rPickUp = null;
    private Script_PickUp _rightPickUp
    {
        get
        {
            return _rPickUp ?? (_rPickUp = _scriptLoco.RightControllerTrObj != null ?
                _scriptLoco.RightControllerTrObj.GetComponent<Script_PickUp>() : null);
        }
    }

    public bool Grabbing { get { return _grabbingWithLeft || _grabbingWithRight; } }

    private const EVRButtonId _grip = EVRButtonId.k_EButton_Grip;
    private Rigidbody _rigidBody = null;
    private GameObject _camRig = null;

    private Vector3 _previousPosLeft = Vector3.zero;
    private Vector3 _previousPosRight = Vector3.zero;

    private bool _grabbingWithLeft = false;
    private bool _grabbingWithRight = false;

    private Vector3 _initColliderSize = Vector3.zero;
    private BoxCollider _collider;
    private const float ColliderSizeMul = 1.5f;

    private void Awake()
    {
        //Cache components
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptLoco.ScriptLocomotionGrab = this;
        _rigidBody = _scriptLoco.RootRigidbody;
        _camRig = _scriptLoco.CameraRig;
    }

    private void Update()
    {
        if (_scriptLoco.PlayerCollider == null || (_leftGrabState == null && _rightGrabState == null))
            return;

        // Prevent grabbing from staying true after collision was lost or if the grip was released
        if (_grabbingWithLeft && (!_leftGrabState.CanGrab || !_scriptLoco.GetPress(_grip, HandSide.Left)))
            DisableGrab(HandSide.Left, _scriptLoco.GetTrackedObject(HandSide.Left), ref _previousPosLeft);

        if (_grabbingWithRight && (!_rightGrabState.CanGrab || !_scriptLoco.GetPress(_grip, HandSide.Right)))
            DisableGrab(HandSide.Right, _scriptLoco.GetTrackedObject(HandSide.Right), ref _previousPosRight);

        if (_leftGrabState != null)
        {
            // Grab with left controller
            if (_leftGrabState.CanGrab)
                GrabWith(HandSide.Left, _scriptLoco.GetTrackedObject(HandSide.Left),
                    ref _previousPosLeft, ref _grabbingWithLeft);

            // Prevent pressed from staying true whilst buttons have been let go
            if (_leftGrabState.PickUpEffects != null &&
                !_leftGrabState.PickUpEffects.Hover && _leftGrabState.PickUpEffects.Pressed
                && !_scriptLoco.GetPress(_grip, HandSide.Left))
                _leftGrabState.PickUpEffects.Pressed = false;

            // Prevent right controller from overwriting the left grip
            if (_grabbingWithRight && _leftGrabState.CanGrab && _grabbingWithLeft)
                return;
        }

        if (_rightGrabState != null)
        {
            // Grab with right controller
            if (_rightGrabState.CanGrab)
                GrabWith(HandSide.Right, _scriptLoco.GetTrackedObject(HandSide.Right),
                    ref _previousPosRight, ref _grabbingWithRight);

            // Prevent pressed from staying true whilst buttons have been let go
            if (_rightGrabState.PickUpEffects != null &&
                !_rightGrabState.PickUpEffects.Hover && _rightGrabState.PickUpEffects.Pressed
                && !_scriptLoco.GetPress(_grip, HandSide.Right))
                _rightGrabState.PickUpEffects.Pressed = false;
        }

        // Delay gravity being activated
        // Giving the player a chance to correct accidents
        if (_grabbingWithLeft || _grabbingWithRight || _rigidBody.useGravity)
            return;

        // Drop the player by turning gravity back on
        _rigidBody.useGravity = true;
    }

    private void DisableGrab(HandSide hand, SteamVR_TrackedObject obj, ref Vector3 prevPos)
    {
        ToggleState(true, hand);
        // Allow the player to lunge/throw themselves
        _rigidBody.velocity = (prevPos - obj.transform.position) / Time.deltaTime;
    }

    public void ForceStopGrab()
    {
        _grabbingWithLeft = false;
        _grabbingWithRight = false;
    }

    private void ToggleState(bool state, HandSide hand)
    {
        if (!state)
            _rigidBody.useGravity = false;

        switch (hand)
        {
            case HandSide.None:
                Debug.LogWarning("Hand was none!", gameObject);
                break;

            case HandSide.Left:

                _grabbingWithLeft = !state;

                if (_grabbingWithLeft)
                    _grabbingWithRight = false;

                break;

            case HandSide.Right:

                _grabbingWithRight = !state;

                if (_grabbingWithRight)
                    _grabbingWithLeft = false;

                break;
        }

        // Update the hands according to the grab state
        if (_leftPickUp != null && _grabbingWithLeft == _leftPickUp.HandsAreVisible)
            _leftPickUp.HideHands(!_grabbingWithLeft);

        if (_rightPickUp != null && _grabbingWithRight == _rightPickUp.HandsAreVisible)
            _rightPickUp.HideHands(!_grabbingWithRight);
    }

    /// <summary>
    /// Shorthand for running the identical grab behaviour for both controllers
    /// </summary>
    /// <param name="handSide">Hand side identifying the controller</param>
    /// <param name="obj">The tracked object script attached to the controller</param>
    /// <param name="prevPos">Reference to the previous position</param>
    /// <param name="grabState">Reference to the locally stored grabbing state</param>
    private void GrabWith(HandSide handSide, SteamVR_TrackedObject obj, ref Vector3 prevPos, ref bool grabState)
    {
        // Allow the player to catch themselves whilst falling
        // by holding down the grip in terror
        if (_rigidBody.velocity.y < -1f)
        {
            prevPos = obj.transform.position;
            if (_scriptLoco.GetPress(_grip, handSide))
                ToggleState(false, handSide);
        }

        // Disable gravity
        if (_scriptLoco.GetPressDown(_grip, handSide))
        {
            ToggleState(false, handSide);

            var handGrabState = handSide == HandSide.Left ? _leftGrabState : _rightGrabState;
            // Set pick up effect state (grab state is updated in ToggleState)
            if (handGrabState != null && handGrabState.PickUpEffects != null)
                handGrabState.PickUpEffects.Pressed = true;

            // Reset previous collider size
            if (_collider != null)
                _collider.size = _initColliderSize;

            // Scale collider (more forgiving collision loss)
            _collider = handGrabState.CurrentSelection.GetComponent<BoxCollider>();
            if (_collider != null)
            {
                _initColliderSize = _collider.size;
                _collider.size *= ColliderSizeMul;
            }
        }

        if (grabState)
        {
            _camRig.transform.position += prevPos - obj.transform.position;

            // Allow the player to slow their fall
            if (_rigidBody.velocity.y < 0.001f)
                _rigidBody.velocity /= 2f;
            else
                _rigidBody.velocity = Vector3.zero;
        }

        prevPos = obj.transform.position;
    }
}

using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// Script containing all the references to variables neccesary for locomotion and controller interactability
/// </summary>
public class Script_LocomotionBase : Script_Singleton<Script_LocomotionBase>
{
    #region Camera rig components
    private GameObject _cameraRig = null;
    /// <summary>
    /// Reference to the SteamVR camera rig in the current scene
    /// </summary>
    public GameObject CameraRig
    {
        get
        {
            //Find camera rig if not yet set
            if (_cameraRig == null)
            {
                _cameraRig = FindObjectOfType<SteamVR_PlayArea>().gameObject;
#if DEBUG
                Debug.Log("Camera rig reference in " + name + " set to: " + CameraRig.name, CameraRig);

                if (_cameraRig == null)
                    Debug.LogError("No camera rig found in the current scene via SteamVR_PlayArea!");
#endif
            }
            return _cameraRig;
        }
    }

    private Rigidbody _rootRigidbody = null;
    /// <summary>
    /// Reference to the rigidbody on the SteamVR camera rig in the current scene
    /// </summary>
    public Rigidbody RootRigidbody
    {
        get
        {
            //Find root rigidbody if not yet set
            if (_rootRigidbody == null)
            {
                _rootRigidbody = CameraRig.GetComponent<Rigidbody>();
#if DEBUG
                if (_rootRigidbody == null)
                    Debug.LogError("Failed to get rigidbody on camera rig!");
#endif
            }
            return _rootRigidbody;
        }
    }

    private Collider _collider = null;
    /// <summary>
    /// Reference to the collider keeping the player from falling through the ground (using the player collider follow script)
    /// </summary>
    public Collider PlayerCollider
    {
        get
        {
            //Find collider if not yet set
            if (_collider == null)
            {
                // Fetch every object in the rig hierarchy tagged with "Player"
                var playerTaggedObjects = CameraRig.GetComponentsInChildren<Transform>().Where(obj => obj.gameObject.CompareTag("Player")).ToArray();
                _collider = playerTaggedObjects.First(obj => obj.GetComponent<Script_PlayerColliderFollow>() != null).GetComponent<Collider>();
#if DEBUG
                if (_collider == null)
                    Debug.LogWarning("Failed to get collider on the camera rig via Script_PlayerColliderFollow!");
#endif
            }
            return _collider;
        }
    }

    private SteamVR_ControllerManager _controllerManager = null;
    /// <summary>
    /// Reference to the SteamVR controller manager on the camera rig (contains references to the left and right controller if needed)
    /// </summary>
    public SteamVR_ControllerManager ControllerManager
    {
        get
        {
            //Find controller manager if not yet set
            if (_controllerManager == null)
            {
                _controllerManager = CameraRig.GetComponent<SteamVR_ControllerManager>();
#if DEBUG
                if (_controllerManager == null)
                    Debug.LogError("Failed to get controller manager on camera rig!");
#endif
            }
            return _controllerManager;
        }
    }

    /// <summary>
    /// Calculate and return the offset of the player compared to the camera rig
    /// </summary>
    public Vector3 HeadPositionOnGround { get { return new Vector3(SteamVR_Render.Top().Head.position.x, CameraRig.transform.position.y, SteamVR_Render.Top().Head.position.z); } }

    /// <summary>
    /// Reference to the collision check script in the current scene
    /// </summary>
    private Script_PlayerCollisionCheck _scriptCollisionCheck = null;
    public Script_PlayerCollisionCheck ScriptCollisionCheck
    {
        get
        {
            // Attempt to fetch the collision script if it hasn't been set yet
            return _scriptCollisionCheck = _scriptCollisionCheck ?? CameraRig.GetComponentInChildren<Script_PlayerCollisionCheck>();
        }

        set { _scriptCollisionCheck = value; }
    }
    #endregion

    #region Locomotion components
    private Script_Locomotion_TeleDash _scriptTeleDash = null;
    /// <summary>
    /// Reference to the teleportation dash script in the current scene
    /// </summary>
    public Script_Locomotion_TeleDash ScriptLocomotionDash
    {
        get
        {
            // Attempt to fetch the teleport dash script if it hasn't been set yet
            if (_scriptTeleDash == null)
            {
                var scripts = FindObjectsOfType<Script_Locomotion_TeleDash>();
#if DEBUG
                if (scripts.Length > 1)
                    Debug.LogError("There are " + scripts.Length + " LocomotionTeleDash scripts in the scene!");
#endif
                _scriptTeleDash = scripts.FirstOrDefault();
            }

            return _scriptTeleDash;
        }

        set { _scriptTeleDash = value; }
    }

    private Script_LocomotionGrab _scriptLocoGrab = null;
    /// <summary>
    /// Reference to the grabbing(/climbing) script in the current scene
    /// </summary>
    public Script_LocomotionGrab ScriptLocomotionGrab
    {
        get
        {
            // Attempt to fetch the grab script if it hasn't been set yet
            if (_scriptLocoGrab == null)
            {
                var scripts = FindObjectsOfType<Script_LocomotionGrab>();
#if DEBUG
                if (scripts.Length > 1)
                    Debug.LogError("There are " + scripts.Length + " LocomotionGrab scripts in the scene!");
#endif
                _scriptLocoGrab = scripts.FirstOrDefault();
            }

            return _scriptLocoGrab;
        }

        set { _scriptLocoGrab = value; }
    }

    /// <summary>
    /// Shorthand for disabling or enabling the locomotion and collision fading behaviour (for preventing physics or fading glitches)
    /// </summary>
    /// <param name="state"></param>
    public void ToggleLocomotionAndFading(bool state)
    {
        ScriptLocomotionDash.ForceStopDash();
        ScriptLocomotionDash.enabled = state;

        ScriptCollisionCheck.enabled = state;

        ScriptLocomotionGrab.ForceStopGrab();
        ScriptLocomotionGrab.enabled = state;
    }
    #endregion

    #region Controller components
    /// <summary>
    /// Shorthand for fetching the controller object via the hand side
    /// </summary>
    /// <param name="handSide">Hand side identifying the controller</param>
    /// <returns></returns>
    public SteamVR_TrackedObject GetTrackedObject(HandSide handSide)
    {
        switch (handSide)
        {
            case HandSide.None:
                return null;
            case HandSide.Left:
                return LeftControllerTrObj;
            case HandSide.Right:
                return RightControllerTrObj;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Shorthand for fetching the controller device via the hand side
    /// </summary>
    /// <param name="handSide">Hand side identifying the controller</param>
    /// <returns></returns>
    public SteamVR_Controller.Device GetController(HandSide handSide)
    {
        switch (handSide)
        {
            case HandSide.None:
                return null;
            case HandSide.Left:
                return LeftController;
            case HandSide.Right:
                return RightController;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private SteamVR_TrackedObject _leftCtrlObj = null;
    /// <summary>
    /// Reference to the tracked object script of the left controller (get the transform via this)
    /// </summary>
    public SteamVR_TrackedObject LeftControllerTrObj
    {
        get
        {
            if (ControllerManager == null || ControllerManager.left == null || !ControllerManager.left.activeSelf)
                return null;

            if (_leftCtrlObj == null)
            {
                _leftCtrlObj = ControllerManager.left.GetComponent<SteamVR_TrackedObject>();
#if DEBUG
                if (_leftCtrlObj == null)
                    Debug.LogError("Failed to get left controller tracked object from controller manager!");
#endif
            }

            return _leftCtrlObj;
        }
    }

    /// <summary>
    /// Reference to the SteamVR left controller script (get button input with this)
    /// </summary>
    public SteamVR_Controller.Device LeftController
    {
        get
        {
            return LeftControllerTrObj != null ?
                (LeftControllerTrObj.gameObject.activeSelf ?
                SteamVR_Controller.Input((int)LeftControllerTrObj.index) : null) : null;
        }
    }

    private SteamVR_TrackedObject _rightCtrlObj = null;
    /// <summary>
    /// Reference to the tracked object script of the right controller (get the transform via this)
    /// </summary>
    public SteamVR_TrackedObject RightControllerTrObj
    {
        get
        {
            if (ControllerManager == null || ControllerManager.right == null || !ControllerManager.right.activeSelf)
                return null;

            if (_rightCtrlObj == null)
            {
                _rightCtrlObj = ControllerManager.right.GetComponent<SteamVR_TrackedObject>();
#if DEBUG
                if (_rightCtrlObj == null)
                    Debug.LogError("Failed to get right controller tracked object from controller manager!");
#endif
            }

            return _rightCtrlObj;
        }
    }

    /// <summary>
    /// Reference to the SteamVR right controller script (get button input with this)
    /// </summary>
    public SteamVR_Controller.Device RightController
    {
        get
        {
            return RightControllerTrObj != null ?
                (RightControllerTrObj.gameObject.activeSelf ?
                SteamVR_Controller.Input((int)RightControllerTrObj.index) : null) : null;
        }
    }

    public HandSide GetHandSideFromObject(GameObject obj)
    {
        if (LeftControllerTrObj != null && obj == LeftControllerTrObj.gameObject)
            return HandSide.Left;
        if (RightControllerTrObj != null && obj == RightControllerTrObj.gameObject)
            return HandSide.Right;

        return HandSide.None;
    }

    private Script_PickUp _leftPickUp = null;
    private Script_PickUp _rightPickUp = null;
    public Script_PickUp GetPickUpFromHand(HandSide hand)
    {
        switch (hand)
        {
            case HandSide.Left:

                if (_leftPickUp == null && LeftControllerTrObj == null)
                    return null;

                return _leftPickUp = _leftPickUp ?? LeftControllerTrObj.GetComponent<Script_PickUp>();

            case HandSide.Right:

                if (_rightPickUp == null && RightControllerTrObj == null)
                    return null;

                return _rightPickUp = _rightPickUp ?? RightControllerTrObj.GetComponent<Script_PickUp>();

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    #endregion

    // Shorthand for enumerations
    public Valve.VR.EVRButtonId TriggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    public Valve.VR.EVRButtonId GripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    public Valve.VR.EVRButtonId MenuButton = Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;
    public Valve.VR.EVRButtonId TouchPad = Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;

    #region Controller press check methods
    /// <summary>
    /// Returns true whilst the button is held down
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetRightPress(Valve.VR.EVRButtonId button)
    {
        return RightController != null && RightController.GetPress(button);
    }

    /// <summary>
    /// Returns true whilst the button is held down
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetLeftPress(Valve.VR.EVRButtonId button)
    {
        return LeftController != null && LeftController.GetPress(button);
    }

    /// <summary>
    /// Returns true the frame the user starts pressing down the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetRightPressDown(Valve.VR.EVRButtonId button)
    {
        return RightController != null && RightController.GetPressDown(button);
    }

    /// <summary>
    /// Returns true the frame the user starts pressing down the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetLeftPressDown(Valve.VR.EVRButtonId button)
    {
        return LeftController != null && LeftController.GetPressDown(button);
    }

    /// <summary>
    /// Returns true the first frame the user releases the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetRightPressUp(Valve.VR.EVRButtonId button)
    {
        return RightController != null && RightController.GetPressUp(button);
    }
    /// <summary>
    /// Returns true the first frame the user releases the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    public bool GetLeftPressUp(Valve.VR.EVRButtonId button)
    {
        return LeftController != null && LeftController.GetPressUp(button);
    }

    private float GetLeftHairTrigger()
    {
        return LeftController != null ? LeftController.GetAxis(TriggerButton).x : 0f;
    }

    private float GetRightHairTrigger()
    {
        return RightController != null ? RightController.GetAxis(TriggerButton).x : 0f;
    }

    /// <summary>
    /// Returns true whilst the button is held down
    /// </summary>
    /// <param name="button">The button to check input for</param>
    /// <param name="handSide">The hand side identifying the left/right controller</param>
    public bool GetPress(Valve.VR.EVRButtonId button, HandSide handSide)
    {
        return handSide == HandSide.Left && GetLeftPress(button) ||
               handSide == HandSide.Right && GetRightPress(button);
    }

    /// <summary>
    /// Returns true the frame the user starts pressing down the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    /// <param name="handSide">The hand side identifying the left/right controller</param>
    public bool GetPressDown(Valve.VR.EVRButtonId button, HandSide handSide)
    {
        return handSide == HandSide.Left && GetLeftPressDown(button) ||
                handSide == HandSide.Right && GetRightPressDown(button);
    }

    /// <summary>
    /// Returns true the first frame the user releases the button
    /// </summary>
    /// <param name="button">The button to check input for</param>
    /// <param name="handSide">The hand side identifying the left/right controller</param>
    public bool GetPressUp(Valve.VR.EVRButtonId button, HandSide handSide)
    {
        return handSide == HandSide.Left && GetLeftPressUp(button) ||
               handSide == HandSide.Right && GetRightPressUp(button);
    }

    /// <summary>
    /// Return the value of the trigger axis (0 = released, 1 = held down)
    /// </summary>
    /// <param name="handSide">The hand side identifying the left/right controller</param>
    /// <returns></returns>
    public float GetHairTrigger(HandSide handSide)
    {
        return handSide == HandSide.Left ? GetLeftHairTrigger() : GetRightHairTrigger();
    }
    #endregion
}

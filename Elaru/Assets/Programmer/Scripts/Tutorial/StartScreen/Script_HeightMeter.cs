using System;
using UnityEngine;

/// <summary>
/// Script for setting the player's height via a height meter prop in the game
/// </summary>
[Obsolete]
public class Script_HeightMeter : MonoBehaviour
{
    private Script_LocomotionBase _base = null;
    private Script_PlayerInformation _playerScript = null;

    private float _prevPos = 0f;
    private const float MinY = 0f;
    private const float MaxY = 3f;

    private bool _right = false;
    private bool _left = false;
    private bool _allowMove = false;
    private bool _progressOnce = true;
    public float ControllerPosY { get; private set; }

    [SerializeField]
    Script_LeverHeightMeter _scriptLever = null;

    [SerializeField]
    private MeshRenderer _renderer = null;
    private const string ColorShaderId = "_EmissionColor";

    [SerializeField]
    private Script_Tutorial _scriptTutorial = null;

    private void Start()
    {
        //Cache singletons
        _base = Script_LocomotionBase.Instance;
        _playerScript = Script_PlayerInformation.Instance;

        // Fetch stuff
        if (_renderer == null)
            _renderer = GetComponentInChildren<MeshRenderer>();

        // Set initial height of meter
        var pos = transform.localPosition;
        pos.y = _playerScript.PlayerHeight <= 0f ? Script_SaveFileManager.DefaultHeight : _playerScript.PlayerHeight;
        transform.localPosition = pos;

        // Apply initial height to lever as well
        _prevPos = transform.localPosition.y;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!ControllerCheck(other.gameObject))
            return;

        // Haptic feedback
        Script_TactileFeedback.Instance.SendShortVib(500, _left ? HandSide.Left : HandSide.Right);

        // Turn on the emission
        _renderer.material.SetColor(ColorShaderId, Color.yellow);
    }

    private void OnTriggerStay(Collider other)
    {
        // Check if controllers are hovering over the height meter
        if (!ControllerCheck(other.gameObject))
            return;

        //Check if grip is not held down
        if (!_base.GetPress(_base.GripButton, _right ? HandSide.Right : HandSide.Left))
            return;

        // Allow movement if grip buttons are pressed
        _allowMove = true;
        _renderer.material.SetColor(ColorShaderId, Color.black);

        // Apply new height to meter
        ControllerPosY = other.transform.localPosition.y;

        // Apply new height to lever
        //_scriptLever.ControllerPosY = ControllerPosY;
    }

    //private void OnTriggerExit(Collider other)
    //{
    //    if (!ControllerCheck(other.gameObject))
    //        return;

    //    // Trigger loading text in tutorial
    //    Progress();
    //    _renderer.material.SetColor(ColorShaderId, Color.black);
    //}

    //private void Progress()
    //{
    //    if (_allowMove && _progressOnce)
    //    {
    //        _scriptTutorial.ProgressState = Script_Tutorial.Progress.HeightMeasure;
    //        _scriptTutorial.NextStep();
    //        _progressOnce = false;
    //        _allowMove = false;
    //    }
    //}

    private bool ControllerCheck(GameObject obj)
    {
        //Check which controller is pressed
        _right = obj == _base.RightControllerTrObj.gameObject;
        _left = obj == _base.LeftControllerTrObj.gameObject;
        return _right || _left;
    }

    private void Update()
    {
        if (!_allowMove)
            return;

        // Disable
        //if (_right)
        //    ControllerMove(_base.RightController);
        //else if (_left)
        //    ControllerMove(_base.LeftController);

        // Move height meter
        if (Math.Abs(_prevPos) > 1e-5)
        {
            //Keep within bounds
            if (transform.localPosition.y > MinY &&
                transform.localPosition.y < MaxY)
            {
                var displace = ControllerPosY - _prevPos;
                transform.Translate(0f, displace, 0f);

                // Haptic feedback whilst moving
                Script_TactileFeedback.Instance.SendShortVib(250, _right ? HandSide.Right : HandSide.Left);
            }
            // Reset to max position
            else if (transform.localPosition.y > MaxY)
                transform.localPosition.Set(transform.localPosition.x, MaxY, transform.localPosition.z);
            // Reset to min position
            else if (transform.localPosition.y < MinY)
                transform.localPosition.Set(transform.localPosition.x, MinY, transform.localPosition.z);
        }
        _prevPos = ControllerPosY;
    }

    //private void ControllerMove(SteamVR_Controller.Device controller)
    //{
    //    // Disable when button is released
    //    if (controller.GetPressUp(_base.GripButton))
    //        Progress();
        
    //    // Subtle haptic feedback
    //    if (controller.GetPressDown(_base.GripButton))
    //        controller.TriggerHapticPulse(250);
    //}
}

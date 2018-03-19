using System;
using UnityEngine;

public class Script_Locomotion_TeleDash : MonoBehaviour
{
    private Script_LocomotionBase _scriptLoco = null;
    private Script_WeaponManager _weaponManager = null;
    private Collider _colliderPlayer = null;

    //Teleportation vars
    [SerializeField]
    public float MaxDistance = 20.0f;
    public float MaxAngle = 45.0f;
    public float MaxHeight = 3.0f;
    public float DashSpeed = 10.0f;
    public float DashTime = 1.0f;
    private const float _planeOffset = 0.01f;
    public bool Dashing { get; private set; }
    public bool CanDash { get { return _canMove; } }
    public bool TryingToDash { get; private set; }

    private bool _detectFloor = false;
    private bool _canMove = false;
    private Vector3 _playerOffset;
    public LayerMask Ignore;

    //Controls checking
    private bool _rightPressed;
    private bool _leftPressed;

    //Limitation vars
    private bool _limitedArea = false;
    private Vector3 _limitedAreaCenter;
    private float _limitedRange = 0f;

    //Indication vars
    private GameObject _showPlayerNewPosition;
    private Script_ColorLerp _scriptColor = null;

    //Checking if anything is above you
    public float MinimumStandingHeight = 2.0f;

    //Locking variables
    [SerializeField]
    private GameObject _limitationSphere = null;
    private int _lockCount = 0;

    [SerializeField]
    private LineRenderer _lineRenderer = null;

    private Transform _rig = null;

    private Script_PlayerCollisionCheck _scriptCollision = null;
    private int _count = 0;
    private Color _colorGood = new Color(0f, 1f, 0f, 0.3f);
    private Color _colorBad = new Color(1f, 0f, 0f, 0.3f);

    private Script_AudioManager _scriptAudio = null;
    private const string _teleportSFX = "Teleport";
    private bool _playedSound = false;

    private const string PathPlanePrefab = "Prefabs/Pre_PlaneTeleport";
    private const string TagPlayer = "Player";
    private const string TagPickUp = "PickUp";
    private const string TagSword = "Sword";
    private const string TagGun = "Gun";

    private void Awake()
    {
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptLoco.ScriptLocomotionDash = this;

        _weaponManager = Script_WeaponManager.Instance;
        _scriptAudio = Script_AudioManager.Instance;
    }

    private void Start()
    {
        //Cache locomotion base vars
        _colliderPlayer = _scriptLoco.PlayerCollider;
#if DEBUG
        if (_colliderPlayer == null)
            Debug.LogWarning("Failed to get the player collider from the locomotion base!");
#endif

        if (_scriptLoco.CameraRig != null)
            _rig = _scriptLoco.CameraRig.transform;
#if DEBUG
        else
            Debug.LogWarning("Failed to get camera rig from the locomotion base!");
#endif

        //Create teleport location indication
        _showPlayerNewPosition = Instantiate(Resources.Load(PathPlanePrefab)) as GameObject;
        _showPlayerNewPosition.transform.position = transform.position;
        _showPlayerNewPosition.SetActive(false);

        //Teleport indication icon and color changing
        _scriptColor = _showPlayerNewPosition.GetComponentInChildren<Script_ColorLerp>();

#if DEBUG
        if (_scriptColor == null)
            Debug.LogWarning("Failed to get color script on teleport plane!");
#endif

        //Deactivate region lock indication at start
        if (_limitationSphere != null)
            _limitationSphere.SetActive(false);

        if (_lineRenderer != null)
        {
            _lineRenderer.gameObject.SetActive(false);
            _lineRenderer.transform.SetParent(null);
        }

        _scriptCollision = _scriptLoco.ScriptCollisionCheck;
    }

    private void Update()
    {
        if (_scriptLoco == null || _rig == null)
            return;

        // If neither controllers are on, return
        if (_scriptLoco.RightController == null && _scriptLoco.LeftController == null)
            return;

        TryingToDash = _scriptLoco.GetRightPress(_scriptLoco.TouchPad) || _scriptLoco.GetLeftPress(_scriptLoco.TouchPad);

        // Prevent dash if both touchpads are pressed
        if (_scriptLoco.GetRightPress(_scriptLoco.TouchPad) && _scriptLoco.GetLeftPress(_scriptLoco.TouchPad))
        {
            _canMove = false;
            _showPlayerNewPosition.SetActive(false);
            _lineRenderer.gameObject.SetActive(false);
            _leftPressed = false;
            _rightPressed = false;
            return;
        }

        if (!Dashing)
        {
            // Check if right controller exists, check if right touchpad has been pressed
            if (_scriptLoco.GetRightPress(_scriptLoco.TouchPad))
                SetNewPosition(true);

            // Check if left controller exists, check if left touchpad has been pressed
            else if (_scriptLoco.GetLeftPress(_scriptLoco.TouchPad))
                SetNewPosition(false);

            // Check if either controller has been released
            if (_scriptLoco.GetRightPressUp(_scriptLoco.TouchPad) && _rightPressed ||
                _scriptLoco.GetLeftPressUp(_scriptLoco.TouchPad) && _leftPressed)
            {
                // Allow the dash motion if the player has released the designated pressed touchpad
                if (_showPlayerNewPosition != null)
                {
                    _showPlayerNewPosition.SetActive(false);
                    if (_lineRenderer != null)
                        _lineRenderer.gameObject.SetActive(false);
                }
#if DEBUG
                else
                    Debug.LogWarning("No teleport texture plane set on teleDash locomotion script!");
#endif

                if (_canMove)
                    Dashing = true;

                // Check for an object between the player and the destination
                RaycastHit hit;
                Vector3 dest = _showPlayerNewPosition.transform.position + (Script_PlayerInformation.Instance.IsInBase ? new Vector3(0, 1, 0) : new Vector3(0, 3, 0));
                var playerPos = _scriptLoco.PlayerCollider.transform.position;
                if (Physics.Raycast(dest, (playerPos - dest).normalized, out hit))
                {
                    if (!(hit.transform.CompareTag(TagPlayer) || hit.transform.CompareTag(TagPickUp) ||
                        hit.transform.CompareTag(TagSword) || hit.transform.CompareTag(TagGun)))
                    {
                        //Disable teleport if something was hit
                        Dashing = false;
                        CanTeleport(false);
                    }
                    else if (_scriptLoco.GetRightPressUp(_scriptLoco.TouchPad) || _scriptLoco.GetLeftPressUp(_scriptLoco.TouchPad))
                        SetNewSafePlace();
                }
            }
        }

        // Perform the dashing motion
        if (Dashing)
        {
            var newPos = new Vector3(
                _showPlayerNewPosition.transform.position.x + _playerOffset.x,
                _detectFloor ? _rig.position.y : _showPlayerNewPosition.transform.position.y,
                _showPlayerNewPosition.transform.position.z + _playerOffset.z);

            //Check if movement is longer than treshhold
            var move = _rig.position - newPos;
            var magnitude = move.sqrMagnitude;
            if (magnitude > 0.1f * 0.1f)
            {
                // Play teleport sound at player position
                if (!_playedSound)
                {
                    _scriptAudio.PlaySFX(_teleportSFX, _rig.position, Mathf.Clamp01(magnitude / 200f));
                    _playedSound = true;
                }

                //Deactivate gun laser when teleporting 
                if (_weaponManager.Gun != null && _weaponManager.Gun.PickUpObjectScript.BeingHeld)
                    _weaponManager.Gun.ShowVisualModEffects(false);

                _rig.position = Vector3.MoveTowards(_rig.position, newPos, DashSpeed * Time.deltaTime);
            }
            else
            {
                //Activate gun laser after teleport
                if (_weaponManager.Gun != null && _weaponManager.Gun.PickUpObjectScript.BeingHeld)
                    _weaponManager.Gun.ShowVisualModEffects(true);

                Dashing = false;
                _canMove = false;
                _playedSound = false;
            }
        }
    }

    private void SetNewSafePlace()
    {
        // Set the new safe zone to the last valid teleport location
        _count = (_count + 1) % 2;
        if (_count == 1)
            Script_PlayerCollisionCheck.TransformSafeZone = _showPlayerNewPosition.transform.position;
    }

    public void OverrideSafePlace(Vector3 position)
    {
        Script_PlayerCollisionCheck.TransformSafeZone = position;
    }

    private void SetNewPosition(bool right)
    {
        if (right) _rightPressed = _scriptLoco.GetRightPress(_scriptLoco.TouchPad);
        else _leftPressed = _scriptLoco.GetLeftPress(_scriptLoco.TouchPad);

        Dashing = false;

        //Do a raycast to check for teleport location
        Transform pressedController = null;
        if (_rightPressed && right)
            pressedController = _scriptLoco.RightControllerTrObj.transform;
        else if (_leftPressed && !right)
            pressedController = _scriptLoco.LeftControllerTrObj.transform;

        //Safety check
        if (pressedController == null)
            return;

        if (SteamVR_Render.Top() == null || SteamVR_Render.Top().Head == null)
            return;

        RaycastHit hit;
        if (Physics.Raycast(pressedController.position, pressedController.forward, out hit, float.MaxValue, ~Ignore))
        {
            //Show indication
            _showPlayerNewPosition.SetActive(true);
            _lineRenderer.gameObject.SetActive(true);

            // Calculate the offset of the player compared to the camera rig
            var headPos = _scriptLoco.HeadPositionOnGround;
            _playerOffset = _rig.position - headPos;

            // Set teleport plane offset from surface
            var pos = hit.point + hit.normal * _planeOffset;

            // Update teleport indicator
            var rot = Quaternion.FromToRotation(_showPlayerNewPosition.transform.up, hit.normal) * _showPlayerNewPosition.transform.rotation;
            _showPlayerNewPosition.transform.position = pos;
            _showPlayerNewPosition.transform.rotation = rot;

            // Update 
            _lineRenderer.SetPosition(0, pressedController.position);
            _lineRenderer.SetPosition(1, pos);

            //Check if player fits in new location
            var height = hit.point.y - headPos.y;
            var angle = Vector3.Angle(Vector3.up, hit.normal);
            var between = _limitedArea ? (pos - _limitedAreaCenter) : (pos - headPos);
            var range = _limitedArea ? _limitedRange : MaxDistance;

            //Check if player height fits in new location
            var playerCanFit = !Physics.Raycast(_showPlayerNewPosition.transform.position,
                _showPlayerNewPosition.transform.up, MinimumStandingHeight, ~Ignore);

            // Set the teleport plane texture according to the maximum teleport distance
            CanTeleport(angle <= MaxAngle && between.sqrMagnitude <= range * range && height <= MaxHeight && playerCanFit);

            // Allow collision to fade out after first teleport
            if (_canMove && !_scriptCollision.PlayerHasMoved)
                _scriptCollision.PlayerHasMoved = true;
        }
    }

    /// <summary>
    /// Allow/disallow the player to teleport
    /// </summary>
    /// <param name="state">State of the teleportation</param>
    private void CanTeleport(bool state)
    {
        var newColor = state ? _colorGood : _colorBad;
        _scriptColor.TargetColor = newColor;
        _lineRenderer.startColor = newColor;
        _lineRenderer.endColor = newColor;
        _canMove = state;
    }

    /// <summary>
    /// Enable / Disable limited region where player can teleport inside of
    /// </summary>
    /// <param name="limited">Is player locked to this region</param>
    /// <param name="center">Center of limited area</param>
    /// <param name="lockRange">Range of limited area</param>
    /// <param name="forceUnlock">Forcefully unlock player, ignoring lock count</param>
    public void LockRegion(bool limited, Vector3? center = null, float lockRange = 0f, bool forceUnlock = false)
    {
        //Update lock counter
        _lockCount += limited ? 1 : -1;
        if (_lockCount < 0)
            _lockCount = 0;

        //Limit player if being locked from anywhere
        _limitedArea = _lockCount > 0;

        //Check force unlock
        if (forceUnlock)
        {
            _limitedArea = false;
            _lockCount = 0;
        }

        //Set limitation values
        if (center != null)
            _limitedAreaCenter = center.Value;

        _limitedRange = lockRange;


        if (limited && _limitationSphere != null)
        {
            //Show region indication sphere
            _limitationSphere.SetActive(true);
            _limitationSphere.transform.SetParent(null);
            _limitationSphere.transform.position = _limitedAreaCenter;
            _limitationSphere.transform.localScale = new Vector3(lockRange, 1f, lockRange);
        }
        else if (_limitationSphere != null && _lockCount == 0)
        {
            //Disable region indication sphere
            _limitationSphere.SetActive(false);
            _limitationSphere.transform.SetParent(transform);
            _limitationSphere.transform.localPosition = Vector3.zero;
        }
    }

    public void ForceStopDash()
    {
        Dashing = false;
        _canMove = false;
    }
}

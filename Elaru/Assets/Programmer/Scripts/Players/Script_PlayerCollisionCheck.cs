using System.Linq;
using UnityEngine;

/// <summary>
/// Script used for fading out the SteamVR camera whilst colliding with the world (using a TRIGGER on the main camera)
/// </summary>
[RequireComponent(typeof(Collider))]
public class Script_PlayerCollisionCheck : MonoBehaviour
{
    private static float FadeOffset = 0.25f;
    public static float FadeOutTime = 0.25f;
    private Color _colorFade = Color.clear;
    private Collider _collider = null;
    private float _extent = 0f;
    private Script_Locomotion_TeleDash _scriptDash = null;
    private Script_LocomotionGrab _scriptGrab = null;

    public static Vector3 TransformSafeZone = Vector3.zero;

    [SerializeField]
    private BoxCollider _colliderEye = null;
    [SerializeField]
    private LayerMask _layersToFade = 0;
    [SerializeField]
    private Collider[] _listCollidersToIgnore = null;
    [SerializeField]
    private Collider[] _listCollidersToPardon = null;
    private Vector3 _initSizeColliderEye = Vector3.zero;

    public bool PlayerHasMoved = false;
    public bool Active = true;

    private Script_LocomotionBase _scriptLoco = null;
    private Transform _cameraRig = null;
    private Collider _playerCollider = null;

    private Transform _playerPositionBeforeFade = null;
    private bool _positionHasBeenSet = false;
    private float _timerStuck = 0f;
    private const float MaxStuckTime = 1f;
    private Collider _thisTrigger = null;

    private void Awake()
    {
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptLoco.ScriptCollisionCheck = this;
    }

    private void Start()
    {
        // Fetch bounds from box collider (filter out the colliders, since we want the trigger)
        _thisTrigger = GetComponents<Collider>().FirstOrDefault(coll => coll.isTrigger);
        if (_thisTrigger == null)
            Debug.LogWarning("Failed to get collider on game object!");
        _extent = _thisTrigger.bounds.extents.x;

        // Fetch locomotion scripts from singleton
        _scriptDash = _scriptLoco.ScriptLocomotionDash;
        _scriptGrab = _scriptLoco.ScriptLocomotionGrab;

        //Save original collider size
        _initSizeColliderEye = _colliderEye.size;

        // Ignore all the colliders on the player
        IgnoreArrayOfColliders(_scriptLoco.RootRigidbody.GetComponentsInChildren<Collider>());
        // Ignore all the colliders on the gun
        var gun = FindObjectOfType<Script_Gun>();
        if (gun != null)
            IgnoreArrayOfColliders(gun.GetComponentsInChildren<Collider>());
        // Ignore all the colliders on the sword
        var sword = FindObjectOfType<Script_Sword>();
        if (sword != null)
            IgnoreArrayOfColliders(sword.GetComponentsInChildren<Collider>());

        // Ignore any additional colliders set in the inspector
        IgnoreCollision(true, _listCollidersToIgnore);

        // Pardon colliders if necessary
        IgnoreCollision(false, _listCollidersToPardon);

        // Get camera rig and set the player's initial position (update this when a fade starts)
        _cameraRig = _scriptLoco.CameraRig.transform;

        _playerPositionBeforeFade = new GameObject("Player position before collision").GetComponent<Transform>();
        _playerPositionBeforeFade.position = _cameraRig.position;

        _playerCollider = _scriptLoco.PlayerCollider;
    }

    private void IgnoreArrayOfColliders(params Collider[] colliders)
    {
        foreach (var coll in colliders)
            Physics.IgnoreCollision(coll, _thisTrigger, true);
    }

    /// <summary>
    /// Shorthand for ignoring an array of colliders according to the state passed in the parameters
    /// </summary>
    /// <param name="colliders">Array of colliders</param>
    /// <param name="state">To ignore or not to ignore</param>
    /// <param name="colliderToIgnore">Collider to ignore</param>
    public void IgnoreCollision(bool state, params Collider[] colliders)
    {
        if (colliders == null || colliders.Length <= 0)
            return;

        foreach (var collider in colliders)
        {
            if (collider == null)
            {
                Debug.LogWarning("Collider was null in collision check array!", gameObject);
                continue;
            }

            Physics.IgnoreCollision(collider, _thisTrigger, false);
        }
    }

    /// <summary>
    /// Check if a layermask is included in the fade layermasks variable
    /// </summary>
    /// <param name="mask1">Layermask to compare</param>
    /// <returns></returns>
    private bool IsFadeLayer(int mask1)
    {
        return ((1 << mask1) & _layersToFade.value) > 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsFadeLayer(other.gameObject.layer))
            return;

        // Ignore triggers
        if (other.isTrigger)
        {
            Physics.IgnoreCollision(other, _colliderEye);
            return;
        }

        if (_collider != null)
        {
            if (other == _collider)
                return;

            // Replace the collider if the new collider is closer
            if (DistanceFromCollider(other.transform.position) < DistanceFromCollider(_collider.transform.position))
                return;
        }

        _collider = other;
        // Make the eye collider smaller (so the player can put their head closer to the walls)
        _colliderEye.size = Vector3.one / 100f;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_collider == null)
            return;

        if (other.isTrigger)
            return;

        //Ignore colliders that are not on fade layer
        if (!IsFadeLayer(other.gameObject.layer))
            return;

        //Ignore collider self
        if (other == _collider)
            return;

        //Replace collider if new one is closer
        if (DistanceFromCollider(other.transform.position) < DistanceFromCollider(_collider.transform.position))
            _collider = other;
    }

    private void OnTriggerExit(Collider other)
    {
        if (_collider == other)
        {
            _collider = null;

            // Reset the SteamVR fade
            ResetFade();

            // Reset collider size
            _colliderEye.size = _initSizeColliderEye;
        }
    }

    /// <summary>
    /// Calculate the distance from the collider
    /// </summary>
    /// <param name="position">Position to compare with the distance check</param>
    /// <returns></returns>
    private float DistanceFromCollider(Vector3 position)
    {
        // Calculate distance from wall
        var dist = Vector3.Distance(transform.position, position);
        return dist / _extent;
    }

    private void LateUpdate()
    {
        // Skip this if the player hasn't moved yet
        if (!Active || !PlayerHasMoved)
            return;

        // Skip this if there is no collision
        if (_collider == null || Time.timeSinceLevelLoad < 1f)
            return;

        // Save the player's initial location when the fade starts or when the player starts climbing
        if (!_positionHasBeenSet && (_colorFade.a > 0f || _scriptGrab.Grabbing))
            SetPlayerPosition();

        // Fetch the distance for fading the screen
        var distance = DistanceFromCollider(_collider.ClosestPointOnBounds(transform.position));

        // Lerp between collisions (prevents the screen from flickering when clipping from collider to collider)
        _colorFade.a = Mathf.Lerp(_colorFade.a, 1.0f - distance + FadeOffset * (1.0f - distance)
            , Time.deltaTime * 10f);

        // Fade the screen
        SteamVR_Fade.View(_colorFade, 0f);

        // Disable locomotion when the screen is fully faded
        _scriptDash.enabled = _scriptGrab.enabled = !(_colorFade.a >= 1f);

        // Check if the player is stuck for too long
        if (_colorFade.a >= 1f)
        {
            _timerStuck += Time.deltaTime;
            _scriptLoco.RootRigidbody.isKinematic = true;
        }

        // Player seems to be stuck? Reset their position
        if (_timerStuck > MaxStuckTime)
        {
            _timerStuck = 0f;
            ResetPlayerPosition();
        }
    }

    private void SetPlayerPosition()
    {
        _playerPositionBeforeFade.position = _cameraRig.position;

        var forward = _playerPositionBeforeFade.position - _collider.ClosestPointOnBounds(_playerPositionBeforeFade.position);

        // Save the forward orientation
        _playerPositionBeforeFade.rotation = Quaternion.Euler(forward);

        _positionHasBeenSet = true;
    }

    private void ResetPlayerPosition()
    {
        // Overwrite for locations with glitchy colliders
        if (TransformSafeZone != Vector3.zero)
        {
            _cameraRig.position = TransformSafeZone;
            SteamVR_Fade.View(Color.clear, FadeOutTime);
            return;
        }

        _scriptLoco.RootRigidbody.isKinematic = false;
        _positionHasBeenSet = false;
    }

    //private void ReorientPlayer()
    //{
    //    var angle = Vector3.Angle(Vector3.right, _playerPositionBeforeFade.forward.normalized);
    //    _cameraRig.transform.Rotate(Vector3.up, angle);
    //}

    private void ResetFade(bool resetPlayerPosition = false)
    {
        if (resetPlayerPosition)
            ResetPlayerPosition();

        _scriptLoco.RootRigidbody.isKinematic = false;

        // Clear up the fade
        ClearFade();

        // Allow the next fade to set a new position (unless the player is still climbing)
        if (!_scriptGrab.Grabbing)
            _positionHasBeenSet = false;

        // Fail-safe, re-enable locomotion
        _scriptDash.enabled = _scriptGrab.enabled = true;
    }

    private void ClearFade()
    {
        SteamVR_Fade.View(Color.clear, FadeOutTime);
    }

    private void OnDisable()
    {
        ResetFade();
    }
}
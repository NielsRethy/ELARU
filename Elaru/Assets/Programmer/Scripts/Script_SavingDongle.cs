using System.Linq;
using UnityEngine;

/// <summary>
/// Pick up that allows the player to quick save the game when plugged into the hand not holding it
/// </summary>
[RequireComponent(typeof(Script_PickUpObject))]
[RequireComponent(typeof(Rigidbody))]
public class Script_SavingDongle : MonoBehaviour
{
    //Singleton cacheing
    private Script_SaveFileManager _scriptSave = null;
    private Script_LocomotionBase _scriptBase = null;

    private Script_PickUpObject _pickUpObjectScript = null;
    private Collider _thisTrigger = null;

    [SerializeField]
    private Vector3 _initialPosition = Vector3.zero;
    private Quaternion _initialRotation = Quaternion.identity;
    private Rigidbody _rigidBody = null;

    [SerializeField]
    private float _distanceUntilSave = 0.05f;
    private float _distance = 0f;
    private const float DelayDistanceCheck = 0.3f;
    private float _timerDelay = 0f;

    private bool _dongleIsInOtherHand = false;
    private bool _saving = false;

    /// <summary>
    /// Shorthand for the left controller tracked object
    /// Property because controllers can be turned off/on during gameplay
    /// </summary>
    private GameObject _objectLeftHand
    { get { return _scriptBase.LeftControllerTrObj.gameObject; } }
    /// <summary>
    /// Shorthand for the right controller tracked object
    /// Property because controllers can be turned off/on during gameplay
    /// </summary>
    private GameObject _objectRightHand
    { get { return _scriptBase.RightControllerTrObj.gameObject; } }

    private bool _left = false;

    //Hand pick up scripts
    private Script_PickUp _leftPickUp = null;
    private Script_PickUp _scriptLeftPickUp
    {
        get { return _leftPickUp ?? (_leftPickUp = _objectLeftHand.GetComponent<Script_PickUp>()); }
    }

    private Script_PickUp _rightPickUp = null;
    private Script_PickUp _scriptRightPickUp
    {
        get { return _rightPickUp ?? (_rightPickUp = _objectRightHand.GetComponent<Script_PickUp>()); }
    }


    public static LineRenderer LineRenderer = null;
    private Color _lineMatColor = Color.clear;
    private const string _lineMaterial = "Mat_SavingDongleConnection";

    private void Awake()
    {
        //Cache components
        _scriptSave = Script_SaveFileManager.Instance;
        _scriptBase = Script_LocomotionBase.Instance;
        _pickUpObjectScript = GetComponent<Script_PickUpObject>();
    }

    private void Start()
    {
        // Get the trigger attached to this object
        var colliders = GetComponents<Collider>();
        if (colliders == null)
            Debug.LogWarning("Failed to get any colliders on saving dongle?", gameObject);
        else
            _thisTrigger = colliders.FirstOrDefault(coll => coll.isTrigger);
        if (_thisTrigger == null)
            Debug.LogWarning("Failed to get trigger collider on saving dongle", gameObject);

        // Get the rigidbody attached
        _rigidBody = GetComponent<Rigidbody>();
        if (_rigidBody == null)
            Debug.LogWarning("Failed to get rigidbody on saving dongle", gameObject);

        // Set initial state
        _rigidBody.isKinematic = true;
        _initialRotation = transform.rotation;
        transform.position = _initialPosition;

        // Create (static) line renderer, unless it exists already
        if (LineRenderer != null)
            return;

        var obj = new GameObject("SavingDongleLineRenderer");
        LineRenderer = obj.AddComponent<LineRenderer>();
        // Turn off initially
        LineRenderer.gameObject.SetActive(false);
        // Set line renderer settings
        LineRenderer.positionCount = 2;
        LineRenderer.widthMultiplier = 0.1f;
        LineRenderer.loop = true;
        // Fetch the material
        LineRenderer.material = Resources.Load("Material/" + _lineMaterial) as Material;
    }

    // Check if the dongle hit the other hand that isn't holding the dongle
    private void OnTriggerEnter(Collider other)
    {
        // If either hand doesn't exist, forget about it
        if (_objectLeftHand == null ||
            _objectRightHand == null)
            return;

        // Ignore anything that isn't a hand
        if (other.gameObject != _objectLeftHand &&
            other.gameObject != _objectRightHand)
        {
            Physics.IgnoreCollision(other, _thisTrigger);
            return;
        }

        // If the player isn't holding the object, ignore this
        if (!_pickUpObjectScript.BeingHeld)
            return;

        // Check if saving dongle hit either of the player's hand
        if (_left && DongleHitHand(_objectRightHand, other.gameObject, HandSide.Right, _scriptRightPickUp))
            _dongleIsInOtherHand = true;
        if (!_left && DongleHitHand(_objectLeftHand, other.gameObject, HandSide.Left, _scriptLeftPickUp))
            _dongleIsInOtherHand = true;
    }

    /// <summary>
    /// Shorthand for checking if the specified game object hit the hand game object
    /// </summary>
    /// <param name="hand">The hand game object to match the other object with</param>
    /// <param name="other">The other game object</param>
    /// <param name="side">The hand side to check for</param>
    private bool DongleHitHand(GameObject hand, GameObject other, HandSide side, Script_PickUp otherPickUp)
    {
        // Ignore collision with the hand that's holding the dongle already
        if (_pickUpObjectScript.ControlHandSide == side)
            return false;

        // Check if the designated hand is equal to hand that collided
        if (hand != null && hand != other.gameObject)
            return false;

        // Check if the player is already holding another object in their other hand
        if (otherPickUp != null && otherPickUp.IsHoldingObject)
            return false;

        return true;
    }

    private void OnTriggerExit(Collider other)
    {
        // If the saving has started, ignore this
        if (_saving || !_dongleIsInOtherHand)
            return;

        // Check if the opposite hand left the dongle's trigger
        if (_left && other.gameObject != _objectRightHand ||
            !_left && other.gameObject != _objectLeftHand)
            return;

        _dongleIsInOtherHand = false;
    }

    private void Update()
    {
        // Whilst the pick up is being held
        if (_pickUpObjectScript.BeingHeld)
        {
            // Determine which side the object is being held in
            _left = _pickUpObjectScript.ControlHandSide == HandSide.Left;

            // Update the line renderer's positions and texture
            if (_objectLeftHand == null || _objectRightHand == null)
                LineRenderer.gameObject.SetActive(false);
            else
            {
                LineRenderer.gameObject.SetActive(true);

                // Set the line renderer's parent and reset it's position
                if (LineRenderer.transform.parent == null)
                {
                    LineRenderer.transform.SetParent(_left ? _objectLeftHand.transform : _objectRightHand.transform);
                    LineRenderer.transform.localPosition = Vector3.zero;
                    LineRenderer.SetPosition(0, Vector3.zero);
                }

                // Set the line render's position to the target hand (the one not holding the dongle)
                UpdateLineRenderer(
                    _left ? _objectLeftHand.transform.position : _objectRightHand.transform.position,
                    _left ? _objectRightHand.transform.position : _objectLeftHand.transform.position);
            }

            // Ensure the rigidbody is dynamic whilst being held
            _rigidBody.isKinematic = false;
        }
        else if (LineRenderer.gameObject.activeSelf)
            //Disable line renderer
            LineRenderer.gameObject.SetActive(false);

        // If the player isn't holding the dongle, ignore the remainder of this code
        if (!_dongleIsInOtherHand)
            return;

        // Don't do a distance check every single frame
        _timerDelay -= Time.deltaTime;
        if (_timerDelay > 0f)
            return;

        // Start saving the game as soon as the saving dongle is dropped inside the player's hand
        if (DistanceCheck(_left ? _objectLeftHand.transform.position : _objectRightHand.transform.position))
            _saving = true;
        // Reset distance check delay
        else
            _timerDelay = DelayDistanceCheck;

        // Actually save the game
        if (_saving)
        {
            _scriptSave.SaveSceneData();
            _saving = false;

            // Drop the saving dongle (out of the hand it's held in)
            if (_left)
                _scriptLeftPickUp.Drop();
            else
                _scriptRightPickUp.Drop();

            // Reset the dongle to its initial state
            _rigidBody.isKinematic = true;
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            // Reset line renderer
            LineRenderer.transform.SetParent(null);
            LineRenderer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Shorthand for checking the distance between the hand and dongle position
    /// </summary>
    /// <param name="handPos">Position of the hand</param>
    /// <returns></returns>
    private bool DistanceCheck(Vector3 handPos)
    {
        return (handPos - transform.position).sqrMagnitude < _distanceUntilSave * _distanceUntilSave;
    }

    /// <summary>
    /// Shorthand for updating the line renderer's positions and texture(s)
    /// </summary>
    /// <param name="hand">Position of the holding the dongle</param>
    /// <param name="targetHand">Position of the hand not holding the dongle</param>
    private void UpdateLineRenderer(Vector3 hand, Vector3 targetHand)
    {
        LineRenderer.gameObject.SetActive(true);

        // Update position
        LineRenderer.SetPosition(0, hand);
        LineRenderer.SetPosition(1, targetHand);

        // Update texture
        if (LineRenderer.material.mainTexture != null)
        {
            //TODO: Fade when close
            Debug.Log(_distance / _distanceUntilSave);
            _lineMatColor.a = _distance / _distanceUntilSave;
            LineRenderer.material.SetColor("_EmissionColor", _lineMatColor);
            //_lineMatOffset.x += (_distance / _distanceUntilSave) * 100f * Time.deltaTime;
            //LineRenderer.material.SetTextureOffset("_MainTex", _lineMatOffset);
        }
    }
}

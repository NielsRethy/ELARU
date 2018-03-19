using UnityEngine;

/// <summary>
/// Script for handling the collision state of the grab locomotion on each controller
/// </summary>
public class Script_LocomotionGrabState : MonoBehaviour
{
    private Script_LocomotionBase _scriptLoco = null;

    [SerializeField]
    private LayerMask _layerMask = 4;

    public bool CanGrab { get; private set; }

    private HandSide _handSide = HandSide.None;
    private const ushort HapticPulseStrength = 1000;

    private Script_LocomotionGrab _scriptGrabbing = null;
    private Script_TactileFeedback _scriptTactile = null;

    public GameObject CurrentSelection { get; private set; }
    private GameObject _effectSelection = null;

    private AudioSource _audioSource = null;

    private Script_PickUpEffects _effectScript = null;

    private void Awake()
    {
        //Cache singletons
        _scriptLoco = Script_LocomotionBase.Instance;
        _scriptTactile = Script_TactileFeedback.Instance;
    }

    private void Start()
    {
        _scriptGrabbing = _scriptLoco.ScriptLocomotionGrab;

        CurrentSelection = null;
        CanGrab = false;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            Debug.LogWarning("No audio source found on " + name + " for pick up sound effects");

        AssignHandSide();
    }

    private void OnEnable()
    {
        // Refresh controller manager
        //if (Time.timeSinceLevelLoad > 0.1f)
        //_scriptLoco.ControllerManager.Refresh();

        // Re-assign hand side (controllers can turn on/off during gameplay)
        AssignHandSide();
    }

    /// <summary>
    /// Get pick up effects script of the currently selected game object
    /// </summary>
    public Script_PickUpEffects PickUpEffects
    {
        get
        {
            // Ignore this if nothing was selected
            if (CurrentSelection == null)
                return null;

            // If the same object is still selected,
            // return the previously obtained pick up effects script
            if (_effectSelection == CurrentSelection)
                return _effectScript;

            // Update the selection for the next comparison
            _effectSelection = CurrentSelection;

            // Get script from new selection
            _effectScript = _effectSelection.GetComponent<Script_PickUpEffects>();

            if (_effectScript == null)
            {
                Debug.Log("GrabState failed to get a pick up effects script from " + _effectSelection.name);
                return null;
            }

            // Also set the audio source 
            if (_audioSource != null)
                // Set climbable object's audio source reference to the player's hand
                _effectScript.AudioSource = _audioSource;

            return _effectScript;
        }
    }

    /// <summary>
    /// Shorthand for assigning the hand side to identify which controller the grab state belongs to
    /// </summary>
    private void AssignHandSide()
    {
        if (_scriptLoco.LeftControllerTrObj != null)
            _handSide = _scriptLoco.LeftControllerTrObj == GetComponent<SteamVR_TrackedObject>() ? HandSide.Left : HandSide.Right;
    }

    private void OnTriggerStay(Collider other)
    {
        // If locomotion is disabled, ignore this
        if (_scriptGrabbing == null || !_scriptGrabbing.enabled || !IsInLayerMask(other.gameObject.layer))
            return;

        // If the other object is already selected, ignore this
        if ((CurrentSelection != null && CurrentSelection == other.gameObject) ||
            (PickUpEffects != null && PickUpEffects.Hover))
            return;

        // Select the climbable object
        Select(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (CurrentSelection != null)
            // If selected object is deselected
            Deselect(other.gameObject);
    }

    /// <summary>
    /// Shorthand for checking if a particular game object is inside the layermask
    /// </summary>
    /// <param name="layer">Layer to compare</param>
    private bool IsInLayerMask(int layer)
    {
        return (1 << layer & _layerMask.value) != 0;
    }

    /// <summary>
    /// Selection behaviour for when the player is hovering their hand inside something grabbable
    /// </summary>
    /// <param name="other">Game object to check the layer mask of and to fetch the emissive script from</param>
    private void Select(GameObject other)
    {
        // If their was a previous emission script saved, 'unhover' it
        if (PickUpEffects != null)
            PickUpEffects.Hover = false;

        // Set a reference to the object (used to compare new selections later)
        CurrentSelection = other;

        // Allow grabbing
        CanGrab = true;

        // Set emissive
        if (PickUpEffects != null)
            PickUpEffects.Hover = true;

        // Haptic feedback
        _scriptTactile.SendShortVib(HapticPulseStrength, _handSide);
    }

    /// <summary>
    /// Selection behaviour for when the player removes their hand from something grabbable
    /// </summary>
    /// <param name="other">Game object to fetch the emissive of</param>
    public void Deselect(GameObject other)
    {
        // Ensure the deselected object is the one that was selected before
        if (CurrentSelection != other.gameObject)
            return;

        // Disable grabbing
        CanGrab = false;

        // Turn off the emissive object
        if (PickUpEffects != null)
            PickUpEffects.Hover = false;

        // Reset variables
        CurrentSelection = null;
    }
}

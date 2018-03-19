using UnityEngine;

public class Script_LeverHandTracking : Script_PickUpEffects
{
    //Axis to rotate the lever around
    [SerializeField]
    private Vector3 _rotationAxis = new Vector3(1, 0, 0);
    [SerializeField]
    private GameObject _endRot = null;
    [SerializeField]
    private bool _center = false;
    [SerializeField]
    private GameObject _centerRot = null;

    private bool _isHeld = false;
    private bool _isInTrigger = false;
    private HandSide _handside = HandSide.None;
    private Quaternion _startRot = Quaternion.identity;
    public bool Locked { get; set; }

    private GameObject _controller = null;

    private Script_LocomotionBase _locBase;
    private Script_TactileFeedback _tactileFeedback;

    public Quaternion GetSolvedStateRot(bool top)
    {
        return _centerRot.transform.localRotation;
    }

    private new void Start()
    {
        base.Start();

        //Cache needed components
        _locBase = Script_LocomotionBase.Instance;
        _tactileFeedback = Script_TactileFeedback.Instance;
        _startRot = transform.localRotation;
        if (_center)
            transform.localRotation = _centerRot.transform.localRotation;
    }

    public void ResetLever()
    {
        transform.localRotation = _centerRot.transform.localRotation;
        Release();
    }

    private void Update()
    {
        //Movement
        if (!_isInTrigger)
            return;

        if (_locBase.GetPress(_locBase.GripButton, _handside))
        {
            _isHeld = true;

            //Rotation of lever over correct axis + pulse
            var cPos = _controller.transform.position;
            if (_rotationAxis.x > 0f)
                cPos.x = transform.parent.transform.position.x;
            else if (_rotationAxis.y > 0f)
                cPos.y = transform.parent.transform.position.y;
            else if (_rotationAxis.z > 0f)
                cPos.z = transform.parent.transform.position.z;

            _tactileFeedback.SendShortVib(250, _handside);
            transform.LookAt(cPos);

            //Rotation limiter / lever in wall
            if (Vector3.Dot(-transform.parent.forward, transform.forward) <= 0)
            {
                Release();
                var lvr = transform.parent.GetComponent<Script_Lever>().IsTop;
                gameObject.transform.localRotation = lvr ? _startRot : _endRot.transform.rotation;
            }
        }

        if (_locBase.GetPressUp(_locBase.GripButton, _handside))
            Release();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isHeld)
        {
            //Check if other is player's hand
            var pickup = other.GetComponent<Script_PickUp>();
            if (pickup == null)
                return;
            //Check if hand is empty
            if (pickup.IsHoldingObject)
                return;

            //Grip the lever when hand grip is pressed
            if (_locBase.GetPress(_locBase.GripButton, pickup.Hand))
                SetTriggerHand(pickup, other);
        }
    }

    private void SetTriggerHand(Script_PickUp pickup, Collider other)
    {
        _handside = pickup.Hand;
        _isInTrigger = true;
        _controller = other.gameObject;
        _isHeld = true;
    }

    private void Release()
    {
        _isInTrigger = false;
        _handside = HandSide.None;
        _controller = null;
        _isHeld = false;
    }

    private new void OnTriggerExit(Collider other)
    {
        base.OnTriggerExit(other);

        if (other.gameObject != _controller)
            return;

        Release();
    }
}

using UnityEngine;

/// <summary>
/// A script for adding rotation and sine wave floating 'animations' to a transform.
/// </summary>
public class Script_FloatAndRotate : MonoBehaviour
{
    //Does object need to rotate
    private bool _animate = true;

    //What object to rotate
    [SerializeField]
    private Transform _target = null;
    [SerializeField]
    private Transform _parent = null;

    private Script_PickUpObject _pickUpObj = null;

    [Space(10)]
    [SerializeField]
    [Tooltip("Turn off the animation when the target is too far away (Break Distance) from the parent")]
    private bool _breakWhenTargetIsTooFar = false;

    [SerializeField]
    private float _breakDistance = 0.25f;
    [SerializeField]
    private Vector3 _axis = Vector3.zero;
    private float _angle = 0f;

    private static float InitCheckDelay = 3.5f;
    private float _checkDelayTimer = 0f;

    private Vector3 _initPosition = Vector3.zero;
    private Vector3 _initPositionParent = Vector3.zero;

    private void Start()
    {
        // Set the target to this transform if null
        if (_target == null)
            _target = transform;

        // Set the initial position and a random initial rotation
        _initPosition = _target.localPosition;
        if (_parent != null)
            _initPositionParent = _parent.position;
        _target.localRotation = Quaternion.Euler(0, Random.Range(0f, 180f), 0);

        _pickUpObj = _target.GetComponent<Script_PickUpObject>();
    }

    private void Update()
    {
        // Don't set position whilst held
        if (_pickUpObj != null && _pickUpObj.BeingHeld)
            return;

        // Ignore this if the target isn't on
        if (_target != null && !_target.gameObject.activeSelf)
            return;

        if (_breakWhenTargetIsTooFar)
        {
            // Don't do distance checks every frame
            _checkDelayTimer -= Time.deltaTime;

            if (_checkDelayTimer < 0)
            {
                // Stop animating if the rotation target is out of range
                if (_breakWhenTargetIsTooFar && (_target.position - transform.position).sqrMagnitude > _breakDistance)
                    Destroy(this);

                // Reset the delay timer
                _checkDelayTimer = _animate ? InitCheckDelay : InitCheckDelay * 3f; ;
            }
        }

        // Animate the transform
        // Float
        var wave = Mathf.Sin(Time.timeSinceLevelLoad) / 1000f;

        if (_parent != null)
            _parent.position = _initPositionParent + (_axis == Vector3.zero ? _target.up : _axis) * wave;
        else
            _target.localPosition = _initPosition + (_axis == Vector3.zero ? _target.up : _axis) * wave;

        // Rotate
        if (_axis == Vector3.zero || _axis.y != 0)
            if (_parent != null)
                _angle = _parent.eulerAngles.y;
            else
                _angle = _target.localEulerAngles.y;

        else if (_axis.x != 0)
            if (_parent != null)
                _angle = _parent.eulerAngles.x;
            else
                _angle = _target.localEulerAngles.x;

        else if (_axis.z != 0)
            if (_parent != null)
                _angle = _parent.eulerAngles.z;
            else
                _angle = _target.localEulerAngles.z;

        _angle += Time.deltaTime * 5f;

        if (_parent != null)
            _parent.rotation = Quaternion.Euler(_axis == Vector3.zero ? _angle * Vector3.up : _angle * _axis);
        else
            _target.localRotation = Quaternion.Euler(_axis == Vector3.zero ? _angle * Vector3.up : _angle * _axis);
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(_target != null ? _target.position : _parent != null ? _parent.position : null, _breakDistance * 2);
    //}
}

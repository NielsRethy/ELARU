using UnityEngine;

/// <summary>
/// A script for controlling flying camera motions through the game. (used for recording video footage of the game, disable the player and put this on a new camera)
/// </summary>
[RequireComponent(typeof(Camera))]
public class Script_CameraPan : MonoBehaviour
{
    [SerializeField]
    private float _speed = 1f;
    private float _initSpeed = 1f;
    [SerializeField]
    private float _sensitivity = 0.25f;
    private float _actSpeed = 0.0f;

    private Vector3 _lastDir = Vector3.zero;
    private Vector3 _lastMouse = Vector3.zero;
    private Vector3 _direction = Vector3.zero;

    [SerializeField]
    private bool _inverted = false;
    private bool _useMouse = true;
    private bool _mouseState = false;

    private Camera _camera = null;

    private void Start()
    {
        _initSpeed = _speed;
        // Hide mouse initiality
        ToggleMouse(_mouseState = false);
        Debug.LogWarning("There's a Script_CameraPan in the scene", gameObject);

        // Disable all other cameras
        Camera[] cameras = new Camera[0];
        Camera.GetAllCameras(cameras);
        if (cameras != null)
            foreach (Camera camera in cameras)
                camera.enabled = false;
        // Except this one
        _camera = GetComponent<Camera>();
        _camera.enabled = true;
    }

    private void ToggleMouse(bool state)
    {
        Cursor.visible = state;
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Confined;
    }

    private void Update()
    {
        // Show/hide the mouse cursor
        if (Input.GetMouseButtonDown(0))
            ToggleMouse(_mouseState = !_mouseState);

        // Reset the camera speed
        if (Input.GetKeyDown(KeyCode.R))
            _speed = _initSpeed;

        // Dash cam speed
        if (Input.GetKeyDown(KeyCode.LeftShift))
            _speed = 30f;

        // Toggle mouse look
        if (Input.GetKeyDown(KeyCode.M))
            _useMouse = !_useMouse;

        if (_useMouse)
        {
            // Calculate rotation via mouse movement
            _lastMouse = Input.mousePosition - _lastMouse;
            if (!_inverted) _lastMouse.y = -_lastMouse.y;
            _lastMouse *= _sensitivity;
            _lastMouse = new Vector3(transform.eulerAngles.x + _lastMouse.y, transform.eulerAngles.y + _lastMouse.x, 0);
            transform.eulerAngles = _lastMouse;
            _lastMouse = Input.mousePosition;
        }

        // Adjust field of view
        if (Input.GetKey(KeyCode.F))
            _camera.fieldOfView--;

        if (Input.GetKey(KeyCode.O))
            _camera.fieldOfView++;

        // Camera movement
        _direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            _direction.z += 1f;
        if (Input.GetKey(KeyCode.A))
            _direction.x -= 1f;
        if (Input.GetKey(KeyCode.S))
            _direction.z -= 1f;
        if (Input.GetKey(KeyCode.D))
            _direction.x += 1f;

        _direction.Normalize();

        // Pan according to camera forward
        if (Input.GetKey(KeyCode.Space))
            _direction += _camera.transform.forward.normalized;

        // Actually move the camera
        if (_direction != Vector3.zero)
        {
            // Move gradually
            if (_actSpeed < _speed)
                _actSpeed += _speed * (Time.deltaTime * 10f);
            else
                _actSpeed = 1.0f;

            _lastDir = _direction;
        }
        else
        {
            // Stop gradually
            if (_actSpeed > 0)
                _actSpeed -= _speed * (Time.deltaTime * 10f);
            else
                _actSpeed = 0.0f;
        }

        transform.Translate(_lastDir * _actSpeed * Time.deltaTime);
    }
}

using UnityEngine;

public class Script_Ladder : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField]
    private Animator _animator = null;

    [SerializeField]
    private bool _startOpen = true;

    private const string _closed = "Closed";
    private const string _open = "Open";
    private bool _isOpen = false;

    [Header("Effects")]
    [SerializeField]
    private Light[] _lights = null;
    [SerializeField]
    private SkinnedMeshRenderer _rendererWithLights = null;
    private const string _shaderID = "_EmissionColor";

    private void Start()
    {
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
            Debug.LogWarning("Failed to get Animator on: " + name, gameObject);
        }

        if (_animator != null && _startOpen)
            ToggleLadder();
    }

    public void ToggleLadder()
    {
        if (_animator == null)
            return;

        _animator.SetBool(_open, _isOpen = !_animator.GetBool(_open));

        // Update lights
        if (_lights != null && _lights.Length > 0)
            foreach (Light light in _lights)
                light.color = _isOpen ? Color.green : Color.red;

        // Update texture
        if (_rendererWithLights != null)
            _rendererWithLights.material.SetColor(_shaderID, _isOpen ? Color.green : Color.red);
    }

    public bool LadderIsClosed { get { return !_isOpen; } }
}

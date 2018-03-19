using UnityEngine;

/// <summary>
/// Script for lerping between two colors repeatedly
/// </summary>
[RequireComponent(typeof(Renderer))]
public class Script_ColorLerp : MonoBehaviour
{
    public bool Active = true;

    private int _materialPulseDirection = 1;
    private float _lerpProgress = 1f;
    [SerializeField]
    private int _materialID = 0;
    private Material _material = null;

    [Space(10)]
    [SerializeField]
    private Color _targetColor = Color.yellow;
    public Color TargetColor
    {
        get { return _targetColor; }
        set
        {
            _targetColor = value;
            // Reset color
            _newColor = value;
            SetColor();
        }
    }
    private Color _newColor = Color.clear;

    [SerializeField]
    private Color _initialColor = Color.clear;
    [SerializeField]
    [Tooltip("Use the shader's default color instead of the custom initial value above")]
    private bool _useShaderDefault = false;

    [Space(10)]
    [SerializeField]
    [Tooltip("Use the shader ID defined below instead of the main color")]
    private bool _useShaderID = false;
    [SerializeField]
    private string _shaderID = "_EmissionColor";

    public Texture MainTexture { get { return _material.mainTexture; } set { _material.mainTexture = value; } }

    [Space(10)]
    [SerializeField]
    [Tooltip("How far should the color lerp back into the 'initial color'")]
    [Range(0, 1)]
    private float _minRange = 0.4f;
    [SerializeField]
    [Tooltip("How far should the color lerp towards the 'target color'")]
    [Range(0, 1)]
    private float _maxRange = 1f;

    [Space(10)]
    [SerializeField]
    [Tooltip("Lerp both the shader ID and main color")]
    private bool _LerpIdAndMainColor = false;

    [SerializeField]
    private Renderer[] _addRenderers = null;
    [SerializeField]
    private string[] _addRendererShaderIDs = null;

    private void Awake()
    {
        //Get material from object
        var rnderer = GetComponent<Renderer>();
        if (rnderer == null)
            Debug.LogError("Failed to get renderer for color lerp script on " + name, gameObject);

        if (rnderer.materials.Length > 1 && _materialID <= rnderer.materials.Length)
            _material = rnderer.materials[_materialID];
        else
            _material = rnderer.material;

        // Attempt to fetch the shader ID as a color (so it throws an error for us if the shader doesn't have the ID)
        if (_useShaderID)
            _material.GetColor(_shaderID);

        // Set initial color
        if (_useShaderDefault)
            _initialColor = _material.GetColor(_shaderID);

        //Start with initial color
        _newColor = _initialColor;
        SetColor();
    }

    private void Start()
    {
        //Set color in shader
        if (_useShaderID)
            _material.SetColor(_shaderID, _initialColor);
        else
            _material.color = _initialColor;
    }

    private void Update()
    {
        if (!Active)
            return;

        // Toggle the lerp direction according to the lerp's progress
        if (_lerpProgress <= _minRange)
            _materialPulseDirection = 1;
        else if (_lerpProgress >= _maxRange)
            _materialPulseDirection = -1;

        // Lerp towards the new color and apply it
        _newColor = Color.Lerp(_initialColor, TargetColor, 1f * (_lerpProgress += Time.deltaTime * _materialPulseDirection));

        SetColor();
    }

    public void SetColor(bool overwrite = false)
    {
        //Overwrite to start color
        if (overwrite)
            _newColor = _initialColor;

        //Set color from shader id to new color
        if (_useShaderID || _LerpIdAndMainColor)
            _material.SetColor(_shaderID, _newColor);

        //Set material color to new color
        if (!_useShaderID || _LerpIdAndMainColor)
            _material.color = _newColor;

        if (_addRenderers == null || _addRenderers.Length <= 0)
            return;
        for (int i = 0; i < _addRenderers.Length; ++i)
            if (_addRenderers[i] != null)
                _addRenderers[i].material.SetColor(_addRendererShaderIDs[i], _newColor);
    }
}

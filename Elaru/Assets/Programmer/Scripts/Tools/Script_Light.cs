using System.Collections;
using UnityEngine;

/// <summary>
/// Simple script for controlling lights through events
/// </summary>
public class Script_Light : MonoBehaviour
{
    private bool _state = false;

    [SerializeField]
    private bool _startOff = true;
    [SerializeField]
    private bool _flashing = false;
    [SerializeField]
    private float _flashDelay = 1f;
    private float _flashTimer = 0f;

    [SerializeField]
    private Renderer _renderer = null;
    [SerializeField]
    private Light[] _lights = null;

    [Header("Leave color clear to use shader default")]
    [SerializeField]
    private Color _colorOn = Color.clear;
    [Space(10)]
    [SerializeField]
    private Color _colorOff = Color.red;
    private const string ShaderID = "_EmissionColor";

    private float _flickerTimer = 1f;

    private void Start()
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();

        if (_renderer == null)
            Debug.LogWarning("Failed to get mesh renderer on: " + name, gameObject);

        if (_colorOn == Color.clear)
            _colorOn = _renderer.material.color;

        SetLights(!_startOff);
    }

    private void Update()
    {
        if (_flashing)
            if (_flashTimer > 0f)
                _flashTimer -= Time.deltaTime;
            else
            {
                ToggleLights();
                _flashTimer = _flashDelay;
            }
    }

    public void ToggleFlickeringLight(float time)
    {
        ToggleLights(true, time);
    }

    public void ToggleLights(bool flicker = false, float flickerTime = 1f)
    {
        SetLights(_state = !_state);

        if (!flicker)
            return;

        _flickerTimer = flickerTime;
        StartCoroutine(Flicker());
    }

    public void SetLights(bool state)
    {
        if (_lights != null && _lights.Length > 0)
            foreach (Light light in _lights)
                light.color = state ? _colorOn : _colorOff;

        if (_renderer != null && _renderer.materials.Length > 0)
            foreach (Material material in _renderer.materials)
                material.SetColor(ShaderID, state ? _colorOn : _colorOff);
    }

    private IEnumerator Flicker()
    {
        while (_flickerTimer > 0)
        {
            _flickerTimer -= Time.deltaTime;

            SetIntensity(Random.Range(0f, 10f) / 1000f, Random.Range(1f, 10f) / 10f);

            yield return new WaitForSeconds(Random.Range(1, 10) / 1000f);
        }
    }

    private void SetIntensity(float lightIntensity = 1f, float rendererIntensity = 1f)
    {
        if (_lights != null && _lights.Length > 0)
            foreach (Light light in _lights)
            {
                light.intensity = lightIntensity;

                // Reset back to initial state if 0 has been reached
                if (_flickerTimer <= 0f)
                    light.intensity = 1f;
            }

        if (_renderer != null && _renderer.materials.Length > 0)
            foreach (Material material in _renderer.materials)
            {
                var random = rendererIntensity;
                var initMaterialColor = _state ? _colorOn : _colorOff;

                material.SetColor(ShaderID, Color.Lerp(initMaterialColor, Color.black, random));

                // Reset back to initial state if 0 has been reached
                if (_flickerTimer <= 0f)
                    material.SetColor(ShaderID, _state ? _colorOn : _colorOff);
            }
    }
}

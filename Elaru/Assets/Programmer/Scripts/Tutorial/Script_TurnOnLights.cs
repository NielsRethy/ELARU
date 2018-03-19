using System;
using System.Collections.Generic;
using UnityEngine;

public class Script_TurnOnLights : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> _lights = new List<GameObject>();
    private List<Material> _mats = new List<Material>();
    private List<Light> _lightChildren = new List<Light>();
    private Color _colorOn = Color.green;
    private const string SoundLights = "SpotLightToggle";

    private void Start()
    {
        //Cache lights materials and turn them off
        foreach (GameObject l in _lights)
        {
            _mats.Add(l.GetComponent<MeshRenderer>().materials[1]);
            l.transform.GetChild(0).gameObject.SetActive(false);
            _lightChildren.Add(l.transform.GetChild(0).GetComponent<Light>());
        }

        _colorOn = _lightChildren[0].color;

        ResetLamps();
    }

    public void TurnOnLights()
    {
        //Make lights shine red
        foreach (Material mat in _mats)
        {
            mat.color = _colorOn;
            mat.SetColor("_EmissionColor", _colorOn);
        }

        //Enable light game objects
        ToggleLight(true);

        foreach (Light l in _lightChildren)
            l.color = _colorOn;
    }

    private void ToggleLight(bool state)
    {
        foreach (GameObject l in _lights)
        {
            l.transform.GetChild(0).gameObject.SetActive(state);

            //Play sound at light position
            if (Time.timeSinceLevelLoad > 0.1f)
                Script_AudioManager.Instance.PlaySFX(SoundLights, l.transform.position);
        }
    }

    public void ResetLamps()
    {
        //Turn off lights
        foreach (Material mt in _mats)
        {
            mt.color = Color.white;
            mt.SetColor("_EmissionColor", Color.black);
        }

        //Enable light game objects
        ToggleLight(false);
    }
}

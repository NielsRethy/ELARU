using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_OpenFence : MonoBehaviour
{
    [SerializeField]
    private float _speed = 0.1f;
    [SerializeField]
    private float _openDelay = 0.75f;

    private bool _openDoor = false;
    //private float _timer = 0.0f;
    private Quaternion _startRot = Quaternion.identity;

    [SerializeField]
    private GameObject _endRot = null;
    [SerializeField]
    private List<GameObject> _lights = new List<GameObject>();
    [SerializeField]
    private GameObject _mat = null;
    [SerializeField]
    private bool _override = false;


    private void Start()
    {
        //Set variables
        _startRot = transform.rotation;
        _openDoor = _override;
        SetColor(_openDoor);
    }

    private void Update()
    {

        //Rotate fence
        if (_openDoor)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, _endRot.transform.rotation, _speed * Time.deltaTime);
            //_timer += Time.deltaTime;
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, _startRot, _speed * Time.deltaTime);
        }

        ////Disable door when fully open
        //if (_timer > 5.0f)
        //{
        //    _timer = 0.0f;
        //    _openDoor = false;
        //    enabled = false;
        //}
    }

    /// <summary>
    /// Call this to open the door (pref in action)
    /// </summary>

    private void SetColor(bool isGreen)
    {
        var rnderer = _mat.GetComponent<SkinnedMeshRenderer>();
        if (!rnderer)
            return;

        if (isGreen)
        {
            rnderer.material.SetColor("_EmissionColor", Color.green);
            foreach (GameObject light in _lights)
            {
                light.GetComponent<Light>().color = Color.green;
            }
        }
        else
        {
            rnderer.material.SetColor("_EmissionColor", Color.red);
            foreach (GameObject light in _lights)
            {
                light.GetComponent<Light>().color = Color.red;
            }
        }
    }
    public void Open()
    {
        StartCoroutine(OpenClose(true));
        SetColor(true);
    }

    private IEnumerator OpenClose(bool onOff)
    {
        yield return new WaitForSeconds(_openDelay);
        _openDoor = onOff;
        Script_AudioManager.Instance.PlaySFX("Door", transform.position);
    }

    public void Close()
    {
        StartCoroutine(OpenClose(false));
        SetColor(false);
    }
}

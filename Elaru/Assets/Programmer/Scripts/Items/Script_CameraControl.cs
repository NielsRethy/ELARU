using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_CameraControl : MonoBehaviour {
    [SerializeField]
    private Script_Camera _camScript = null;
    [SerializeField]
    private Script_CameraPuzzle _camScript2 = null;
    [SerializeField]
    private GameObject _lightLamp = null;
    [SerializeField]
    private GameObject _startPos = null;
    [SerializeField]
    private GameObject _downPos = null;
    [SerializeField]
    private GameObject _cameraHead = null;

    private bool _deactivate = false;

    public void TurnOffCamera()
    {
        if (_camScript != null) Destroy(_camScript);
        else Destroy(_camScript2);
        _deactivate = true;

        //turn off light
        _lightLamp.SetActive(false);
        //turn off glow (eye)

        //play turnoff sound
    }

    private void Update()
    {
        if (_deactivate)
        {
            var targetPoint = _downPos.transform.position;
            var targetRotation = Quaternion.LookRotation(targetPoint - _cameraHead.transform.position, Vector3.up);
            _cameraHead.transform.rotation = Quaternion.Lerp(_cameraHead.transform.rotation, targetRotation, Time.deltaTime * 0.5f);
            var vec = (targetPoint - _cameraHead.transform.position);
            if (Vector3.Angle(_cameraHead.transform.forward, vec) < 0.1f) _deactivate=false;
        }
    }
}

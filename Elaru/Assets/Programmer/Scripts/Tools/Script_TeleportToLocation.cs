using UnityEngine;

public class Script_TeleportToLocation : MonoBehaviour
{
    [SerializeField]
    private Transform[] _transforms = new Transform[9];

    [SerializeField]
    private Transform[] _transforms2 = new Transform[0];
    private int _transforms2Index = 0;
    [SerializeField]
    private string _buttonTransforms2 = "n";

    private Transform _player = null;

    private void Start()
    {
        _player = Script_LocomotionBase.Instance.CameraRig.transform;
    }

    private void Update()
    {
        for (int i = 0; i < 9; ++i)
            if (Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                Script_LocomotionBase.Instance.ScriptLocomotionDash.ForceStopDash();
                _player.position = _transforms[i].position + Vector3.up;
            }

        if (Input.GetKeyDown(_buttonTransforms2))
        //_transforms2Index = ++_transforms2Index % _transforms2.Length;
        {
            Script_LocomotionBase.Instance.ScriptLocomotionDash.ForceStopDash();
            _player.position = _transforms2[++_transforms2Index % _transforms2.Length].position;
        }
    }
}

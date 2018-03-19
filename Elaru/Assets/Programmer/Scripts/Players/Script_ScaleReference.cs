using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script that was used to measure heights using raycasts.
/// Might be useful for the artists still?
/// </summary>
[System.Obsolete("Script is only used for measuring -> no use in game")]
public class Script_ScaleReference : MonoBehaviour
{
    private Script_LocomotionBase _base = null;
    [SerializeField]
    private Text _textMonitor = null;
    [SerializeField]
    private Text _textEye = null;

    private void Start()
    {
        _base = Script_LocomotionBase.Instance;
    }

#if UNITY_EDITOR
    void Update()
    {
        // Get controller height from the ground
        if (_base.RightController != null)
        {
            string height = "C_height: " + _base.RightControllerTrObj.transform.position.y.ToString("0.00") + "(m)";
            _textMonitor.text = height;
            _textEye.text = height;
        }

        // Get raycast hit position height
        if (_base.GetRightPress(_base.TriggerButton))
        {
            RaycastHit hit2;
            if (Physics.Raycast(transform.position, transform.forward, out hit2))
            {
                RaycastHit hit3;
                if (Physics.Raycast(hit2.point, -Vector3.up, out hit3, Mathf.Infinity))
                {
                    if (hit3.point.y - hit2.point.y > 0f)
                    {
                        string rayHitHeight = "\nR_height: " + hit3.distance.ToString("0.00") + "(m)";
                        _textMonitor.text += rayHitHeight;
                        _textEye.text += rayHitHeight;
                    }
                }
            }
        }
    }
#endif
}

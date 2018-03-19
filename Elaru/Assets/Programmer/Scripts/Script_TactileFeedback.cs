using System.Collections;
using UnityEngine;

public class Script_TactileFeedback : Script_Singleton<Script_TactileFeedback>
{
    private Script_LocomotionBase _base = null;

    void Awake()
    {
        //Cache variables
        _base = Script_LocomotionBase.Instance;
    }

    /// <summary>
    /// Call this every frame to send a long vibration to the controller
    /// </summary>
    public void SendLongVibration(float length, float strength, HandSide hand)
    {
        //Send vibration to correct hand's device
        switch (hand)
        {
            case HandSide.Left:
                if (_base.LeftController != null && _base.LeftControllerTrObj.gameObject.activeSelf)
                    StartCoroutine(LongVib(length, strength, _base.LeftController));
                break;
            case HandSide.Right:
                if (_base.RightController != null && _base.RightControllerTrObj.gameObject.activeSelf)
                    StartCoroutine(LongVib(length, strength, _base.RightController));
                break;
        }
    }

    private IEnumerator LongVib(float length, float strength, SteamVR_Controller.Device controller)
    {
        for (float i = 0; i < length; i += Time.deltaTime)
        {
            controller.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
            yield return null;
        }
    }

    /// <summary>
    /// Call this every frame to send a small vibration to the called controller
    /// </summary>
    public void SendShortVib(ushort duration, HandSide hand)
    {
        switch (hand)
        {
            case HandSide.Left:
                if (_base.LeftController != null && _base.LeftControllerTrObj.gameObject.activeSelf)
                    _base.LeftController.TriggerHapticPulse(duration);
                break;
            case HandSide.Right:
                if (_base.RightController != null && _base.RightControllerTrObj.gameObject.activeSelf)
                    _base.RightController.TriggerHapticPulse(duration);
                break;
        }
    }
}

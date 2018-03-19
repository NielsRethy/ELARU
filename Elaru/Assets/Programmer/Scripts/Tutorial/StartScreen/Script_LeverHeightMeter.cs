using System.Collections;
using UnityEngine;

[System.Obsolete]
public class Script_LeverHeightMeter : Script_Lever
{
    private Script_PlayerInformation _scriptPlayer = null;
    private Script_LocomotionBase _scriptLoco = null;
    private Script_Locomotion_TeleDash _scriptDash = null;
    private Script_LocomotionGrab _scriptGrab = null;

    private Script_LeverHandTracking _scriptHandTracking = null;

    private bool _invokeOnce = false;
    [SerializeField]
    private Transform _teleportLocation = null;
    private Coroutine _coroutine;
    private const float FadeTime = 1f;

    private void Awake()
    {
        //Cache singletons
        _scriptPlayer = Script_PlayerInformation.Instance;
        _scriptLoco = Script_LocomotionBase.Instance;
    }

    private void Start()
    {
        _scriptDash = _scriptLoco.ScriptLocomotionDash;
        _scriptGrab = _scriptLoco.ScriptLocomotionGrab;

        _scriptHandTracking = GetComponentInChildren<Script_LeverHandTracking>();
    }

    public override void BotTrigger()
    {
        StopAllCoroutines();
        IsTop = false;
        StartCoroutine(ReleaseGrip());
    }

    public override void TopTrigger()
    {
        StopAllCoroutines();
        IsTop = true;
        StartCoroutine(ReleaseGrip());
    }

    private IEnumerator ReleaseGrip()
    {
        while ((_scriptHandTracking.HandInPickUp == HandSide.Right && _scriptLoco.GetRightPress(_scriptLoco.GripButton)) ||
               (_scriptHandTracking.HandInPickUp == HandSide.Left && _scriptLoco.GetLeftPress(_scriptLoco.GripButton)))
        {
            yield return null;
        }


        // Wait for lever to be pulled down
        if (IsTop)
        {
            Application.Quit();
        }
        else
        {
            ToggleLocomotion(false);
            SteamVR_Fade.View(Color.black, FadeTime);
            Invoke("TeleportPlayer", FadeTime);
        }
        yield break
            ;
    }

    private void ToggleLocomotion(bool state)
    {
        _scriptDash.enabled = state;
        _scriptGrab.enabled = state;
    }

    private void TeleportPlayer()
    {
        // Store the player's new height
        _scriptLoco.CameraRig.transform.position = _teleportLocation.position;
        ToggleLocomotion(true);

        SteamVR_Fade.View(Color.clear, FadeTime);
        //Script_SaveFileManager.SaveData.SetHeight(ControllerPosY, ControllerPosY / 2f);
    }
}

using UnityEngine;

/// <summary>
/// A script for allowing the player to skip the loading screen tutorial
/// </summary>
public class Script_LoadOnClick : MonoBehaviour
{
    private Script_Tutorial _scriptTut = null;
    private Script_PickUpEffects _scriptEffects = null;
    private Script_LocomotionBase _scriptLoco = null;
    private const float FadeTime = 1f;

    private void Start()
    {
        _scriptTut = FindObjectOfType<Script_Tutorial>();
        _scriptEffects = GetComponent<Script_PickUpEffects>();
        _scriptLoco = Script_LocomotionBase.Instance;
    }

    private void Update()
    {
        if (_scriptTut.Async == null)
            return;

        // If the player grips the effects trigger
        if (_scriptEffects.Hover && _scriptLoco.GetPressDown(_scriptLoco.GripButton, _scriptEffects.HandInPickUp))
        {
            // Play sound
            _scriptEffects.Pressed = true;
            _scriptEffects.enabled = false;
            // Allow the city to load
            SteamVR_Fade.View(Color.black, FadeTime);
            Invoke("LoadCityScene", FadeTime);
        }
    }

    private void LoadCityScene()
    {
        _scriptTut.AllowSceneActivation();
    }
}

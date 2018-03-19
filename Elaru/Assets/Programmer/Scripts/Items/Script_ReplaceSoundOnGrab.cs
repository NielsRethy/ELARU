using UnityEngine;

[RequireComponent(typeof(Script_PickUpEffects))]
[RequireComponent(typeof(Script_PickUpObject))]
public class Script_ReplaceSoundOnGrab : MonoBehaviour
{
    private Script_PickUpEffects _scriptEffects = null;
    private Script_PickUpObject _scriptObject = null;
    [Header("Replacement sounds")]
    public string ReplacementHover = "PickupHover";
    public string ReplacementPressed = "PickupPressed";
    public string ReplacementRelease = "PickupRelease";

    private void Start()
    {
        _scriptEffects = GetComponent<Script_PickUpEffects>();
        _scriptObject = GetComponent<Script_PickUpObject>();
        _scriptObject.OnGrab += ReplaceSound;
    }

    private void ReplaceSound(GameObject obj)
    {
        if (ReplacementHover.Length > 0)
            _scriptEffects.SoundEffectOnHover = ReplacementHover;

        if (ReplacementPressed.Length > 0)
            _scriptEffects.SoundEffectOnPressed = ReplacementPressed;

        if (ReplacementRelease.Length > 0)
            _scriptEffects.SoundEffectOnPressed = ReplacementRelease;

        _scriptObject.OnGrab -= ReplaceSound;
    }
}

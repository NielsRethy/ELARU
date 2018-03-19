using UnityEngine;

/// <summary>
/// A replacement script for pick up object. For props with limited interactibility
/// </summary>
[RequireComponent(typeof(Script_PickUpEffects))]
public class Script_PropSound : MonoBehaviour
{
    private Script_PickUpEffects _scriptEffects = null;
    private Script_LocomotionBase _scriptLoco = null;

    private Script_PickUp _pickUpFromHand = null;
    private bool _wasInTrigger = false;
    private void Start()
    {
        _scriptEffects = GetComponent<Script_PickUpEffects>();
        _scriptLoco = Script_LocomotionBase.Instance;
    }

    private void Update()
    {
        if (_scriptEffects.Hover)
        {
            _wasInTrigger = true;

            if (_scriptLoco.GetPressDown(_scriptLoco.GripButton, _scriptEffects.HandInPickUp))
                ShowController(false);
            else if (_scriptLoco.GetPressUp(_scriptLoco.GripButton, _scriptEffects.HandInPickUp))
                ShowController(true);

            return;
        }
        else if (_wasInTrigger)
        {
            ShowController(true);
            _wasInTrigger = false;
        }
    }

    private void ShowController(bool state)
    {
        if (_pickUpFromHand == null)
            _pickUpFromHand = _scriptLoco.GetPickUpFromHand(_scriptEffects.HandInPickUp);

        _pickUpFromHand.HideHands(!state);
        _scriptEffects.Pressed = !state;
    }
}

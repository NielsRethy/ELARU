using UnityEngine;

/// <summary>
/// A script for animating the hand models of the player
/// </summary>
[RequireComponent(typeof(Animator))]
public class Script_HandAnimation : MonoBehaviour
{
    private HandSide _hand;
    private Animator _animator = null;
    private Script_LocomotionBase _scriptLoco = null;
    private Script_Locomotion_TeleDash _scriptDash = null;

    private const string GripParam = "Grip";
    private const string TriggerParam = "Trigger";
    private const string TouchParam = "Touchpad";

    private Renderer _renderer = null;
    public Material LightMaterial { get { return _renderer.materials[0]; } }
    private const string ShaderID = "_EmissionColor";

    private float _timer = 0f;
    private const float TimerDelay = 2f;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        if (_animator == null)
            Debug.Log("Failed to get hand animator on: " + name);

        _scriptLoco = Script_LocomotionBase.Instance;
        _hand = _scriptLoco.GetHandSideFromObject(transform.parent.gameObject);
        _scriptDash = _scriptLoco.ScriptLocomotionDash;

        _renderer = GetComponentInChildren<Renderer>();
    }

    private void Update()
    {
        // Animate hand model grip open
        if (_animator != null && _animator.enabled)
        {
            // Update animator via controller input
            _animator.SetFloat(GripParam, _scriptLoco.GetPress(_scriptLoco.GripButton, _hand) ? 1f : 0f);
            _animator.SetFloat(TriggerParam, _scriptLoco.GetHairTrigger(_hand));
            _animator.SetFloat(TouchParam, _scriptLoco.GetPress(_scriptLoco.TouchPad, _hand) ? 1f : 0f);
        }
        else
            Debug.LogWarning("No animator set!");

        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
            return;
        }
        else
            _timer = TimerDelay;

        // Update lights on hand
        LightMaterial.SetColor(ShaderID, _scriptDash.TryingToDash ? (_scriptDash.CanDash ? Color.green : Color.red) : Color.clear);
    }
}

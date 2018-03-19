using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Script_ExitOnTrigger : MonoBehaviour
{
    [SerializeField]
    private bool _exit = false;
    private Script_LocomotionBase _scriptLoco = null;
    private Collider _thisCollider = null;
    private const float FadeTime = 1f;
    private bool _alreadyExiting = false;

    [SerializeField]
    private Transform _teleportLocation;

    private void Awake()
    {
        _scriptLoco = Script_LocomotionBase.Instance;
        _thisCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other != _scriptLoco.PlayerCollider)
        {
            Physics.IgnoreCollision(other, _thisCollider);
            return;
        }

        // Fade out
        SteamVR_Fade.View(Color.black, FadeTime);

        // Turn off all locomotion
        //_scriptLoco.ToggleLocomotionAndFading(false);

        Invoke("Exit", FadeTime);
        _alreadyExiting = true;
    }

    private void Exit()
    {
        if (_exit)
        {
            Script_SaveFileManager.Instance.SaveSceneData();
            Invoke("Quit", FadeTime);
            return;
        }
        else
        {
            Script_SaveFileManager.Instance.SaveSceneData(true);
            Script_PlayerInformation.Instance.EnableCity(true);

            //Teleport player
            _scriptLoco.CameraRig.transform.position = _teleportLocation.position;
            _scriptLoco.ScriptLocomotionDash.OverrideSafePlace(_teleportLocation.position);

            //Stop teleport unlock player
            _scriptLoco.ScriptLocomotionDash.ForceStopDash();
            _scriptLoco.ScriptLocomotionDash.LockRegion(false, null, 0f, true);
        }

        Invoke("FadeIn", FadeTime);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#if DEBUG
        Debug.Log("Quit game");
#endif
#endif
        Application.Quit();
    }

    private void UnlockPlayer()
    {
        _scriptLoco.ToggleLocomotionAndFading(true);
        Invoke("FadeIn", FadeTime);
    }

    private void FadeIn()
    {
        SteamVR_Fade.View(Color.clear, FadeTime);
        _alreadyExiting = false;
    }
}

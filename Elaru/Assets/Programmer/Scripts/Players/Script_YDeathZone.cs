using UnityEngine;

/// <summary>
/// Script for preventing the player from falling off the world
/// </summary>
public class Script_YDeathZone : MonoBehaviour
{
    private Collider _player = null;
    private Transform _cameraRig = null;
    private Script_LocomotionBase _scriptLoco = null;
    private Script_PlayerCollisionCheck _scriptCollision = null;

    private RaycastHit hit;

    private void Awake()
    {
        _scriptLoco = Script_LocomotionBase.Instance;
    }

    private void Start()
    {
        _player = _scriptLoco.PlayerCollider;
        _cameraRig = _scriptLoco.CameraRig.transform;
        _scriptCollision = _scriptLoco.ScriptCollisionCheck;
    }

    private void LateUpdate()
    {
        if (_cameraRig.position.y > transform.position.y)
            return;

        // Quick fade screen
        _scriptCollision.Active = false;
        SteamVR_Fade.View(Color.black, 0.05f);

        // Attempt to find the player's previous postion by raycasting from above
        if (Physics.Raycast(new Vector3(_player.transform.position.x, 10f, _player.transform.position.z), Vector3.down, out hit))
        {
            _scriptLoco.CameraRig.transform.position = hit.point;
            ClearFade();
            return;
        }

        // Reset position to last known safe zone
        _scriptLoco.CameraRig.transform.position = Script_PlayerCollisionCheck.TransformSafeZone;

        // Clear fade
        ClearFade();
    }

    private void ClearFade()
    {
        // Clear fade
        SteamVR_Fade.View(Color.clear, Script_PlayerCollisionCheck.FadeOutTime);

        // Enable collision after fade
        Invoke("EnableCollision", Script_PlayerCollisionCheck.FadeOutTime);
    }

    private void EnableCollision()
    {
        _scriptCollision.Active = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireCube(transform.position, new Vector3(100f, 0.1f, 100f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 1f);
        Gizmos.DrawWireCube(transform.position, new Vector3(100f, 0.1f, 100f));
    }
}

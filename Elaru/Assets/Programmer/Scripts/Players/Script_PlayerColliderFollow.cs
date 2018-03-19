using UnityEngine;

/// <summary>
/// Script used to match the x and z position of a transform to the main camera position (camera (eye) on camera rig)
/// </summary>
[RequireComponent(typeof(Collider))]
public class Script_PlayerColliderFollow : MonoBehaviour
{
    private BoxCollider _collider = null;

    [SerializeField]
    private bool _debugForwardRay = false;

    private void Start()
    {
        // Fetch the collider
        _collider = GetComponent<BoxCollider>();
        if (_collider == null)
            Debug.LogWarning("Failed to get box collider on " + name);

        // Fetch the player height
        var height = Script_PlayerInformation.Instance.PlayerHeight;
        // Default to 1m
        if (height <= 0f)
            height = 1f;

        SetColliderHeight(height);
    }

    public void SetColliderHeight(float height)
    {
        if (_collider == null)
            return;

        // Set collider height to player height
        _collider.size = new Vector3(_collider.size.x, height, _collider.size.z);
        _collider.center = new Vector3(0f, height / 2f, 0f);
    }

    private void Update()
    {
        if (Camera.main == null)
            return;

        // Match the position of the player's HMD (ignore the height)
        transform.localPosition = new Vector3(Camera.main.transform.localPosition.x, 0f, Camera.main.transform.localPosition.z);

        // Match the orientation as well (y axis only)
        transform.localEulerAngles = new Vector3(0f, Camera.main.transform.localEulerAngles.y, 0f);

        if (_debugForwardRay)
            Debug.DrawRay(transform.position, transform.forward);
    }
}

using UnityEngine;

/// <summary>
/// Script used for pointing a TV monitor at the player, currently unused.
/// Might be useful if we add anything that needs to follow the player again.
/// </summary>
[System.Obsolete()]
public class Script_LookAt : MonoBehaviour
{
    [SerializeField]
    private Transform _target = null;
    [SerializeField]
    private Vector3 _parentRotation = Vector3.zero;

    private void LateUpdate()
    {
        Quaternion lookRotation = Quaternion.LookRotation(_target.position - transform.position, Vector3.up);
        transform.localEulerAngles = new Vector3(0f, -lookRotation.eulerAngles.y - _parentRotation.y, 0f);
    }
}

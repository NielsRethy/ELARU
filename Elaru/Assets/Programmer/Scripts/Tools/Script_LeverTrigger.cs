using UnityEngine;

/// <summary>
/// Script to check if lever entered trigger zone
/// </summary>
public class Script_LeverTrigger : MonoBehaviour
{
    [SerializeField]
    private bool _isTop = false;

    [Header("Optional")]
    [SerializeField]
    private Script_Lever _leverScr = null;

    private void Start()
    {
        if (_leverScr == null)
            _leverScr = transform.parent.GetComponent<Script_Lever>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Call trigger event on attached lever
        if (other.tag.Equals("Lever"))
            _leverScr.CallTrigger(_isTop);
    }
}

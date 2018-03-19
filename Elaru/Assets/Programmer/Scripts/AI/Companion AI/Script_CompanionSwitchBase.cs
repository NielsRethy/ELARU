using UnityEngine;

public class Script_CompanionSwitchBase : MonoBehaviour
{

    [SerializeField] private Script_CompanionAI _companion = null;
    private static bool _isActive = true;
    private static bool _isAlive = false;

    public static bool IsActive
    {
        get
        {
            if (_isAlive)
            {
                return _isActive;
            }
            return false;
        }
    }

    // Use this for initialization
    void Start()
    {
        _isAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsActive && Script_PlayerInformation.Instance.IsInBase)
        {
            _isActive = false;
            _companion.SetDeActive();
        }
        else if (!IsActive && !Script_PlayerInformation.Instance.IsInBase)
        {
            _isActive = true;
            _companion.gameObject.SetActive(true);
            _companion.SetActived();
        }
    }
}

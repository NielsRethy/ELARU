using UnityEngine;

public class Script_CombatLocomotion : MonoBehaviour
{
    Script_Locomotion_TeleDash _loco;
    bool _isLocked = false;
    private void Start()
    {
        _loco = Script_LocomotionBase.Instance.ScriptLocomotionDash;
    }

    private void Update()
    {
        //Check if player should be locked by combat
        //float dis = 0f;
        //bool isLock = Script_ManagerEnemy.Instance.GetLocked(out dis, transform.position);
        //dis *= 1.1f;
        //if (_isLocked != isLock)
        //{
        //    _loco.LockRegion(isLock, transform.position, dis);
        //    _isLocked = isLock;
        //}
        //else if (isLock)
        //{
        //    _loco.LockRegion(isLock, transform.position, dis);
        //}
    }
}

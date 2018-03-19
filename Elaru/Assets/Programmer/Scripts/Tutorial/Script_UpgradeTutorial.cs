using System.Collections;
using UnityEngine;

public class Script_UpgradeTutorial : MonoBehaviour
{
    private Script_TutorialManager _mng = null;
    private Script_LocomotionBase _base = null;
    //Upgrade station in base
    [SerializeField]
    private Script_UpgradeStation _upgradeStation = null;
    //Companion in base
    [SerializeField]
    private Script_CompanionTutorial _companion = null;

    [SerializeField]
    private Script_SplineController _splineCTRL = null;

    private bool _ivoked = false;

    private void Start()
    {
        //Cache needed components
        _mng = GetComponent<Script_TutorialManager>();
        _base = Script_LocomotionBase.Instance;
    }

    public void LockLocomotion(bool lockLoc)
    {
        //Lock player to area around upgrade station
        var lockPos = _upgradeStation.transform.position;
        lockPos.y = _base.CameraRig.transform.position.y;
        _base.ScriptLocomotionDash.LockRegion(lockLoc, lockPos, 6f);
    }

    public void CheckForUpgradeCycle()
    {
        //Subscribe to cycle complete event of upgrade station to check if tutorial is finished
        _upgradeStation.OnCycleComplete += CycleComplete;
    }

    private void CycleComplete()
    {
        if (_upgradeStation.OnCycleComplete != null)
            _upgradeStation.OnCycleComplete -= CycleComplete;

        //Finish tutorial
        _mng.SetType(TutorialType.Finish);
    }

    public void CompanionShowMap()
    {
        //Let companion show map after he went over the path
        Invoke("ShowMap", 6f);
    }

    public void ShowMap()
    {
        //Show map
        //Debug.Log("Show map");
        if (_companion != null)
            _companion.ShowMiniMap(true);

        //Close map after short showing
        Invoke("CloseMap", 5f);
    }

    public void DeactivateRobot(GameObject robot)
    {
        if (robot != null)
            robot.SetActive(false);
    }

    private void CloseMap()
    {
        //Close map
        //Debug.Log("Close map");
        if (_companion != null)
            _companion.ShowMiniMap(false);

        //Enable locomotion again
        _base.ScriptLocomotionDash.LockRegion(false);

        //Replace tutorial companion with actual companion
        _companion.GetComponent<Script_ReplaceCompnanion>().ReplaceObject();
    }

    private void Update()
    {
        if (_mng.Type == TutorialType.None && (_base.CameraRig.transform.position - _upgradeStation.transform.position).sqrMagnitude <= 5f * 5f)
            _mng.SetType(TutorialType.Start);
    }

    public void SetNewPath(GameObject path)
    {
        if (!_ivoked)
            StartCoroutine(SetPath(path));
    }

    private IEnumerator SetPath(GameObject path)
    {
        _ivoked = true;
        yield return new WaitForSeconds(10.0f);
        _splineCTRL.SplineRoot = path;
        _splineCTRL.Duration = 12;
        _splineCTRL.StartAgain();
    }
}

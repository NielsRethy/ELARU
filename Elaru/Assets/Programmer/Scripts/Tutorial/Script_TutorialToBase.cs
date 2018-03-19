using System.Collections;
using UnityEngine;

public class Script_TutorialToBase : MonoBehaviour
{
    private Script_TutorialManager _mng = null;
    private Script_LocomotionBase _base = null;

    private float _timer = 0.0f;
    private const float TimerMax = 6.0f;

    //Distance player needs to be from end pos to finish
    [SerializeField] private float _maxDis = 2.0f;
    //Location player needs to reach at the end
    [SerializeField] private GameObject _endPos = null;
    //Companion spline controller
    [SerializeField] private Script_SplineController _splineCTRL = null;
    //Cutscene robot
    [SerializeField] private Script_DeafAI _deafRobot = null;

    private GameObject _path;
    private bool _rotate;

    private void Start()
    {
        //Cache needed components
        _mng = GetComponent<Script_TutorialManager>();
        _base = Script_LocomotionBase.Instance;
        //_deafRobot.Disable();
    }

    public void SetNewPath(GameObject path)
    {
        StartCoroutine(SetPath(path));
    }
    private IEnumerator SetPath(GameObject path)
    {
        yield return new WaitForSeconds(0.35f);
        _path = path;
        _rotate = true;


    }

    public void LockLoco(bool onOff)
    {
        _base.ScriptLocomotionDash.LockRegion(onOff, this.transform.position);
    }

    void Update()
    {
        switch (_mng.Type)
        {
            case TutorialType.Start:
                _timer += Time.deltaTime;
                if (_timer > TimerMax)
                {
                    _timer = 0.0f;
                    _mng.SetType(TutorialType.QuestObj);
                }
                break;

            case TutorialType.QuestObj:
                //Finish tutorial if player reached end position
                var pos = _base.CameraRig.transform.position;
                if ((_endPos.transform.position - pos).sqrMagnitude <= _maxDis * _maxDis)
                    _mng.SetType(TutorialType.Finish);
                
                break;
        }

        if (_rotate)
        {
            //  _splineCTRLNewRobot.enabled = false;
            // _splineCTRLNewRobot.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = true;
            Vector3 lTargetDir = _path.transform.GetChild(1).position - _splineCTRL.gameObject.transform.position;
            lTargetDir.y = 0.0f;
            _splineCTRL.gameObject.transform.rotation = Quaternion.RotateTowards(_splineCTRL.gameObject.transform.rotation, Quaternion.LookRotation(lTargetDir), Time.deltaTime * 100);

            if ((_splineCTRL.gameObject.transform.eulerAngles - Quaternion.LookRotation(lTargetDir).eulerAngles).magnitude < 0.4f * 0.4f)
            {
                _splineCTRL.SplineRoot = _path;
                _splineCTRL.StartAgain();
                _splineCTRL.Duration = 12;
                _rotate = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Start tutorial when player enters range
        if (other.tag == "Player")
        {
            _mng.SetType(TutorialType.Start);
            Destroy(GetComponent<Collider>());
        }
    }
}

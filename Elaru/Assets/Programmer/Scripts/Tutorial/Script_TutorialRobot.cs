using UnityEngine;

public class Script_TutorialRobot : MonoBehaviour
{
    private Script_TutorialManager _mng = null;
    private Script_LocomotionBase _base = null;

    private float _timer = 0.0f;
    [SerializeField] private float _timerMax = 2.0f;
    [SerializeField] private GameObject path1 = null;
    [SerializeField] private GameObject path2 = null;

    [SerializeField] private GameObject _robotPos = null;
    [SerializeField] private float _maxDis = 2.0f;


    void Start()
    {
        //Cache needed components
        _mng = transform.GetComponent<Script_TutorialManager>();
        _base = Script_LocomotionBase.Instance;
    }


    void Update()
    {
        switch (_mng.Type)
        {
            case TutorialType.None:
                //start tutorial if player is in range
                var plPos = _base.CameraRig.transform.position;
                if ((_robotPos.transform.position - plPos).sqrMagnitude <= _maxDis * _maxDis)
                    _mng.SetType(TutorialType.Start);
                break;

            case TutorialType.Start:
                //Check if companion has been fixed yet -> switch to next stage
                if (_robotPos.GetComponent<Script_CompanionTutorial>().IsFixed)
                    _mng.SetType(TutorialType.QuestObj);
                break;

            case TutorialType.QuestObj:
                //Switch state to finish after a while
                _timer += Time.deltaTime;
                if (_timer > _timerMax)
                {
                    _timer = 0.0f;
                    _mng.SetType(TutorialType.Finish);
                }
                break;
        }
    }

    public void CompleteQuest()
    {
        Debug.Log("Quest Completed.");
    }
}

using UnityEngine;

public class Script_TutorialSound : MonoBehaviour
{
    private Script_TutorialManager _mng = null;
    private Script_LocomotionBase _base = null;

    [SerializeField] private GameObject _flyingRobot = null;

    private float _timer = 0.0f;
    private const float TimerMax = 10.0f;

    private void Start()
    {
        //Cache needed components
        _mng = GetComponent<Script_TutorialManager>();
        _base = Script_LocomotionBase.Instance;
    }


    void Update()
    {
        //When tutorial started start checking for objective after a while
        if (_mng.Type == TutorialType.Start)
        {
            _timer += Time.deltaTime;
            if (_timer > TimerMax)
            {
                _timer = 0.0f;
                _mng.SetType(TutorialType.QuestObj);
            }
        }
    }

    public void LockLoco(bool onOff)
    {
        _base.ScriptLocomotionDash.LockRegion(onOff, transform.position, 7.0f);
    }

    public void TurnOnRobot()
    {
        _flyingRobot.gameObject.SetActive(true);
    }

    public void Finished()
    {
        _mng.SetType(TutorialType.Finish);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Start quest when player enters trigger
        if (other.tag == "Player")
        {
            _mng.SetType(TutorialType.Start);
            Destroy(GetComponent<Collider>());
        }
    }
}

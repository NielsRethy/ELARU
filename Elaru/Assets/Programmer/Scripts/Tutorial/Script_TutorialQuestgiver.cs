using UnityEngine;

public class Script_TutorialQuestgiver : MonoBehaviour
{
    private Script_TutorialManager _mng = null;
    private Script_LocomotionBase _base = null;

    private Script_QuestGiver _questGiver = null;
    [SerializeField]
    private GameObject _questGiverObj = null;

    private void Start()
    {
        //Cache needed components
        _mng = GetComponent<Script_TutorialManager>();
        _base = Script_LocomotionBase.Instance;
        _questGiver = _questGiverObj.GetComponent<Script_QuestGiver>();
        _questGiverObj.SetActive(false);
    }

    public void ActivateTutorial()
    {
        _mng.SetType(TutorialType.Start);
    }

    public void TurnOnScreens()
    {
        _questGiverObj.SetActive(true);
    }

    public void CheckQuestAcception()
    {
        //Check if player has accepted the quest yet
        if (_questGiver.IsQuestActive())
            _mng.SetType(TutorialType.Finish);
    }

    private void Update()
    {
        CheckQuestAcception();
    }

    public void LockLocomotion(bool lockLoc)
    {
        //Lock player to radius around quest giver
        var lockPos = _questGiver.gameObject.transform.position;
        lockPos.y = _base.CameraRig.transform.position.y;
        _base.ScriptLocomotionDash.LockRegion(lockLoc, lockPos, 3f);
    }

    private void OnTriggerEnter(Collider other)
    {
        //Start checking for quest and disable collider so it doesn't get triggered again
        if (_mng.Type == TutorialType.Start && other.tag == "Player")
        {
            _mng.SetType(TutorialType.QuestObj);
            Destroy(GetComponent<Collider>());
        }
    }
}

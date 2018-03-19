using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_IntoBaseTutorial : MonoBehaviour
{
    [SerializeField]
    private GameObject _oldRobot = null;
    [SerializeField]
    private Script_SplineController _splineCTRLNewRobot = null;

    private Script_TutorialManager _mng = null;

    private List<int> _durations = new List<int> { 5, 12 };
    private int _currentIndex = 0;
    private float _timer = 0f;
    private const float StartTimer = 1f;

    private void Start()
    {
        //Save tutorial manager
        _mng = GetComponent<Script_TutorialManager>();
    }

    public void Update()
    {
        switch (_mng.Type)
        {
            case TutorialType.Start:

                _timer += Time.deltaTime;

                if (_timer > StartTimer)
                    _mng.SetType(TutorialType.QuestObj);

                break;

            //case TutorialType.Finish:
            //    break;
        }
    }

    public void SetRobot()
    {
        _oldRobot.gameObject.SetActive(false);
    }

    public void PuzzleSolved(GameObject path)
    {
        if (_currentIndex < _durations.Count)
            StartCoroutine(SetNewPath(path));
        Debug.Log(_currentIndex);
                //_mng.SetType(TutorialType.Finish);
    }

    private IEnumerator SetNewPath(GameObject path)
    {
        ++_currentIndex;
        yield return new WaitForSeconds(1.0f);
        //Make companion follow the next path
        _splineCTRLNewRobot.gameObject.GetComponent<Script_CompanionTutorial>().IsFixed = true;
        _splineCTRLNewRobot.SplineRoot = path;
        _splineCTRLNewRobot.Duration = _durations[_currentIndex-1];
        _splineCTRLNewRobot.StartAgain();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Check if player entered area
        if (other.tag == "Player")
        {
            _mng.SetType(TutorialType.Start);
            Destroy(GetComponent<Collider>());
        }
    }

}

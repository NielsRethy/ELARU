using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_WaitForPlayerHere : MonoBehaviour {
    [SerializeField]
    private Script_SplineController _splineCTRLNewRobot = null;
    [SerializeField]
    private float _durations;
    [SerializeField]
    private GameObject path;
    private bool _triggered = false;
    private bool _rotate;


    void Update()
    {
        if (_rotate)
        {
          //  _splineCTRLNewRobot.enabled = false;
           // _splineCTRLNewRobot.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = true;
            Vector3 lTargetDir = path.transform.GetChild(1).position - _splineCTRLNewRobot.gameObject.transform.position;
            lTargetDir.y = 0.0f;
            _splineCTRLNewRobot.gameObject.transform.rotation = Quaternion.RotateTowards(_splineCTRLNewRobot.gameObject.transform.rotation, Quaternion.LookRotation(lTargetDir), Time.deltaTime * 100);

            if ((_splineCTRLNewRobot.gameObject.transform.eulerAngles - Quaternion.LookRotation(lTargetDir).eulerAngles).magnitude < 0.4f * 0.4f)
            {
                _splineCTRLNewRobot.gameObject.GetComponent<Script_CompanionTutorial>().IsFixed = true;
                _splineCTRLNewRobot.SplineRoot = path;
                _splineCTRLNewRobot.Duration = _durations;
                _splineCTRLNewRobot.StartAgain();
                _rotate = false;
            }
        }
    }
    private IEnumerator SnP()
    {
        yield return new WaitForSeconds(0.35f);
        _rotate = true;
        //_splineCTRLNewRobot.gameObject.GetComponent<Script_CompanionTutorial>().IsFixed = true;
        //_splineCTRLNewRobot.SplineRoot = path;
        //_splineCTRLNewRobot.Duration = _durations;
        //_splineCTRLNewRobot.StartAgain();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Player")) return;
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(SnP());
    }


}

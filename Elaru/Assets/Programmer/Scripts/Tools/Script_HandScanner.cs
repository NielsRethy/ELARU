using System;
using UnityEngine;

public class Script_HandScanner : MonoBehaviour
{
    //Can scanner be triggered multiple times
    [SerializeField]
    private bool _isReusable = false;

    private bool _isTriggered = false;
    private float _timer = 0.0f;
    private const float MaxTime = 1.0f;
    private Script_TactileFeedback _feedbackMng = null;

    //Action to execute on press
    public Action TriggeredAction;

    private void Start()
    {
        //Cache tactile feedback instance
        _feedbackMng = Script_TactileFeedback.Instance;
    }

    private void OnTriggerStay(Collider other)
    {
        if (_isTriggered || other.GetComponent<Script_PickUp>() == null)
            return;

        var hand = other.GetComponent<Script_PickUp>().Hand;

        //Time how long hand has been in trigger and vibrate
        if (!_isTriggered)
        {
            _timer += Time.deltaTime;
            _feedbackMng.SendLongVibration(1, 0.1f * (_timer * 2), hand);
        }

        //Is hand in trigger long enough
        if (_timer > MaxTime)
        {
            //Call trigger action
            _timer = 0.0f;
            if (TriggeredAction != null)
                TriggeredAction.Invoke();

            _isTriggered = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isReusable)
        {
            _isTriggered = false;
            _timer = 0f;
        }

        //Reset when leave trigger before opening box
        if (_isTriggered)
            return;
        _timer = 0.0f;
    }
}

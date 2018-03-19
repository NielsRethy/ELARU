using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class Script_BossPlugger : MonoBehaviour
{
    [SerializeField]
    private List<Script_PickUpObject> _pullList = new List<Script_PickUpObject>();

    [SerializeField]
    private UnityEvent _completeEvent;

    public Action OnComplete;

    void Start()
    {
        _pullList.ForEach(x => x.OnGrab += PullObject);
        _pullList.ForEach(x => x.GetComponent<Rigidbody>().isKinematic = true);
    }

    private void PullObject(GameObject obj)
    {
        var pu = obj.GetComponent<Script_PickUpObject>();
        pu.OnGrab -= PullObject;

        //Reset object rigidbody
        if (!pu.ToggleKinematicWhilstHeld)
            obj.GetComponent<Rigidbody>().isKinematic = false;

        //Remove object from check list
        _pullList.Remove(pu);

        //If list is empty, complete
        if (_pullList.Count <= 0)
        {
            Debug.Log("Boss plugging completed");
            if (_completeEvent != null)
                _completeEvent.Invoke();

            if (OnComplete != null)
                OnComplete.Invoke();
        }
    }

    public void Test()
    {
        PullObject(_pullList[0].gameObject);
    }
}

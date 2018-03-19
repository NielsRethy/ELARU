using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_DeactiveObjectOnEnter : MonoBehaviour {

    [SerializeField]
    private GameObject _toDisable;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MainCamera"))
            _toDisable.SetActive(false);
    }
}

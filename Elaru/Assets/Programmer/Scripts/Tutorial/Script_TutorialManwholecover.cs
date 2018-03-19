using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_TutorialManwholecover : MonoBehaviour {
    //[SerializeField]
    //private List<GameObject> _lamps = new List<GameObject>();
    private Script_TurnOnLights _turnOnLightScript = null;
	// Use this for initialization
	void Start () {
        _turnOnLightScript = GetComponent<Script_TurnOnLights>();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != "Player") return;
        _turnOnLightScript.TurnOnLights();
    }
}

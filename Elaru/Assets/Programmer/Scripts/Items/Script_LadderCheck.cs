using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_LadderCheck : MonoBehaviour {
    [SerializeField]
    private bool _useTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && _useTrigger)
        {
            FindObjectOfType<Script_CompanionAI>().SetLadderAtive(this.gameObject);
        }
    }
}

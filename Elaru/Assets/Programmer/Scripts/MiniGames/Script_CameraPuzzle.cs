using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_CameraPuzzle : MonoBehaviour {
    //Variables
    [SerializeField]
    private Script_OpenFence _door = null;
    [SerializeField]
    private Script_TurnOnLights _guideLights = null;

    //private bool _seesPlayer = false;

    private void Start()
    {
        _door.Open();
    }

    //testing functions
    //public void Clsdr()
    //{
    //    _door.Close();
    //    _guideLights.TurnOnLights();
    //}

    //public void Opndr()
    //{
    //    _door.Open();
    //    _guideLights.ResetLamps();
    //}

    //Triggers
    private void OnTriggerEnter(Collider other)
    {
        //Check for player
        if (Script_LocomotionBase.Instance.PlayerCollider != other) return;
        _door.Close();
        _guideLights.TurnOnLights();
    }

    private void OnTriggerExit(Collider other)
    {
        //Check for player
        if (Script_LocomotionBase.Instance.PlayerCollider != other) return;
        _door.Open();
        _guideLights.ResetLamps();
    }
}

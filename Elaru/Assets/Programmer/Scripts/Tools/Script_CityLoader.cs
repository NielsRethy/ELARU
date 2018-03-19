using System.Collections.Generic;
using UnityEngine;

public class Script_CityLoader : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> _unloadList = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Player"))
            return;

        //If player enters area unload all objects
        EnableObjects(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.tag.Equals("Player"))
            return;

        //If player leaves area, enable all objects
        EnableObjects(true);
    }

    private void EnableObjects(bool value)
    {
        foreach (var obj in _unloadList)
            obj.SetActive(value);
    }
}

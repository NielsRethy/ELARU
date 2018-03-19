using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_RespawnObjects : MonoBehaviour
{

    [SerializeField]
    private List<GameObject> _objects = new List<GameObject>();
    [SerializeField]
    private bool _useTrigger = false;
    [SerializeField]
    private List<string> _objectName = new List<string>();

    private List<Vector3> _positionsObjs = new List<Vector3>();
    private int _count = 0;

    private void Start()
    {
        foreach (GameObject obj in _objects)
        {
            _positionsObjs.Add(obj.transform.position);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_useTrigger) return;
        ResetObjects(other);
    }

    public void ResetObjects(Collider other)
    {
        foreach (string name in _objectName)
        {
            if (other.name == name)
            {
                _objects[_count].transform.position = _positionsObjs[_count];
            }
            _count++;
        }
        _count = 0;
    }
}

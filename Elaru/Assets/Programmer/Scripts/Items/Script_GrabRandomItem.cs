using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Script_PickUpEffects))]
public class Script_GrabRandomItem : MonoBehaviour
{
    private Script_PickUpEffects _scriptEffects = null;
    private Script_LocomotionBase _scriptLoco = null;

    [SerializeField]
    private static List<GameObject> _objectsToGrab = new List<GameObject>();
    private const string PathPrefab = "Prefabs/";
    private const string PathBottle1 = "DestructibleBottle1";
    private const string PathBottle2 = "DestructibleBottle2";
    private const string PathBottleBrick = "DestructibleBrick";
    private static GameObject _bottle1 = null;
    private static GameObject _bottle2 = null;
    private static GameObject _brick = null;

    private static List<Script_PickUpObject> _scriptsGrabPickUps = new List<Script_PickUpObject>();

    [SerializeField]
    private bool _infinite = false;
    private int _infIndex = 0;
    private const int AmountOfPrefabs = 5;

    [SerializeField]
    private int _amountInPile = 3;

    private void Start()
    {
        _scriptEffects = GetComponent<Script_PickUpEffects>();
        _scriptLoco = Script_LocomotionBase.Instance;

        if (_bottle1 == null)
            _bottle1 = Resources.Load(PathPrefab + PathBottle1) as GameObject;
        if (_bottle2 == null)
            _bottle2 = Resources.Load(PathPrefab + PathBottle2) as GameObject;
        if (_brick == null)
            _brick = Resources.Load(PathPrefab + PathBottleBrick) as GameObject;

        if (_objectsToGrab.Count <= 0)
            PopulateListsWithGarbage();
    }

    private void PopulateListsWithGarbage()
    {
        for (int i = 0; i < AmountOfPrefabs; i++)
        {
            _objectsToGrab.Add(Instantiate(_bottle1));
            _objectsToGrab.Add(Instantiate(_bottle2));
            _objectsToGrab.Add(Instantiate(_brick));
        }

        foreach (GameObject obj in _objectsToGrab)
        {
            _scriptsGrabPickUps.Add(obj.GetComponent<Script_PickUpObject>());
            obj.SetActive(false);
        }
    }

    private void Update()
    {
        if (_scriptsGrabPickUps == null)
            return;

        // Check if the hand was gripped
        if (_scriptEffects.Hover && _scriptLoco.GetPressDown(_scriptLoco.GripButton, _scriptEffects.HandInPickUp))
        {
            _scriptEffects.Pressed = true;

            var index = ++_infIndex % _scriptsGrabPickUps.Count;
            var randomPickUp = _scriptsGrabPickUps[index];
            randomPickUp.gameObject.SetActive(true);
            randomPickUp.transform.SetParent(null);
            randomPickUp.GetComponent<Script_Destructible>().ResetContent();

            randomPickUp.transform.position = _scriptLoco.GetTrackedObject(_scriptEffects.HandInPickUp).transform.position;

            var pickScript = _scriptLoco.GetPickUpFromHand(_scriptEffects.HandInPickUp);

            pickScript.SetPickUp(randomPickUp);

            // Turn off hover effect
            _scriptEffects.Pressed = false;

            if (_infinite)
                return;

            // Prevent this game object from giving items
            if (--_amountInPile <= 0)
            {
                foreach (Renderer rnderer in _scriptEffects.Renderers)
                    rnderer.gameObject.SetActive(false);

                Destroy(this);
            }
        }
    }
}

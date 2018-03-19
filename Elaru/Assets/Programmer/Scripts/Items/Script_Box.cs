using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType
{
    None,
    Black,
    Blue,
    DarkBlue,
    Green,
    Orange,
    Pink,
    Purple,
    Red,
    White,
    Yellow,
    All
}

public class Script_Box : MonoBehaviour
{
    //Items that can be spawned
    [SerializeField]
    private List<GameObject> _items = new List<GameObject>();
    //Item type to spawn
    [SerializeField]
    private ItemType _itemType = ItemType.Black;

    //Box lid rotation vars
    [SerializeField]
    private GameObject _hinge = null;
    [SerializeField]
    private GameObject _handle = null;
    [SerializeField]
    private Vector3 _rotationAxis = new Vector3(1, 0, 0);
    [SerializeField]
    private GameObject _endRot = null;


    private Quaternion _startRot = Quaternion.identity;
    private Vector3 _startPos = Vector3.zero;
    private Vector3 _startVec = Vector3.zero;
    private bool _isHeld = false;
    private bool _isInTrigger = false;
    private bool _spawnedItem = false;
    private HandSide _handside = HandSide.None;
    private Script_LocomotionBase _locBase;
    private Script_TactileFeedback _tactileFeedback;
    private GameObject _controller = null;
    private Script_PickUpEffects _emisObj;
    private bool _activated = false;

    [SerializeField]
    private GameObject _particleEffect = null;
    private ParticleSystem[] _particles = null;

    #region Sounds
    private const string PathBase = "Box";
    private const string PathOpen = "Open";
    private const string PathClose = "Close";
    private const string PathLocked = "Locked";
    private const string PathOpening = "Opening";
    private const string PathRelease = "Release";
    private Script_AudioManager _scriptAudio = null;
    private const float DelayOpeningSound = 0.2f;
    private float _openingSoundTimer = 0f;
    private bool _closedOnce = false;
    private bool _lockedOnce = false;
    private float _deltaAngle = 0f;
    #endregion

    private void Start()
    {
        //Cache needed components
        _locBase = Script_LocomotionBase.Instance;
        _tactileFeedback = Script_TactileFeedback.Instance;
        _scriptAudio = Script_AudioManager.Instance;
        _emisObj = GetComponentInChildren<Script_PickUpEffects>();
        _startRot = transform.rotation;
        _startPos = transform.position;
        _startVec = transform.forward;

        _particles = _particleEffect.GetComponentsInChildren<ParticleSystem>();
    }

    private void Update()
    {
        if (_items.Count > 0 && _items[(int)_itemType - 1] != null && _items[(int)_itemType - 1].GetComponent<Script_Collectable>().Activated && !_activated)
        {
            _activated = true;

            //Turn off lights on the side of the box
            var light = GetComponent<Script_Light>();
            if (light != null)
                GetComponent<Script_Light>().SetLights(false);

        }

        if (_locBase.GetPressDown(_locBase.GripButton, _handside))
        {
            //Turn off the particles playing inside the case
            ToggleParticle(false);
            _scriptAudio.PlaySFX(PathBase + PathOpen, transform.position);
        }

        if (_locBase.GetPress(_locBase.GripButton, _handside))
        {
            _isHeld = true;

            //Check if need to spawn item
            if (!_spawnedItem)
            {
                _spawnedItem = true;
                SpawnItem();
            }

            //Rotation of lever over correct axis + pulse
            var cPos = _controller.transform.position;
            if (_rotationAxis.x > 0f)
                cPos.x = _hinge.transform.position.x;
            else if (_rotationAxis.y > 0f)
                cPos.y = _hinge.transform.position.y;
            else if (_rotationAxis.z > 0)
                cPos.z = _hinge.transform.position.z;

            //Offset hand in y
            cPos.y -= 0.1f;

            //Debug.Log(cPos);
            cPos = transform.worldToLocalMatrix.MultiplyPoint(cPos);
            cPos = -cPos;
            cPos = transform.localToWorldMatrix.MultiplyPoint(cPos);
            _hinge.transform.LookAt(cPos);


            //Rotation limiter
            //var vecEnd = (_endRot.transform.position - _hinge.transform.position);
            var vecStart = (_startPos - _hinge.transform.position);

            var a = Vector3.Angle(_hinge.transform.forward, _endRot.transform.forward);
            _deltaAngle -= a;

            if (Mathf.Abs(_deltaAngle) > 0.05f)
            {
                //Play opening sound whilst moving (and on delay to prevent sfx spam)
                if (_openingSoundTimer <= 0f)
                {
                    _scriptAudio.PlaySFX(PathBase + PathOpening, transform.position, Mathf.Abs(_deltaAngle));
                    _openingSoundTimer = DelayOpeningSound;
                }
                else
                    _openingSoundTimer -= Time.deltaTime;

                // Tacticle feedback
                var vibStrength = Convert.ToUInt16(1000 * Mathf.Clamp01(Mathf.Abs(_deltaAngle)));
                _tactileFeedback.SendShortVib(vibStrength, _handside);
            }
            _deltaAngle = a;

            if (a /*< 90 || a */> 89)
            {
                transform.rotation = _startRot;
                //Play sound
                if (!_closedOnce)
                {
                    _scriptAudio.PlaySFX(PathBase + PathClose, transform.position);
                    _closedOnce = true;
                }
            }
            else
                _closedOnce = false;

            if (a < 4)
            {
                // Debug.Log("Box hing over limit");

                //Release hand
                Release();
                //Spawn item
                //SpawnItem();
                //Play sound
                if (!_lockedOnce)
                {
                    _scriptAudio.PlaySFX(PathBase + PathLocked, transform.position);
                    _lockedOnce = true;
                }

                transform.rotation = _endRot.transform.rotation;

                //Turn off script
                Invoke("TurnOffScript", 1.0f);
            }
        }

        if (_locBase.GetPressUp(_locBase.GripButton, _handside))
        {
            Release();
            //Play sound
            _scriptAudio.PlaySFX(PathBase + PathRelease, transform.position);
        }
    }

    private void Release()
    {
        _isInTrigger = false;
        _handside = HandSide.None;
        _controller = null;
        _isHeld = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isHeld)
        {
            //Check if other is player's hand
            var pickup = other.GetComponent<Script_PickUp>();
            if (pickup == null)
                return;
            //Check if hand is empty
            if (pickup.IsHoldingObject)
                return;

            //Grip the lever when hand grip is pressed
            if (_locBase.GetPress(_locBase.GripButton, pickup.Hand))
                SetTriggerHand(pickup, other);

            _emisObj.Hover = true;

        }
        else
        {
            _emisObj.Hover = false;
        }
    }

    private void SetTriggerHand(Script_PickUp pickup, Collider other)
    {
        _handside = pickup.Hand;
        _isInTrigger = true;
        _controller = other.gameObject;
        _isHeld = true;
    }

    //private void Activated()
    //{

    //}

    private void SpawnItem()
    {
        if (_items.Count <= 0)
            return;

        //Set correct item
        _items[(int)_itemType - 1].gameObject.SetActive(true);
        _items[(int)_itemType - 1].transform.SetParent(null);
    }

    private void TurnOffScript()
    {
        //Destroy script
        //Destroy(this);
        enabled = false;
    }

    public void ToggleParticle(bool state)
    {
        if (_particles == null || _particles.Length <= 0)
            return;

        foreach (var particle in _particles)
            if (!state)
                particle.Stop();
            else
                particle.Play();
    }

    public void Reset()
    {
        Release();
        transform.rotation = _startRot;
        ToggleParticle(true);
        _lockedOnce = false;
    }
}

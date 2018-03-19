using System.Collections.Generic;
using UnityEngine;
using System;

public class Script_TeleportOnClick : MonoBehaviour
{
    //Where to teleport to
    [SerializeField]
    private GameObject _teleportLoc = null;

    [SerializeField]
    private Vector3 _teleportOffset = Vector3.zero;

    //Does player automatically teleport when entering trigger (in base)
    //[SerializeField]
    //private bool _isTrigger = false;

    //List of objects to activate / deactivate when teleporting
    [SerializeField]
    private List<GameObject> _city = new List<GameObject>();

    private float _timer = 0.0f;
    private const float FadeTime = 1f;
    private bool _teleport = false;
    private Script_LocomotionBase _locoInstance = null;
    private Script_PlayerCollisionCheck _scriptCollision = null;
    private Script_AudioManager _scriptAudio;

    [SerializeField]
    private bool _goesToBase = true;

    [SerializeField]
    private string _soundEffectName = "";
    private bool _playedSound = false;

    private bool _spawnCompanion = true;
    [SerializeField]
    private Script_ReplaceCompnanion _replaceCom = null;

    public Action TeleportAction;

    private void Start()
    {
        //Get variables
        _locoInstance = Script_LocomotionBase.Instance;
        _scriptCollision = _locoInstance.CameraRig.GetComponentInChildren<Script_PlayerCollisionCheck>();
        _scriptAudio = Script_AudioManager.Instance;

        //Teleport to child if teleport location not set in editor
        if (_teleportLoc == null)
            _teleportLoc = transform.GetChild(0).gameObject;
    }

    private void Update()
    {
        if (_teleport)
        {
            _locoInstance.RootRigidbody.isKinematic = true;

            _timer += Time.deltaTime;

            // Play a sound if there is one (once)
            if (_soundEffectName.Length > 0 && !_playedSound)
            {
                Script_AudioManager.Instance.PlaySFX(_soundEffectName, transform.position);
                _playedSound = true;

                // Fade out screen
                SteamVR_Fade.View(Color.black, FadeTime);
            }
        }

        if (_timer < FadeTime || !_teleport)
            return;

        //Reset
        _timer = 0;
        _teleport = false;

        //Teleport player
        TeleportPlayer();
    }

    // For function test method (so we can teleport to the manhole in the inspector)
    public void TeleportPlayer()
    {
        //Safety unlock all region locking
        _locoInstance.ToggleLocomotionAndFading(false);

        _locoInstance.ScriptLocomotionDash.LockRegion(false, null, 0f, true);

        _locoInstance.CameraRig.transform.position = _teleportLoc.transform.position + _teleportOffset;
        //Activate / Deactivate part of city for performance
        foreach (GameObject obj in _city)
        {
            obj.SetActive(!_goesToBase);
        }

        if (Script_QuestGiver.Instance.IsQuestActive() && _spawnCompanion)
        {
            _spawnCompanion = false;
            if (_replaceCom != null) _replaceCom.ReplaceObject();
            Script_HideIfCompanionIsNotActive.Instance.UnhideMesh();
        }

        _locoInstance.ScriptLocomotionDash.OverrideSafePlace(_teleportLoc.transform.position + _teleportOffset);
        //_locoInstance.CameraRig.transform.position = _teleportLoc.transform.position;
        Script_PlayerInformation.Instance.IsInBase = _goesToBase;
        _locoInstance.ToggleLocomotionAndFading(true);

        // Cross fade to base music or mute
        if (_goesToBase)
        {
            _scriptAudio.StopMusic();
            _scriptAudio.StartPlayerBaseAmbient();
            // Play the music if the player has been in the base before
            if (!_spawnCompanion)
                _scriptAudio.ForceStartBaseMusic();
        }
        else
        {
            _scriptAudio.StopAmbient();
            _scriptAudio.StopMusic();
        }

        // Re-enable collision
        //_locoInstance.RootRigidbody.isKinematic = false;
        Invoke("PleaseWork", 1f);
        _locoInstance.RootRigidbody.velocity = Vector3.zero;
        _locoInstance.RootRigidbody.angularVelocity = Vector3.zero;
        _locoInstance.RootRigidbody.position = _teleportLoc.transform.position + _teleportOffset;

        Invoke("FadeInScreen", FadeTime);

        if (TeleportAction != null)
            TeleportAction.Invoke();
    }

    private void PleaseWork()
    {
        _teleport = false;
        _locoInstance.RootRigidbody.isKinematic = false;
    }

    private void FadeInScreen()
    {
        //Fade screen out
        SteamVR_Fade.View(Color.clear, FadeTime);

        // Allow sound to play again
        _playedSound = false;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    //Teleport if type is trigger
    //    _teleport = _isTrigger || _teleport;
    //    if (_teleport && Script_QuestManager.Instance.QuestLinks.Count > 0 && _spawnCompanion)
    //    {
    //        _spawnCompanion = false;
    //        _replaceCom.ReplaceObject();
    //    } 
    //}

    private void OnTriggerStay(Collider other)
    {
        //On press / Open hatch
        //Teleport the player
        if (_teleport || !enabled)
            return;
        var spu = other.GetComponent<Script_PickUp>();
        if (spu == null)
            return;

        var hand = spu.Hand;
        if (_locoInstance.GetPressDown(_locoInstance.GripButton, hand))
            _teleport = true;


    }
}

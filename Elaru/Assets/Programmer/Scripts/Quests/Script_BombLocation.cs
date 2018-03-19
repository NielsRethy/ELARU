using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_BombLocation : MonoBehaviour
{
    [SerializeField]
    private GameObject _hologram = null;
    [SerializeField]
    private GameObject _bomb = null;
    [SerializeField]
    AudioClip[] _soundeffects;

    public bool IsPlaced { get; private set; }

    public void Activate()
    {
        //Show location where to place bomb
        _hologram.SetActive(true);
        _bomb.SetActive(false);
        GetComponent<BoxCollider>().enabled = true;
    }

    //private void Start()
    //{
    //    //Disable objects when not activated yet
    //    _bomb.SetActive(false);
    //    _hologram.SetActive(false);
    //}

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlaced || other.name != "Bomb")
            return;

        //Hide bomb when placed
        other.gameObject.transform.parent.gameObject.SetActive(false);
        IsPlaced = true;
        Invoke("Placed", 0.2f);
        //Play sound
        Script_AudioManager.Instance.PlaySFX("PickupReleased", this.GetComponent<AudioSource>());
        var hand = other.gameObject.GetComponent<Script_PickUpObject>().ControlHandSide;
        var pu = Script_LocomotionBase.Instance.GetPickUpFromHand(hand);
        if (pu != null)
            pu.Drop();
    }

    void Placed()
    {
        //Replace hologram with bomb
        _hologram.SetActive(false);
        _bomb.SetActive(true);
    }

    public void ResetBomb()
    {
        IsPlaced = false;
        _hologram.SetActive(true);
    }
}

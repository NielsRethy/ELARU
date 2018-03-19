using System;
using System.Collections;
using UnityEngine;

public class Script_UpgradeStationTeleport : MonoBehaviour
{
    private const string TagGun = "Gun";
    private const string TagSword = "Sword";

    private BoxCollider _thisCollider = null;

    public Transform WeaponInTeleport { get; private set; }
    private Script_Weapon _weaponScript = null;
    private Script_PickUpObject _scriptPickUpWeapon = null;

    private const float LerpSpeed = 2f;
    private const float RotateSpeed = 20f;

    public Action OnObjectInCenter;
    public Action OnObjectLeftCenter;
    private bool _objectIsInCenter = false;

    private GameObject _handInZone = null;

    [SerializeField]
    private GameObject _portalParticles;
    [SerializeField]
    private GameObject _dissapearParticles;
    [SerializeField]
    private GameObject _appearParticles;

    public enum WeaponType
    {
        None,
        Sword,
        Gun
    }
    public WeaponType Type { get; set; }

    private void Start()
    {
        //Cache collider
        _thisCollider = GetComponent<BoxCollider>();

        if (_portalParticles != null)
            _portalParticles.SetActive(false);

        if (_dissapearParticles)
            _dissapearParticles.SetActive(false);

        if (_appearParticles)
            _appearParticles.SetActive(false);
    }

    private void LateUpdate()
    {
        //Check if there is weapon in teleporter
        if (WeaponInTeleport == null || _scriptPickUpWeapon == null || _scriptPickUpWeapon.BeingHeld)
            return;

        //Move object to center
        var movePos = _handInZone != null ? _handInZone.transform.position : transform.position;
        WeaponInTeleport.position = Vector3.Lerp(WeaponInTeleport.position, movePos, LerpSpeed * Time.deltaTime);

        //Rotate object in teleporter
        var rotDiff = RotateSpeed * Time.deltaTime;
        WeaponInTeleport.transform.Rotate(.5f * rotDiff, rotDiff, .3f * -rotDiff);

        //Check if object is in center
        var inRange = (WeaponInTeleport.position - transform.position).sqrMagnitude < .1f * .1f;
        if (!_objectIsInCenter && inRange && OnObjectInCenter != null)
        {
            OnObjectInCenter.Invoke();
            _objectIsInCenter = true;
        }

        //Check if object left center
        else if (_objectIsInCenter && !inRange)
        {
            if (OnObjectLeftCenter != null)
                OnObjectLeftCenter.Invoke();

            _objectIsInCenter = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") || FindTagInChildren(other.gameObject, "Hand"))
        {
            _handInZone = other.gameObject;
            return;
        }

        //Check if there's already an object in the teleporter
        if (WeaponInTeleport != null)
            return;

        //Ignore if other is not a weapon
        if (!ObjectIsWeapon(other.gameObject))
        {
            Physics.IgnoreCollision(other, _thisCollider);
            return;
        }

        //Save weapon info
        Type = other.CompareTag(TagGun) ? WeaponType.Gun : WeaponType.Sword;
        WeaponInTeleport = other.transform;
        _weaponScript = WeaponInTeleport.GetComponent<Script_Weapon>();
        _weaponScript.IsInTeleporter = true;
        _scriptPickUpWeapon = WeaponInTeleport.GetComponent<Script_PickUpObject>();

        if (_scriptPickUpWeapon.BeingHeld)
        {
            _scriptPickUpWeapon.OnRelease += ReleaseInTeleporter;
            return;
        }

        _scriptPickUpWeapon.OnGrab += DisableKinematicOnGrab;

        //Disable rigidbody actions
        var rb = WeaponInTeleport.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        //Show teleport particles
        if (_portalParticles != null)
            _portalParticles.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == _handInZone)
            _handInZone = null;

        if (WeaponInTeleport == null || other.transform != WeaponInTeleport)
            return;


        WeaponInTeleport.GetComponent<Rigidbody>().isKinematic = false;

        //Disable portal particles
        if (_portalParticles != null)
            _portalParticles.SetActive(false);

        //Reset values
        _scriptPickUpWeapon.OnRelease -= ReleaseInTeleporter;
        _scriptPickUpWeapon.OnGrab -= DisableKinematicOnGrab;
        WeaponInTeleport = null;
        _weaponScript.IsInTeleporter = false;
        _weaponScript = null;
        Type = WeaponType.None;
    }

    private bool ObjectIsWeapon(GameObject obj)
    {
        return obj.CompareTag(TagGun) || obj.CompareTag(TagSword);
    }

    public IEnumerator ThrowOutWeapon(float delayTime = .75f)
    {
        yield return new WaitForSeconds(delayTime);

        if (_dissapearParticles)
            _dissapearParticles.SetActive(false);

        //Show explosion particles
        if (_appearParticles != null)
            _appearParticles.SetActive(true);

        yield return new WaitForSeconds(2f);

        var rb = WeaponInTeleport.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddForce((transform.forward + transform.right + transform.up) * 3, ForceMode.Impulse);
        
        yield return new WaitForSeconds(1.5f);

        if (_appearParticles != null)
            _appearParticles.SetActive(false);
    }

    private void DisableKinematicOnGrab(GameObject o)
    {
        WeaponInTeleport.GetComponent<Rigidbody>().isKinematic = false;
        if (_scriptPickUpWeapon)
            _scriptPickUpWeapon.OnGrab -= DisableKinematicOnGrab;

        //Disable portal particles
        if (_portalParticles != null)
            _portalParticles.SetActive(false);
    }

    private void ReleaseInTeleporter(GameObject o)
    {
        _scriptPickUpWeapon.OnGrab += DisableKinematicOnGrab;

        //Disable rigidbody actions
        var rb = WeaponInTeleport.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        //Disable teleport particles
        if (_portalParticles != null)
            _portalParticles.SetActive(true);
    }

    public void StartUpgradeParticles()
    {
        if (_portalParticles != null)
            _portalParticles.SetActive(false);
        if (_dissapearParticles != null)
            _dissapearParticles.SetActive(true);
    }

    private bool FindTagInChildren(GameObject o, String tag)
    {
        foreach (Transform child in o.transform)
        {
            if (child.CompareTag(tag))
                return true;
        }

        return false;
    }
}
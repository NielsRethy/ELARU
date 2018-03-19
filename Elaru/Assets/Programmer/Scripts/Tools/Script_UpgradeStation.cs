using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class Script_UpgradeStation : MonoBehaviour
{
    [SerializeField]
    private Script_UpgradeStationTeleport _attachedTeleporter;

    //Images that can be shown
    // 0 - 2 Gun type images
    // 3 - 5 Gun visual mod images
    // 6 - 8 General mod images
    [SerializeField]
    private List<Sprite> _images = new List<Sprite>();

    //Conveyor belt
    [SerializeField]
    private GameObject _path = null;

    //Screen images
    [SerializeField]
    private Image _img1 = null;
    [SerializeField]
    private Image _img2 = null;
    [SerializeField]
    private Image _img3 = null;

    private Script_Gun _gun = null;
    private Script_Sword _sword = null;

    //Current upgrade number
    private int _upgradeNr = 1;

    private Script_PlayerInformation _playerInfo = null;

    //Attached lever
    [SerializeField]
    private Script_UpgradeHatchLever _upgradeHatchLeverScript = null;

    //Position to teleport weapon to after upgrade completion
    [SerializeField]
    private Transform _weaponDoneSpawnPosition = null;

    [SerializeField]
    private Script_SplineController _splineCtrlGun = null;
    [SerializeField]
    private Script_SplineController _splineCtrlSword = null;

    //Called when upgrade cycle is completed
    public Action OnCycleComplete;

    private Animator _animController;

    [SerializeField]
    private Script_CollisionArea _rangeCollisionArea = null;

    void Start()
    {
        //Cache components
        _animController = GetComponent<Animator>();
        _playerInfo = Script_PlayerInformation.Instance;

        //Show empty images when teleporter has object inside
        _attachedTeleporter.OnObjectInCenter += () =>
        {
            _gun = null;
            _sword = null;
            switch (_attachedTeleporter.Type)
            {
                case Script_UpgradeStationTeleport.WeaponType.None:
                    return;
                case Script_UpgradeStationTeleport.WeaponType.Sword:
                    _sword = _attachedTeleporter.WeaponInTeleport.GetComponent<Script_Sword>();
                    ShowStartImages();
                    break;

                case Script_UpgradeStationTeleport.WeaponType.Gun:
                    _gun = _attachedTeleporter.WeaponInTeleport.GetComponent<Script_Gun>();
                    ShowStartImages();
                    break;
            }

            //Open the screen
            SetAnimation("OpenScreen");

            //Show empty images on screen
            if (!_img1.gameObject.activeInHierarchy)
                StartCoroutine(ShowIcons(true, 1f));
        };
        _attachedTeleporter.OnObjectLeftCenter += () =>
        {
            _gun = null;
            _sword = null;
            StartCoroutine(ShowIcons(false));
            //Safety hide icons when switching states fast
            StartCoroutine(ShowIcons(false, 1f));
            SetAnimation("CloseScreen");
        };

        //Close screen at start
        SetAnimation("CloseScreen");
        StartCoroutine(ShowIcons(false));
    }

    public void Pressed(int btnNr)
    {
        //Check if button press is valid
        if (_attachedTeleporter.WeaponInTeleport == null || _attachedTeleporter.Type == Script_UpgradeStationTeleport.WeaponType.None 
            || _upgradeNr > _playerInfo.NrOfUpgradeSlots)
            return;
        
        switch (_upgradeNr)
        {
            case 1:
                if (_gun != null)
                {
                    //Set gun type and show visual mod images
                    _gun.Type = (Script_Gun.GunType)(btnNr - 1);
                    _img1.sprite = _images[3];
                    _img2.sprite = _images[4];
                    _img3.sprite = _images[5];
                    Debug.Log("Button 1 gun pressed");

                    //Clear weapon of previous mods
                    _gun.RemoveAllMods();

                    StartCoroutine(MakeWeaponDisappear(_gun.gameObject));
                }
                if (_sword != null)
                {
                    //Set first sword mod and show next images
                    _sword.AddMod((Script_Weapon.BoostType)(btnNr - 1));
                    ShowGeneralUpgradeImages();
                    Debug.Log("Button 2 sword pressed");

                    //Clear weapon of previous mods
                    _sword.RemoveAllMods();

                    StartCoroutine(MakeWeaponDisappear(_sword.gameObject));
                }
                break;
            case 2:
                if (_gun != null)
                {
                    //Set visual mod and show mod images
                    _gun.VisualModType = (Script_Gun.ModificationType)(btnNr - 1);
                    ShowGeneralUpgradeImages();
                    Debug.Log("Button 2 gun pressed");
                }
                if (_sword != null)
                {
                    //Set sword mod and show mod images
                    _sword.AddMod((Script_Weapon.BoostType)(btnNr - 1));
                    ShowGeneralUpgradeImages();
                    Debug.Log("Button 2 sword pressed");
                }
                break;
            default:
                if (_gun != null)
                    _gun.AddMod((Script_Weapon.BoostType)(btnNr - 1));
                if (_sword != null)
                    _sword.AddMod((Script_Weapon.BoostType)(btnNr - 1));
                Debug.Log("Default pressed");
                break;
        }

        _upgradeNr++;

        //Check if cycle complete
        if (_upgradeNr > _playerInfo.NrOfUpgradeSlots)
        {
            //Close the screen
            StartCoroutine(ShowIcons(false));
            SetAnimation("CloseScreen");
            
           Invoke("ShowWeaponAfterCycle", 2f);

            //Throw weapon out of teleporter
            StartCoroutine(_attachedTeleporter.ThrowOutWeapon(0f));

            if (OnCycleComplete != null)
                OnCycleComplete.Invoke();
        }
    }

    private IEnumerator MakeWeaponDisappear(GameObject obj, float delay = .8f)
    {
        yield return new WaitForSeconds(delay);

        //Hide weapon in teleporter
        ShowWeapon(obj, false);

        //Disable teleporter particles
        _attachedTeleporter.StartUpgradeParticles();
    }

    private void ShowWeaponAfterCycle()
    {
        //Show the weapon in teleporter again
        var obj = _gun != null ? _gun.gameObject : _sword.gameObject;
        ShowWeapon(obj, true);

        //Enable gun visual mod
        if (_gun != null)
        {
            _gun.ShowVisualMod(true);
            _gun.ToggleEffects(false);
        }
    }

    public void SetNewPath(Script_SplineController splnCtrl)
    {
        splnCtrl.SplineRoot = _path;
        splnCtrl.StartAgain();
        splnCtrl.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = true;
    }

    public void StopSplineGun(GameObject o)
    {
        _splineCtrlGun.enabled = false;
        _splineCtrlGun.PauseSplineInterpolation(true);
        _splineCtrlGun.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = false;

        if (_gun != null)
        {
            var pickUpScript = _gun.GetComponent<Script_PickUpObject>();
            if (pickUpScript != null)
                pickUpScript.OnGrab -= StopSplineGun;
        }
    }

    private void ShowWeapon(GameObject o, bool value)
    {
        o.GetComponent<Script_PickUpObject>().enabled = value;
        o.GetComponentsInChildren<Renderer>().ForEach(x => x.enabled = value);
        o.GetComponentsInChildren<Light>().ForEach(x => x.enabled = value);
    }

    public void StopSplineSword(GameObject o)
    {
        _splineCtrlSword.enabled = false;
        _splineCtrlSword.PauseSplineInterpolation(true);
        _splineCtrlSword.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = false;

        if (_sword != null)
        {
            var pickUpScript = _sword.GetComponent<Script_PickUpObject>();
            if (pickUpScript != null)
                pickUpScript.OnGrab -= StopSplineSword;
        }
    }

    public void ShowStartImages()
    {
        //Show gun type images
        if (_gun != null)
        {
            _img1.sprite = _images[0];
            _img2.sprite = _images[1];
            _img3.sprite = _images[2];
            _upgradeNr = 1;
        }
        //Show sword mod images
        if (_sword != null)
        {
            ShowGeneralUpgradeImages();
            _upgradeNr = 1;
        }
    }

    private void ShowGeneralUpgradeImages()
    {
        _img1.sprite = _images[6];
        _img2.sprite = _images[7];
        _img3.sprite = _images[8];
    }

    private void ShowEmptyImages()
    {
        //Open the hatch
        SetAnimation("OpenHatch");

        _img1.sprite = _images[9];
        _img2.sprite = _images[9];
        _img3.sprite = _images[9];
    }

    private void SetAnimation(String animName)
    {
        ResetAnimations();
        _animController.SetBool(animName, true);
    }

    private void ResetAnimations()
    {
        //Reset all booleans in animator
        _animController.SetBool("OpenHatch", false);
        _animController.SetBool("CloseHatch", false);
        _animController.SetBool("OpenScreen", false);
        _animController.SetBool("CloseScreen", false);
        _animController.SetBool("ActivateConveyorBelt", false);
        _animController.speed = 1f;
    }

    private void StopAnimation()
    {
        _animController.speed = 0;
    }
    
    private IEnumerator ShowIcons(bool v, float delayTime = 0f)
    {
        yield return new WaitForSeconds(delayTime);
        _img1.transform.parent.parent.parent.gameObject.SetActive(v);
    }
}


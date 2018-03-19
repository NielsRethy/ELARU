using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Script_Weapon : MonoBehaviour
{
    //Combat stats
    public float Damage = 1;
    public float RechargeSpeed = 1;
    public float RangeFactor = 1;

    //Reset values
    private float _startDamage;
    private float _startRechargeSpeed;
    private float _startRange;

    //Percentage increase per mod
    private float _damageScalePerMod = .1f;
    private float _rechargeSpeedScalePerMod = .1f;
    private float _rangeScalePerMod = .1f;

    public List<BoostType> ModList = new List<BoostType>();

    //Overheating vars
    protected float _overHeating = 0f;
    [SerializeField]
    protected float _heatFactor = 1f;
    protected bool _isOverHeated = false;

    //Auto teleport vars
    private Transform _playerTransform;
    [SerializeField]
    private float _autoTeleportDistance = 10f;
    public bool HasBeenFound { get; protected set; }
    public Script_PickUpObject PickUpObjectScript { get; protected set; }
    [SerializeField]
    protected bool _teleportToLocationInsteadOfDocks = false;
    [SerializeField]
    protected Vector3 _teleportToLocation;

    public bool IsInTeleporter { get; set; }

    public enum BoostType
    {
        Damage,
        RechargeSpeed,
        Range,
        None
    }


    public void LoadWeapon(List<int> mods, bool moveWeapon)
    {
        //Load mods from saved list
        foreach (int m in mods)
        {
            AddMod((BoostType)m);
        }

        //Teleport weapon away so it gets docked automatically
        if (moveWeapon)
            gameObject.transform.position = new Vector3(0, 5000, 0);
    }

    public List<int> GetMods()
    {
        //Convert mods to int list for saving
        List<int> tempStorage = new List<int>();
        for (int i = 0; i < ModList.Count; i++)
        {
            tempStorage.Add((int)ModList[i]);
        }

        return tempStorage;
    }

    private void Awake()
    {
        //Set up base stats from inspector
        _startDamage = Damage;
        _startRechargeSpeed = RechargeSpeed;
        _startRange = RangeFactor;

        //Cache player transform
        _playerTransform = Script_LocomotionBase.Instance.CameraRig.transform;

        //Cache base PickUp script
        PickUpObjectScript = GetComponent<Script_PickUpObject>();

        //Look for first grab to see if teleporting is valid
        PickUpObjectScript.OnGrab += HeldFirstTime;
    }


    public void AddMod(BoostType newMod)
    {
        //Add mod to list
        ModList.Add(newMod);

        //Check for mod use achievement
        Script_AchievementManager.Instance.AddModToAchievement();

        //Update combat stats with new mod
        UpdateModdedValues();
    }

    public void RemoveMod(BoostType mod)
    {
        //Remove mod from list and update combat stats
        ModList.Remove(mod);
        UpdateModdedValues();
    }

    private void UpdateModdedValues()
    {
        //Start with base values
        ResetValues();

        //Adjust combat stats with mod values
        foreach (var m in ModList)
        {
            switch (m)
            {
                case BoostType.Damage:
                    Damage *= 1 + _damageScalePerMod;
                    string var = tag == "Sword" ? "SwordDamage" : "GunDamage";
                    if (Script_BehaviorTreeFramework.PBB.ContainsKey(var))
                        Script_BehaviorTreeFramework.PBB[var].Value = Damage;
                    break;
                case BoostType.RechargeSpeed:
                    RechargeSpeed *= 1 + _rechargeSpeedScalePerMod;
                    break;
                case BoostType.Range:
                    RangeFactor *= 1 + _rangeScalePerMod;
                    break;
                case BoostType.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private void ResetValues()
    {
        //Go back to start values
        Damage = _startDamage;
        RechargeSpeed = _startRechargeSpeed;
        RangeFactor = _startRange;
    }

    public void RemoveAllMods()
    {
        //Clear weapon of all mods and update stats accordingly
        Debug.Log("Cleared all mods");
        ModList.Clear();
        UpdateModdedValues();
    }

    /// <summary>
    /// Vibrates controller for extended time
    /// </summary>
    /// <param name="length">How long to vibrate for</param>
    /// <param name="strength">Intensity of vibration</param>
    /// <param name="controller">Controller to vibrate</param>
    protected IEnumerator LongVib(float length, float strength, SteamVR_Controller.Device controller)
    {
        for (float i = 0; i < length; i += Time.deltaTime)
        {
            controller.TriggerHapticPulse((ushort)Mathf.Lerp(0, 3999, strength));
            yield return null;
        }
    }

    /// <summary>
    /// Check if weapon needs to autoteleport
    /// </summary>
    protected bool IsTooFarFromPlayer()
    {
        if (!HasBeenFound)
            return false;
        return (_playerTransform.position - transform.position).sqrMagnitude >=
               _autoTeleportDistance * _autoTeleportDistance;
    }

    /// <summary>
    /// Check if player has touched the weapon before
    /// </summary>
    /// <param name="o"></param>
    public void HeldFirstTime(GameObject o)
    {
        if (PickUpObjectScript.OnGrab != null)
            PickUpObjectScript.OnGrab -= HeldFirstTime;
        HasBeenFound = true;
    }

    //Returns whether Gun is held by player
    public bool HeldByPlayer()
    {
        return PickUpObjectScript.BeingHeld;
    }
}

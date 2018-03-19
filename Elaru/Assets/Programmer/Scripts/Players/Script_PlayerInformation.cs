using UnityEngine;
using Valve.VR;

public class Script_PlayerInformation : Script_Singleton<Script_PlayerInformation>
{
    //Vars for keeping track of level
    public int PlayerLevel { get; private set; }

    public int PlayerExp { get; private set; }

    [SerializeField]
    private int _xpNeeded = 1000;
    private float _levelScalingStat = .05f; //Use this to scale the stats with (5%)

    //Player stats
    [SerializeField]
    private int _maxHealth = 100;
    [SerializeField]
    private float _noiseLevel = 1f;
    [SerializeField]
    private float _visibilityLevel = 1f;
    [SerializeField]
    private float _nrOfUpgradeSlots = 2;
    [SerializeField]
    private float _slowDownTime = 1f;
    private int _currentHealth = 0;
    private int _amountOfDataKeys = 0;

    //Accesors for stats that are important to enemies
    public float SlowDownTime { get { return _slowDownTime; } }
    public float VisibilityLevel { get { return _visibilityLevel; } }
    public float NoiseLevel { get { return _noiseLevel; } }

    //Accesors upgrade slots for upgrade bench
    public float NrOfUpgradeSlots
    {
        get { return _nrOfUpgradeSlots; }
        private set { _nrOfUpgradeSlots = value; }
    }

    /// <summary>
    /// Get will return the amount of datakeys, set will add the value to the total amount of keys.
    /// </summary>
    public int AmountOfDataKeys { get { return _amountOfDataKeys; } set { _amountOfDataKeys += value; } }

    //Combat getter setter
    public bool IsInCombat { get; set; }

    //Respawn position
    public Vector3 PlayerSpawnPosition = Vector3.zero;

    //Track player size
    public float PlayerWaistHeight { get; set; }
    public float PlayerHeight { get; set; }

    private Script_LocomotionBase _locomotionInstance;

    public bool IsInBase { get; set; }

    [SerializeField]
    private GameObject _cityObject = null;

    void Awake()
    {
        //Set up
        _currentHealth = _maxHealth;
        _locomotionInstance = Script_LocomotionBase.Instance;
        if (PlayerSpawnPosition == Vector3.zero)
            PlayerSpawnPosition = _locomotionInstance.transform.position;

        //Safety to avoid infinite leveling loops
        if (_xpNeeded < 1)
            _xpNeeded = 1000;
    }

    public void GainXP(int xpAmount)
    {
        //Add to current xp
        PlayerExp += xpAmount;

        //If level treshold is reached, level up
        if (PlayerExp > _xpNeeded)
            LevelUp();
    }

    private void LevelUp(bool forceLevel = false)
    {
        while (PlayerExp > _xpNeeded || forceLevel) //Maybe somehow player has enough xp to level up twice
        {
            Debug.Log("Playered leveled up");
            PlayerLevel++;

            //Substract xp needed for level
            PlayerExp -= _xpNeeded;

            //Scale new xp needed
            _xpNeeded = (int)(_xpNeeded * (1 + _levelScalingStat));

            //Adjust stats
            _noiseLevel = _noiseLevel * (1 - _levelScalingStat);
            _visibilityLevel = _visibilityLevel * (1 - _levelScalingStat);

            _slowDownTime = (int)(_maxHealth * (1 + _levelScalingStat));

            //Update player new max health and heal player
            _maxHealth = (int)(_maxHealth * (1 + _levelScalingStat));
            _currentHealth = _maxHealth;

            //Gain upgrade slots every 3 levels till you have 7 slots (7 == max)
            if (PlayerLevel % 3 == 0 && _nrOfUpgradeSlots < 7)
                _nrOfUpgradeSlots++;

            forceLevel = false;
        }
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;

        //Companion looks sad if player got hit
        Script_CompanionAI.CompanionState = Script_CompanionAI.CompanionMode.Sad;

        //Check if player is dead
        if (_currentHealth <= 0)
            Die();
    }

    private void Die()
    {
#if DEBUG
        Debug.Log("Player died");
#endif
        //Disable city
        EnableCity(false);

        // Prevent teleport or any locomotion on death
        Script_LocomotionBase.Instance.ToggleLocomotionAndFading(false);

        //Put player in respawn position
        _locomotionInstance.CameraRig.transform.position = PlayerSpawnPosition;
        _locomotionInstance.ScriptLocomotionDash.OverrideSafePlace(PlayerSpawnPosition);

        Script_LocomotionBase.Instance.ToggleLocomotionAndFading(true);

        //Reset health
        _currentHealth = _maxHealth;
        //Unlock region for safety
        _locomotionInstance.ScriptLocomotionDash.LockRegion(false);
        //Track death achievement
        Script_AchievementManager.Instance.AddDeathToAchievement();

        //Clear locomotion locking
        Script_LocomotionBase.Instance.ScriptLocomotionDash.LockRegion(false, null, 0f, true);

        //Stop teleport
        Script_LocomotionBase.Instance.ScriptLocomotionDash.ForceStopDash();

        // Play the game over sound
        Script_AudioManager.Instance.PlaySFX("GameOver", Vector3.zero, 1f, 1f, 0f, false, Script_AudioManager.SoundType.SFX_2D);
    }

    public void LoadFromSave(int playerLevel, int xp)
    {
        PlayerExp = 0;
        PlayerLevel = 0;
        //Level up till loaded level
        for (var i = 0; i < playerLevel - 1; ++i)
        {
            LevelUp(true);
        }
        PlayerExp = xp;

        //Safety if somehow player saves with more xp needed than to level up?
        //Should be impossible, but just in case
        if (PlayerExp > _xpNeeded)
            LevelUp();
    }

    public void EnableCity(bool value)
    {
        if (_cityObject != null)
            _cityObject.SetActive(value);
    }
}

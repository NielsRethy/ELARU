using System.Collections.Generic;

public class Script_WeaponManager : Script_Singleton<Script_WeaponManager>
{
    public Script_Sword Sword { get; private set; }
    public Script_Gun Gun { get; private set; }

    //Lists to save weapon data
    private List<int> _swordMods = new List<int>();
    private List<int> _gunMods = new List<int>();
    private List<bool> _found = new List<bool>();

    public List<int> GunMods
    {
        get { SaveMods(true); return _gunMods; }
        set { _gunMods = value; }
    }
    public List<int> SwordMods
    {
        get { SaveMods(false); return _swordMods; }
        set { _swordMods = value; }
    }

    public List<bool> FoundWeapons
    {
        get { SaveMods(false); return _found; }
        set { _found = value; }
    }

    private void SaveMods(bool isGun)
    {
        //Get save data from weapons
        if (isGun)
        {
            if (Gun == null) return;
            _gunMods = Gun.GetMods();
            _found[0] = Gun.HasBeenFound;
        }
        else
        {
            if (Sword == null) return;
            _swordMods = Sword.GetMods();
            _found[1] = Gun.HasBeenFound;
        }
    }

    public void Awake()
    {
        _found.Add(false);
        _found.Add(false);
    }

    public void Load()
    {
        //Load into weapons
        Invoke("LoadWeapons", 0.5f);
    }

    private void LoadWeapons()
    {
        //Set up weapons with loaded data
        if (Gun != null)
        {
            if (_found[0])
                Gun.HeldFirstTime(null);
            Gun.LoadWeapon(_gunMods, _found[0]);
        }
        if (Sword != null)
        {
            if (_found[1])
                Sword.HeldFirstTime(null);
            Sword.LoadWeapon(_swordMods, _found[1]);
        }
    }

    public void RegisterWeapon(Script_Gun gunScript)
    {
        Gun = gunScript;
    }

    public void RegisterWeapon(Script_Sword swordScript)
    {
        Sword = swordScript;
    }
}

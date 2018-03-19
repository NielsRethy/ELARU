using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_TrophyManager : Script_Singleton<Script_TrophyManager>
{
    [SerializeField]
    private List<Script_Trophy> _trophies = new List<Script_Trophy>();

    public void RegisterPickup(ItemType type)
    {
                _trophies[((int)type)-1].FoundCollectible();
    }
}

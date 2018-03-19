using System;
using System.Collections.Generic;
using UnityEngine;

public class Script_KillAchievement : Script_Achievement
{
    [Serializable]
    enum EnemyKillType
    {
        All,
        Deaf,
        Blind,
    }

    [SerializeField]
    //What type of enemy to kill for this achievement
    private EnemyKillType KillType = EnemyKillType.All;

    //Stat track vars
    private uint _numberToKill = 10;
    private uint _numberKilled = 0;

    void Awake()
    {
        if (_tierList.Count == 0)
            return;

        //Set up achievement checking
        Type = AchievementType.Kill;
        Script_AchievementManager.Instance.RegisterAchievement(this);
        _numberToKill = _tierList[_currentTier];
    }

    public void CheckAchievementUpdate(GameObject enemy)
    {
        if (_completed)
            return;

        //Check enemy type
        Script_BlindAI bs = null;
        Script_DeafAI ds = null;
        if (KillType == EnemyKillType.Deaf || KillType == EnemyKillType.All)
            ds = enemy.GetComponent<Script_DeafAI>();
        if (KillType == EnemyKillType.Blind || KillType == EnemyKillType.All)
            bs = enemy.GetComponent<Script_BlindAI>();
        
        //Check if enemy type corresponds with achievement kill type
        bool validKill = false;
        switch (KillType)
        {
            case EnemyKillType.All:
                validKill = ds != null || bs != null;
                break;
            case EnemyKillType.Deaf:
                validKill = ds != null;
                break;
            case EnemyKillType.Blind:
                validKill = bs != null;
                break;
        }

        //Count valid kill
        if (validKill)
            ++_numberKilled;

        if (_numberKilled == _numberToKill)
        {
            //Complete current tier
            Complete(_currentTier);

            //Start next tier
            if (_currentTier < _tierList.Count)
                _numberToKill = _tierList[_currentTier];
        }
    }

    public override AchievementSaveData GetSaveData()
    {
        //Generate save data from achievement info
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            IntSave = _numberKilled,
            CurrentCompletedTier = _currentTier
        };

        return sd;
    }

    public override void LoadFromSaveData(AchievementSaveData sd)
    {
        base.LoadFromSaveData(sd);
        _numberKilled = sd.IntSave;
    }
}

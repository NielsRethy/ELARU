using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_DeathAchievement : Script_Achievement
{
    private uint _deathCount = 0;
    private uint _achieveDeathToll = 1;

    private void Awake()
    {
        if (_tierList.Count == 0)
            return;

        //Set up achievement checking
        Type = AchievementType.Death;
        Script_AchievementManager.Instance.RegisterAchievement(this);

        //Start next tier
        if (_currentTier < _tierList.Count)
            _achieveDeathToll = _tierList[_currentTier];
    }

    public void AddDeath()
    {
        if (_completed)
            return;

        ++_deathCount;

        if (_deathCount == _achieveDeathToll)
        {
            //Complete current tier
            Complete(_currentTier);
            //Start next tier
            if (_currentTier < _tierList.Count - 1)
                _achieveDeathToll = _tierList[_currentTier];
        }
    }

    public override AchievementSaveData GetSaveData()
    {
        //Generate save data from achievement info
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            IntSave = _deathCount,
            CurrentCompletedTier =  _currentTier
        };

        return sd;
    }

    public override void LoadFromSaveData(AchievementSaveData sd)
    {
        base.LoadFromSaveData(sd);
        _deathCount = sd.IntSave;
    }
}

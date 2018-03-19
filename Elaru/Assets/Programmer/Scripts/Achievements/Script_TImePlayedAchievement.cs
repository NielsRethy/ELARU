using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_TImePlayedAchievement : Script_Achievement
{
    private float _timePlayed = 0f;
    private float _timeToPlay = 30f;

    private void Awake()
    {
        if (_tierList.Count == 0)
            return;

        //Set up achievement checking
        Script_AchievementManager.Instance.RegisterAchievement(this);
        _timeToPlay = _tierList[_currentTier];
    }

    private void Update()
    {
        if (_completed)
            return;

        _timePlayed += Time.deltaTime;

        if (_timePlayed >= _timeToPlay)
        {
            //Complete current tier
            Complete(_currentTier);

            //Start next tier
            if (_currentTier < _tierList.Count)
                _timeToPlay = _tierList[_currentTier];
        }
    }

    public override AchievementSaveData GetSaveData()
    {   
        //Generate save data from achievement info
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            FloatSave = _timePlayed,
            CurrentCompletedTier = _currentTier
        };

        return sd;
    }

    public override void LoadFromSaveData(AchievementSaveData sd)
    {
        base.LoadFromSaveData(sd);
        _timePlayed = sd.FloatSave;
    }
}

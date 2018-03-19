using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_CollectionAchievement : Script_Achievement
{
    [SerializeField]
    private ItemType TypeToCollect = ItemType.None;

    private uint _numberToCollect = 0;
    private uint _collectedAmount = 0;

    private void Awake()
    {
        if (_tierList.Count == 0)
            return;

        //Set up achievement tracking
        Type = AchievementType.Collection;
        Script_AchievementManager.Instance.RegisterAchievement(this);

        //Start next tier
        if (_currentTier < _tierList.Count)
            _numberToCollect = _tierList[_currentTier];
    }

    public void CheckAchievementUpdate(GameObject collectedObject)
    {
        if (_completed)
            return;

        //Check if collected type matches achievement type
        var collectableScript = collectedObject.GetComponent<Script_Collectable>();
        if (TypeToCollect == ItemType.All || collectableScript.Type == TypeToCollect)
            ++_collectedAmount;

        //Check if current tier complete
        if (_collectedAmount >= _numberToCollect)
        {
            Complete(_currentTier);
            if (_currentTier < _tierList.Count - 1)
                _numberToCollect = _tierList[_currentTier];
        }
    }

    public override AchievementSaveData GetSaveData()
    {
        //Generate save data from achievement info
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            IntSave = _collectedAmount,
            CurrentCompletedTier =  _currentTier
        };

        return sd;
    }

    public override void LoadFromSaveData(AchievementSaveData sd)
    {
        base.LoadFromSaveData(sd);
        _collectedAmount = sd.IntSave;
    }
}

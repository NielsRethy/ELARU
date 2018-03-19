using UnityEngine;

public class Script_ModUseAchievment : Script_Achievement
{
    private uint _modsUsed = 0;
    private uint _achieveToll = 2;

    private void Awake()
    {
        if (_tierList.Count == 0)
            return;

        //Set up achievement checking
        Type = AchievementType.Modding;
        Script_AchievementManager.Instance.RegisterAchievement(this);

        //Start next tier
        if (_currentTier < _tierList.Count)
            _achieveToll = _tierList[_currentTier];
    }

    public void AddModUsed()
    {
        if (_completed)
            return;

        ++_modsUsed;

        if (_modsUsed >= _achieveToll)
        {
            //Complete current tier
            Complete(_currentTier);
            Debug.Log("Completed tier " + _currentTier);
            //Start next tier
            if (_currentTier < _tierList.Count - 1)
                _achieveToll = _tierList[_currentTier];
        }
    }

    public override AchievementSaveData GetSaveData()
    {
        //Generate save data from achievement info
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            IntSave = _modsUsed,
            CurrentCompletedTier = _currentTier
        };

        return sd;
    }

    public override void LoadFromSaveData(AchievementSaveData sd)
    {
        base.LoadFromSaveData(sd);
        _modsUsed = sd.IntSave;
    }
}

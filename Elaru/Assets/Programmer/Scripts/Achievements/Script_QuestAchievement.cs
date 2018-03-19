using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class Script_QuestAchievement : Script_Achievement
{
    [SerializeField]
    private List<Script_QuestLinker> _reportProgressLinks;

    private void Awake()
    {
        if (_reportProgressLinks.Count == 0)
            return;

        Type = AchievementType.Quest;
        Script_AchievementManager.Instance.RegisterAchievement(this);

        //Report on current link completion
        _reportProgressLinks[_currentTier].OnComplete += AchievementUpdate;
    }

    private void AchievementUpdate()
    {
        if (_completed)
            return;

        _reportProgressLinks[_currentTier].OnComplete -= AchievementUpdate;

        //Complete current tier
        Complete(_currentTier);
        _reportProgressLinks[_currentTier].OnComplete += AchievementUpdate;

        Debug.Log("Quests " + Script_QuestManager.Instance.GetCompletion() * 100 + "% completed");
    }
}

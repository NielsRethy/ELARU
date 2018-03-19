using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Script_AchievementManager : Script_Singleton<Script_AchievementManager>
{
    //List of all achievements
    public List<Script_Achievement> Achievements { get; private set; }

    //Lists of achievements by type
    private List<Script_CollectionAchievement> _collectionAchievements = new List<Script_CollectionAchievement>();
    private List<Script_KillAchievement> _killAchievements = new List<Script_KillAchievement>();
    private List<Script_DeathAchievement> _deathAchievements = new List<Script_DeathAchievement>();
    private List<Script_ModUseAchievment> _modAchievements = new List<Script_ModUseAchievment>();
   
    public void RegisterAchievement(Script_Achievement a)
    {
        if (Achievements == null)
            Achievements = new List<Script_Achievement>();

        if (Achievements.Contains(a))
        {
            Debug.Log("Tried to register same achievement more than once");
            return;
        }

        Achievements.Add(a);

        //Put achievements together in scene
        a.transform.SetParent(gameObject.transform);

        //Register achievement in correct type list
        switch (a.GetAchievementType())
        {
            case Script_Achievement.AchievementType.Collection:
                _collectionAchievements.Add(a as Script_CollectionAchievement);
                break;
            case Script_Achievement.AchievementType.Kill:
                _killAchievements.Add(a as Script_KillAchievement);
                break;
            case Script_Achievement.AchievementType.Death:
                _deathAchievements.Add(a as Script_DeathAchievement);
                break;
            case Script_Achievement.AchievementType.Modding:
                _modAchievements.Add(a as Script_ModUseAchievment);
                break;
            case Script_Achievement.AchievementType.Undefined:
                break;
        }
    }

    public void UpdateCollectionAchievements(GameObject collectedObject)
    {
        //Update all collection achievements with newly collected item
        _collectionAchievements.ForEach(x => x.CheckAchievementUpdate(collectedObject));
    }

    public void UpdateKillAchievement(GameObject enemy)
    {
        //Update kill achievement with newly killed enemy
        _killAchievements.ForEach(x => x.CheckAchievementUpdate(enemy));
    }

    public void AddDeathToAchievement()
    {
        //Add death to death achievements
        _deathAchievements.ForEach(x => x.AddDeath());
    }

    public void AddModToAchievement()
    {
        //Add mod usage to mod use achievements
        _modAchievements.ForEach(x => x.AddModUsed());
    }

    public List<AchievementSaveData> GetAchievementSaveDatas()
    {
        List<AchievementSaveData> r = new List<AchievementSaveData>();
        Achievements.ForEach(x => r.Add(x.GetSaveData()));
        return r;
    }

    public void LoadAchievementsFromSaveDatas(List<AchievementSaveData> lsd)
    {
        foreach (var sd in lsd)
        {
            //Find achievement that matches ID
            var a = Achievements.FirstOrDefault(x => x.GetID() == sd.ID);
            if (a != null)
                //Load achievement data
                a.LoadFromSaveData(sd);
        }
    }
}

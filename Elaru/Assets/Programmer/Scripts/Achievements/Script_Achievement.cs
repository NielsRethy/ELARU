using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
//Used to save achievement info that matters
public class AchievementSaveData
{
    public uint ID = 0;
    public bool Completed = false;
    public uint IntSave = 0;
    public float FloatSave = 0f;
    public int CurrentCompletedTier = 0;
    public List<int> CollectionIndices = null;
}

public class Script_Achievement : MonoBehaviour
{
    public enum AchievementType
    {
        Collection,
        Kill,
        Quest,
        Death,
        Modding,
        Undefined,
    }

    [SerializeField]
    private String _achievementName = null;

    protected AchievementType Type = AchievementType.Undefined;
    protected bool _completed = false;
    public int CompletedTiers { get { return _currentTier; } }

    [SerializeField]
    protected List<uint> _tierList;
    protected int _currentTier = 0;

    protected uint _uniqueId;

    protected void Complete(int tier)
    {
        Debug.Log("Completed tier " + _currentTier);
        //Check if last tier is completed
        if (tier == _tierList.Count - 1)
        {
            _completed = true;
            Debug.Log("Completed all tiers of achievement: " + _achievementName);
        }

        ++_currentTier;
    }

    public AchievementType GetAchievementType()
    {
        return Type;
    }

    public virtual AchievementSaveData GetSaveData()
    {
        //Generate base save data
        AchievementSaveData sd = new AchievementSaveData
        {
            ID = GetID(),
            Completed = _completed,
            CurrentCompletedTier = _currentTier
        };

        return sd;
    }

    public virtual void LoadFromSaveData(AchievementSaveData sd)
    {
        if (GetID() != sd.ID)
        {
            Debug.Log("Loading savedata from wrong ID");
            return;
        }

        //Load base achievement vars
        _completed = sd.Completed;
        _currentTier = sd.CurrentCompletedTier;
    }

    private void GenerateID()
    {
        //Generate unique ID based on name and position
        var objectNameHash = name.GetHashCode();
        var achieveNameHash = _achievementName.GetHashCode();
        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f));

        _uniqueId = (uint)(objectNameHash + posHash + achieveNameHash * (int)Type + _tierList.Count);
    }

    public uint GetID()
    {
        if (_uniqueId == 0)
            GenerateID();
        return _uniqueId;
    }
}

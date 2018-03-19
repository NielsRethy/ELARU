using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
//Class for saving linker data
public class QuestLinkerSaveData
{
    public uint ID;
    public bool LinkStarted;
    public bool LinkCompleted;
    public List<QuestSaveData> QuestSaveList;
}

public class Script_QuestLinker : MonoBehaviour
{
    public bool IsMainQuest = false;

    //Display text vars
    public string QuestLinkName;
    public string QuestDescription;

    //List of quests contained in this linker
    public List<Script_BaseQuest> QuestList = new List<Script_BaseQuest>();

    //Vars to show text in front of player when new quest is active
    private GameObject _questTextPrefab = null;
    [SerializeField]
    private float _questTextShowTime = 5f;

    //Quest state vars
    public bool IsLinkStarted { get; private set; }
    public bool IsLinkCompleted { get; set; }
    public Action OnComplete;

    //Currently active quest tracking
    public Script_BaseQuest ActiveQuest { get; set; }
    private int _activeQuestIndex;

    private uint _questLinkId;

    [SerializeField]
    private int _xpReward = 500;


    //Text display in front of player vars
    private struct QuestInfo
    {
        public QuestInfo(string title, string descr, float showTime)
        {
            Title = title;
            Description = descr;
        }
        public string Title;
        public string Description;
    }

    private Queue<QuestInfo> _questTextQueue = new Queue<QuestInfo>();

    private bool _currentlyShowingText = false;


    private void Awake()
    {
        //TODO: Check out quest manager relevance
        if (IsMainQuest)
            Script_QuestManager.Instance.RegisterLink(this);

        //Load text prefab
        _questTextPrefab = Resources.Load<GameObject>("Prefabs/QuestText");

        //Link quests to questlinker
        foreach (var quest in QuestList)
            quest.ParentQuestLinker = this;

        //Check for automatic start
        if (IsLinkStarted)
            BeginLink();
    }


    private void ShowQuestText(string qName, string description)
    {
        //Add text to queue
        _questTextQueue.Enqueue(new QuestInfo(qName, description, _questTextShowTime));

        //Run text queue if not active yets
        if (!_currentlyShowingText)
            ShowNextText();
    }

    private void ShowNextText()
    {
        //Text queue completed
        if (_questTextQueue.Count == 0)
        {
            _currentlyShowingText = false;
            return;
        }

        //Show first text from queue
        var currQuestInfo = _questTextQueue.Dequeue();
        var playerTransform = Camera.main.gameObject.transform;
        var t = Instantiate(_questTextPrefab, playerTransform.position + 2 * playerTransform.forward, playerTransform.rotation);
        t.transform.SetParent(playerTransform);
        t.transform.GetChild(0).GetComponent<Text>().text = currQuestInfo.Title;
        t.transform.GetChild(1).GetComponent<Text>().text = currQuestInfo.Description;
        Destroy(t, _questTextShowTime);
        _currentlyShowingText = true;

        //Show next text after a while
        Invoke("ShowNextText", _questTextShowTime);
    }

    public void BeginLink()
    {
        IsLinkStarted = true;

        //Start first quest
        _activeQuestIndex = 0;
        ActiveQuest = QuestList[0];
        ActiveQuest.StartQuest();
        ActiveQuest.OnComplete += CurrentQuestComplete;
        ActiveQuest.OnFail += CurrentQuestFailed;
        ShowQuestText(ActiveQuest.QuestName, ActiveQuest.QuestDescription);
    }

    public void CheckActiveQuest()
    {
        if (!QuestList[_activeQuestIndex].IsActive)
            QuestList[_activeQuestIndex].StartQuest();
    }

    public void ResetLink()
    {
        //Reset all quests
        foreach (var q in QuestList)
            q.ResetQuest();

        //Reset linker state
        IsLinkStarted = false;
        IsLinkCompleted = false;

        Debug.Log("Link has been reset");
    }

    public void SetActiveQuestLink(int linkNr)
    {
        _activeQuestIndex = linkNr;
        IsLinkStarted = false;
        IsLinkCompleted = false;
        Script_QuestGiver.Instance.SetAccepted = false;
    }

    private void CurrentQuestComplete()
    {
        ShowQuestText(ActiveQuest.QuestName, "Completed");

        //Check if link is complete
        if (_activeQuestIndex == QuestList.Count - 1)
        {
            Debug.Log("Quest link completed " + QuestLinkName);
            IsLinkCompleted = true;

            //Give player xp reward
            Script_PlayerInformation.Instance.GainXP(_xpReward);

            //Call complete action
            if (OnComplete != null)
                OnComplete.Invoke();
        }

        //Start next quest in link
        if (_activeQuestIndex < QuestList.Count - 1 && !QuestList[_activeQuestIndex + 1].IsActive)
        {
            //Unsub events from completed quest
            ActiveQuest.OnComplete -= CurrentQuestComplete;
            ActiveQuest.OnFail -= CurrentQuestFailed;

            //Start next quest
            ++_activeQuestIndex;
            if (_activeQuestIndex >= QuestList.Count)
                return;
            ActiveQuest = QuestList[_activeQuestIndex];
            ActiveQuest.StartQuest();
            ActiveQuest.OnComplete += CurrentQuestComplete;
            ActiveQuest.OnFail += CurrentQuestFailed;

            //Show quest text to player
            ShowQuestText(ActiveQuest.QuestName, ActiveQuest.QuestDescription);
        }
    }

    private void CurrentQuestFailed()
    {
        ShowQuestText(ActiveQuest.QuestName, "Failed");
    }

    private void GenerateLinkID()
    {
        //GenerateID based quests and position
        for (var i = 0; i < QuestList.Count; ++i)
        {
            var questID = QuestList[i].GetQuestID();
            _questLinkId += (uint)(questID * (i + .5f));
        }

        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f));
        _questLinkId += (uint)(posHash + QuestList.Count);
    }

    public uint GetLinkID()
    {
        if (_questLinkId == 0)
            GenerateLinkID();
        return _questLinkId;
    }

    public QuestLinkerSaveData GetSaveData()
    {
        //Get individual quest save data
        var baseQuestSaveDatas = new List<QuestSaveData>();
        QuestList.ForEach(x => baseQuestSaveDatas.Add(x.GetSaveData()));

        //Create link save data
        var sd = new QuestLinkerSaveData()
        {
            ID = GetLinkID(),
            LinkCompleted = IsLinkCompleted,
            LinkStarted = IsLinkStarted,
            QuestSaveList = baseQuestSaveDatas
        };
        return sd;
    }

    public void LoadFromSaveData(QuestLinkerSaveData sd)
    {
        if (sd.ID != GetLinkID())
        {
            Debug.Log("Trying to load quest from invalid ID");
            return;
        }

        //Load completion and started state
        IsLinkCompleted = sd.LinkCompleted;
        IsLinkStarted = sd.LinkStarted;

        //Load individual objective states
        foreach (var q in QuestList)
        {
            var index = QuestList.IndexOf(q);
            q.LoadFromSaveData(sd.QuestSaveList[index]);
        }

        //Start first non completed quest
        var completedQuests = QuestList.Count(x => x.IsCompleted);
        _activeQuestIndex = completedQuests;

        if (!IsLinkStarted)
            return;

        ActiveQuest = QuestList[_activeQuestIndex];
        ActiveQuest.StartQuest();
        ActiveQuest.OnComplete += CurrentQuestComplete;
        ActiveQuest.OnFail += CurrentQuestFailed;
        ShowQuestText(ActiveQuest.QuestName, ActiveQuest.QuestDescription);
    }
}

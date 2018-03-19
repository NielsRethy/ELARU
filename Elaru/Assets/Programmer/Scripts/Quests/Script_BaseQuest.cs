using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
//Serializable class to save important quest info
public class QuestSaveData
{
    public uint ID = 0;
    public bool Completed = false;
    public List<int> CollectedIndices = null;
    public bool QuestStarted = false;
}

[Serializable]
public enum QuestType
{
    Kill,
    Collect,
    Follow,
    Exploration,
    Destroy,
    Boss
}



public class Script_BaseQuest : MonoBehaviour
{
    [Header("Quest variables")]
    public Transform StartPosition;
    public Transform EndPosition;
    public QuestType Type;

    //Objects that are important for this quest
    //Kill -> Kill targets
    //Collect -> Objects to collect
    //Follow -> Target to follow
    //Exploration -> Empty
    public List<GameObject> InteractableObjects = new List<GameObject>();

    //Keep track of spawned quest targets
    private List<GameObject> _spawnedObjects = new List<GameObject>();
    //Keep track of already collected objects
    private List<int> _collectedIndices = new List<int>();

    //Linker this quest belongs to
    public Script_QuestLinker ParentQuestLinker { get; set; }

    //Info to display
    public String QuestName;
    public String QuestDescription;

    //Quest completed
    public bool IsCompleted { get; private set; }
    public Action OnComplete;

    //Vars for distance checks
    [SerializeField]
    private float _handInDistance = 4f;

    private float _followRange = 20f;
    private const float _maxOutOfRangeTime = 5f;
    private float _outOfRangeTimer = 0f;

    private bool _startedFollow = false;

    //Quest started vars
    private bool _questStarted = false;
    public bool IsActive { get { return _questStarted; } }

    //Player
    private GameObject _cameraRig = null;

    //Can not be seen for certain quests
    public bool PlayerCanNotBeSeen = false;
    private bool _playerIsSeen = false;

    [SerializeField]
    private Script_BombLocation _bombLocation = null;

    [SerializeField]
    private Script_ObjectiveParticleManager _particleManager = null;

    private uint _uniqueQuestId;

    //Action to invoke when quest fails
    public Action OnFail;
    private Script_LocomotionBase _locBase = null;

    [SerializeField]
    private Script_OpenFence _bossDoor = null;


    void Start()
    {
        //Cache player
        _cameraRig = Script_LocomotionBase.Instance.CameraRig;
        _locBase = Script_LocomotionBase.Instance;

        //Disable interactable quest original targets
        foreach (var o in InteractableObjects)
            o.SetActive(false);

        //Hide bomb location at start
        if (_bombLocation != null)
            _bombLocation.gameObject.SetActive(false);

        if (_particleManager != null)
            _particleManager.SetActive(false);

        OnFail += () =>
        {
            Debug.Log("Quest failed");
            ParentQuestLinker.ResetLink();
            ParentQuestLinker.SetActiveQuestLink(0);
        };
    }

    private void OnDrawGizmos()
    {
        if (EndPosition != null) Gizmos.DrawWireSphere(EndPosition.transform.position, _handInDistance);
    }

    //Check quest completion
    void Update()
    {
        //Do not update completed or unstarted quests
        if (IsCompleted || !_questStarted)
            return;

        //Check if player has been seen while he wasn't supposed to
        if (PlayerCanNotBeSeen && _playerIsSeen)
        {
            FailQuest();
            return;
        }

        //Update quest according to type
        switch (Type)
        {
            case QuestType.Kill:
                KillQuestCheck();
                break;
            case QuestType.Collect:
                CollectQuestCheck();
                break;
            case QuestType.Follow:
                FollowQuestCheck();
                break;
            case QuestType.Exploration:
                ExplorationQuestCheck();
                break;
            case QuestType.Destroy:
                DestroyQuestCheck();
                break;
            case QuestType.Boss:
                BossQuest();
                break;
            default:
                throw new ArgumentOutOfRangeException("Quest has unknown type, can not be checked");
        }

        if (IsCompleted)
            return;

        //Update minimap for moving target
        if (Type == QuestType.Kill || Type == QuestType.Follow)
        {
            if (ParentQuestLinker.IsMainQuest)
                Script_MinimapUpdater.Instance.SetMainQuest(true, InteractableObjects[0].transform.position);
            else
                Script_MinimapUpdater.Instance.SetSideQuest(true, InteractableObjects[0].transform.position);
        }
    }

    private void BossQuest()
    {
        if (_bossDoor != null)
            _bossDoor.Open();
        CompleteQuest();
    }

    private void DestroyQuestCheck()
    {
        if (_bombLocation == null)
        {
            Debug.Log("Quest was set to destoy mission without a bomb location.");
            return;
        }

        //Check if bomb has been placed
        if (_bombLocation.IsPlaced)
            CompleteQuest();
    }


    //Check if targets have been killed
    void KillQuestCheck()
    {
        foreach (var o in _spawnedObjects)
        {
            //Active target -> target is still alive
            if (o != null && o.activeSelf)
                return;
        }

        //Complete quest if all targets are dead
        CompleteQuest();
    }

    //Check if all interactables have been collected
    void CollectQuestCheck()
    {
        foreach (var o in _spawnedObjects)
        {
            var index = _spawnedObjects.IndexOf(o);
            //Check if object is outside of deliver range
            if ((o.transform.position - EndPosition.position).sqrMagnitude > _handInDistance * _handInDistance)
            {
                //Remove item if it was collected before
                if (_collectedIndices.Contains(index))
                {
                    _collectedIndices.Remove(index);
                    Debug.Log("Collected " + _collectedIndices.Count + " / " + InteractableObjects.Count + " objects");
                }
            }
            //Object inside range
            else if (!_collectedIndices.Contains(index))
            {
                //Add it to collected if it is not in it yet
                _collectedIndices.Add(index);
                Debug.Log("Collected " + _collectedIndices.Count + " / " + InteractableObjects.Count + " objects");
            }
        }

        //Check if player collected all objects
        if (_collectedIndices.Count != InteractableObjects.Count)
            return;

        CompleteQuest();

        HandSide hand = HandSide.None;
        //Destroy collected items
        foreach (GameObject obj in _spawnedObjects)
        {
            hand = obj.GetComponent<Script_PickUpObject>().ControlHandSide;
            if (hand != HandSide.None)
                _locBase.GetPickUpFromHand(hand).Drop();

            if (!obj.CompareTag("Sword"))
                Destroy(obj);
        }
    }

    //Follow quest checking
    void FollowQuestCheck()
    {
        if (!_startedFollow)
        {
            //Check if player is in range for first time -> start follow quest
            if ((_cameraRig.transform.position - _spawnedObjects[0].transform.position).sqrMagnitude <
                _followRange * _followRange)
            {
                _startedFollow = true;
                //TODO: follow target start patrolling
            }
            return;
        }

        if (!_spawnedObjects[0].activeSelf || _spawnedObjects[0] == null)
        {
            FailQuest();
            return;
        }

        //Check if player is out of range
        if ((_cameraRig.transform.position - _spawnedObjects[0].transform.position).sqrMagnitude >
                _followRange * _followRange)
            _outOfRangeTimer += Time.deltaTime;

        else
            _outOfRangeTimer = 0f;

        //Failed quest after too long out of range
        if (_outOfRangeTimer >= _maxOutOfRangeTime)
            FailQuest();

        //Target arrived at destination and player is in range
        if ((_spawnedObjects[0].transform.position - EndPosition.transform.position).sqrMagnitude <= _handInDistance * _handInDistance &&
                (_cameraRig.transform.position - _spawnedObjects[0].transform.position).sqrMagnitude <= _followRange * _followRange)
            CompleteQuest();
    }

    //Check if player reached exploration target
    void ExplorationQuestCheck()
    {
        if ((_cameraRig.transform.position - EndPosition.position).sqrMagnitude < _handInDistance * _handInDistance)
        {
            if (_spawnedObjects.Count > 0)
            {
                foreach (var o in InteractableObjects)
                {
                    var kt = Instantiate(o, o.transform.position, o.transform.rotation);
                    kt.name = "QuestTarget";
                    kt.SetActive(true);
                    _spawnedObjects.Add(kt);
                }
            }
            CompleteQuest();
        }
    }

    public void StartQuest()
    {
        _questStarted = true;
        _playerIsSeen = false;

        if (_particleManager != null)
        {
            _particleManager.SetActive(true);
        }

        //Spawn quest targets copies -> copies because of easy reset
        if (Type == QuestType.Kill || Type == QuestType.Collect || Type == QuestType.Follow || Type == QuestType.Destroy)
        {
            foreach (var o in InteractableObjects)
            {
                var kt = Instantiate(o, o.transform.position, o.transform.rotation);
                kt.name = "QuestTarget";
                kt.SetActive(true);
                _spawnedObjects.Add(kt);
            }
        }

        //Activate bomb location
        if (_bombLocation != null)
        {
            _bombLocation.gameObject.SetActive(true);
            _bombLocation.Activate();
        }

        //Set target position on minimap
        var miniMap = Script_MinimapUpdater.Instance;
        if (miniMap != null && EndPosition != null)
        {
            if (ParentQuestLinker.IsMainQuest)
                miniMap.SetMainQuest(true, EndPosition.position);
            else
                miniMap.SetSideQuest(true, EndPosition.position);
        }
    }

    public void ResetQuest()
    {
        //Reset tracking
        _questStarted = false;
        IsCompleted = false;
        _startedFollow = false;
        _outOfRangeTimer = 0f;

        //Destroy all spawned objects
        foreach (var ko in _spawnedObjects)
            Destroy(ko);

        _spawnedObjects.Clear();

        //Reset bomb location
        if (_bombLocation != null)
        {
            _bombLocation.ResetBomb();
            _bombLocation.gameObject.SetActive(false);
        }
    }

    void FailQuest()
    {
        ResetQuest();

        Debug.Log("Quest failed");
        if (OnFail != null)
            OnFail.Invoke();

        //Remove indication from minimap
        if (ParentQuestLinker.IsMainQuest)
            Script_MinimapUpdater.Instance.SetMainQuest(false);
        else
            Script_MinimapUpdater.Instance.SetSideQuest(false);
    }

    /// <summary>
    /// Sets whether player was seen by enemies
    /// </summary>
    public void PlayerWasSeen()
    {
        _playerIsSeen = true;
    }

    //Method called when quest completed
    private void CompleteQuest()
    {
        IsCompleted = true;

        //Remove indication on minimap
        var miniMap = Script_MinimapUpdater.Instance;
        if (miniMap != null)
        {
            if (ParentQuestLinker.IsMainQuest)
                miniMap.SetMainQuest(false);
            else
                miniMap.SetSideQuest(false);
        }

        //Invoke complete action if there is one
        if (OnComplete != null)
            OnComplete.Invoke();
    }

    private void GenerateID()
    {
        //Take object name into account
        var objectNameHash = name.GetHashCode();
        //Quest name hash
        var nameHash = QuestName.GetHashCode();
        //Quest description hash
        var descrHash = QuestDescription.GetHashCode();
        //Position hash
        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f));

        //Combine hashes
        _uniqueQuestId = (uint)(objectNameHash * nameHash / (1 / (descrHash + .5f)) + posHash + InteractableObjects.Count);
    }

    public uint GetQuestID()
    {
        if (_uniqueQuestId == 0)
            GenerateID();
        return _uniqueQuestId;
    }

    public QuestSaveData GetSaveData()
    {
        //Create save data based on quest info
        QuestSaveData sd = new QuestSaveData()
        {
            ID = GetQuestID(),
            Completed = IsCompleted,
            CollectedIndices = _collectedIndices,
            QuestStarted = _questStarted
        };
        return sd;
    }

    public void LoadFromSaveData(QuestSaveData sd)
    {
        if (sd.ID != GetQuestID())
        {
            Debug.Log("Trying to load quest from invalid ID");
            return;
        }

        IsCompleted = sd.Completed;
        _collectedIndices = sd.CollectedIndices;
        //Queststarting is set from quest linker loading
    }
}

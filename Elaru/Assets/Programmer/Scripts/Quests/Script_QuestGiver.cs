using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Script_QuestGiver : Script_Singleton<Script_QuestGiver>
{
    //Main quest vars
    [SerializeField]
    private List<Script_QuestLinker> _mainQuestLineLinkers;
    private int _activeMainQuestIndex = 0;

    //Images
    [SerializeField]
    private Sprite _questGiverEmptySprite = null;
    [SerializeField]
    private Sprite _questGiverSprite = null;
    [SerializeField]
    private Image _questGiverImage = null;
    [SerializeField]
    private GameObject _minimap = null;


    //Side quest vars
    private Script_QuestLinker _currentSideQuest;
    private Script_QuestLinker _changeSideQuest;
    [SerializeField]
    private Script_CollisionArea _sideQuestDockingArea;

    //World space UI vars
    [SerializeField]
    private Text _mainQuestNameText;
    [SerializeField]
    private Text _mainQuestDescriptionText;
    [SerializeField]
    private Text _sideQuestNameText;
    [SerializeField]
    private Text _sideQuestDescriptionText;

    //Hand scanners
    [SerializeField]
    private Script_HandScanner _yesHandScanner;
    [SerializeField]
    private Script_HandScanner _noHandScanner;

    public bool SetAccepted { private get { return _accepted; } set { _accepted = value; } }

    private bool _accepted;

    public bool IsQuestActive()
    {
        return _mainQuestLineLinkers[_activeMainQuestIndex].IsLinkStarted;
    }

    private enum ScreenState
    {
        Off,
        On,
        ChoosingSideQuest
    }

    private ScreenState _currentScreenState = ScreenState.Off;

    private void Awake()
    {
        //Clear Screen
        _mainQuestNameText.text = "";
        _mainQuestDescriptionText.text = "";

        _sideQuestNameText.text = "";
        _sideQuestDescriptionText.text = "";

        //Set up side quest disc docking action
        _sideQuestDockingArea.TriggerEnterAction += DiskDroppedInBay;

        //Set up hand scanners to start
        _yesHandScanner.TriggeredAction += PressedYes;
        _noHandScanner.TriggeredAction += PressedNo;

        //SetImages
        _questGiverImage.sprite = _questGiverEmptySprite;
        _minimap.SetActive(false);
    }

    /// <summary>
    /// Shows current main quest info on screen, switches to next one if old is completed
    /// </summary>
    public void ActivateMainQuestScreen()
    {
        //Switch to next quest if old one has been completed
        if (_mainQuestLineLinkers[_activeMainQuestIndex].IsLinkCompleted)
            ++_activeMainQuestIndex;

        //Show current quest info on screen
        if (_activeMainQuestIndex < _mainQuestLineLinkers.Count)
            ShowMainQuestInfo(_mainQuestLineLinkers[_activeMainQuestIndex]);

    }

    /// <summary>
    /// Begins current quest if needed, clears screen
    /// </summary>
    public void CloseMainQuestScreen()
    {
        //Ignore if all quests have been completed
        if (_activeMainQuestIndex >= _mainQuestLineLinkers.Count)
            return;

        //Start current quest if it is not started yet
        if (!_mainQuestLineLinkers[_activeMainQuestIndex].IsLinkStarted && _accepted)
            _mainQuestLineLinkers[_activeMainQuestIndex].BeginLink();

        //Clear screen
        _mainQuestNameText.text = "";
        _mainQuestDescriptionText.text = "";
        //SetImages
        _questGiverImage.sprite = _questGiverEmptySprite;
        _minimap.SetActive(false);
    }

    public void ActivateSideQuestScreen()
    {
        //Show potential new side quest
        if (_changeSideQuest != null)
            ShowSideQuestInfo(_changeSideQuest);
        //Show active side quest if there is no new one
        else if (_currentSideQuest != null)
            ShowSideQuestInfo(_currentSideQuest);
        //Clear screen if there is no side quest
        else
        {
            _sideQuestNameText.text = "";
            _sideQuestDescriptionText.text = "";
        }
    }

    public void CloseSideQuestScreen()
    {
        if (_currentSideQuest == null)
            return;

        //Start current side quest if it is not started yet
        if (!_currentSideQuest.IsLinkStarted)
            _currentSideQuest.BeginLink();

        //Clear screen
        _sideQuestNameText.text = "";
        _sideQuestDescriptionText.text = "";
    }

    public void AcceptNewSideQuest(bool accept)
    {
        if (_changeSideQuest == null)
            return;

        if (accept)
        {
            if (_currentSideQuest != null)
            {
                //Reset old side quest
                _currentSideQuest.ResetLink();
                _currentSideQuest.OnComplete -= ActiveSideQuestCompleted;
                //Eject previous side quest disc
                EnableDisk(_currentSideQuest.gameObject, true);

                //Stop checking for discs for short time to avoid re entering of same quest
                _sideQuestDockingArea.TriggerEnterAction -= DiskDroppedInBay;
                Invoke("EnableSideQuestDock", 1f);
            }

            _currentSideQuest = _changeSideQuest;
            _currentSideQuest.OnComplete += ActiveSideQuestCompleted;
            _changeSideQuest = null;
        }
        else
        {
            //Eject change side quest disc
            EnableDisk(_changeSideQuest.gameObject, true);
            _changeSideQuest = null;

            //Stop checking for discs for short time to avoid re entering of same quest
            _sideQuestDockingArea.TriggerEnterAction -= DiskDroppedInBay;
            Invoke("EnableSideQuestDock", 1f);
        }

        //Update screen if necessary
        ActivateSideQuestScreen();
    }

    private void EnableSideQuestDock()
    {
        _sideQuestDockingArea.TriggerEnterAction += DiskDroppedInBay;
    }

    private void ActiveSideQuestCompleted()
    {
        Debug.Log("active side quest completed");
        //Destroy disk to avoid doing same quest twice
        Destroy(_currentSideQuest.gameObject);
        _currentSideQuest = null;
    }

    private void UpdateMinimapOnEnable()
    {
        Script_MinimapUpdater.Instance.SetMainQuest(true, _mainQuestLineLinkers[_activeMainQuestIndex].QuestList[0].EndPosition.position);
    }

    public void EnableScreenTexts()
    {
        //Set quest to minimap
        _currentScreenState = ScreenState.On;
        Invoke("UpdateMinimapOnEnable", 0.2f);
        
        //Show quest info
        ActivateSideQuestScreen();
        ActivateMainQuestScreen();
        _currentScreenState = ScreenState.On;

        //Lock player to region
        var playerPos = Script_LocomotionBase.Instance.CameraRig.transform.position;
        var pos = transform.position;
        pos.y = playerPos.y;
        Script_LocomotionBase.Instance.ScriptLocomotionDash.LockRegion(true, pos, 4f);

        //SetImages
        _questGiverImage.sprite = _questGiverSprite;
        _minimap.SetActive(true);
    }

    public void CloseScreens()
    {
        CloseMainQuestScreen();
        CloseSideQuestScreen();
        _currentScreenState = ScreenState.Off;
        //Free player
        Script_LocomotionBase.Instance.ScriptLocomotionDash.LockRegion(false, null, 0, true);

        //SetImages
        _questGiverImage.sprite = _questGiverEmptySprite;
        _minimap.SetActive(false);
    }

    private void DiskDroppedInBay(Collider other)
    {
        if (other.tag == "PickUp" && _changeSideQuest == null)
        {
            var ql = other.GetComponent<Script_QuestLinker>();
            if (ql != null)
            {
                //other.gameObject.SetActive(false);
                EnableDisk(other.gameObject, false);
                _changeSideQuest = ql;
                //Update screen to show new side quest info
                EnableScreenTexts();
                //Change screen state
                _currentScreenState = ScreenState.ChoosingSideQuest;
            }
        }
    }

    private void PressedYes()
    {
        Debug.Log("Pressed yes");
        if (_currentScreenState == ScreenState.On)
        {
            _accepted = true;
            CloseScreens();
        }
        else if (_currentScreenState == ScreenState.ChoosingSideQuest)
        {
            AcceptNewSideQuest(true);
            CloseScreens();
            _currentScreenState = ScreenState.Off;
        }
        else if (_currentScreenState == ScreenState.Off)
        {
            EnableScreenTexts();
        }
    }

    private void PressedNo()
    {
        if (_currentScreenState == ScreenState.On)
        {
            //SET no quest
            Script_MinimapUpdater.Instance.SetMainQuest(false);

            //Turn off screen
            CloseScreens();
            _currentScreenState = ScreenState.Off;
        }
        else if (_currentScreenState == ScreenState.ChoosingSideQuest)
        {
            AcceptNewSideQuest(false);
            CloseScreens();
            _currentScreenState = ScreenState.Off;
        }
    }

    private void ShowMainQuestInfo(Script_QuestLinker ql)
    {
        //Show quest name and description on screen
        _mainQuestNameText.text = ql.QuestLinkName;
        _mainQuestDescriptionText.text = ql.QuestDescription;
    }

    private void ShowSideQuestInfo(Script_QuestLinker ql)
    {
        //Show quest name and description on screen
        _sideQuestNameText.text = ql.QuestLinkName;
        _sideQuestDescriptionText.text = ql.QuestDescription;
    }

    private void EnableDisk(GameObject o, bool enable)
    {
        o.GetComponent<Rigidbody>().isKinematic = !enable;
        o.GetComponent<MeshRenderer>().enabled = enable;
        o.GetComponent<BoxCollider>().enabled = enable;
    }

    public void PlayerIsSeen()
    {
        if (_mainQuestLineLinkers == null)
            return;

        if (_activeMainQuestIndex < _mainQuestLineLinkers.Count && _mainQuestLineLinkers[_activeMainQuestIndex].ActiveQuest != null)
            _mainQuestLineLinkers[_activeMainQuestIndex].ActiveQuest.PlayerWasSeen();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Show screen when player comes in range
        if (other.tag == "Player")
            //Show quest info
            EnableScreenTexts();
    }

    //private void OnTriggerExit(Collider other)
    //{
    //    //Close screens when player comes in range
    //    if (other.tag == "Player" && !_accepted)
    //        CloseScreens();
    //}

    //TODO: delete update after testing
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Y))
            PressedYes();
        if (Input.GetKeyDown(KeyCode.N))
            PressedNo();
    }
}

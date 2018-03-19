using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Script_MGLeverManager : MonoBehaviour
{
    //State enum of lever
    public enum LeverMinigameState
    {
        Up,
        Down,
        None
    }

    //Levers that are part of the puzzle
    [SerializeField]
    private List<Script_LeverMinigame> _levers = new List<Script_LeverMinigame>();
    //Answer sequence
    [SerializeField]
    private List<LeverMinigameState> _correctAnswer = new List<LeverMinigameState>();
    //Time player has to complete the puzzle
    [SerializeField]
    private float _resetTimerMax = 5.0f;

    private Script_LeverHandTracking[] _children;

    private bool _solved = false;
    private bool _checkForReset = false;
    private uint _id = 0;
    private float _resetTimer = 0.0f;

    //Event to call on puzzle completion
    public UnityEvent OnComplete;
    public Action OnCompleteAction;

    //Getter setter state of puzzle (used in save)
    public bool IsPuzzleSolved
    {
        get { return _solved; }
        set { _solved = value; Complete(); }
    }

    private void Start()
    {
        //Get variables
        Script_MinigameManager.Instance.RegisterPuzzle(this);

        //Generate custom id for savefile
        GenerateID();

        //Set states
        _children = GetComponentsInChildren<Script_LeverHandTracking>();

        //Safety check
        if (_levers.Count != _correctAnswer.Count)
            Debug.LogError("Number of levers does not match length of correct password");
    }

    public void Complete()
    {
        Debug.Log("Lever puzzle completed");

        //Deactivate levers
        foreach (var l in _levers)
            l.gameObject.transform.GetChild(0).GetComponent<Script_LeverHandTracking>().enabled = false;

        //Activate the complete state
        if (OnComplete != null)
            OnComplete.Invoke();

        if (OnCompleteAction != null)
            OnCompleteAction.Invoke();

        //Lock levers
        for (var i = 0; i < _levers.Count; i++)
            _children[i].Locked = true;
        
        _solved = true;

        //Register completed puzzle for loading
        Script_MinigameManager.Instance.SetCompleted(_id);
    }

    private void Update()
    {
        if (!_checkForReset || _solved)
            return;

        //Check reset timer
        _resetTimer += Time.deltaTime;
        if (!(_resetTimer > _resetTimerMax))
            return;

        //Reset puzzle
        _resetTimer -= _resetTimerMax;

        for (var i = 0; i < _levers.Count; i++)
            _children[i].ResetLever();

        foreach (var l in _levers)
        {
            l.IsTop = false;
            l.ResetLamp();
            l.State = LeverMinigameState.None;
        }

        _checkForReset = false;
    }

    //Called when minigame lever enters a trigger
    public void CheckSolved()
    {
        _checkForReset = true;
        _solved = true;

        //Check if levers are in correct position
        for (var i = 0; i < _levers.Count; i++)
        {
            if (_levers[i].State != _correctAnswer[i])
            {
                _solved = false;
                break;
            }
        }

        if (_solved)
            Complete();
    }
    private void GenerateID()
    {
        //Generate custom id
        var objectNameHash = name.GetHashCode();
        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f)) + 7;

        _id = (uint)(objectNameHash + posHash);
    }

    public uint GetID()
    {
        //Returns the custom id
        if (_id == 0)
            GenerateID();
        return _id;
    }
}

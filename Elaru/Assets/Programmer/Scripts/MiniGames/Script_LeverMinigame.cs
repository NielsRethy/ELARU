using UnityEngine;

public class Script_LeverMinigame : Script_Lever
{
    //Private vars
    [SerializeField]
    private GameObject _topLamp = null;
    [SerializeField]
    private GameObject _botLamp = null;

    private Script_MGLeverManager _manager = null;
    private Material _botLampMat = null;
    private Material _topLampMat = null;

    public Script_MGLeverManager.LeverMinigameState State { get; set; }

    private void Start()
    {
        State = Script_MGLeverManager.LeverMinigameState.None;

        //Get variables
        if (_manager == null && transform.parent != null)
            _manager = transform.parent.GetComponent<Script_MGLeverManager>();
        else if (_manager == null)
            _manager.GetComponent<Script_MGLeverManager>();

        _botLampMat = _botLamp.GetComponent<MeshRenderer>().materials[1];
        _topLampMat = _topLamp.GetComponent<MeshRenderer>().materials[1];

        //Reset all lamps
        ResetLamp();
    }

    public override void BotTrigger()
    {
        ResetLamp();
        //Set state
        State = Script_MGLeverManager.LeverMinigameState.Down;
        IsTop = false;

        //Reset then change color
        _botLampMat.color = Color.yellow;
        _botLampMat.SetColor("_EmissionColor", Color.red);
        if (_manager != null)
            _manager.CheckSolved();
    }

    public override void TopTrigger()
    {
        ResetLamp();
        //Set state
        State = Script_MGLeverManager.LeverMinigameState.Up;
        IsTop = true;

        //Reset then change color
        _topLampMat.color = Color.yellow;
        _topLampMat.SetColor("_EmissionColor", Color.green);
        if (_manager != null)
            _manager.CheckSolved();
    }

    public void ResetLamp()
    {
        //Resets color of meshes
        //State = Script_MGLeverManager.LeverMinigameState.None;
        _topLampMat.color = Color.white;
        _topLampMat.SetColor("_EmissionColor", Color.black);
        _botLampMat.color = Color.white;
        _botLampMat.SetColor("_EmissionColor", Color.black);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class Script_BossAI : MonoBehaviour
{
    public enum BossStage
    {
        Unfound,
        Start, //Started "Bossfight"
        Crane, //Crane Rotates To Let Player On Shoulder
        Shoulder, //Player Has To Solve Puzzle In Shoulder Path To Allow Access To Scaffolding
        Bottom, //Player Has To Throw Debris In Other Shoulder Pannel To Lower Lader To Neck
        Head, //Player Has To End Robot In Head
        Finished
    }

    private static BossStage _stage = BossStage.Unfound;

    [SerializeField]
    private Transform _crane;
    [SerializeField]
    private Script_Ladder _craneLadder;
    [SerializeField]
    private Script_Ladder _debrisLadder;
    private bool _ladderDropped = false;
    [SerializeField]
    private Transform _flap;
    [SerializeField]
    private Transform _head;

    private float _headRotationSpeed = 50f;
    private float _craneRotationSpeed = 10f;

    private Script_Locomotion_TeleDash _loco;
    private float _lockRange = 3f;

    //Handle With Teleport On Click To Go Inside Shoulder Path
    [SerializeField]
    private Script_TeleportOnClick _teleportToShoulderArea = null;
    [SerializeField]
    private Script_TeleportOnClick _teleportBackFromShoulderArea = null;
    [SerializeField]
    private Script_BossPlugger _shoulderPlugChecking = null;
    [SerializeField]
    private List<Script_Light> _shoulderRoomLights = new List<Script_Light>();
    [SerializeField]
    private Script_CollisionArea _debrisThrowArea = null;
    [SerializeField]
    private List<GameObject> _debrisBlades = new List<GameObject>();
    [SerializeField]
    private Script_Light _debrisLight = null;
    private int _debrisCount = 0;
    private int _debrisNeeded = 1;

    [SerializeField]
    private Script_MGLeverManager _headLeverMinigame;

    [SerializeField]
    private Script_TeleportOnClick _teleportToHeadArea;
    [SerializeField]
    private Script_TeleportOnClick _teleportBackFromHeadArea;
    [SerializeField]
    private Script_BossPlugger _headPlugChecking;
    [SerializeField]
    private List<Script_Light> _headRoomLights = new List<Script_Light>();
    [SerializeField]
    private Script_Light _headHatchLight = null;

    // Use this for initialization
    void Start()
    {
        _loco = Script_LocomotionBase.Instance.ScriptLocomotionDash;

        //Deactivate all teleport objects at start
        _teleportToShoulderArea.TeleportAction += DisableCity;
        _teleportToShoulderArea.enabled = false;
        _teleportBackFromShoulderArea.TeleportAction += EnableCity;
        _teleportBackFromShoulderArea.enabled = false;
        _teleportToHeadArea.TeleportAction += DisableCity;
        _teleportToHeadArea.enabled = false;
        _teleportBackFromHeadArea.TeleportAction += EnableCity;
        _teleportBackFromHeadArea.enabled = false;

        _headLeverMinigame.enabled = false;

        _shoulderRoomLights.ForEach(x => x.SetLights(false));
        _headRoomLights.ForEach(x => x.SetLights(false));
    }

    private void DisableCity()
    {
        Script_PlayerInformation.Instance.EnableCity(false);
    }

    private void EnableCity()
    {
        Script_PlayerInformation.Instance.EnableCity(true);
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 angles;
        switch (_stage)
        {
            case BossStage.Crane:
                //Rotate head
                angles = _head.localEulerAngles;
                angles.z += _headRotationSpeed * Time.deltaTime;
                _head.localEulerAngles = angles;
                break;

            case BossStage.Shoulder:
                //Rotate crane
                if (_crane.localEulerAngles.y < 34f && !_ladderDropped)
                {
                    float angle = _craneRotationSpeed * Time.deltaTime;
                    angles = _crane.localEulerAngles;
                    angles.z += angle;
                    _crane.localEulerAngles = angles;
                }
                else if (!_ladderDropped)
                {
                    _ladderDropped = true;
                    _craneLadder.ToggleLadder();
                }

                //Rotate head
                angles = _head.localEulerAngles;
                angles.z += _headRotationSpeed * Time.deltaTime;
                _head.localEulerAngles = angles;
                break;

            case BossStage.Bottom:
                //Rotate head
                angles = _head.localEulerAngles;
                angles.z += _headRotationSpeed * Time.deltaTime;
                _head.localEulerAngles = angles;

                if (_debrisCount < _debrisNeeded)
                    _debrisBlades.ForEach(x => x.transform.Rotate(0, _headRotationSpeed, 0));

                break;

            case BossStage.Head:
                //Rotate head to front
                if (_head.transform.localEulerAngles.z > 5f)
                {
                    float angle = _headRotationSpeed * Time.deltaTime;
                    angles = _head.localEulerAngles;
                    angles.z += angle;
                    _head.localEulerAngles = angles;
                    if (_head.transform.localEulerAngles.z < 20f)
                        _headRotationSpeed = 50f * _head.transform.localEulerAngles.z / 40f;
                }
                break;
        }
    }

    public void PuzzleComplete(int stage)
    {
        _stage = (BossStage)stage;

        switch (_stage)
        {
            case BossStage.Start:
                //Nothing special to do on start complete
                break;

            case BossStage.Crane:
                //Crane stage completed -> Start shoulder stage
                //Activate teleport handles to shoulderroom
                _teleportToShoulderArea.enabled = true;

                _loco.LockRegion(false, null, 0f, true);

                //Go to next stage when plugging is completed
                _shoulderPlugChecking.OnComplete += () =>
                {
                    PuzzleComplete((int)BossStage.Shoulder);
                    _flap.localEulerAngles = new Vector3(348, 191, 172);
                };
                break;

            case BossStage.Shoulder:
                //Shoulder stage completed -> Start bottom stage
                //Let player go back
                _teleportBackFromShoulderArea.enabled = true;

                //Turn on lights
                _shoulderRoomLights.ForEach(x => x.SetLights(true));

                //Prevent from going in again
                _teleportToShoulderArea.enabled = false;

                //Check for debris throwing
                _debrisThrowArea.TriggerEnterAction += DebrisTriggerEnter;

                //Drop ladder to debris
                _debrisLadder.ToggleLadder();
                break;

            case BossStage.Bottom:
                //Bottom stage complete -> start head stage
                //Open teleport to head area
                _teleportToHeadArea.enabled = true;

                //Go to next stage when plugging completed
                _headPlugChecking.OnComplete += () => { PuzzleComplete((int)BossStage.Head); };

                //Turn on light at hatch
                if (_headHatchLight != null)
                    _headHatchLight.SetLights(true);
                break;

            case BossStage.Head:
                //Head stage complete -> Finished
                //Let player come back from head area
                _teleportBackFromHeadArea.enabled = true;

                //Turn on lights
                _headRoomLights.ForEach(x => x.SetLights(true));

                //Prevent going back to head area
                _teleportToHeadArea.enabled = false;
                break;

            case BossStage.Finished:
                Script_AudioManager.Instance.PlaySFX("Victory", Vector3.zero, 1f, 1f, 0f, false, Script_AudioManager.SoundType.SFX_2D);
                return;
        }

        ++_stage;
        Debug.Log("Current stage: " + _stage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && _stage == BossStage.Unfound || _stage == BossStage.Start)
        {
            Debug.Log("Player entered collider");
            PuzzleComplete((int)BossStage.Start);
        }
    }

    private void DebrisTriggerEnter(Collider other)
    {
        if (other.tag == "PickUp")
        {
            ++_debrisCount;
            var destr = other.GetComponent<Script_Destructible>();
            if (destr != null)
                destr.DestroyBlock();
            else
                Destroy(other.gameObject);

            if (_debrisCount >= _debrisNeeded)
            {
                if (_debrisLight != null)
                    _debrisLight.SetLights(true);
                _debrisThrowArea.TriggerEnterAction -= DebrisTriggerEnter;
                //Turn on lights around lever
                _headLeverMinigame.enabled = true;
                _headLeverMinigame.OnCompleteAction += () => PuzzleComplete((int)BossStage.Bottom);
            }
        }
    }
}

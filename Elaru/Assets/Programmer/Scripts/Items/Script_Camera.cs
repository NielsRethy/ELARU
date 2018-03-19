using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Script_Camera : MonoBehaviour
{
    //Variables
    [SerializeField]
    private float _visibilityTimer = 1.5f;
    [SerializeField]
    private bool _alarmDeaf = false;
    [SerializeField]
    private bool _alarmBlind = false;
    [SerializeField]
    private GameObject _cameraHead = null;
    [SerializeField]
    private List<GameObject> _rotationPoints = new List<GameObject>(2); // Add 2 points to rotate towards.
    [SerializeField]
    private float _movementSpeed = 0.5f;
    [SerializeField]
    private float _lingerTime = 0.9f;
    [SerializeField]
    private bool _isPuzzleCam = false;

    private float _timer = 0.0f;
    private bool _alarmed = false;
    private bool _seesPlayer = false;
    private int _moveLeft = 0;
    private Vector3 playerPos = Vector3.zero;

    private Vector3 _oldRotCheck = Vector3.zero;

    [SerializeField]
    private Script_Light _lightControl = null;

    //Triggers
    private void OnTriggerEnter(Collider other)
    {
        //Check for player
        if (!other.tag.Equals("Player")) return;
        _seesPlayer = true;
            //_lightControl.SetLights(true);
    }

    private void OnTriggerExit(Collider other)
    {
        //Check for player
        if (!other.tag.Equals("Player")) return;
        _seesPlayer = false;
        //_lightControl.SetLights(false);
    }

    //Update
    private void Update()
    {
        _lightControl.SetLights(!_seesPlayer);
        //Can the camera see the player
        if (_seesPlayer)
        {
            //Set alarmed state
            Script_QuestGiver.Instance.PlayerIsSeen();
            if (playerPos == Vector3.zero) playerPos = Script_LocomotionBase.Instance.CameraRig.transform.position;
            AlarmAi();
        }
        else
        {
            var targetPoint = _rotationPoints[_moveLeft].transform.position;
            var targetRotation = Quaternion.LookRotation(targetPoint - _cameraHead.transform.position, Vector3.up);
            _cameraHead.transform.rotation = Quaternion.Lerp(_cameraHead.transform.rotation, targetRotation, Time.deltaTime * _movementSpeed);
            var vec = (targetPoint - _cameraHead.transform.position);
            if (Vector3.Angle(_cameraHead.transform.forward, vec) < _lingerTime)
                _moveLeft = _moveLeft == 0 ? 1 : 0;
            playerPos = Vector3.zero;
            // Debug.Log("angle = " + rot);
        }
    }

    /// <summary>
    /// Call this function to send an alarm to the ai.
    /// </summary>
    private void AlarmAi()
    {
        Debug.Log("Can see the player");

        if (_alarmDeaf)
        {
            //Send nudes lol, alarm to deaf robots
            Script_ManagerEnemy.Instance.Light(playerPos, 50);
        }
        if (_alarmBlind)
        {
            //Send alarm to blind robots
            Script_ManagerEnemy.Instance.Sound(playerPos,Script_EnemyBase.SoundType.Alarm, 50);
        }
    }
}

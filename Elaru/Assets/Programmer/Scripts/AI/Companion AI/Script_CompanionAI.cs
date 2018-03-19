using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Script_CompanionAI : Script_BehaviorTreeFramework
{
    //Editor 
    [SerializeField] private GameObject _player = null;

    [SerializeField] private float _speedCompanion = 2.0f;
    [SerializeField] private float _rotateSpeedCompanion = 10.0f;
    [SerializeField] private float _moveRadius = 10.0f;
    [SerializeField] private float _maxDistance = 10.0f;
    [SerializeField] private float _teleportDistance = 20.0f;

    [SerializeField] private bool _vr = true;

    // [SerializeField] private Powers _activePowerUp = Powers.Scanner;
    [SerializeField] private float _timeBetweenPowers = 10.0f;

    [SerializeField] private float _radiusScannerPowerUp = 100;
    [SerializeField] private float _speedScannerPowerUp = 3.0f;

    [SerializeField] private float _scannerTimerPowerUp = 2;
    [SerializeField] private float _distanceCloseMap = 10.0f;

    // [SerializeField] private float _shieldTimerPowerUp = 2;
    [SerializeField] private Color _depthColor = Color.blue;

    [SerializeField] private float _radiusHelping = 10.0f;
    [SerializeField] private Animator _animation;
    [SerializeField] private float _hoverDistance = 1.0f;
    [SerializeField] private float _hoverSpeed = 1.0f;
    [SerializeField] private GameObject _map;
    [SerializeField] private static AudioSource _soundComp;



    private bool _battleMode = false;

    //companion mood
    public enum CompanionMode
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Elaru,
        Error
    }

    static CompanionMode _companionState;
    public static List<Texture> Faces = new List<Texture>();
    private static Material _matCompanionScreen;
    private static bool _exist = false;
    private static bool _canChangeMood = true;

    public static CompanionMode CompanionState
    {
        get { return _companionState; }
        set
        {
            if (!_exist || !_canChangeMood)
                return;

            //Cooldown on mood change
            _canChangeMood = false;
            Script_LocomotionBase.Instance.Invoke("ResetMood", 3.5f);

            //Update companion face texure
            _matCompanionScreen.mainTexture = Faces[(int) value];
            _matCompanionScreen.SetTexture("_AlphaTex", Faces[(int) value]);

            //play sound
            switch (value)
            {
                case CompanionMode.Happy:
                Script_AudioManager.Instance.PlaySFX("CompEmoHappy", _soundComp);
                    break;
                case CompanionMode.Sad:
                    Script_AudioManager.Instance.PlaySFX("CompEmoSad", _soundComp);
                    break;
                case CompanionMode.Angry:
                    Script_AudioManager.Instance.PlaySFX("CompEmoAngry", _soundComp);
                    break;
                case CompanionMode.Elaru:
                    Script_AudioManager.Instance.PlaySFX("CompEmoElaru", _soundComp);
                    break;
                case CompanionMode.Error:
                    Script_AudioManager.Instance.PlaySFX("CompEmoError", _soundComp);
                    break;
            }
            _companionState = value;
        }
    }


    private Vector3 _randomPosition;
    private NavMeshAgent _navAgent;
    private Script_CompanionObstacleCheck _companion;
    private Script_CompanionColliderEmpty _scannerPos;
    private Script_CompanionColliderEmpty _movePos = null;
    private Script_LocomotionBase _base = null;

    private float _beginHeight = 0.0f;
    private bool _mapMode = false;
    private bool _powerUpMode = false;
    private float _timer = 0.0f;
    private List<GameObject> _enemies = new List<GameObject>();
    private Shader _depthShader;
    private Shader _standardShader;
    private bool _canActivatePower = true;
    private Quaternion _rotation;
    private bool _changeColorEnemies = false;

    private bool _isInHelpMode;
    private bool _ladder;
    private GameObject _helpObj;
    private bool _ladderClosed;
    private LayerMask _enemyLayer;
    private LayerMask _companionLayer;
    //private bool _hover = true;
    private float _angle = 0.0f;
    private bool _moveToScannerPos = true;
    private GameObject _shield;
    private bool _startShield;
    private Material _matCompanion;

    public bool SetActive = false;
    private bool _isActive = true;

    private bool _showMap = false;

    #region Actions

    private Action aMoveRandom;
    private Action aNewLocation;
    private Action aRunCompanionAI;

    private Action aTeleportPlayer;

    //  private Action aBattleMode;
    private Action aMap;

    private Action aActivatePower;
    private Action aActivateHelpMode;
    private Action aEmpty;

    #endregion

    #region Conditions

    Func<bool> fIsInRadius;
    Func<bool> fFindNewLocation;
    Func<bool> fCheckDistance;
    Func<bool> fPlayerInRange;

    Func<bool> fTeleportToPlayer;

    //  Func<bool> fBattleMode;
    Func<bool> fShowMap;

    Func<bool> fHelpMode;
    Func<bool> fPower;

    #endregion

    private void Start()
    {
     
       // _speedScannerPowerUp /= 100.0f;
        _exist = true;
        _enemyLayer = 1 << LayerMask.NameToLayer("Enemies");
        _companionLayer = 1 << LayerMask.NameToLayer("CompanionHelp");
        //Behavior initializing 
        ConditionsInit();
        ActionsInit();
        BehaviorTreeInit();
        _soundComp = GetComponentInChildren<AudioSource>();
        //Cache components
        _base = Script_LocomotionBase.Instance;
        _navAgent = GetComponent<NavMeshAgent>();
        NavMeshHit myNavHit;
        if (NavMesh.SamplePosition(transform.position, out myNavHit, 100, -1))
        {
            transform.position = myNavHit.position;
        }
        _companion = GetComponentInChildren<Script_CompanionObstacleCheck>();

        var newPos = transform.position;
        newPos.y = _player.transform.position.y;
        transform.position = newPos;
        _rotation = _companion.transform.localRotation;

        if (_player != null)
        {
            var colliderEmptyScripts = _player.GetComponentsInChildren<Script_CompanionColliderEmpty>();
            _scannerPos = colliderEmptyScripts[0];
        }

        _beginHeight = _companion.transform.localPosition.y;

        //Cache shaders for switching with ability
       

        //Set mood for companion
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion_Neutral"));
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion_Happy"));
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion_Sad"));
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion_Angry"));
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion ELARUscreen"));
        Faces.Add(Resources.Load<Texture>("CompanionFaces/Companion ERRORscreen"));

        _matCompanionScreen = Resources.Load<Material>("CompanionFaces/Mat_CompanonEmotionscreenfront");
        CompanionState = CompanionMode.Happy;

        //Create shield for shield ability
       _shield = Instantiate(Resources.Load<GameObject>("Shield_Companion"));
        _shield.SetActive(false);

        _matCompanion = Resources.Load<Material>("CompanionFaces/mat_CompanionBody");
        _matCompanion.SetColor("_EmissionColor", Color.green);
        _shield.GetComponent<Renderer>().material.SetFloat("_FadeValue", 1);

        _depthShader = Shader.Find("Custom/Shader_Depth");
        _standardShader = Shader.Find("Standard");

    }

    /// <summary>
    ///  Initialize conditions
    /// </summary>
    private void ConditionsInit()
    {
        fIsInRadius = IsPostitionInRadius;
        fFindNewLocation = IsNewLocation;
        fCheckDistance = () => _companion.CheckInFront();
        fPlayerInRange = IsPlayerOutOfRange;
        fTeleportToPlayer = IsDistanceTooFar;
        //fBattleMode = IsBattelMode;
        fShowMap = ShowTheMap;
        fPower = IsPowerupActive;
        fHelpMode = IsHelpMode;
    }

    /// <summary>
    ///  Initialize Actions
    /// </summary>
    private void ActionsInit()
    {
        aMoveRandom = RandomMovementAroundPlayer;
        aNewLocation = NewLoction;
        aTeleportPlayer = TeleportToBack;
        /// aBattleMode = GoToBattleModePosition;
        aMap = Map;
        aActivatePower = ActivateScanner;
        aActivateHelpMode = MoveToLadder;
        aEmpty = () => { };
    }

    /// <summary>
    ///  Initialize Behaviortree
    /// </summary>
    private void BehaviorTreeInit()
    {
        aRunCompanionAI = Sequencer(
            Selector(fPower, aActivatePower,
                Selector(fHelpMode, aActivateHelpMode,
                    Selector(fShowMap, aMap,
                        Selector(fTeleportToPlayer, aTeleportPlayer,
                            Sequencer(
                                Sequencer(
                                    Sequencer(
                                        Conditional(IsNewLocation, aNewLocation),
                                        Conditional(fIsInRadius, aMoveRandom)),
                                    Conditional(fCheckDistance, aNewLocation)),
                                Conditional(fPlayerInRange, aNewLocation)))))),
            aEmpty);
    }

    void Update()
    {

        if (aRunCompanionAI != null)
        {
            aRunCompanionAI();
        }

        //Check if in VR 
        if (_vr)
        {
            //Add time to see if menu buttons is hold or pressed
            if (_base.GetRightPress(_base.MenuButton) && _canActivatePower && !_mapMode)
            {
                Script_AudioManager.Instance.PlaySFX("CompEmoElaru", _soundComp);
                _powerUpMode = true;
            }

            //Change to map mode
            if (_base.GetLeftPressDown(_base.MenuButton) && !_startShield && !_powerUpMode)
            {
                if (_showMap)
                {
                    DisableMap();
                }
                _mapMode = !_mapMode;
            }

            if (_base.GetRightPress(_base.MenuButton) && _base.GetLeftPressDown(_base.MenuButton))
            {
                DisableShield();
                DisableMap();
            }
        }
        else
        {
            //Add time to see if menu buttons is hold or pressed
            if (Input.GetKey(KeyCode.P) && _canActivatePower && !_mapMode)
            {
                Script_AudioManager.Instance.PlaySFX("CompEmoElaru", _soundComp);
                _powerUpMode = true;
            }

            //Change to map mode
            if (Input.GetKeyDown(KeyCode.M) && !_startShield && !_powerUpMode)
            {
                Script_AudioManager.Instance.PlaySFX("CompEmoElaru", _soundComp);
                if (_showMap)
                {
                    DisableMap();
                }
                _mapMode = !_mapMode;

            }
            if (Input.GetKey(KeyCode.P) && Input.GetKeyDown(KeyCode.M))
            {
                DisableShield();
                DisableMap();
            }
        }

        //if (_hover)
        //{
        //    _angle += _hoverSpeed * Time.deltaTime;
        //    if (_angle > 360)
        //    {
        //        _angle -= 360;
        //    }
        //    _companion.transform.localPosition = new Vector3(_companion.transform.localPosition.x,
        //        _hoverDistance * Mathf.Sin(_angle * Mathf.PI / 180), _companion.transform.localPosition.z);
        //}

        //Change enemies that are seen through wall according to their alert state
        if (_changeColorEnemies)
        {
            foreach (var e in _enemies)
            {
                _depthColor = _stateColor[e.GetComponent<Script_EnemyBase>().GetState()];
                ChangeToDepthSeeThrough(e);
            }
        }


        if (!_isInHelpMode && !_startShield && !_powerUpMode && !_mapMode)
        {
            Vector3 newLocation = Vector3.zero;
            newLocation.y = _beginHeight;
            _companion.transform.localPosition = Vector3.Lerp(_companion.transform.localPosition, newLocation, 0.01f);
            _companion.transform.localRotation = _rotation;


            //if (Mathf.Abs(_companion.transform.localPosition.y - newLocation.y) < 0.1f)
            //{
            //    _angle += _hoverSpeed * Time.deltaTime;
            //    if (_angle > 360)
            //    {
            //        _angle -= 360;
            //    }
            //    _companion.transform.localPosition = new Vector3(_companion.transform.localPosition.x,
            //        _hoverDistance * Mathf.Sin(_angle * Mathf.PI / 180), _companion.transform.localPosition.z);
            //}
        }

        if (_startShield)
        {
         
            if (_shield.transform.localScale.x <= _radiusScannerPowerUp)
            {
                _shield.transform.localScale += new Vector3(5f / 20f, 5f / 20f, 5f / 20f);
                float value = Mathf.Lerp(_shield.GetComponent<Renderer>().material.GetFloat("_FadeValue"), 0, 0.005f);
                _shield.GetComponent<Renderer>().material.SetFloat("_FadeValue", value);
                
            }
            else
            {
            DisableShield();
            }
        }

        if (_showMap)
        {
            _map.SetActive(true);
        }
    }

    #region ActionsMethods

    //Move the companion around the player on a random position
    private void RandomMovementAroundPlayer()
    {
        if (!_navAgent.enabled || !_navAgent.isOnNavMesh)
            return;

        _navAgent.SetDestination(_randomPosition);
        _navAgent.speed = _speedCompanion;
        _navAgent.angularSpeed = 10 * _rotateSpeedCompanion;
    }

    //Find new random location around the player
    private void NewLoction()
    {
        _randomPosition = RandomPos(_player.transform.position, 1.5f, _moveRadius, -1);
    }

    //Teleport companion back to the player back side
    private void TeleportToBack()
    {
        if (!_navAgent || !_navAgent.enabled)
            return;

        if (_navAgent.isOnNavMesh)
        {
            //Recalculate path
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.enabled = false;
        }
        else
        {
            _navAgent.enabled = false;
        }

        //Move behind player
        var playerBack = _player.transform.position + -_player.transform.forward * 2.0f;
        transform.position = playerBack;

        _navAgent.enabled = true;
    }
    //Show map
    private void Map()
    {
       
        if (!_showMap)
        {
           // _hover = false;

            if (_navAgent.hasPath)
            {
                _navAgent.isStopped = true;
                _navAgent.ResetPath();
                _navAgent.isStopped = false;
                _navAgent.enabled = false;
            }

            if (_movePos == null)
            {
                _movePos = Instantiate(_scannerPos, _player.transform);
                _movePos.transform.parent = null;
            }

            var dis = (new Vector2(transform.position.x, transform.position.z) -
                       new Vector2(_movePos.transform.position.x,
                           _movePos.transform.position.z)).sqrMagnitude;

            if (dis > 0.1f * 0.1f)
            {
                _companion.transform.LookAt(new Vector3(_movePos.transform.position.x, _companion.transform.position.y, _movePos.transform.position.z));


                var newLocation = _movePos.transform.position;
                newLocation.y = transform.position.y;
                transform.position = Vector3.Lerp(transform.position, newLocation, 0.1f);

                var comPos = _companion.transform.position;
                comPos.y = _movePos.transform.position.y;
                _companion.transform.position = Vector3.Lerp(_companion.transform.position, comPos, 0.1f);


            }
            else
            {
                _companion.transform.LookAt(new Vector3(_player.transform.position.x,_companion.transform.position.y, _player.transform.position.z));
                _showMap = true;
                _animation.SetBool("Map", true);
                Destroy(_movePos.gameObject);
            }
        }
    }
    void ActivateScanner()
    {

        if (_canActivatePower)
        {
            //_hover = false;

            if (_moveToScannerPos)
            {

                //_animation.SetBool("StartFly", true);

                if (_navAgent.hasPath)
                {
                    _navAgent.isStopped = true;
                    _navAgent.ResetPath();
                    _navAgent.isStopped = false;
                    _navAgent.enabled = false;
                    _animation.SetBool("ReturnToIdle", false);
                }

                if (_movePos == null)
                {
                    _movePos = Instantiate(_scannerPos, _player.transform);
                    _movePos.transform.parent = null;
                }

                var dis = (new Vector2(transform.position.x, transform.position.z) -
                           new Vector2(_movePos.transform.position.x,
                               _movePos.transform.position.z)).sqrMagnitude;

                if (dis > 0.1f * 0.1f)
                {

                    _companion.transform.LookAt(new Vector3(_movePos.transform.position.x, _companion.transform.position.y, _movePos.transform.position.z));

                    var newLocation = _movePos.transform.position;
                    newLocation.y = transform.position.y;
                    transform.position = Vector3.Lerp(transform.position, newLocation, 0.1f);

                    var comPos = _companion.transform.position;
                    comPos.y = _movePos.transform.position.y;
                    _companion.transform.position = Vector3.Lerp(_companion.transform.position, comPos, 0.1f);


                }
                else
                {
                   
                    _moveToScannerPos = false;
                }
            }
            else
            {
                if (!_animation.GetBool("StartScanner"))
                {
                    _companion.transform.LookAt(new Vector3(_player.transform.position.x, _companion.transform.position.y, _player.transform.position.z));
                    _animation.SetBool("StartScanner", true);
                }

                Invoke("ShieldWait",1.5f);
                _canActivatePower = false;
                Destroy(_movePos);
                Script_AudioManager.Instance.PlaySFX("CompScan", _soundComp);
            }
        }
    }


    void MoveToLadder()
    {
        Script_AudioManager.Instance.PlaySFX("CompEmoHappy", _soundComp);
        HelpPlayer(_ladder, _helpObj);
    }
    #endregion

    #region ConditionsMethods

    //Check if you need to find a new location
    private bool IsNewLocation()
    {
        if (_navAgent == null)
            return false;

        return !_navAgent.hasPath;
    }

    //Check if the player is out of range
    private bool IsPlayerOutOfRange()
    {
        return (_player.transform.position - transform.position).sqrMagnitude > _maxDistance * _maxDistance;
    }

    //Check if randomposition is in a radius from the player
    private bool IsPostitionInRadius()
    {
        if (_navAgent == null)
            return false;

        return (_randomPosition - _player.transform.position).sqrMagnitude < _moveRadius * _moveRadius;
    }

    //Check if the distance between the player and companion is to far
    private bool IsDistanceTooFar()
    {
        return (transform.position - _player.transform.position).sqrMagnitude > _teleportDistance * _teleportDistance;
    }

  

    //Check if you need to show to map
    private bool ShowTheMap()
    {
        if (_mapMode && (transform.position - _player.transform.position).sqrMagnitude > _distanceCloseMap * _distanceCloseMap)
        {
            _mapMode = false;
            DisableMap();
        }
        return _mapMode;
    }

    //Check if power up is active 
    private bool IsPowerupActive()
    {

        return _powerUpMode;
    }

    private bool IsHelpMode()
    {
       
        return _isInHelpMode;
         
     
    }
    #endregion

    #region Enumerator

    private IEnumerator DisableDepth()
    {
        yield return new WaitForSeconds(_scannerTimerPowerUp);
        //Remove depth shader from enemies
        foreach (var e in _enemies)
            GetRidOfDepthSeeThrough(e);

        _enemies.Clear();

        //Cooldown for next power activation
        yield return new WaitForSeconds(_timeBetweenPowers);
        _canActivatePower = true;
        _changeColorEnemies = false;
    }

    private void ShieldWait()
    {
        _startShield = true;
        //Cooldown between powers
        _powerUpMode = false;
        _canActivatePower = false;

        _shield.SetActive(true);
        _shield.transform.position = _companion.transform.position;
        _enemies.Clear();
        _changeColorEnemies = true;

        //Find enemies in ability range
        Collider[] hitColliders = Physics.OverlapSphere(_player.transform.position, _radiusScannerPowerUp,
            _enemyLayer, QueryTriggerInteraction.Collide);


        foreach (var enemys in hitColliders)
        {
            if (enemys.CompareTag("Enemy"))
            {
                //Change enemies to see through walls shader
                ChangeToDepthSeeThrough(enemys.gameObject);
                _enemies.Add(enemys.gameObject);
            }

        }

        //Disable see through after a while
        StartCoroutine(DisableDepth());
    }

    private void ActivateAbilityAgain()
    {
        _canActivatePower = true;
        //_hover = true;
        _matCompanion.SetColor("_EmissionColor", Color.green);
    }

    //private void SheidlRepeat()
    //{
    //    _shield.transform.localScale += new Vector3(0.03f, 0.03f, 0.03f);
    //}

    private IEnumerator OpenLadder(GameObject helpObj)
    {

        helpObj.GetComponent<Script_Ladder>().ToggleLadder();
        yield return new WaitForSeconds(3.0f);
        _ladder = false;
        _isInHelpMode = false;
        _powerUpMode = false;
        _canActivatePower = false;
        _navAgent.enabled = true;
        _animation.SetBool("ReturnToIdle", true);
        _animation.SetBool("StartHelping", false);
        _animation.SetBool("StartFly", false);
        yield return new WaitForSeconds(_timeBetweenPowers);
        _canActivatePower = true;
        _ladderClosed = true;
        //_hover = true;
    }

    private static void ResetMood()
    {
        _canChangeMood = true;
    }
    #endregion

    /// <summary>
    ///  Get random pos on navmesh
    /// </summary>
    protected Vector3 RandomPos(Vector3 origin, float min, float max, int area)
    {
        NavMeshHit hit;
        Vector3 dir;
        float dis;
        int maxTries = 20;
        int tries = 0;
        do
        {
            ++tries;
            dir = UnityEngine.Random.insideUnitSphere;
            dis = UnityEngine.Random.Range(min, max);
            dir *= dis;
            origin.y += 1f;
            dir += origin;
        } while (!NavMesh.SamplePosition(dir, out hit, dis, NavMesh.AllAreas) && tries < maxTries);

        if (!NavMesh.SamplePosition(dir, out hit, dis, area))
            NavMesh.SamplePosition(transform.position, out hit, dis, area);

        return hit.position;
    }

    /// <summary>
    ///  Change shader to depth shader form the components and all the children
    /// </summary>
    void ChangeToDepthSeeThrough(GameObject o)
    {
        foreach (var ren in o.GetComponentsInChildren<Renderer>())
        {
            if (!ren.GetComponent<ParticleSystem>() && !ren.GetComponent<TrailRenderer>())
            {
                Material mat = ren.material;
                Material newMat = new Material(_depthShader);

                //Copy properties from old to new material
                newMat.CopyPropertiesFromMaterial(mat);
                newMat.SetColor("_ZColor", _depthColor);

                //Update material
                ren.material = newMat;
                if (o.transform.childCount <= 0)
                    continue;

                //Update material on all children
                foreach (Transform child in o.transform)
                {
                    var cRen = child.GetComponent<Renderer>();
                    if (cRen != null)
                        cRen.material = newMat;
                }
            }
         
        }
    }

    /// <summary>
    ///  Change shader back to original shader
    /// </summary>
    void GetRidOfDepthSeeThrough(GameObject o)
    {
        foreach (var ren in o.GetComponentsInChildren<Renderer>())
        {
            if (!ren.GetComponent<ParticleSystem>() && !ren.GetComponent<TrailRenderer>())
            {
                Material mat = ren.material;
                Material newMat = new Material(_standardShader);

                //Copy properties from depth to standard material
                newMat.CopyPropertiesFromMaterial(mat);
                ren.material = newMat;
                if (o.transform.childCount <= 0)
                    continue;

                //Update material on all children
                foreach (Transform child in o.transform)
                {
                    var cRen = child.GetComponent<Renderer>();
                    if (cRen != null)
                        cRen.material = newMat;
                }
            }
        }
    }

    private void HelpPlayer(bool ladder, GameObject helpObj)
    {
        if (ladder)
        {
          
            if (!Physics.Linecast(_companion.transform.position, helpObj.GetComponentsInChildren<Transform>()[1].position))
            {
                //_animation.SetBool("StartFly", true);
                if (helpObj.GetComponent<Script_Ladder>().LadderIsClosed)
                {
                    if (!_animation.GetBool("StartFly"))
                    {
                       // _hover = false; 
                        _animation.SetBool("StartFly", true);
                    }

                    _ladderClosed = false;
                    var dis = (new Vector2(transform.position.x, transform.position.z) -
                               new Vector2(helpObj.GetComponentsInChildren<Transform>()[1].position.x,
                                   helpObj.GetComponentsInChildren<Transform>()[1].position.z)).sqrMagnitude;

                    if (dis > 0.1f * 0.1f)
                    {
                        var dir = helpObj.GetComponentsInChildren<Transform>()[0].position;
                        dir.y = 0; // kill height differences
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir),
                            10.0f * Time.deltaTime);

                        var newLocation = helpObj.GetComponentsInChildren<Transform>()[1].position;
                        newLocation.y = transform.position.y;
                        transform.position = Vector3.Lerp(transform.position, newLocation, 0.05f);

                        var comPos = _companion.transform.position;
                        comPos.y = helpObj.GetComponentsInChildren<Transform>()[1].position.y;
                        _companion.transform.position = Vector3.Lerp(_companion.transform.position, comPos, 0.05f);
                    }
                    else
                    {
                        if (!_animation.GetBool("StartHelping"))
                        {
                            _animation.SetBool("StartHelping", true);
                            _animation.SetBool("StartFly", false);
                            _animation.SetBool("ReturnToIdle", false);
                        }
                        if (helpObj.GetComponent<Script_Ladder>().LadderIsClosed)
                        {
                            if (!_animation.GetCurrentAnimatorStateInfo(0).IsName("Help"))
                            {
                                StartCoroutine(OpenLadder(helpObj));
                                helpObj.tag = "LadderDone";
                            }
                        }
                    }
                }
            }
            else
            {
                _ladder = false;
                _isInHelpMode = false;
                _powerUpMode = false;
                _navAgent.enabled = true;
                _canActivatePower = true;
                _ladderClosed = true;
                _timer = 0.0f;
            }
        }

    }

    public void SetLadderAtive(GameObject obj)
    {
        //_animation.SetBool("StartFly", true);
        _ladder = true;
        _isInHelpMode = true;
        _helpObj = obj.gameObject;

        _navAgent.isStopped = true;
        _navAgent.ResetPath();
        _navAgent.isStopped = false;    
        _navAgent.enabled = false;
        _ladderClosed = false;
    }

    public void SetDeActive()
    {
        if (_navAgent)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.enabled = false;
        }

        this.gameObject.SetActive(false);

    }
    public void SetActived()
    {
        _navAgent.enabled = true;

    }

    private void DisableMap()
    {
        _showMap = false;
        _map.SetActive(false);
        _navAgent.enabled = true;
        _animation.SetBool("ReturnToIdle", true);
        _animation.SetBool("Map", false);
        //_hover = true;
    }

    private void DisableShield()
    {
        _startShield = false;
        _shield.SetActive(false);
        _shield.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        _shield.GetComponent<Renderer>().material.SetFloat("_FadeValue", 1);
        _animation.SetBool("ReturnToIdle", true);
        _animation.SetBool("StartScanner", false);
        _powerUpMode = false;
        _canActivatePower = false;
        _navAgent.enabled = true;
        _moveToScannerPos = true;
        _matCompanion.SetColor("_EmissionColor", Color.red);
        Invoke("ActivateAbilityAgain", _timeBetweenPowers);
        //_hover = true;
    }
}

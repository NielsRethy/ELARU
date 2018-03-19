using UnityEngine;
using UnityEngine.AI;
using System;

public class Script_DeafAI : Script_EnemyBase
{
    enum ObjectInView
    {
        Head,
        Hands
    }
    #region Variables
    /// <summary>
    ///   Transform of the gun
    /// </summary>
    [SerializeField]
    private Transform _gun = null;
    /// <summary>
    ///   Transform of the eyes / camera
    /// </summary>
    [SerializeField]
    private Transform _eyes = null;
    /// <summary>
    ///   Bullet to instantiate
    /// </summary>
    [SerializeField]
    private GameObject _bullet = null;
    /// <summary>
    ///   Position to instantiate the bullet
    /// </summary>
    [SerializeField]
    private Transform _bulletStart = null;
    /// <summary>
    ///   Probability (max 1f) of looking back
    /// </summary>
    [SerializeField]
    private float _paranoia = 0f;
    /// <summary>
    ///   Probability (max 1f) of wandering
    /// </summary>
    [SerializeField]
    private float _wanderChance = 0.1f;

    //[SerializeField]
    //Transform _armAimIK;

    [SerializeField]
    private GameObject _preShootParticle = null;

    private Script_LocomotionBase _locoBase = null;

    float _emission = 0f;

    float _talkChance = 0.2f;

    [SerializeField]
    private Vector2 _soundFalloff = new Vector2(2, 50);

    #endregion
    #region Conditions
    /// <summary>
    ///   Is player getting to close to AI
    /// </summary>
    Func<bool> fNeedToBackOff;
    /// <summary>
    ///   AI can see player 
    /// </summary>
    Func<bool> fPlayerInVision;

    Func<bool> fFarFromPlayer;
    Func<bool> fHandInView;
    #endregion
    #region Actions
    /// <summary>
    ///   AI reaction to seeing player
    /// </summary>
    Action aSeesPlayer;
    /// <summary>
    ///   AI reaction to not seeing player
    /// </summary>
    Action aDoesnSeePlayer;
    /// <summary>
    ///   AI is backing away from player
    /// </summary>
    Action aBackOff;
    /// <summary>
    ///   AI turns head around, (coroutine that turns bool on and off)
    /// </summary>
    Action aLooksBack;
    Action aWalkToHands;

    Action aSaysSurrender;
    Action aSaysDetected;
    Action aSaysCoastClear;
    Action aSaysWrong;

    Action aRunDeafAI = () => { };
    #endregion
    #region Class Methods
    private void Start()
    {
        BlackBoardInit();
        ConditionsInit();
        ActionsInit();
        BehaviorTreeInit();
        _locoBase = Script_LocomotionBase.Instance;

        if (_preShootParticle != null)
            _preShootParticle.SetActive(false);
    }
    /// <summary>
    ///   Initialize blackboard variables
    /// </summary>
    void BlackBoardInit()
    {
        _type = Script_ManagerEnemy.EnemyType.Deaf;
        InitBB();
        AddVariable<bool>("IsLookingBack", false);
        AddVariable<float>("LookBackTargetX",0f);
        AddVariable<float>("FOV", 60f);
        AddVariable<float>("FOVVertical", 70f); //WIP
        AddVariable<float>("MaxRotateAngle", 8.4f); //WIP
        AddVariable<float>("ViewDistance", 50f);
        AddVariable<float>("BackOffDis", 10f);
        ChangeVariable<Transform>("GunTransform", _gun);
        ChangeVariable<bool>("CanSee", true);
        ChangeVariable<bool>("CanHear", false);
        AddVariable<Transform>("Eyes", _eyes);
        AddVariable<bool>("IsAiming", false);
        AddVariable<bool>("IsRotatingToAim", false);
        AddVariable<ObjectInView>("ObjInView", ObjectInView.Head);
        AddVariable<GameObject>("Bullet", _bullet);
        AddVariable<Transform>("BulletStart", _bulletStart);
        AddVariable<Vector3>("AimTarget", new Vector3());
        AddVariable<Renderer>("Renderer", _renderer);
        AddVariable<Texture>("StartEmmisive", _renderer.material.GetTexture("_EmissionMap"));
        //AddVariable<Transform>("ArmAimIK", _armAimIK);
    }
    /// <summary>
    ///   Link funcs to methods
    /// </summary>
    void ConditionsInit()
    {
        fNeedToBackOff = MNeedToBackOff;
        fPlayerInVision = MPlayerInVision;
        fFarFromPlayer = MFarFromPlayer;
        fHandInView = MSeeingHands;
    }

    /// <summary>
    ///   Link actions to methods
    /// </summary>
    void ActionsInit()
    {
        InitActions();
        aSeesPlayer = MSeesPlayer;
        aDoesnSeePlayer = MDoesntSeePlayer;
        aBackOff = MBackOff;
        aLooksBack = MLooksBack;
        aWalkToHands = MWalkToHand;
        aSaysCoastClear = MSaysCoastClear;
        aSaysDetected = MSaysDetected;
        aSaysSurrender = MSaysSurrender;
        aSaysWrong = MSaysWrong;
    }

    /// <summary>
    ///   Set up behavior tree
    /// </summary>
    void BehaviorTreeInit()
    {
        /// <summary>
        ///   Check if player is visible
        /// </summary>
        Action aCheckPlayer = Selector(fPlayerInVision,
                        aSeesPlayer,
                        aDoesnSeePlayer);
        /// <summary>
        ///   Aim,shoot and swarm towards target
        /// </summary>
        Action aShootAndAim = /*Sequencer(
                                Probable(0.9f, aCheckPlayer),*/
                                Sequencer(
                                    Conditional(fFarFromPlayer, Conditional(fCanCall(new int[] { aSwarmPlayer.GetHashCode(), 5 }),aSwarmPlayer)),
                                    Sequencer(
                                        Sequencer(
                                            Selector(And(And(fCanHitPlayer, fCanShootAgain), Not(fIsShooting)),
                                                aShoot,
                                                Conditional(fHandInView, Conditional(fCanCall(new int[] { aWalkToHands.GetHashCode(), 5 }), aWalkToHands))),
                                            aAim),
                                    aAlarmFriends));

        /// <summary>
        ///   Relocation during combat
        /// </summary>
        Action aFightAndFlight = /*Sequencer(
                                    aSwarmPlayer,*/
                                    Selector(fNeedToBackOff,
                                        Conditional(Not(fIsShooting), Conditional(fCanCall(new int[] { aBackOff.GetHashCode(), 5 }), aBackOff)),
                                        Conditional(fCanHitPlayer, aStopWalking))/*)*/;


        Action aTalkSur = Probable(_talkChance, aSaysSurrender);
        Action aTalkClear = Probable(_talkChance, aSaysCoastClear);
        Action aTalkDet = Probable(_talkChance, aSaysDetected);
        Action aTalkWrong = Probable(_talkChance, aSaysWrong);

        /// <summary>
        ///   Paranoia looking back
        /// </summary>
        Action aParanoia = Probable(_paranoia,
                               Sequencer(
                                   aLooksBack,
                                   Conditional(fCanCall(new int[] { aTalkWrong.GetHashCode(), 15 }),aTalkWrong)));

        aSearchAndDestroy = Selector(And(Not(fIsShooting), fHasForgottenAlarm),
                                Sequencer(
                                    aPatrollingCheck,
                                    Conditional(fCanCall(new int[] {aTalkClear.GetHashCode(),15}),aTalkClear)),
                                aSearchPlayer);

        aSlowingTime = Sequencer(
                         Conditional(fCanCall(new int[] { aSlowDown.GetHashCode(), _slowDownInterval }),
                            Sequencer(
                                aSlowDown,
                                aTalkDet)),
                         Conditional(fSlowDownDone, aResetTime));

        aRunDeafAI = Selector(fIsDead,
                        Conditional(fCanCall(new int[] { aDie.GetHashCode(), 10 }), aDie),
                        Sequencer(
                            Sequencer(
                                Conditional(fSlowDownDone, Conditional(fCanCall(new int[] { aResetTime.GetHashCode(), _slowDownInterval }),aResetTime)),
                                aCheckPlayer),
                            Selector(fKnowsPlayerLoc,
                                Sequencer(
                                    Sequencer(
                                        aSlowingTime,
                                        Conditional(fCanCall(new int[] { aTalkSur.GetHashCode(), 15 }), aTalkSur)),
                                     Sequencer(
                                        aFightAndFlight,
                                        aShootAndAim)),
                                /*Sequencer(
                                    aCheckPlayer,*/
                                    Selector(fWasAlarmed,
                                        aSearchAndDestroy,
                                        Selector(fHasPath,
                                            Selector(fIsPatrolling,
                                                Sequencer(
                                                    Probable(0.2f, Conditional(fIsAINearby, aGetFriendlyPath)),
                                                    Conditional(Or(fCloseToPatrolPoint,fIsStuck), 
                                                        Sequencer(
                                                            /*Sequencer(*/
                                                                aNextWayPoint,
                                                                /*Conditional(fCanCall(new int[] { aParanoia.GetHashCode(), 5 }), aParanoia),*/ //TEST
                                                            Conditional(fIsPastRepeatPoint,
                                                                Probable(_wanderChance, aSwitchToWander))))),
                                                Selector(fIsWandering,
                                                    Selector(fIsDoneWandering,
                                                        aPatrollingCheck,
                                                        Conditional(Or(fIsDoneWithCurrentWanderPoint, fCloseToWanderPoint),
                                                            aNextWanderPoint)),
                                                    aLookAround)),
                                            aLookAround)))));

    }

    private void Update()
    {
        if (_running)
        {
            UpdateBB();
            //var anim = BB["Animator"].Value as Animator;
            //var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            //Debug.Log(stateInfo.IsTag("Shooting"));
            aRunDeafAI();
        }
    }
    public void Kill()
    {
        BB["Health"].Value = 0f;
        BB["ReceivedDamage"].Value = true;
        CanTakeDamage = false;
    }
    //Move bones after animation
    //private void LateUpdate()
    //{
    //    //if (_running)
    //    //{
           
    //    //    if (BB.ContainsKey("IsLookingBack") && (bool)BB["IsLookingBack"].Value)
    //    //    {
    //    //        var dir = (float)BB["LookBackTargetX"].Value;
    //    //        var eyes = BB["Eyes"].Value as Transform;
    //    //        if (eyes != null)
    //    //            eyes.Rotate(new Vector3(0f, dir, 0f), Space.Self);
    //    //    }
    //    //}
    //}

    /// <summary>
    ///   Pauze AI behavior
    /// </summary>
    public void Disable()
    {
        _running = false;
    }

    /// <summary>
    ///   Start AI behavior
    /// </summary>
    public void Enable()
    {
        _running = true;
    }

    //Visualize death transition
    private void OnEnable()
    {
        if (_died)
        {
            var r = BB["Renderer"].Value as Renderer;
            var mat = r.material;
            mat.SetColor("_EmissionColor", Color.white);
            var tex = BB["StartEmmisive"].Value as Texture;
            mat.SetTexture("_EmissionMap", tex);
        }
    }

    protected override void HurtAnimation()
    {
        //base.HurtAnimation();
        var pPos = ((Transform)PBB["PlayerTransform"].Value).position;
        var dir = pPos - transform.position;
        //var fwdAngle = Vector3.Angle(transform.forward, dir);
        var fwdAngle = FantasizedAngle(transform.forward, dir,true);
        //var backAngle = 180 - fwdAngle;
        var backAngle = FantasizedAngle(-transform.forward, dir,true);
        var FOV = (float)BB["FOV"].Value;
        int hitIndex = 0;
        if (fwdAngle < FOV)
        {
            hitIndex = 0;
            //Debug.Log("Hit Front: " + fwdAngle);
        }
        else if (backAngle < FOV)
        {
            hitIndex = 1;
            //Debug.Log("Hit Back " + backAngle);
        }
        else
        {
            hitIndex = 2;
            //Debug.Log("Hit Side " + fwdAngle + "||" + backAngle);
        }
        ((Animator)BB["Animator"].Value).SetInteger("hitIndex", hitIndex);
        ((Animator)BB["Animator"].Value).SetTrigger("hit");

        base.HurtAnimation();
        //var particles = BB["DefectParticles"].Value as ParticleSystem[];
        //foreach (var particle in particles)
        //{
        //    particle.Play();
        //}
    }

    public override void Alarm(Vector3 targetPosition, bool isPlayer)
    {
        StopCoroutine(LookAround());
        base.Alarm(targetPosition, isPlayer);
    }
    #endregion
    #region Functions
    private bool MNeedToBackOff()
    {
        var pPos = ((Transform) PBB["PlayerTransform"].Value).position;
        var pos = ((Transform) BB["Transform"].Value).position;
        var minDis = (float)BB["BackOffDis"].Value;
        var dis = (pPos - pos).sqrMagnitude;

        return dis < minDis * minDis;
    }

    private bool MPlayerInVision()
    {
        var pPos = ((Transform) PBB["PlayerTransform"].Value).position;
        var transEye = BB["Eyes"].Value as Transform;
        var fwd = Quaternion.AngleAxis(-90, transEye.up) * transEye.forward;
        var pos = transEye.position;

        var maxDis = (float)BB["ViewDistance"].Value;
        if (Vector3.Distance(pPos,pos) > maxDis)
            return false;

        var dir = pPos - pos;

        var FOV = (float)BB["FOV"].Value;
        var FOVvert = (float)BB["FOVVertical"].Value;
        //var horAngle = Vector3.Angle(fwd, dir);
        var horAngle = FantasizedAngle(fwd, dir,true);
        //var verAngle = Vector3.Angle(transEye.up, dir);
        var verAngle = FantasizedAngle(transEye.up, dir,true);
        verAngle = 90 - verAngle;
        verAngle = Mathf.Abs(verAngle);
        //Test vertical FOV on wall Arno scene
        
        if (horAngle > FOV || /*(verAngle < FOVvert && verAngle > -FOVvert)*/verAngle > FOVvert)
            return false;

        var layerMask = (1 << 8);
        layerMask |= (1 << 9);
        layerMask |= (1 << 11);
        bool lineCast = !Physics.Linecast(pos, pPos, ~layerMask);

        if (lineCast)
        {
            BB["ObjInView"].Value = ObjectInView.Head;
            return true;
        }

        //Check left hand
        var hand = _locoBase.LeftControllerTrObj;
        if (hand != null)
        {
            pPos = hand.transform.position;
            lineCast = !Physics.Linecast(pos, pPos, ~layerMask);
        }

        if (lineCast)
        {
            BB["ObjInView"].Value = ObjectInView.Hands;
            return true;
        }

        //Check right hand
        hand = _locoBase.RightControllerTrObj;
        if (hand != null)
        {
            pPos = hand.transform.position;
            lineCast = !Physics.Linecast(pos, pPos, ~layerMask);
        }

        if (lineCast) BB["ObjInView"].Value = ObjectInView.Hands;

        return lineCast;
    }

    private bool MSeeingHands()
    {
        if ((ObjectInView)BB["ObjInView"].Value == ObjectInView.Hands)
        {
            return true;
        }
        return false;
    }

    protected override bool MHasPath()
    {
        //Script_ManagerEnemy.Instance.LockPlayer(transform.position, false,gameObject.GetHashCode());
        return base.MHasPath();
    }

    private bool MFarFromPlayer()
    {
        var pos = transform.position;
        var playerPos = (PBB["PlayerTransform"].Value as Transform).position;
        var disToP = Vector3.Distance(pos, playerPos);

        var maxDis = (float)BB["BackOffDis"].Value * 2f;

        return disToP > maxDis;
    }
    #endregion
    #region Actions
    private void MSeesPlayer()
    {
        StopCoroutine(LookAround());

        if ((bool)BB["IsLookingBack"].Value)
        {
            BB["IsLookingBack"].Value = false;
            var eyes = BB["Eyes"].Value as Transform;
            eyes.localEulerAngles = new Vector3(1.69f, -4.362f, 87.94701f);
        }
        
        var pPos = ((Transform) PBB["PlayerTransform"].Value).position;
        BB["KnowsPlayer"].Value = true;
        //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Attacking);

        ((Animator) BB["Animator"].Value).SetBool("seesPlayer", true);
        var agent = BB["Agent"].Value as NavMeshAgent;
        BB["PreviousPos"].Value = (Vector3)BB["AlarmPosition"].Value;
        BB["AlarmPosition"].Value = RandomPos(pPos, 1f, 5f, agent.areaMask);
        BB["AlarmTimer"].Value = 0f;
        BB["IsPatrolling"].Value = false;
        BB["IsWandering"].Value = false;
        BB["IsAlarmPosEst"].Value = false;

        Script_QuestGiver.Instance.PlayerIsSeen();

        _mngr.LockPlayer(transform.position, true, gameObject.GetHashCode());

    }

    private void MDoesntSeePlayer()
    {
        ((Animator) BB["Animator"].Value).SetBool("seesPlayer", false);
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.angularSpeed = 120f;
        BB["KnowsPlayer"].Value = false;

        //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Searching);

        BB["IsAiming"].Value = false;

        _mngr.LockPlayer(transform.position, false, gameObject.GetHashCode());

        ((Animator)BB["Animator"].Value).SetBool("sideStep", false);

    }

    private void MLosePlayer()
    {
        BB["KnowsPlayer"].Value = false;
    }

    private void MBackOff()
    {
        var pPos = ((Transform) PBB["PlayerTransform"].Value).position;
        var pos = ((Transform) BB["Transform"].Value).position;
        var dir = (pos - pPos).normalized;
        dir *= 5f;
        dir += pos;
        var agent = BB["Agent"].Value as NavMeshAgent;
        var wP = RandomPos(dir, 1f, 4f, agent.areaMask);
        agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Backing Off");
        //Debug.Log("BackOff");
    }

    private void MWalkToHand()
    {
        Vector3 pos =  ((Transform)PBB["PlayerTransform"].Value).position;
        BB["AlarmPosition"].Value = pos;
        pos = RandomPos(pos, 1f, 2f, NavMesh.AllAreas);
        (BB["Agent"].Value as NavMeshAgent).SetDestination(pos);
        //Debug.Log(this.gameObject.name + "Walking to hands");
    }

    protected override void MAim()
    {
        base.MAim();

        var trans = BB["Transform"].Value as Transform;
        var gun = BB["GunTransform"].Value as Transform ?? trans;

        var player = ((Transform) PBB["PlayerTransform"].Value).position;
        var agent = BB["Agent"].Value as NavMeshAgent;

        if (Vector3.Distance(gun.position, player) > (float)BB["GunRange"].Value)
        {
            var tP = (Vector3)BB["AlarmPosition"].Value;
            //Debug.Log("Aiming");
            if (agent.destination != tP)
                agent.SetDestination(tP);
            //Debug.Log(this.gameObject.name + "Walking to player");
        }

        var pos = gun.position;
        player.y = pos.y;
        player.y -= 1f;
        var dir = player /*- pos*/;

        BB["IsAiming"].Value = true;
        BB["AimTarget"].Value = dir;
        dir -= pos;

        var maxAngle = (float)BB["MaxRotateAngle"].Value;
        dir = dir.normalized;
        //float angle = Vector3.Angle(transform.forward, dir);
        float angle = FantasizedAngle(transform.forward, dir);

        var anim = (Animator)BB["Animator"].Value;
        //Debug.Log(angle);
        if (Mathf.Abs(angle) > maxAngle && !anim.GetCurrentAnimatorStateInfo(0).IsTag("Shooting"))
        {
            BB["IsRotatingToAim"].Value = true;
            var rS = (float)BB["RotateSpeed"].Value;
            var scale = (float)PBB["TimeScale"].Value;
            var newDir = Vector3.Lerp(trans.forward, dir, Time.deltaTime * rS * scale);
            trans.rotation = Quaternion.LookRotation(newDir/*dir*/);
            anim.SetBool("sideStep", true);
            if (angle > 0)
                anim.SetBool("isRight", true);
            else
                anim.SetBool("isRight", false);
        }
        else
        {
            anim.SetBool("sideStep", false);
            BB["IsRotatingToAim"].Value = false;
        }

        agent.angularSpeed = 0f;
    }

    protected override void MDie()
    {
        var anim = (Animator)BB["Animator"].Value;
        if (!anim.GetCurrentAnimatorStateInfo(0).IsTag("Dying"))
        {
            anim.SetTrigger("die");
        }
        (BB["Agent"].Value as NavMeshAgent).SetDestination(transform.position);
        //StartCoroutine(_explForce.Play());
        //StartCoroutine(Die());
        Invoke("Explode", 2f);
    }

    private void Explode()
    {  
        var particles = BB["DeathParticles"].Value as ParticleSystem[];
        foreach (var particle in particles)
        {
            particle.Play();
        }
        Invoke("Disappear", 1f);
        Invoke("BaseDie", 2.0f);
    }
    private void Disappear()
    {
        (BB["Mesh"].Value as GameObject).SetActive(false);
    }
    private void BaseDie()
    {
        base.MDie();
    }

    System.Collections.IEnumerator Die()
    {
        float time = 6f;
        float iVal = 0.05f;
        var r = BB["Renderer"].Value as Renderer;
        var mat = r.material;
        while (_emission < 1f)
        {
            _emission += (1f / time) * iVal;
            Color colour = Color.red * Mathf.LinearToGammaSpace(_emission);
            mat.SetColor("_EmissionColor", colour);
            mat.SetTexture("_EmissionMap", new Texture());
            yield return new WaitForSeconds(iVal);
        }
        base.MDie();
    }

    protected override void MShoot()
    {
        base.MShoot();
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.angularSpeed = 0f;
        ((Animator)BB["Animator"].Value).SetBool("sideStep", false);

        ActivateShootPreparationParticle();

        Invoke("shootPartOne", 0.5f);
        Invoke("ActivateShootPreparationParticle", .5f);
        Invoke("shootPartOne", 1f);
    }

    void shootPartOne()
    {
        var bullet = BB["Bullet"].Value as GameObject;
        var bulletStart = BB["BulletStart"].Value as Transform;
        Instantiate<GameObject>(bullet, bulletStart.position, _bulletStart.rotation);
        (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotFire", transform.position,false,_soundFalloff.x,_soundFalloff.y);
    }

    void ActivateShootPreparationParticle()
    {
        if (_preShootParticle != null)
        {
            _preShootParticle.SetActive(false);
            _preShootParticle.SetActive(true);
        }
    }
    //System.Collections.IEnumerator GunDelay()
    //{
    //    var bullet = BB["Bullet"].Value as GameObject;
    //    var bulletStart = BB["BulletStart"].Value as Transform;
    //    yield return new WaitForSeconds(0.5f);
    //    Instantiate<GameObject>(bullet, bulletStart.position, _bulletStart.rotation);
    //    (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotFire", transform.position);
    //    yield return new WaitForSeconds(0.5f);
    //    Instantiate<GameObject>(bullet, bulletStart.position, _bulletStart.rotation);
    //    (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotFire", transform.position);
    //}

    private void MLooksBack()
    {
        StartCoroutine(LookAround());
        BB["IsLookingBack"].Value = true;
    }

    System.Collections.IEnumerator LookAround()
    {
        var eyes = BB["Eyes"].Value as Transform;
        var side = RandomSide();
        var extreemRechts = -eyes.forward /** side*/;
        //Debug.Log("LookingAround");
        //Debug.Log(this.gameObject.name + "Paranoia");
        while (FantasizedAngle(extreemRechts, eyes.forward, true) > 15f) 
        {
            BB["LookBackTargetX"].Value = (float)BB["LookBackTargetX"].Value+ side * Mathf.Lerp(eyes.localRotation.y, 180, Time.deltaTime * 0.3f);
            //_agent.SetDestination(transform.position);
            yield return null;
        }
        while (FantasizedAngle(-extreemRechts, eyes.forward, true) > 15f) 
        {
            BB["LookBackTargetX"].Value = (float)BB["LookBackTargetX"].Value- side *Mathf.Lerp(eyes.localRotation.y, 180, Time.deltaTime * 0.3f);
            //_agent.SetDestination(transform.position);
            yield return null;
        }

        //var wayPoints = BB["WayPoints"].Value as System.Collections.Generic.List<Transform>;
        //var nrWP = (int)BB["CurrentWayPoint"].Value;
        //var agent = BB["Agent"].Value as NavMeshAgent;
        //var wP = wayPoints[nrWP].position;
        //agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Paranoia Done");
        BB["IsLookingBack"].Value = false;
        yield break;
    }

    protected override void MSwarmPlayer()
    {
        var pos = transform.position;
        var playerPos = (PBB["PlayerTransform"].Value as Transform).position;
        //var dis = Vector3.Distance(pos, playerPos);
        //dis -= Time.deltaTime * (float)BB["AproachSpeed"].Value;

        var dis = (float)BB["BackOffDis"].Value * 1.5f;

        var agent = BB["Agent"].Value as NavMeshAgent;
        var targetPos = RandomPos(playerPos, dis, dis, agent.areaMask);

        agent.SetDestination(targetPos);
        //Debug.Log(this.gameObject.name + "Swarming player");
        //Debug.Log("Swarming");
    }

    protected override void MSwitchToPatrol()
    {
        base.MSwitchToPatrol();
        ((Animator)BB["Animator"].Value).SetBool("sideStep", false);
    }
    protected override void MSwitchToWander()
    {
        base.MSwitchToWander();
        ((Animator)BB["Animator"].Value).SetBool("sideStep", false);
    }

    private void MSaysSurrender()
    {
        (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotVoiceSurrender", transform.position, false, _soundFalloff.x, _soundFalloff.y);
    }
    private void MSaysDetected()
    {
        (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotVoiceDetected", transform.position, false, _soundFalloff.x, _soundFalloff.y);
    }
    private void MSaysCoastClear()
    {
        (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotVoiceCoastClear", transform.position, false, _soundFalloff.x, _soundFalloff.y);
    }
    private void MSaysWrong()
    {
        (BB["Audio"].Value as Script_AudioManager).PlaySFX("DeafRobotVoiceWrong", transform.position,false, _soundFalloff.x, _soundFalloff.y);
    }
}
#endregion

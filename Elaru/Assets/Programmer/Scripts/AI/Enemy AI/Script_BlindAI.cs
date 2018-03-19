using UnityEngine;
using UnityEngine.AI;
using System;

public class Script_BlindAI : Script_EnemyBase
{
    #region Variables and Structs
    /// <summary>
    ///   Extents of the AI in 3D space
    /// </summary>
    [SerializeField]
    private float _extents = 1f;

    /// <summary>
    ///   Rigidbody used by AI
    /// </summary>
    [SerializeField]
    private Rigidbody _rigidbody;

    [SerializeField]
    private float _chargeSpeed = 10f;

    [SerializeField]
    private float _chaseSpeed = 6f;

    /// <summary>
    ///   Status of alarm
    /// </summary>
    enum AlarmLevel
    {
        Alarmed,
        Cautious,
        Invalid
    }
    #endregion
    #region Conditions
    /// <summary>
    ///   Did AI detect sound
    /// </summary>
    Func<bool> fSoundDetected;
    /// <summary>
    ///   Did AI hear stronger sound
    /// </summary>
    Func<bool> fHearsStrongerSound;
    /// <summary>
    ///   Did AI hear the same sound
    /// </summary>
    Func<bool> fHearsSameSound;
    /// <summary>
    ///   Does AI have correct angle to charge
    /// </summary>
    Func<bool> fHasCorrectChargeAngle;
    /// <summary>
    ///   Current AI alarm level
    /// </summary>
    Func<int> fAlarmLevel;
    #endregion
    #region Actions
    /// <summary>
    ///   Rotate AI towards target
    /// </summary>
    Action aRotateToPlayer;
    /// <summary>
    ///   Charge towards target
    /// </summary>
    Action aCharge;
    /// <summary>
    ///   Steer vertically towards target
    /// </summary>
    Action aSteerToAttackPos;
    /// <summary>
    ///   Reset last heard sound
    /// </summary>
    Action aResetNewSound;
    /// <summary>
    ///   Reset tracked sound
    /// </summary>
    Action aResetOldSound;
    Action aRunBlindAI = null;
    #endregion
    #region Class Methods
    private void Start()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponentInChildren<Rigidbody>();

        BlackBoardInit();
        ConditionsInit();
        ActionsInit();
        BehaviorTreeInit();
    }
    /// <summary>
    ///   Initialize blackboard variables
    /// </summary>
    void BlackBoardInit()
    {
        //SOUND DETECTION
        AddVariable<SoundInfo>("ActiveSound", new SoundInfo());
        AddVariable<SoundInfo>("NewSound", new SoundInfo()); //Changed via enemy manager using overlapsphere on new sounds
        AddVariable<float>("SoundThreshold", 1f / 30f); //Minimum "decibels"
        AddVariable<float>("Extents", _extents);
        AddVariable<float>("DescendingAcc", 0.05f);
        AddVariable<float>("ChargeSpeed", _chargeSpeed);
        AddVariable<float>("ChaseSpeed", _chaseSpeed);
        AddVariable<float>("StartSpeed", 3.5f);
        AddVariable<float>("ExplosionRadius", 9f);
        AddVariable<Rigidbody>("RigidBody", _rigidbody);
        AddVariable<AlarmLevel>("AlarmLevel", AlarmLevel.Invalid);
        AddVariable<Renderer>("Renderer", _renderer);

        _type = Script_ManagerEnemy.EnemyType.Blind;
        InitBB();

        ChangeVariable<float>("RotateSpeed", 2.5f);
    }
    /// <summary>
    ///   Link conditions to methods
    /// </summary>
    void ConditionsInit()
    {
        fSoundDetected = MSoundDetected;
        fHearsSameSound = MSameSoundDetected;
        fHearsStrongerSound = MHearsMoreImportantSound;
        fHasCorrectChargeAngle = MCorrectAngle;
        fAlarmLevel = MAlarmLevel;
    }
    /// <summary>
    ///   List actions to methods
    /// </summary>
    void ActionsInit()
    {
        InitActions();
        aRotateToPlayer = MRotateToPlayer;
        aCharge = MCharge;
        aSteerToAttackPos = MSteerToAttackPos;
        aResetNewSound = MResetSound;
        aResetOldSound = MResetOld;
    }

    /// <summary>
    /// Set up behavior tree
    /// </summary>
    void BehaviorTreeInit()
    {
        Action aEmpty = () => { };

        aSearchPlayer = Selector(fIsWandering,
                            Conditional(fCloseToWanderPoint, aNextWanderPoint),
                            Selector(fCloseToAlarmPoint,
                                aSwitchToWander,
                                aGoToAlarm));

        aSlowingTime = Sequencer(
                             Conditional(fCanCall(new int[] { aSlowDown.GetHashCode(), 30 }), aSlowDown),
                             Conditional(fSlowDownDone, aResetTime));

        /// <summary>
        ///   Check if newly heard sound is more important
        /// </summary>
        Action aSoundCheck = Conditional(fSoundDetected,
                                Selector(fHearsSameSound,
                                    aResetNewSound,
                                    Conditional(fHearsStrongerSound,
                                        aResetNewSound)));

        /// <summary>
        ///   Carrier for when same sound is heard = attack behavior
        /// </summary>
        Action aSameSound = Sequencer(
                                Sequencer(
                                    /*aSlowingTime*/aEmpty,
                                    aResetNewSound),
                                Selector(fCanHitPlayer,
                                    Selector(fHasCorrectChargeAngle,
                                        Conditional(fCanCall(new int[] { aCharge.GetHashCode(), 10 }), aCharge),
                                        aRotateToPlayer),
                                    /*Sequencer(
                                        aSteerToAttackPos,*/
                                    aSearchPlayer));

        /// <summary>
        ///   Carrier for when different sound is heard = search behavior
        /// </summary>
        Action aDifferentSound = Sequencer(
                                    aResetNewSound,
                                    aSearchPlayer);

        /// <summary>
        ///   Array of two possible reactions to sound
        /// </summary>
        Action[] aAlarmLevel = { aSameSound, aDifferentSound };
        /// <summary>
        ///   Carrier for alarm behavior
        /// </summary>
        Action aAlarm = Selector(fHasForgottenAlarm,
                                    Sequencer(
                                        aPatrollingCheck,
                                        aResetOldSound),
                                    NumericBranching(fAlarmLevel, aAlarmLevel));


        aRunBlindAI = Selector(fIsDead,
                                Conditional(fCanCall(new int[] { aDie.GetHashCode(), 10 }), aDie),
                                Sequencer(
                                    Sequencer(
                                        Conditional(fSlowDownDone, aResetTime),
                                        aSoundCheck),
                                    Selector(fWasAlarmed,
                                        aAlarm,
                                        Selector(fHasPath,
                                            Selector(fIsPatrolling,
                                                Sequencer(
                                                    Probable(1f, Conditional(fIsAINearby, aGetFriendlyPath)),
                                                    Conditional(fCloseToPatrolPoint,
                                                        Sequencer(
                                                            aNextWayPoint,
                                                            Conditional(fIsPastRepeatPoint,
                                                                Probable(0.10f, aSwitchToWander))))),
                                                Selector(fIsWandering,
                                                    Selector(fIsDoneWandering,
                                                        aPatrollingCheck,
                                                        Conditional(Or(fIsDoneWithCurrentWanderPoint, fCloseToWanderPoint),
                                                            aNextWanderPoint)),
                                                    aEmpty)),
                                            aEmpty))));
    }

    private void Update()
    {
        UpdateBT();
        if ((bool)BB["WasAlarmed"].Value)
            //Increase alarm timer
            BB["AlarmTimer"].Value = (float)BB["AlarmTimer"].Value + Time.deltaTime;

        if ((bool)BB["IsWandering"].Value)
        {
            BB["SecondsSinceStartWander"].Value = (float)BB["SecondsSinceStartWander"].Value + Time.deltaTime;
            BB["SecondsSinceWanderPoint"].Value = (float)BB["SecondsSinceWanderPoint"].Value + Time.deltaTime;
        }

        //Increase slow-motion timer
        BB["SlowDownTimer"].Value = (float)BB["SlowDownTimer"].Value + Time.deltaTime;

        var vel = (BB["Agent"].Value as NavMeshAgent).velocity;
        //var angle = Vector3.Angle(transform.forward, vel);
        var angle = FantasizedAngle(transform.forward, vel);
        float speed = Mathf.Abs(angle) > 90f ? -1 : 1;
        speed *= vel.sqrMagnitude;
        (BB["Animator"].Value as Animator).SetFloat("velocity", speed);

        var gun = BB["GunTransform"].Value as Transform ?? (BB["RigidBody"].Value as Rigidbody).transform;
        //Debug.DrawRay(gun.position, gun.forward * 10f, Color.black);

        if (!MIsDead())
        {
            Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), GetState());
        }

        aRunBlindAI();
    }
    #endregion
    #region Helper and Public Functions
    /// <summary>
    ///   Alarm AI passing position and sound alarm type
    /// </summary>
    public void Alarm(Vector3 tP, SoundType type)
    {
        SoundInfo sound = new SoundInfo(tP, type, transform.position);

        BB["NewSound"].Value = sound;
        BB["AlarmTimer"].Value = 0f;
        BB["WasAlarmed"].Value = true;

        if (type == SoundType.Alarm)
            BB["ActiveSound"].Value = sound;

        var agent = BB["Agent"].Value as NavMeshAgent;
        BB["IsWandering"].Value = false;
        //Debug.Log("Alarmed");

        if (agent != null)
            agent.SetDestination(transform.position);
    }

    public override void DealDamage(float damage, Vector3 pos)
    {
        BB["Health"].Value = (float)BB["Health"].Value - damage;
        BB["ReceivedDamage"].Value = true;
        CanTakeDamage = false;
        if ((float)BB["Health"].Value > 0)
        {
            HurtAnimation();
            Alarm(pos, SoundType.Player);
        }
    }

    /// <summary>
    ///   Kill AI and afflict area damage
    /// </summary>
    public void Die(Vector3 cP)
    {
        if ((bool)BB["IsPatrolling"].Value || (bool)BB["IsWandering"].Value)
            return;
        var radius = (float)BB["ExplosionRadius"].Value;
        var damage = (int)BB["GunDamage"].Value;

        Script_ManagerEnemy.Instance.AreaDamage(cP, radius, damage);

        //(BB["Animator"].Value as Animator).SetTrigger("onCollision");
        var body = BB["RigidBody"].Value as Rigidbody;
        body.useGravity = true;
        var con = RigidbodyConstraints.FreezeAll;
        body.constraints = con;
        Invoke("Explode", 1f);
        Invoke("MDie",2.5f);
    }
    public void Explode()
    {
        (BB["Mesh"].Value as GameObject).SetActive(false);
        var particles = BB["DeathParticles"].Value as ParticleSystem[];
        foreach (var particle in particles)
        {
            particle.Play();
        }
    }

    #endregion
    #region Functions

    protected override bool MWasAlarmed()
    {
        (BB["Animator"].Value as Animator).SetBool("alerted", base.MWasAlarmed());
        return base.MWasAlarmed();
    }

    protected override bool MCanHitPlayer()
    {
        var soundPos = ((SoundInfo)BB["ActiveSound"].Value).Position;
        var gun = BB["GunTransform"].Value as Transform ?? ((Rigidbody)BB["RigidBody"].Value).transform;

        var direction = soundPos - gun.position;
        var dis = Vector3.Distance(soundPos, gun.position);
        dis -= (float)BB["ExplosionRadius"].Value;

        var extents = (float)BB["Extents"].Value;

        if (Physics.BoxCast(gun.position,new Vector3(extents/2,extents/2,extents/2), direction,Quaternion.identity, dis))
        {
            (BB["Animator"].Value as Animator).SetBool("steering", true);
            return false;
        }

        //Script_QuestGiver.Instance.PlayerIsSeen(); //ZET AF WNR GEEN VR
        (BB["Animator"].Value as Animator).SetBool("steering", false);
        return true;
    }

    bool MSoundDetected()
    {
        var current = (SoundInfo)BB["NewSound"].Value;
        if (current.Strength == SoundType.Invalid)
            return false;

        BB["WasAlarmed"].Value = true;
        BB["KnowsPlayer"].Value = true;
        //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Searching);
        return true;
    }

    bool MSameSoundDetected()
    {
        var current = (SoundInfo)BB["NewSound"].Value;
        var prev = (SoundInfo)BB["ActiveSound"].Value;

        if ((current.Position - prev.Position).sqrMagnitude < 2f * 2f)
        {
            BB["ActiveSound"].Value = current;
            BB["AlarmLevel"].Value = AlarmLevel.Alarmed;
            //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Attacking);

            return true;
        }

        if (current.Strength == SoundType.Player && prev.Strength == SoundType.Player)
        {
            BB["ActiveSound"].Value = current;
            BB["AlarmLevel"].Value = AlarmLevel.Alarmed;
            //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Attacking);

            return true;
        }
        //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Searching);

        return false;
    }
    bool MHearsMoreImportantSound()
    {
        //If New Sound is more important than previous sound change focus
        var min = (float)BB["SoundThreshold"].Value;
        var current = (SoundInfo)BB["NewSound"].Value;
        if (min > (int)current.Strength / current.Distance)
            return false;

        var prev = (SoundInfo)BB["ActiveSound"].Value;
        if (prev.Strength == SoundType.Invalid)
        {
            BB["ActiveSound"].Value = current;
            BB["AlarmLevel"].Value = AlarmLevel.Cautious;
            return true;
        }

        if ((int)current.Strength / current.Distance > (int)prev.Strength / prev.Distance)
        {
            BB["ActiveSound"].Value = current;
            BB["AlarmLevel"].Value = AlarmLevel.Cautious;
            return true;
        }
        return false;
    }
    private bool MCorrectAngle()
    {
        BB["AlarmTimer"].Value = 0f;

        var soundPos = ((SoundInfo)BB["ActiveSound"].Value).Position;
        var maxAngle = (float)BB["MaxShootAngle"].Value;
        var gun = BB["GunTransform"].Value as Transform ?? (BB["RigidBody"].Value as Rigidbody).transform;

        soundPos.y = gun.position.y;
        Vector3 dir = soundPos - gun.position;
        //float angle = Vector3.Angle(gun.forward, dir.normalized);
        float angle = FantasizedAngle(gun.forward, dir.normalized);

        if (angle <maxAngle)
        {
            (BB["Animator"].Value as Animator).SetTrigger("correctAngle");
        }
        return angle < maxAngle;
    }


    int MAlarmLevel()
    {
        return (int)BB["AlarmLevel"].Value;
    }
    #endregion
    #region Actions
    void MResetSound()
    {
        var soundPos = ((SoundInfo)BB["ActiveSound"].Value).Position;

        BB["AlarmPosition"].Value = soundPos;
        BB["NewSound"].Value = new SoundInfo();
    }

    void MResetOld()
    {
        BB["ActiveSound"].Value = new SoundInfo();
        BB["KnowsPlayer"].Value = false;
        //Script_ManagerEnemy.Instance.SetState(gameObject.GetHashCode(), EnemyState.Patrolling);

        BB["AlarmLevel"].Value = AlarmLevel.Invalid;
    }

    private void MSteerToAttackPos()
    {
        var tP = (Vector3)BB["AlarmPosition"].Value;
        var extents = (float)BB["Extents"].Value;
        var body = BB["RigidBody"].Value as Rigidbody;
        if (body == null)
            return;
        var acc = (float)BB["DescendingAcc"].Value;
        var scale = (float)PBB["TimeScale"].Value;
        var speed = (float)BB["ChaseSpeed"].Value;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.speed = speed;

        if (body.transform.position.y > tP.y && body.transform.localPosition.y > extents)
            body.AddForce(Vector3.down * acc * Time.deltaTime * scale, ForceMode.Acceleration); //TEMP
        else
            body.AddForce(Vector3.up * acc * Time.deltaTime * scale, ForceMode.Acceleration); //TEMP
    }

    private void MCharge()
    {
        //var speed = (float)BB["ChargeSpeed"].Value;
        var tP = (Vector3)BB["AlarmPosition"].Value;
        var rSpeed = (float)BB["RotateSpeed"].Value;
        var body = BB["RigidBody"].Value as Rigidbody;
        if (body == null)
            return;
        var dir = tP - body.position;

        //body.AddForce(dir.normalized * speed, ForceMode.VelocityChange);
        body.transform.forward = Vector3.Lerp(body.transform.forward, dir, Time.deltaTime * rSpeed);
        body.rotation = Quaternion.LookRotation(dir);

        //(BB["Animator"].Value as Animator).SetTrigger("diving");

        //aRunBlindAI = () => {};
        BB["IsPatrolling"].Value = false;
        BB["IsWandering"].Value = false;
        (BB["RigidBody"].Value as Rigidbody).isKinematic = false;
        //(BB["RigidBody"].Value as Rigidbody).useGravity = true;
        var speed = (float)BB["StartSpeed"].Value;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.speed = speed;
        //StartCoroutine("Charge");
        Invoke("mCharge", 2f);
    }
    private void mCharge()
    {
        //yield return new WaitForSeconds(2f);
        var speed = (float)BB["ChargeSpeed"].Value;
        var tP = (Vector3)BB["AlarmPosition"].Value;
        var rSpeed = (float)BB["RotateSpeed"].Value;
        var body = BB["RigidBody"].Value as Rigidbody;
        if (body == null)
            return;
            //yield break;
        var dir = tP - body.position;

        body.AddForce(dir.normalized * speed, ForceMode.VelocityChange);
        body.transform.forward = Vector3.Lerp(body.transform.forward, dir, Time.deltaTime * rSpeed);
        body.rotation = Quaternion.LookRotation(dir);

        (BB["Animator"].Value as Animator).SetTrigger("diving");

        aRunBlindAI = () => { };
        //yield break;
    }

    private void MRotateToPlayer()
    {
        var tP = (Vector3)BB["AlarmPosition"].Value;
        var speed = (float)BB["RotateSpeed"].Value;
        var scale = (float)PBB["TimeScale"].Value;
        var gun = BB["GunTransform"].Value as Transform ?? (BB["RigidBody"].Value as Rigidbody).transform;

        var dir = (tP - gun.position).normalized;

        var angle = FantasizedAngle(gun.forward, dir);
        //var angle = Vector3.Angle(gun.forward, dir);
        //var cross = Vector3.Cross(gun.forward, dir);
        //angle = cross.y > 0 ? angle : -angle;
        ////Debug.Log("Pre-Angle: " + angle);
        //angle = angle % 360; //TEMP
        //angle = angle > 180 ? angle - 360 : angle;
        //angle = angle < -180 ? angle + 360 : angle;
        //angle = angle < 0 ? angle + 360 : angle; //TEMP
        //Debug.Log("Post-Angle: " + angle);
        speed = angle < 180 ? speed : -speed; //TEMP
        gun.Rotate(Vector3.up, angle * speed * Time.deltaTime * scale);
    }
    #endregion
}

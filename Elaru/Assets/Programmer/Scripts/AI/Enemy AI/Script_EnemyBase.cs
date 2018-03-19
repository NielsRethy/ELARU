using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.Linq;

public class Script_EnemyBase : Script_BehaviorTreeFramework
{
    #region Structs, Classes and Enumerations
    /// <summary>
    ///   Light info for AI's using visual cues
    ///   <para> position</para>
    ///   <para> strength</para>
    /// </summary>
    protected class LightInfo
    {
        public Vector3 Position;
        public float Strength;
    }
    /// <summary>
    ///   Sound info for AI's using auditory cues
    ///   <para> _strength</para>
    ///   <para> _distance</para>
    ///   <para> _pos</para>
    /// </summary>
    protected class SoundInfo
    {
        public SoundType Strength;
        public float Distance;
        public Vector3 Position;

        public SoundInfo(Vector3 pos, SoundType strength, Vector3 playerPos)
        {
            Position = pos;
            Distance = Vector3.Distance(pos, playerPos);
            Strength = strength;
        }

        public SoundInfo()
        {
            Position = new Vector3();
            Strength = SoundType.Invalid;
            Distance = 0;
        }
    }

    /// <summary>
    ///   Method AI uses to alarm others
    ///   <para> Friends: Alarms members of personal friendlist</para>
    ///   <para> Overlapsphere: Alarms AI in pre-defined radius</para>
    /// </summary>
    protected enum PatrolContact
    {
        Friends,
        OverlapSphere
    }

    /// <summary>
    ///   Defined the type of a sound for priority uses
    ///   <para> Player: sounds produced by player</para>
    ///   <para> Alarm: sounds produced by alarm</para>
    ///   <para> Other: sounds produced by other</para>
    ///   <para> Invalid: no sound produced</para>
    /// </summary>
    public enum SoundType
    {
        Alarm = 3,
        Player = 2,
        Other = 1,
        Invalid = 0
    }

    /// <summary>
    ///   Values of NavMeshAgent that change during slow-motion
    /// </summary>
    protected class AgentVars
    {
        public AgentVars(float angularSpeed, float speed)
        {
            AngularSpeed = angularSpeed;
            Speed = speed;
        }

        public float AngularSpeed;
        public float Speed;
    }


    #endregion
    #region Variables
    protected NavMeshAgent _agent = null;

    //The waypoints the AI follows
    [SerializeField]
    protected List<Transform> _wayPoints = new List<Transform>();

    //The point from which the path is repeated
    [SerializeField]
    protected int _repeatStart = 0;

    //The members of its patrol if _alarmType is PatrolContact.Friends
    [SerializeField]
    Script_EnemyBase[] _friends = null;

    //The type in which it alarms other AI's
    [SerializeField]
    PatrolContact _alarmType = PatrolContact.OverlapSphere;

    //The animator used by the AI
    [SerializeField]
    Animator _animator = null;

    //Drops datakey on death
    [SerializeField]
    bool _hasDataKey = false;
    [SerializeField]
    List<Light> _lights = new List<Light>();

    /// <summary>
    ///   Renderer to use for death transition
    /// </summary>
    [SerializeField]
    protected Renderer _renderer = null;

    //Value used by the limbs for short immunity interval
    public bool CanTakeDamage;


    //The type of enemy used by the manager for relaying sound and light if necessary
    protected Script_ManagerEnemy.EnemyType _type;

    float _health = 100f;
    protected bool _died = false;

    protected Script_ManagerEnemy _mngr;

    protected bool _running = true;

    protected int _slowDownInterval = 500;

    [SerializeField]
    protected UnityStandardAssets.Effects.ParticleSystemMultiplier _deathParticles;
    [SerializeField]
    protected GameObject _defectParticles;
    [SerializeField]
    protected UnityStandardAssets.Effects.ExplosionPhysicsForce _explForce;

    [SerializeField]
    protected GameObject _mesh;
    #endregion

    #region Conditions
    /// <summary>
    ///   Can the AI hear
    /// </summary>
    protected Func<bool> fCanHear;
    /// <summary>
    ///   Can the AI see
    /// </summary>
    protected Func<bool> fCanSee;
    /// <summary>
    ///   Does AI have waypoints
    /// </summary>
    protected Func<bool> fHasPath;
    /// <summary>
    ///   Is AI patrolling
    /// </summary>
    protected Func<bool> fIsPatrolling;
    /// <summary>
    ///   Is AI stuck
    /// </summary>
    protected Func<bool> fIsStuck;
    /// <summary>
    ///   Is AI wandering
    /// </summary>
    protected Func<bool> fIsWandering;
    /// <summary>
    ///   Is AI close to a waypoint
    /// </summary>
    protected Func<bool> fCloseToPatrolPoint;
    /// <summary>
    ///   Is AI close to a temporary wander path point
    /// </summary>
    protected Func<bool> fCloseToWanderPoint;
    /// <summary>
    ///   Is AI done with wandering
    /// </summary>
    protected Func<bool> fIsDoneWandering;
    /// <summary>
    ///   Is AI wandering towards temporary wander path point, longer than necessary
    /// </summary>
    protected Func<bool> fIsDoneWithCurrentWanderPoint;
    /// <summary>
    ///   Has AI passed the point it needs to start looping
    /// </summary>
    protected Func<bool> fIsPastRepeatPoint;

    /// <summary>
    ///   Does AI know where the player is
    /// </summary>
    protected Func<bool> fKnowsPlayerLoc;
    /// <summary>
    ///   Can AI hit player
    /// </summary>
    protected Func<bool> fCanHitPlayer;
    /// <summary>
    ///   Was the AI alarmed
    /// </summary>
    protected Func<bool> fWasAlarmed;
    /// <summary>
    ///   Is AI close to alarm path point
    /// </summary>
    protected Func<bool> fCloseToAlarmPoint;
    /// <summary>
    ///   Is the current alarm position an estimation
    /// </summary>
    protected Func<bool> fIsAlarmPosEst;
    /// <summary>
    ///   Has the AI given up on searching
    /// </summary>
    protected Func<bool> fHasForgottenAlarm;
    /// <summary>
    ///   Did the shoot delay pass
    /// </summary>
    protected Func<bool> fCanShootAgain;
    /// <summary>
    ///   Is the temporary slow-motion done
    /// </summary>
    protected Func<bool> fSlowDownDone;
    /// <summary>
    ///   Is AI currently shooting
    /// </summary>
    protected Func<bool> fIsShooting;
    /// <summary>
    ///   Check if there are any other robots in the direct area around the player
    /// </summary>
    protected Func<bool> fIsAINearby;

    /// <summary>
    ///   Did the AI die
    /// </summary>
    protected Func<bool> fIsDead;

    #endregion
    #region Actions
    /// <summary>
    ///   Go to next waypoint
    /// </summary>
    protected Action aNextWayPoint;
    /// <summary>
    ///   Go to next temporary wander point
    /// </summary>
    protected Action aNextWanderPoint;
    /// <summary>
    ///   Switch to patrolling behavior
    /// </summary>
    protected Action aSwitchToPatrolling;
    /// <summary>
    ///   Switch to wander behavior
    /// </summary>
    protected Action aSwitchToWander;
    /// <summary>
    ///   Rotate head to scout 
    /// </summary>
    protected Action aLookAround;
    /// <summary>
    ///   Stop walking
    /// </summary>
    protected Action aStopWalking;
    /// <summary>
    ///   Get the waypoint of a nearby enemy
    /// </summary>
    protected Action aGetFriendlyPath;

    /// <summary>
    ///   Go to slow-motion
    /// </summary>
    protected Action aSlowDown;
    /// <summary>
    ///   Go to regular speed
    /// </summary>
    protected Action aResetTime;
    /// <summary>
    ///   Slow-motion carrier
    /// </summary>
    protected Action aSlowingTime;
    /// <summary>
    ///   Switch the attack target
    /// </summary>
    protected Action aSwitchAttackTarget;
    /// <summary>
    ///   Alarm rest of patrol or surroundings
    /// </summary>
    protected Action aAlarmFriends;
    /// <summary>
    ///   Search for player
    /// </summary>
    protected Action aSearchPlayer;
    /// <summary>
    ///   Surround player
    /// </summary>
    protected Action aSwarmPlayer;
    /// <summary>
    ///   Go to alarm position
    /// </summary>
    protected Action aGoToAlarm;
    /// <summary>
    ///   Go to estimated distress location
    /// </summary>
    protected Action aGoToEstLoc;
    /// <summary>
    ///   Search carrier
    /// </summary>
    protected Action aSearchAndDestroy;
    /// <summary>
    ///   Check if can patrol after abandoning search
    /// </summary>
    protected Action aPatrollingCheck;
    /// <summary>
    ///   Aim
    /// </summary>
    protected Action aAim;
    /// <summary>
    ///   Shoot
    /// </summary>
    protected Action aShoot;


    /// <summary>
    ///   Destroy self
    /// </summary>
    protected Action aDie;

    #endregion

    #region Class Methods

    /// <summary>
    ///  Initialize base enemy blackboard variables
    /// </summary>
    protected void InitBB()
    {
        TimeInit();
        //Add self and friends to enemy manager
        List<GameObject> friends = new List<GameObject> { gameObject };
        friends.AddRange(_friends.Select(friend => friend.gameObject));

        _mngr = Script_ManagerEnemy.Instance;

        _mngr.AddPatrol(friends, _type);
        CanTakeDamage = true;
        _agent = GetComponent<NavMeshAgent>();

        //STANDARD
        AddVariable<Transform>("Transform", transform);
        AddVariable<float>("Health", _health);
        AddVariable<float>("MaxHealth", _health);
        AddVariable<bool>("ReceivedDamage", false);
        AddVariable<NavMeshAgent>("Agent", _agent);

        var startAgent = new AgentVars(_agent.angularSpeed, _agent.speed);
        //Start variables of agent and regular speed
        AddVariable<AgentVars>("StartAgent", startAgent);
        AddVariable<List<Transform>>("WayPoints", _wayPoints);
        AddVariable<int>("RepeatStart", _repeatStart);
        AddVariable<int>("CurrentWayPoint", 0);
        _wayPoints.RemoveAll(x => x == null);

        if (_wayPoints.Count > 0)
        {
            if (_wayPoints.Count == 1)
            {
                var parent = _wayPoints[0];
                _wayPoints[0] = parent.GetChild(0);
                for (var i = 1; i < parent.childCount; i++)
                    _wayPoints.Add(parent.GetChild(i));
            }

            AddVariable<Vector3>("WayPoint", _wayPoints[0].position);
            _agent.SetDestination(_wayPoints[0].position);
        } else AddVariable<Vector3>("WayPoint", transform.position);


        AddVariable<bool>("IsPatrolling", true);
        AddVariable<float>("StuckTimer", 0f);
        AddVariable<float>("MaxStuckTime", 5f);
        AddVariable<float>("WayPointMargin", 2f);
        AddVariable<bool>("CanHear", false);
        AddVariable<bool>("CanSee", true);

        //DETECTION
        AddVariable<bool>("KnowsPlayer", false);
        AddVariable<bool>("KnowsCompanion", false);
        AddVariable<Transform>("AttackTarget", null);
        AddVariable<Vector3>("AlarmPosition", new Vector3());
        AddVariable<float>("AproachSpeed", 5f);
        AddVariable<bool>("IsAlarmPosEst", false);
        AddVariable<Vector3>("PreviousPos", new Vector3());
        AddVariable<bool>("WasAlarmed", false);
        AddVariable<float>("MaxAlarmTime", 10f);
        AddVariable<float>("AlarmTimer", 0f);
        AddVariable<Script_EnemyBase[]>("Friends", _friends);
        AddVariable<float>("AlarmRadius", 150f);
        AddVariable<PatrolContact>("AlarmType", _alarmType);

        //WANDER
        AddVariable<float>("SecondsSinceStartWander", 0f); //Absolute wander start
        AddVariable<float>("SecondsSinceWanderPoint", 0f); //Seconds since last chosen direction
        AddVariable<float>("MaxWanderTime", 15f); //Total time to wander
        AddVariable<float>("WanderPointTime", 5f); //Time till new point chosen
        AddVariable<bool>("IsWandering", false);

        //GUN
        AddVariable<Transform>("GunTransform", null);
        AddVariable<float>("MaxShootAngle", 10f);
        AddVariable<float>("GunRange", 50f);
        AddVariable<int>("GunDamage", 50);
        AddVariable<float>("RotateSpeed", 2f);
        AddVariable<float>("ShootDelay", 5f);
        AddVariable<float>("SecondsSinceShot", 0f);

        //TIMESCALE
        AddVariable<float>("SlowDownTime", 2.5f);
        AddVariable<float>("SlowDownTimer", 0f);
        AddVariable<float>("SlowDownScale", 0.2f);

        //ANIMATIONS and SOUND
        AddVariable<Animator>("Animator", _animator);
        AddVariable<Script_AudioManager>("Audio", Script_AudioManager.Instance);

        //PARTICLES
        AddVariable<GameObject>("Mesh", _mesh);
        AddVariable<ParticleSystem[]>("DeathParticles", _deathParticles.GetParticles());

        var particles = BB["DeathParticles"].Value as ParticleSystem[];
        foreach (var particle in particles)
        {
            particle.Stop();
            particle.Clear();
        }
        AddVariable<ParticleSystem[]>("DefectParticles", _defectParticles.GetComponentsInChildren<ParticleSystem>());
        particles = BB["DefectParticles"].Value as ParticleSystem[];
        foreach (var particle in particles)
        {
            particle.Stop();
            particle.Clear();
        }

        foreach (var l in _lights)
            l.color = _stateColor[EnemyState.Patrolling];

    }

    /// <summary>
    ///   Link actions and funcs to methods
    /// </summary>
    protected void InitActions()
    {
        //Conditions
        fCanHear = MCanHear;
        fCanSee = MCanSee;
        fHasPath = MHasPath;
        fIsPatrolling = MIsPatrolling;
        fIsStuck = MIsStuck;
        fIsWandering = MIsWandering;
        fCloseToPatrolPoint = MCloseToPatrolPoint;
        fCloseToWanderPoint = MCloseToWanderPoint;
        fIsDoneWandering = MIsDoneWandering;
        fIsDoneWithCurrentWanderPoint = MIsDoneCurrentWander;
        fIsPastRepeatPoint = MIsPastRepeat;

        fKnowsPlayerLoc = MKnowsPlayerLoc;
        fCanHitPlayer = MCanHitPlayer;
        fWasAlarmed = MWasAlarmed; //TEMP MSS IN CHILD ZETTEN
        fCloseToAlarmPoint = MCloseToAlarmPoint;
        fIsAlarmPosEst = MIsAlarmPosEst;
        fHasForgottenAlarm = MHasForgottenAlarm;//TEMP MSS IN CHILD ZETTEN
        fCanShootAgain = MCanShootAgain;
        fSlowDownDone = MSlowDownDone;
        fIsShooting = MIsShooting;
        fIsAINearby = MIsAiNearby;
        fIsDead = MIsDead;

        //Actions
        aNextWayPoint = MNextWayPoint;
        aNextWanderPoint = MNextWanderPoint;
        aSwitchToPatrolling = MSwitchToPatrol;
        aSwitchToWander = MSwitchToWander;
        aLookAround = MLookAround;
        aStopWalking = MStopWalking;

        aSlowDown = MSlowDown;
        aResetTime = MResetTime;
        aSwitchAttackTarget = MSwitchEnemyTarget;
        aAlarmFriends = MAlarmFriends;
        aGoToAlarm = MGoToAlarm;
        aGoToEstLoc = MGoToEst;
        aGetFriendlyPath = MGetFriendlyPath;

        aSearchPlayer = Selector(fIsWandering,
                            Conditional(Or(fCloseToWanderPoint,fIsStuck), aNextWanderPoint),
                            Selector(Or(fIsStuck,fCloseToAlarmPoint),                               //TEMP needs to be conditional => no net set des every frame
                                Selector(Not(fIsAlarmPosEst),
                                    aGoToEstLoc,
                                    aSwitchToWander),
                                aGoToAlarm));

        aPatrollingCheck = Selector(fHasPath, aSwitchToPatrolling, aSwitchToWander);

        aSwarmPlayer = MSwarmPlayer;

        aAim = MAim;
        aShoot = MShoot;
        aDie = MDie;
    }

    /// <summary>
    ///   Update timers
    /// </summary>
    protected void UpdateBB()
    {
        if (_running)
        {
            UpdateBT();
            if (BB.ContainsKey("IsWandering") && (bool)BB["IsWandering"].Value)
            {
                if ((bool)BB["WasAlarmed"].Value)
                    //Update alarm timer to determine if needs to abandon search
                    BB["AlarmTimer"].Value = (float)BB["AlarmTimer"].Value + Time.deltaTime;

                //Update wander timers
                BB["SecondsSinceStartWander"].Value = (float)BB["SecondsSinceStartWander"].Value + Time.deltaTime;
                BB["SecondsSinceWanderPoint"].Value = (float)BB["SecondsSinceWanderPoint"].Value + Time.deltaTime;
            }

            //Update shoot delay
            if(BB.ContainsKey("SecondsSinceShot"))
                BB["SecondsSinceShot"].Value = (float)BB["SecondsSinceShot"].Value + Time.deltaTime * (float)PBB["TimeScale"].Value;

            //Update slow-motion timer
            if (BB.ContainsKey("SlowDownTimer"))
                BB["SlowDownTimer"].Value = (float)BB["SlowDownTimer"].Value + Time.deltaTime;

            //Update animation speed relative to agent speed and angle
            if (BB.ContainsKey("Agent"))
            {
                var agent = BB["Agent"].Value as NavMeshAgent;
                var vel = agent.velocity;
                //Debug.Log("Vel: " + vel.sqrMagnitude);
                var angle = FantasizedAngle(transform.forward, vel,true);
                //var angle = Vector3.Angle(transform.forward, vel);
                float speed = angle > 90f ? -1 : 1;
                speed *= vel.sqrMagnitude;
                //Debug.Log("Speed: " + speed);
                //Debug.Log(agent.destination);
                ((Animator)BB["Animator"].Value).SetFloat("velocity", speed);
                if ((bool)BB["IsPatrolling"].Value || (bool)BB["IsWandering"].Value || !(bool)BB["KnowsPlayer"].Value)
                {
                    if (vel.magnitude <= 0.5f)
                    {
                        BB["StuckTimer"].Value = (float)BB["StuckTimer"].Value + Time.deltaTime;
                    }
                    else
                    {
                        BB["StuckTimer"].Value = 0f;
                    }
                }
                else
                {
                    BB["StuckTimer"].Value = 0f;
                }
            }
            if (!MIsDead())
            {
                _mngr.SetState(gameObject.GetHashCode(), GetState());
            }
        }
    }

    private void OnEnable()
    {
        //Reset variables on respawn
        if (_died)
        {
            ChangeVariable<float>("SlowDownTimer", 0f);
            ChangeVariable<float>("SecondsSinceShot", 0f);
            ChangeVariable<bool>("IsWandering", false);
            ChangeVariable<float>("SecondsSinceStartWander", 0f);
            ChangeVariable<float>("SecondsSinceWanderPoint", 0f);
            ChangeVariable<bool>("KnowsPlayer", false);
            ChangeVariable<bool>("KnowsCompanion", false);
            ChangeVariable<Transform>("AttackTarget", null);
            ChangeVariable<Vector3>("AlarmPosition", new Vector3());
            ChangeVariable<bool>("IsAlarmPosEst", false);
            ChangeVariable<Vector3>("PreviousPos", new Vector3());
            ChangeVariable<bool>("WasAlarmed", false);
            ChangeVariable<float>("AlarmTimer", 0f);
            ChangeVariable<bool>("IsPatrolling", true);
            ChangeVariable<bool>("ReceivedDamage", false);
            _died = false;
        }
    }

    #endregion

    #region Helper and Public Methods

    /// <summary>
    ///   Return AI position
    /// </summary>
    protected Vector3 GetPos()
    {
        return ((Transform)BB["Transform"].Value).position;
    }

    /// <summary>
    ///   Get random position inside circle
    /// </summary>
    protected Vector3 RandomPos(Vector3 origin, float min, float max, int area)
    {
        NavMeshHit hit;
        Vector3 pos;
        float dis;
        int maxTries = 20;
        int tries = 0;
        do
        {
            ++tries;
            pos = UnityEngine.Random.onUnitSphere;
            dis = UnityEngine.Random.Range(min, max);
            pos *= dis;
            pos += origin;

        } while (!NavMesh.SamplePosition(pos, out hit, dis, area) && tries < maxTries);

        if (!NavMesh.SamplePosition(pos, out hit, dis, area))
            NavMesh.SamplePosition(transform.position, out hit, dis, area);

        return hit.position;
    }
    /// <summary>
    ///   Get the state the AI is in
    /// </summary>
    public EnemyState GetState()
    {
        if (BB.ContainsKey("KnowsPlayer") && (bool)BB["KnowsPlayer"].Value)
            return EnemyState.Attacking;

        if (BB.ContainsKey("WasAlarmed") && (bool)BB["WasAlarmed"].Value)
            return EnemyState.Searching;

        return EnemyState.Patrolling;
    }

    /// <summary>
    ///   Get the waypoint it is/was heading to
    /// </summary>
    public Vector3 GetWayPoint()
    {
        // if (fIsWandering()) return Vector3.one;
        return (Vector3)BB["WayPoint"].Value;
    }
    /// <summary>
    ///   Alarm AI
    /// </summary>
    public virtual void Alarm(Vector3 targetPosition, bool isPlayer)
    {
        BB["AlarmTimer"].Value = 0f;
        var agent = BB["Agent"].Value as NavMeshAgent;
        targetPosition = RandomPos(targetPosition, 1f, 5f, agent.areaMask);
        BB["AlarmPosition"].Value = targetPosition;

        if ((bool)BB["WasAlarmed"].Value)
        {
            BB["IsWandering"].Value = false;
            return;
        }

        BB["WasAlarmed"].Value = true;
        //Debug.Log("GotAlarmedPos");
        agent.SetDestination(targetPosition);
        //Debug.Log(this.gameObject.name + "Going to alarm");
    }

    /// <summary>
    ///   Damage received from unknown location
    /// </summary>
    public virtual void DealDamage(float damage, Vector3 pos)
    {
        BB["Health"].Value = (float)BB["Health"].Value - damage;
        BB["ReceivedDamage"].Value = true;
        //CanTakeDamage = false;
        if ((float)BB["Health"].Value > 0)
        {
            HurtAnimation();
            Alarm(pos, true);
        }
    }

    /// <summary>
    ///   Damage received from visible player
    /// </summary>
    public void DealDamage(float damage, bool isDamaged)
    {
        BB["Health"].Value = (float)BB["Health"].Value - damage;
        BB["ReceivedDamage"].Value = true;
        //CanTakeDamage = false;

        if (isDamaged)
        {
            if ((float)BB["Health"].Value > 0)
            {
                HurtAnimation();
                Alarm(((Transform)PBB["PlayerTransform"].Value).position, true);
            }
        }
    }

    protected virtual void HurtAnimation()
    {
        var particles = BB["DefectParticles"].Value as ParticleSystem[];
        foreach (var particle in particles)
        {
            particle.Play();
        }
    }

    /// <summary>
    ///   Respawn AI
    /// </summary>
    public void Revive()
    {
        BB["Health"].Value = (float)BB["MaxHealth"].Value;
        var wayPoints = BB["WayPoints"].Value as List<Transform>;
        var wayPoint = (Vector3)BB["WayPoint"].Value;

        if (wayPoints != null && wayPoints.Count > 0)
            wayPoint = wayPoints[0].position;

        transform.position = wayPoint;
    }
    #endregion
    #region Bool Methods
    protected bool MIsAiNearby()
    {
        var pos = transform.position;
        var enemies = _mngr.GetNrOfNearbyEnemies(3f, pos); //T3MP M4G1C NUM83R
        return enemies > 0;
    }
    protected bool MCloseToPatrolPoint()
    {
        var pos = transform.position;
        var wayPoint = (Vector3)BB["WayPoint"].Value;
        var margin = (float)BB["WayPointMargin"].Value;

        float dis = (pos - wayPoint).sqrMagnitude;
        return dis < margin * margin;
    }

    protected virtual bool MHasPath()
    {
        var wayPoints = BB["WayPoints"].Value as List<Transform>;
        return wayPoints != null && wayPoints.Count > 0;
    }

    protected bool MSlowDownDone()
    {
        var time = (float)BB["SlowDownTimer"].Value;
        var maxTime = (float)BB["SlowDownTime"].Value;
        return time >= maxTime;
    }

    protected virtual bool MWasAlarmed()
    {
        return (bool)BB["WasAlarmed"].Value;
    }

    protected virtual bool MHasForgottenAlarm()
    {
        var time = (float)BB["AlarmTimer"].Value;
        var maxTime = (float)BB["MaxAlarmTime"].Value;

        if (time >= maxTime)
        {
            BB["AlarmTimer"].Value = 0f;
            BB["WasAlarmed"].Value = false;
            foreach (var l in _lights)
                l.color = _stateColor[EnemyState.Patrolling];

            if (_renderer != null)
            {
                var r = BB["Renderer"].Value as Renderer;
                var mat = r.material;
                mat.SetColor("_EmissionColor", _stateColor[EnemyState.Patrolling]);
            }

            //Debug.Log("Forgot alarm");
            return true;
        }

        foreach (var l in _lights)
        {
            l.color = _stateColor[EnemyState.Searching];
            if (_renderer != null)
            {
                var r = BB["Renderer"].Value as Renderer;
                var mat = r.material;
                mat.SetColor("_EmissionColor", _stateColor[EnemyState.Searching]);
            }
        }

        //Debug.Log("Has not forgotten alamr");
        return false;
    }

    protected bool MIsPatrolling()
    {
        return (bool)BB["IsPatrolling"].Value;
    }

    protected bool MIsDoneWandering()
    {
        var time = (float)BB["SecondsSinceStartWander"].Value;
        var max = (float)BB["MaxWanderTime"].Value;

        if (time >= max)
        {
            BB["SecondsSinceStartWander"].Value = 0f;
            return true;
        }

        return false;
    }

    protected bool MCanHear()
    {
        return (bool)BB["CanHear"].Value;
    }

    protected bool MCanSee()
    {
        return (bool)BB["CanSee"].Value;
    }

    private bool MCloseToAlarmPoint()
    {
        var pos = transform.position;
        var alarmPoint = (Vector3)BB["AlarmPosition"].Value;
        var margin = (float)BB["WayPointMargin"].Value;

        var dis = (pos - alarmPoint).sqrMagnitude;

        //Debug.Log("CloseToAlarm?" + (dis < margin * margin));

        return dis < margin * margin;
    }

    protected virtual bool MCanHitPlayer()
    {
        var gun = BB["GunTransform"].Value as Transform ?? BB["Transform"].Value as Transform;
        var player = ((Transform)PBB["PlayerTransform"].Value).position;
        var maxAngle = (float)BB["MaxShootAngle"].Value;

        var layerMask = (1 << 8);
        layerMask |= (1 << 9);
        layerMask |= (1 << 11);

        //Check posibility of hit
        if (Physics.Linecast(gun.position, player, ~layerMask))
            return false;

        //Check max angle
        Vector3 dir = (player - gun.position).normalized;
        //float angle = Vector3.Angle(-gun.right, dir);
        float angle = FantasizedAngle(-gun.right, dir, true);
        if (!(angle < maxAngle))
            return false;

        //Check max distance
        var dis = Vector3.Distance(gun.position, player);
        var maxDis = (float)BB["GunRange"].Value;
        if (!(dis <= maxDis))
            return false;

        return true;
    }

    private bool MCanShootAgain()
    {
        var time = (float)BB["SecondsSinceShot"].Value;
        var maxTime = (float)BB["ShootDelay"].Value;
        return time >= maxTime;
    }

    private bool MIsShooting()
    {
        var anim = BB["Animator"].Value as Animator;
        var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        //Debug.Log(stateInfo.IsTag("Shooting"));
        return stateInfo.IsTag("Shooting");
    }

    private bool MKnowsPlayerLoc()
    {
        var knowsPlayer = (bool)BB["KnowsPlayer"].Value;
        //var knowsComp = (bool)BB["KnowsCompanion"].Value;
        if (knowsPlayer /*|| knowsComp*/)
        {
            foreach (var l in _lights)
                l.color = _stateColor[EnemyState.Attacking];

            if (_renderer != null)
            {
                var r = BB["Renderer"].Value as Renderer;
                var mat = r.material;
                mat.SetColor("_EmissionColor", _stateColor[EnemyState.Attacking]);
            }
        }

        return knowsPlayer /*|| knowsComp*/;
    }

    private bool MIsDoneCurrentWander()
    {
        var time = (float)BB["SecondsSinceWanderPoint"].Value;
        var maxTime = (float)BB["WanderPointTime"].Value;
        if (time >= maxTime)
        {
            BB["SecondsSinceWanderPoint"].Value = 0f;
            return true;
        }
        return false;
    }

    private bool MCloseToWanderPoint()
    {
        var pos = transform.position;
        var wanderPoint = (Vector3)BB["WayPoint"].Value;
        var margin = (float)BB["WayPointMargin"].Value;

        return (pos - wanderPoint).sqrMagnitude < margin * margin;
    }

    private bool MIsStuck()
    {
        var timer = (float)BB["StuckTimer"].Value;
        var maxTime = (float)BB["MaxStuckTime"].Value;
        return timer > maxTime;
    }

    private bool MIsWandering()
    {
        return (bool)BB["IsWandering"].Value;
    }

    protected bool MIsDead()
    {
        return (float)BB["Health"].Value <= 0;
    }

    private bool MIsAlarmPosEst()
    {
        return (bool)BB["IsAlarmPosEst"].Value;
    }

    private bool MIsPastRepeat()
    {
        var nrWP = (int)BB["CurrentWayPoint"].Value;
        var rP = (int)BB["RepeatStart"].Value;
        return nrWP >= rP;
    }
    #endregion
    #region Void Methods
    protected void MGetFriendlyPath()
    {
        var pos = transform.position;
        var enemies = _mngr.GetNearbyEnemies(3f, pos);
        if (enemies.Count < 1) return;

        var newWayPoint = enemies[0].GetComponent<Script_EnemyBase>().GetWayPoint();
        BB["WayPoint"].Value = newWayPoint;
        (BB["Agent"].Value as NavMeshAgent).SetDestination(newWayPoint);
        //Debug.Log(this.gameObject.name + "Following friend");

    }
    protected void MNextWayPoint()
    {
        var nrWP = (int)BB["CurrentWayPoint"].Value;
        var wayPoints = BB["WayPoints"].Value as List<Transform>;
        var agent = BB["Agent"].Value as NavMeshAgent;
        var rP = (int)BB["RepeatStart"].Value;

        nrWP = nrWP + 1 >= wayPoints.Count ? rP : nrWP + 1;
        BB["CurrentWayPoint"].Value = nrWP;
        var wP = wayPoints[nrWP].position;
        agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Going to next waypoint");
        //Debug.Log("NextWayPoint");
        BB["WayPoint"].Value = wP;
    }

    protected void MSwitchEnemyTarget()
    {
        var knowsPlayer = (bool)BB["KnowsPlayer"].Value;
        var player = PBB["PlayerTransform"].Value as Transform;
        var knowsComp = (bool)BB["KnowsCompanion"].Value;
        var comp = BB["CompanionTransform"].Value as Transform;

        BB["AttackTarget"].Value = knowsPlayer ? player : knowsComp ? comp : null;
    }

    protected virtual void MAlarmFriends()
    {
        var alarmType = (PatrolContact)BB["AlarmType"].Value;
        var pos = (Vector3)BB["AlarmPosition"].Value;
        var knowsPlayer = (bool)BB["KnowsPlayer"].Value;
        var selfPos = transform.position;
        switch (alarmType)
        {
            case PatrolContact.Friends:
                var friends = BB["Friends"].Value as Script_EnemyBase[];
                if (friends == null || friends.Length < 1)
                    return;

                foreach (var friend in friends.Where(x => x != null))
                    friend.Alarm(pos, knowsPlayer);
                break;

            case PatrolContact.OverlapSphere:
                var radius = (float)BB["AlarmRadius"].Value;
                var layerMask = 1 << LayerMask.NameToLayer("Enemies");
                var cols = Physics.OverlapSphere(selfPos, radius, layerMask, QueryTriggerInteraction.Collide);

                foreach (var col in cols)
                {
                    var eB = col.GetComponent<Script_EnemyBase>();
                    if (eB != null)
                        eB.Alarm(pos, knowsPlayer);
                }
                break;
        }
        BB["WasAlarmed"].Value = true;
    }

    //protected void MFriendsNearby()
    //{
    //    var selfPos = transform.position;
    //    var radius = (float)BB["NearbyRadius"].Value;
    //    var layerMask = 1 << LayerMask.NameToLayer("Enemies");
    //    var cols = Physics.OverlapSphere(selfPos, radius, layerMask, QueryTriggerInteraction.Collide);
    //    if (cols.Length > 0)
    //    {
    //        var nAgent = cols[0].GetComponent<Script_EnemyBase>().BB["Agent"].Value as NavMeshAgent;
    //        var agent = BB["Agent"].Value as NavMeshAgent;
    //        agent.SetDestination(nAgent.destination);
    //        Debug.Log(this.gameObject.name + "Backing Off");
    //        //Debug.Log("GotFriendsPos");
    //    }
    //}

    protected virtual void MSwitchToPatrol()
    {
        var nrWP = (int)BB["CurrentWayPoint"].Value;
        var wayPoints = BB["WayPoints"].Value as List<Transform>;
        var agent = BB["Agent"].Value as NavMeshAgent;

        var wP = wayPoints[nrWP].position;
        agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Switching to patrolling");
        //Debug.Log("SwitchToPatrol");
        BB["WayPoint"].Value = wP;
        BB["IsPatrolling"].Value = true;
        BB["IsWandering"].Value = false;
    }

    protected virtual void MAim()
    {
        var player = (PBB["PlayerTransform"].Value as Transform).position;
        var trans = BB["Transform"].Value as Transform;
        var pos = trans.position;
        player.y = pos.y;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.angularSpeed = 0f;
        //Debug.Log("Aim");
    }

    protected virtual void MShoot()
    {
        BB["SecondsSinceShot"].Value = 0f;
        (BB["Animator"].Value as Animator).SetTrigger("shot");
        //Debug.Log("shot");
        StartCoroutine(ShootDelay());
    }

    protected virtual void MSwarmPlayer()
    {
    }

    protected virtual void MLookAround()
    {
    }

    protected virtual void MSwitchToWander()
    {
        var agent = BB["Agent"].Value as NavMeshAgent;

        var wP = RandomPos(transform.position, 6f, 12f, agent.areaMask);

        agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Switching to wander");
        //Debug.Log("SwitchToWander");
        BB["WayPoint"].Value = wP;
        BB["IsPatrolling"].Value = false;
        BB["IsWandering"].Value = true;
    }

    private void MNextWanderPoint()
    {
        var agent = BB["Agent"].Value as NavMeshAgent;

        var wP = RandomPos(transform.position, 6f, 12f, agent.areaMask);
        agent.SetDestination(wP);
        //Debug.Log(this.gameObject.name + "Going to next wander point");
        //Debug.Log("NextWanderPoint");
        BB["WayPoint"].Value = wP;
    }

    private void MGoToAlarm()
    {
        var tP = (Vector3)BB["AlarmPosition"].Value;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.SetDestination(tP);
        //Debug.Log(this.gameObject.name + "Going to alarm position");
        //Debug.Log("GoToAlarm");
        //Debug.Log("Going To Alarm");
    }

    private void MGoToEst()
    {
        var prevPos = (Vector3)BB["PreviousPos"].Value;
        var alarmPos = (Vector3)BB["AlarmPosition"].Value;
        var agent = BB["Agent"].Value as NavMeshAgent;

        var oldDir = (alarmPos - prevPos).normalized;
        var estPos = alarmPos + oldDir * 15f;
        var newTarget = RandomPos(estPos, 2f, 4f, agent.areaMask);

        BB["AlarmPosition"].Value = newTarget;
        BB["IsAlarmPosEst"].Value = true;

        agent.SetDestination(newTarget);
        //Debug.Log(this.gameObject.name + "Going to estimated player position");
        //Debug.Log("GoingToEstimatedLocation");
        //TEMP
    }

    private void MStopWalking()
    {
        var tP = transform.position;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.SetDestination(tP);
        //Debug.Log(this.gameObject.name + "Can Hit player");
        //Debug.Log("StopWalking");
    }
    protected virtual void MDie()
    {
        _mngr.EnemyDestroyed(gameObject.GetHashCode());
        _died = true;
        if (_hasDataKey)
            Script_PlayerInformation.Instance.AmountOfDataKeys = 1;
        gameObject.SetActive(false);
    }
    private void MSlowDown()
    {
        //Debug.Log("SlowDown");
        var scale = (float)BB["SlowDownScale"].Value;
        PBB["TimeScale"].Value = scale;

        var startAgent = BB["StartAgent"].Value as AgentVars;
        var pos = ((Transform) BB["Transform"].Value).position;
        var agent = BB["Agent"].Value as NavMeshAgent;
        var newSpeed = startAgent.Speed * scale;
        var newAngSpeed = startAgent.AngularSpeed * scale;
        agent.speed = newSpeed;
        agent.angularSpeed = newAngSpeed;
        agent.SetDestination(pos);
        //Debug.Log(this.gameObject.name + "Slow Motion");

        BB["SlowDownTimer"].Value = 0f;
        BB["SecondsSinceShot"].Value = 0f;

        BB["WasAlarmed"].Value = true;

        var animator = (BB["Animator"].Value as Animator);
        animator.SetFloat("animationSpeed", scale);

    }

    private void MResetTime()
    {
        //Debug.Log("TimeReset");
        PBB["TimeScale"].Value = 1f;
        var startAgent = BB["StartAgent"].Value as AgentVars;
        var agent = BB["Agent"].Value as NavMeshAgent;
        agent.speed = startAgent.Speed;
        agent.angularSpeed = startAgent.AngularSpeed;

        var animator = (BB["Animator"].Value as Animator);
        animator.SetFloat("animationSpeed", 1f);
    }
    IEnumerator ShootDelay()
    {
        yield return new WaitUntil(fIsShooting);
        ((Animator) BB["Animator"].Value).ResetTrigger("shot");
    }
    #endregion
}

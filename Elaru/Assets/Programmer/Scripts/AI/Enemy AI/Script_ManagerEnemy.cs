using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Script_ManagerEnemy : Script_Singleton<Script_ManagerEnemy>
{

    #region Custom Struct, Class and Enums
    /// <summary>
    ///   Type of enemy
    /// </summary>
    [Serializable]
    public enum EnemyType
    {
        Deaf,
        Blind,
        Invalid
    }

    /// <summary>
    ///   Patrol info
    /// </summary>
    [Serializable]
    public class Patrol
    {
        public Dictionary<int, bool> Enemies;
        public bool IsDefeated = false;
        public float Timer = 0f;
        public EnemyType Type = EnemyType.Blind;

        public Patrol(Dictionary<int, bool> e, EnemyType type)
        {
            Enemies = e;
            Type = type;
        }
    }

    //struct WarningPosition
    //{
    //    public int hashCode;
    //    public Vector3 pos;

    //    public WarningPosition(int i, Vector3 p)
    //    {
    //        hashCode = i;
    //        pos = p;
    //    }
    //}
    #endregion

    #region Variables
    /// <summary>
    ///   List of all patrols
    /// </summary>
    public static List<Patrol> Patrols = new List<Patrol>();
    /// <summary>
    ///   Link between enemy hashes and patrol numbers
    /// </summary>
    public static Dictionary<int, int> ObjectPatrolLink = new Dictionary<int, int>();

    /// <summary>
    ///   Duration a patrol needs to be wiped before resetting
    /// </summary>
    private const float RespawnTime = 600f;

    /// <summary>
    ///   Link between enemy hashes and the objects
    /// </summary>
    static Dictionary<int, GameObject> _enemies = new Dictionary<int, GameObject>();
    private Dictionary<int, Script_EnemyBase.EnemyState> _states = new Dictionary<int, Script_EnemyBase.EnemyState>();
    /// <summary>
    ///   Location of previously played sound
    /// </summary>
    private Vector3 _previousSound = new Vector3();
    private Vector3 _previousLight = new Vector3();

    /// <summary>
    ///   Central position where player movement needs to be locked
    /// </summary>
    //private WarningPosition _warningPos = new WarningPosition(0, Vector3.zero);
    private Bounds _playerBounds = new Bounds();
    /// <summary>
    ///   Is player movement locked
    /// </summary>
    private bool _isLocked = false;

    private List<int> _sees = new List<int>();

    private Vector3 _pPos = new Vector3();
    #endregion

    //Update patrol respawn timers
    void Update()
    {
        //FUNCTIONAL => IN COMMENT FOR TESTING
        //if (Patrols == null)
        //    return;

        //for (var i = 0; i < Patrols.Count; i++)
        //{
        //    if (Patrols[i].IsDefeated)
        //    {
        //        Patrols[i].Timer += Time.deltaTime;
        //        if (Patrols[i].Timer >= RespawnTime)
        //            Respawn(i);
        //    }
        //}
    }

    #region Public Functions
    /// <summary>
    ///   Add patrol by passing a list of gameobjects and a type
    /// </summary>
    public void AddPatrol(List<GameObject> e, EnemyType type)
    {
        var hash = e[0].GetHashCode();

        if (!_enemies.ContainsKey(hash))
            _enemies.Add(hash, e[0]);

        if (ObjectPatrolLink.ContainsKey(hash))
        {
            foreach (var enemy in e)
            {
                int patrolID = ObjectPatrolLink[enemy.GetHashCode()];
                enemy.SetActive(Patrols[patrolID].Enemies[enemy.GetHashCode()]);
                _states[hash] = Script_EnemyBase.EnemyState.Patrolling;
            }
            return; //Already contains this one
        }

        Dictionary<int, bool> patrol = new Dictionary<int, bool>();
        foreach (var enemy in e)
        {
            patrol.Add(enemy.GetHashCode(), true);
            ObjectPatrolLink.Add(enemy.GetHashCode(), Patrols.Count);
        }

        Patrols.Add(new Patrol(patrol, type));
    }

    /// <summary>
    ///   Respawn patrol by number
    /// </summary>
    public void Respawn(int id)
    {
        Patrols[id].IsDefeated = false;
        Patrols[id].Timer = 0f;
        List<int> keys = new List<int>(Patrols[id].Enemies.Keys);
        foreach (var key in keys)
        {
            Patrols[id].Enemies[key] = true;
            _enemies[key].SetActive(true);
            _enemies[key].GetComponent<Script_EnemyBase>().Revive();
        }
    }

    /// <summary>
    ///   Destroy enemy by hash
    /// </summary>
    public void EnemyDestroyed(int hash)
    {
        if (!ObjectPatrolLink.ContainsKey(hash))
            return; //No matching patrol

        Script_AchievementManager.Instance.UpdateKillAchievement(_enemies[hash]);

        int id = ObjectPatrolLink[hash];
        Patrols[id].Enemies[hash] = false;

        Script_CompanionAI.CompanionState = Script_CompanionAI.CompanionMode.Happy;

        _states[hash] = Script_EnemyBase.EnemyState.Patrolling;

        LockPlayer(_enemies[hash].transform.position, false, hash);

        //Is Patrol Wiped Out
        if (Patrols[id].Enemies.Any(enemy => enemy.Value))
            return;

        Patrols[id].IsDefeated = true;
        Patrols[id].Timer = 0f;


    }

    /// <summary>
    ///   Get patrol members by enemy hash
    /// </summary>
    public List<GameObject> GetFriends(int hash)
    {
        List<GameObject> result = new List<GameObject>();
        if (!_enemies.ContainsKey(hash))
            return result; //No such enemy

        int patrolID = ObjectPatrolLink[hash];
        result.AddRange(from e in Patrols[patrolID].Enemies.Keys where e != hash select _enemies[e]);
        return result;
    }

    /// <summary>
    ///   Get enemy type by enemy hash
    /// </summary>
    public EnemyType GetType(int hash)
    {
        if (!ObjectPatrolLink.ContainsKey(hash))
        {
            Debug.Log("Get Type Function didn't find requested hash");
            return EnemyType.Invalid; //temp
        }

        int patrolID = ObjectPatrolLink[hash];
        return Patrols[patrolID].Type;
    }

    /// <summary>
    ///   Deal damage to all enemies in an area
    /// </summary>
    public void AreaDamage(Vector3 origin, float radius, int damage)
    {
        var cols = Physics.OverlapSphere(origin, radius/*, layerMask*/);
        foreach (var col in cols)
        {
            var layer = col.gameObject.layer;
            if (layer == LayerMask.NameToLayer("Enemies"))
                col.GetComponentInParent<Script_EnemyBase>().DealDamage(damage, origin);
            else if (col.transform.CompareTag("MainCamera"))
                Script_PlayerInformation.Instance.TakeDamage(damage);
        }
    }

    /// <summary>
    ///   Play sound witout range based on type
    /// </summary>
    public void Sound(Vector3 origin, Script_EnemyBase.SoundType type)
    {
        Sound(origin, type, (float)type * 20f);
    }

    /// <summary>
    ///   Play sound without range
    /// </summary>
    public void Sound(Vector3 origin, Script_EnemyBase.SoundType type, float range)
    {
        if (origin == _previousSound)
            return; //Same sound already registered

        _previousSound = origin;

        var layerMask = 1 << LayerMask.NameToLayer("Enemies");
        var cols = Physics.OverlapSphere(origin, range, layerMask);
        List<int> warned = new List<int>();

        foreach (var col in cols)
        {
            if (col.transform.parent == null)
                return;

            var hash = col.transform.parent.gameObject.GetHashCode();
            if (GetType(hash) != EnemyType.Blind)
                continue; //Enemy is deaf

            if (!warned.Contains(hash)) //Check if alarmed
            {
                col.GetComponentInParent<Script_BlindAI>().Alarm(origin, type);
                warned.Add(hash);
                foreach (var friend in GetFriends(hash)) //Alarm friends
                    friend.GetComponent<Script_BlindAI>().Alarm(origin, type);
            }
        }
    }

    public void Light(Vector3 origin, float range)
    {
        if (origin == _previousLight)
            return; //Same sound already registered

        _previousLight = origin;

        var layerMask = 1 << LayerMask.NameToLayer("Enemies");
        var cols = Physics.OverlapSphere(origin, range, layerMask, QueryTriggerInteraction.Collide);
        foreach (var col in cols)
        {
            var hash = col.gameObject.GetHashCode();
            if (GetType(hash) != EnemyType.Deaf)
                continue; //Enemy is blind

            col.GetComponent<Script_DeafAI>().Alarm(origin, true);
        }
    }
    /// <summary>
    ///   Lockplayer in certain position
    /// </summary>
    public void LockPlayer(Vector3 pos, bool isLocked, int hash)
    {
        if (isLocked && !_sees.Contains(hash))
            _sees.Add(hash);
        else if (!isLocked && _sees.Contains(hash))
            _sees.Remove(hash);

        if (_sees.Count < 1)
        {
            _isLocked = false;
            _playerBounds.size = Vector3.zero;
        }
        else
        {
            if (_sees.Count == 1)
            {
                //_warningPos = new WarningPosition(hash, pos);
                _playerBounds.center = _pPos;
            }
            //else if (_warningPos.hashCode == hash)
            //{
            //    _warningPos = new WarningPosition(hash, pos);
            //}
            //else if ((_pPos - pos).sqrMagnitude > (_pPos - _warningPos.pos).sqrMagnitude)
            //{
            //    _warningPos = new WarningPosition(hash, pos);
            //} 
            if (isLocked)
            {
                _playerBounds.Encapsulate(pos);
                _isLocked = true;
            }
        }
    }
    /// <summary>
    ///   Get position if player is locked
    /// </summary>
    public bool GetLocked(out float dis, Vector3 pPos)
    {
        //if (_sees.Count == 1)
        //{
        //    _pPos = pPos;
        //}
        _pPos = pPos;

        //dis = Vector3.Distance(_pPos, _warningPos.pos);
        dis = Vector3.Distance(_playerBounds.min, _playerBounds.max) / 2;
        return _isLocked;
    }

    public int GetNrOfNearbyEnemies(float range, Vector3 origin)
    {
        int result = 0;
        foreach (var enemy in _enemies.Values)
        {
            if (enemy != null && enemy.activeSelf)
                result = Vector3.Distance(origin, enemy.transform.position) < range ? result + 1 : result;
        }
        return result;
    }

    public List<GameObject> GetNearbyEnemies(float range, Vector3 origin)
    {
        List<GameObject> result = new List<GameObject>();
        foreach (var enemy in _enemies.Values)
        {
            if (enemy != null && enemy.activeSelf && Vector3.Distance(origin, enemy.transform.position) < range)
                result.Add(enemy);
        }
        return result;
    }

    public int GetNrAttackingEnemies()
    {
        int result = 0;
        foreach (var state in _states.Values)
        {
            if (state == Script_EnemyBase.EnemyState.Attacking)
                ++result;
        }
        return result;
    }
    public List<Vector3> GetAttackingEnemies()
    {
        List<Vector3> result = new List<Vector3>();
        foreach (var state in _states)
        {
            if (state.Value == Script_EnemyBase.EnemyState.Attacking)
                result.Add(_enemies[state.Key].transform.position);
        }
        return result;
    }
    public int GetNrSearchingEnemies()
    {
        int result = 0;
        foreach (var state in _states.Values)
        {
            if (state == Script_EnemyBase.EnemyState.Searching)
                ++result;
        }
        return result;
    }
    public List<Vector3> GetSearchingEnemies()
    {
        List<Vector3> result = new List<Vector3>();
        foreach (var state in _states)
        {
            if (state.Value == Script_EnemyBase.EnemyState.Searching)
                result.Add(_enemies[state.Key].transform.position);
        }
        return result;
    }

    public void SetState(int hash, Script_EnemyBase.EnemyState state)
    {
        _states[hash] = state;
    }
    #endregion
}

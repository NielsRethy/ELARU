using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Script_BehaviorTreeFramework : MonoBehaviour
{

    #region BlackBoard
    /// <summary>
    ///  The personal blackboard of an AI
    /// </summary>
    public Dictionary<string, IVariable> BB = new Dictionary<string, IVariable>();

    /// <summary>
    ///  The public blackboard used by all AI
    /// </summary>
    public static Dictionary<string, IVariable> PBB = new Dictionary<string, IVariable>();

    static Action aTransition = () => { };
    static Action aOne = () => { Debug.Log("1"); };
    static Action aTwo = () => { Debug.Log("2"); };
    static Action aThree = () => { Debug.Log("3"); };
    static Action aFour = () => { Debug.Log("4"); };

    static Func<bool> fTrue = () => true;
    static Func<bool> fAllus = () => false;
    /// <summary>
    ///  List of all tree actions
    /// </summary>
    public List<Action> ActionList = new List<Action> { aTransition, aOne, aTwo, aThree, aFour };
    /// <summary>
    ///  List of all tree conditions
    /// </summary>
    public List<Func<bool>> CondList = new List<Func<bool>> { fTrue, fAllus };
    private void Awake()
    {
        //Variables used by all AI
        PBB = new Dictionary<string, IVariable>
        {
            {"PlayerTransform", new Variable<Transform>(Camera.main.transform) },
            {"TimeScale", new Variable<float>(1f) },
            {"GunDamage", new Variable<float>(10f) },
            {"SwordDamage", new Variable<float>(60f) },
            {"UsingVR", new Variable<bool>(SteamVR.active) },
            {"BulletWidth", new Variable<float>(0.5f) }
        };
    }

    public interface IVariable
    {
        Type DataType { get; }
        object Value { get; set; }
    }

    private interface IVariable<T> : IVariable
    {
        new T Value { get; }
    }

    private class Variable<T> : IVariable<T>
    {
        public Variable(T value)
        {
            Value = value;
        }

        public Type DataType
        {
            get { return typeof(T); }
        }

        object IVariable.Value
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        public T Value { get; private set; }
    }

    /// <summary>
    ///  Adding a variable to the personal blackboard
    /// </summary>
    protected void AddVariable<T>(string varName, T variable)
    {
        if (BB.ContainsKey(varName))
        {
            Debug.Log("Blackboard already containt a variable named: " + varName);
            return; 
        }

        var thing = new Variable<T>(variable);
        BB.Add(varName, thing);
    }

    /// <summary>
    ///  Change a variable from the personal blackboard
    /// </summary>
    protected void ChangeVariable<T>(string varName, T variable)
    {
        //Check if it exists
        if (!BB.ContainsKey(varName))
        {
            Debug.Log("Blackboard doesn't contain a variable named: " + varName);
            return;
        }

        //Check if key type corresponds to new value type
        if (variable.GetType() != BB[varName].DataType)
        {
            Debug.Log(varName + " is not of type: " + variable.GetType());
            return;
        }

        //Change variable in blackboard
        BB[varName].Value = variable;
    }
    #endregion

    #region Combinators
    /// <summary>
    ///  Returns true if both conditions are met
    /// </summary>
    protected Func<bool> And(Func<bool> a, Func<bool> b)
    {
        return () => a() && b();
    }
    /// <summary>
    ///  Returns true if one or both conditions are met
    /// </summary>
    protected Func<bool> Or(Func<bool> a, Func<bool> b)
    {
        return () => a() || b();
    }
    /// <summary>
    ///  Returns true if the condition is not met
    /// </summary>
    public static Func<bool> Not(Func<bool> a)
    {
        return () => !a();
    }

    /// <summary>
    ///  Calls the ifTrue if cond is true and ifFalse if cond returns false
    /// </summary>
    public static Action Selector(Func<bool> cond, Action ifTrue, Action ifFalse)
    {
        return () =>
        {
            if (cond())
                ifTrue();
            else
                ifFalse();
        };
    }

    /// <summary>
    ///  Calls the ifTrue if cond is true
    /// </summary>
    public static Action Conditional(Func<bool> cond, Action ifTrue)
    {
        return () =>
        {
            if (cond())
                ifTrue();
        };
    }

    /// <summary>
    ///  Calls both actions
    /// </summary>
    public static Action Sequencer(Action a, Action b)
    {
        return () => { a(); b(); };
    }

    public static Action Sequencer(Action[] a)
    {
        return () =>
        {
            foreach (var action in a)
            {
                action();
            }
        };
    }

    /// <summary>
    ///  Calls the array member corresponding with the provided int
    /// </summary>
    protected Action NumericBranching(Func<int> cond, Action[] actions)
    {
        return () =>
        {
            if (cond() >= actions.Length)
                Debug.Log("Condition for branch is too high");
            else
                actions[cond()]();
        };
    }

    /// <summary>
    ///  Percentage (between 0 and 1) indicates the chance a is called
    /// </summary>
    protected Action Probable(float percentage, Action a)
    {
        return () =>
        {
            //if (Percentage(percentage))
            //    a();
            if (UnityEngine.Random.value < percentage)
                a();
        };
    }

    private Func<float, bool> Percentage;

    protected bool RandomValueCompare(float percentage)
    {
        if (UnityEngine.Random.value < percentage)
            return true;
        return false;
    }

    /// <summary>
    ///  Percentage indicates the chance a is called if not b is called
    /// </summary>
    protected Action ProbableSelector(float percentage, Action a, Action b)
    {
        return () =>
        {
            if (UnityEngine.Random.value < percentage)
                a();
            else
                b();
        };
    }

    struct TimeComp
    {
        public float Time { get; private set; }
        public float MaxTime { get; private set; }

        public TimeComp(float t, float mT) : this()
        {
            Time = t;
            MaxTime = mT;
        }
    }

    private Dictionary<int, TimeComp> _timeSinceAction = new Dictionary<int, TimeComp>();
    /// <summary>
    ///  Will return true if action can be called based on passed time since last call
    ///  <para>int[0] = Unique ID</para>
    ///  <para>int[1] = Minimum time between calls (optional, will be Mathf.Infinity if empty)</para>
    ///  Example: fCanCall(new int[] { actionA.GetHashCode(), 5 }), will only call actionA maximum once every 5 seconds
    /// </summary>
    protected Func<int[],Func<bool>> fCanCall;

    protected void TimeInit()
    {
        fCanCall = (int[] code) => () => CanCall(code);
        Percentage = (float percentage) => RandomValueCompare(percentage);
    }

    private bool CanCall(int[] code)
    {
        if (!_timeSinceAction.ContainsKey(code[0]))
        {
            if (code.Length == 1)
            {
                _timeSinceAction.Add(code[0], new TimeComp(0f, Mathf.Infinity));
            }
            else
            {
                _timeSinceAction.Add(code[0], new TimeComp(0f, (float)code[1]));
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    //Keeps track of the time since a trigger once conditional was called
    protected void UpdateBT()
    {
        for (var i = 0; i < _timeSinceAction.Count; i++)
        {
            var a = _timeSinceAction.ElementAt(i);
            var newT = new TimeComp(a.Value.Time + Time.deltaTime, a.Value.MaxTime);
            if (newT.Time > newT.MaxTime)
            {
                _timeSinceAction.Remove(a.Key);
                continue;
            }
            _timeSinceAction[a.Key] = newT;
        }
    }
    #endregion

    public enum EnemyState
    {
        Attacking,
        Searching,
        Patrolling
    }
    protected Dictionary<EnemyState, Color> _stateColor = new Dictionary<EnemyState, Color>
    {
        {EnemyState.Searching,new Color(1f,0.7f,0f)},
        {EnemyState.Attacking,Color.red },
        {EnemyState.Patrolling,Color.blue }
    };

    protected float FantasizedAngle(Vector3 a, Vector3 b)
    {
        var angle = Vector3.Angle(a,b);
        var cross = Vector3.Cross(a,b);
        angle = cross.y > 0 ? angle : -angle;
        angle = angle % 360;
        angle = angle > 180 ? angle - 360 : angle;
        angle = angle < -180 ? angle + 360 : angle;
        return angle;
    }
    protected float FantasizedAngle(Vector3 a,Vector3 b,bool absolute)
    {
        if (absolute)
        {
            return Math.Abs(FantasizedAngle(a, b));
        }
        else
        {
            return FantasizedAngle(a, b);
        }
    }

    protected bool RandomBool()
    {
        var val = UnityEngine.Random.value;
        return val < 0.5f;
    }
    protected float RandomSide()
    {
        return RandomBool() ? 1 : -1;
    }
}

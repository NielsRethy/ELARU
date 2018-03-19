#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Script_BTWindow : EditorWindow
{

    [MenuItem("Window/GenerateBT")]
    public static void Init()
    {
        Script_BTWindow window = (Script_BTWindow)EditorWindow.GetWindow(typeof(Script_BTWindow));
        window.Show();
    }

    private class Connection
    {
        public int conditionIndex { get; set; }
        public SelfNode nextAction { get; set; }
        public NodeExtra nodeExtra { get; set; }
        public int triggerOnceMaxTime { get; set; }

        Connection(int c, SelfNode nA, NodeExtra nE, int maxTime)
        {
            conditionIndex = c;
            nextAction = nA;
            nodeExtra = nE;
            triggerOnceMaxTime = maxTime;
        }
        public Connection(int c, SelfNode nA, NodeExtra nE)
        {
            if (nE == NodeExtra.TriggerOnce)
            {
                return;
            }
            conditionIndex = c;
            nextAction = nA;
            nodeExtra = nE;
            triggerOnceMaxTime = 0;
        }
        Connection(int c, SelfNode nA)
        {
            conditionIndex = c;
            nextAction = nA;
            nodeExtra = NodeExtra.Default;
            triggerOnceMaxTime = 0;
        }
    }

    private class SelfNode : object
    {
        public int actionIndex { get; set; }
        public int yValue { get; set; }
        public SelfNode(int a, int y)
        {
            actionIndex = a;
            yValue = y;
        }

        public override bool Equals(object obj)
        {
            return ((SelfNode)obj).actionIndex == actionIndex;
        }
        public override int GetHashCode()
        {
            return actionIndex ^ yValue;
        }
    }
    enum NodeExtra
    {
        Default,
        Not,
        Selector,
        TriggerOnce,
        Transition
    }
    Dictionary<SelfNode, List<Connection>> _nodes = new Dictionary<SelfNode, List<Connection>>();

    MonoScript BTAsset;
    GameObject BTObj;
    Script_BehaviorTreeFramework _BT;
    System.Action FinalAction = () => { };
    Rect _nodeField = new Rect(10, 135, 500, 500);
    Rect _endField = new Rect(10, 635, 500, 500);

    int Aindex = 0;
    int Cindex = 0;
    NodeExtra NIndex = NodeExtra.Default;
    SelfNode LastAdded = null;
    List<System.Action> _actionList;
    string[] actionOptions;
    List<int> shownActions = new List<int>();
    List<Rect> _actionPositions = new List<Rect>();
    int _selectedAction = 0;

    List<System.Func<bool>> _condList;
    string[] condOptions;
    bool _showConditions = false;

    GUIStyle _BTStyle;
    Color _bColor = new Color(0.6f, 0.6f, 1f, 0.5f);
    Vector2 _node = new Vector2(75, 25);
    bool _dragging = false;
    Rect _offset = new Rect(0, 0, 75, 25);

    //Handles
    bool _isStart = true;
    Vector3 _startPoint = Vector3.one;
    Vector3 _endPoint = Vector3.one;
    int _startNode = 0;
    int _endNode = 0;
    Dictionary<Vector3, Vector3> _handles = new Dictionary<Vector3, Vector3>();
    Vector3 _lastAddedHandle;

    private void OnGUI()
    {
        _nodeField.width = position.width - 20f;
        _endField.width = position.width - 20f;
        BaseSettings();
        if (_BT != null)
        {
            if (actionOptions == null || condOptions == null)
            {
                _actionList = _BT.ActionList;
                actionOptions = new string[_actionList.Count];
                for (int i = 0; i < actionOptions.Length; i++)
                {
                    actionOptions[i] = _actionList[i].Method.Name;
                }
                _condList = _BT.CondList;
                condOptions = new string[_condList.Count];
                for (int i = 0; i < condOptions.Length; i++)
                {
                    condOptions[i] = _condList[i].Method.Name;
                }
            }

            EditorGUILayout.BeginHorizontal();
            Aindex = EditorGUILayout.Popup(Aindex, actionOptions);
            if (_showConditions)
            {
                Cindex = EditorGUILayout.Popup(Cindex, condOptions);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            int amount = (int)(_nodeField.width / _node.x);
            if (GUILayout.Button("Add Action"))
            {
                shownActions.Add(Aindex);
                int i = shownActions.Count - 1;
                float x = _node.x * (float)(i % amount);
                x += (float)(_node.x * 0.2);
                float y = _node.y * (float)(i / amount);
                y += (float)(_node.y * 0.2);
                Rect toAdd = new Rect(x, y, _node.x, _node.y);
                _actionPositions.Add(toAdd);
            }

            EditorGUILayout.BeginVertical();
            if (_showConditions)
            {
                NIndex = (NodeExtra)EditorGUILayout.EnumPopup("ConnectionType:", NIndex);
                if (GUILayout.Button("Add Condition"))
                {
                    var nToAdd = new SelfNode(shownActions[_startNode], (int)_actionPositions[_startNode].center.y);
                    var eToAdd = new SelfNode(shownActions[_endNode], (int)_actionPositions[_endNode].center.y);
                    var cToAdd = new Connection(Cindex, eToAdd, NIndex);
                    if (!_nodes.ContainsKey(nToAdd))
                    {
                        Debug.Log("Node Added");
                        _nodes.Add(nToAdd, new List<Connection>());
                    }
                    _nodes[nToAdd].Add(cToAdd);
                    LastAdded = nToAdd;
                    _handles.Add(_startPoint, _endPoint);
                    _lastAddedHandle = _startPoint;
                    _startPoint = Vector3.one;
                    _endPoint = Vector3.one;
                    _showConditions = false;
                }
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            GUILayout.BeginArea(_nodeField, _BTStyle);
            for (int i = 0; i < shownActions.Count; i++)
            {
                int node = shownActions[i];
                Rect rect = _actionPositions[i];
                GUI.Box(rect, actionOptions[node]);
            }
            Dragging();
            DrawingLines();
            GUILayout.EndArea();
            GUILayout.BeginArea(_endField);
            if (GUILayout.Button("Generate Tree"))
            {
                GenerateTree();
            }
            if (GUILayout.Button("Undo Connection") && LastAdded != null)
            {
                _nodes[LastAdded].RemoveAt(_nodes[LastAdded].Count - 1);
                _handles.Remove(_lastAddedHandle);
            }
            if (GUILayout.Button("Clear"))
            {
                _nodes.Clear();
                _handles.Clear();
                _actionPositions.Clear();
                shownActions.Clear();
            }
            GUILayout.EndArea();

        }
    }

    private void GenerateTree()
    {
        //Find First Node
        SelfNode node = new SelfNode(0, 0);
        int yToComp = 10000;
        foreach (var Snode in _nodes.Keys)
        {
            if (Snode.yValue <= yToComp)
            {
                yToComp = Snode.yValue;
                node = Snode;
            }
        }

        FinalAction = AddBranch(node/*, aCurrent*/);
        FinalAction();
    }

    private System.Action AddBranch(SelfNode start/*, System.Action result*/ )
    {
        System.Action result = () => { };
        var connections = _nodes[start];
        var connection = connections[0];
        var cond = _condList[connection.conditionIndex];
        var nextAction = connection.nextAction;
        var action = _actionList[nextAction.actionIndex];
        if (connections.Count == 1)
        {
            if (connection.nodeExtra == NodeExtra.Transition)
            {
                action = AddBranch(nextAction/*, result*/);
            }
            result = Script_BehaviorTreeFramework.Conditional(cond, action);
            return result;
        }
        else if (connections.Count == 2)
        {
            int ifTrue = -1;
            int ifFalse = -1;

            for (int i = 0; i < 2; i++)
            {
                connection = connections[i];
                if (connection.nodeExtra == NodeExtra.Selector)
                {
                    ifTrue = i;
                    ifFalse = (i + 1) % 2;
                    cond = _condList[connection.conditionIndex];
                }
            }
            if (ifTrue != -1) //Is Selector
            {
                connection = connections[ifTrue];
                action = _actionList[connection.nextAction.actionIndex];
                if (connection.nodeExtra == NodeExtra.Transition) action = AddBranch(connection.nextAction/*, result*/);

                connection = connections[ifFalse];
                var actionFalse = _actionList[connection.nextAction.actionIndex];
                if (connection.nodeExtra == NodeExtra.Transition) actionFalse = AddBranch(connection.nextAction/*, result*/);
                result = Script_BehaviorTreeFramework.Selector(cond, action, actionFalse);
                return result;
            }
        }

        System.Action[] sequence = new System.Action[connections.Count];
        for (int i = 0; i < connections.Count; i++)
        {
            connection = connections[i];
            action = _actionList[connection.nextAction.actionIndex];
            if (connection.nodeExtra == NodeExtra.Transition) action = AddBranch(connection.nextAction);
            sequence[i] = action;
        }
        result = Script_BehaviorTreeFramework.Sequencer(sequence);
        return result;
    }
    private void BaseSettings()
    {
        GUILayout.Label("Base Settings", EditorStyles.boldLabel);

        BTAsset = (MonoScript)EditorGUILayout.ObjectField("Behavior Tree Script", BTAsset, typeof(MonoScript), false);

        if (BTObj == null)
        {
            BTObj = GameObject.Find("BTObj");
            if (BTObj == null)
            {
                BTObj = new GameObject("BTObj");
            }
        }
        if (BTObj != null && BTAsset != null && _BT == null)
        {
            _BT = BTObj.GetComponent<Script_BehaviorTreeFramework>();
            if (_BT == null)
            {
                BTObj.AddComponent(BTAsset.GetClass());
                _BT = BTObj.GetComponent<Script_BehaviorTreeFramework>();
            }
        }

        _bColor = EditorGUILayout.ColorField(new GUIContent("Background Color"), _bColor, false, true, false, null);
        if (GUILayout.Button("Change Background Color") || _BTStyle == null)
        {
            _BTStyle = new GUIStyle();
            _BTStyle.normal.background = MakeBackground((int)_nodeField.width,
                (int)_nodeField.height, _bColor);
        }
    }

    private void Dragging()
    {
        var mousePos = Event.current.mousePosition;
        var button = Event.current.button;
        if (Event.current.type == EventType.MouseUp
            && _nodeField.Contains(mousePos + _nodeField.position))
        {
            if (button == 0 && _dragging)
            {
                _dragging = false;
                _offset.center = mousePos;
                _actionPositions[_selectedAction] = _offset;
            }
            else if (button == 1 && !_isStart)
            {
                for (int i = 0; i < _actionPositions.Count; i++)
                {
                    if (_actionPositions[i].Contains(mousePos))
                    {
                        _endPoint = mousePos;
                        _endNode = i;
                        _showConditions = true;
                        _isStart = true;
                    }
                }
            }
            else if (button == 1 && _isStart && !_showConditions)
            {
                for (int i = 0; i < _actionPositions.Count; i++)
                {
                    if (_actionPositions[i].Contains(mousePos))
                    {
                        _isStart = false;
                        _startPoint = mousePos;
                        _startNode = i;
                    }
                }
            }
            Event.current.Use();
        }
        else if (Event.current.type == EventType.MouseDown
                && _nodeField.Contains(mousePos + _nodeField.position)
                && button == 0)
        {
            for (int i = 0; i < _actionPositions.Count; i++)
            {
                if (_actionPositions[i].Contains(mousePos))
                {
                    _selectedAction = i;
                    Event.current.Use();
                }
            }
        }
        else if (Event.current.type == EventType.MouseDrag
                    && _nodeField.Contains(mousePos + _nodeField.position)
                    && button == 0)
        {
            for (int i = _actionPositions.Count - 1; i >= 0; i--)
            {
                if (_actionPositions[i].Contains(mousePos))
                {
                    _offset.center = mousePos;
                    _dragging = true;
                    Event.current.Use();
                }
            }
        }
    }

    private void DrawingLines()
    {
        Handles.BeginGUI();
        Handles.color = Color.red;
        foreach (var line in _handles)
        {
            Handles.DrawLine(line.Key, line.Value);
        }
        Handles.EndGUI();
    }

    private Texture2D MakeBackground(int width, int height, Color col)
    {
        Color[] tex = new Color[width * height];
        for (int i = 0; i < tex.Length; i++)
        {
            tex[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(tex);
        result.Apply();
        return result;
    }
}

#endif
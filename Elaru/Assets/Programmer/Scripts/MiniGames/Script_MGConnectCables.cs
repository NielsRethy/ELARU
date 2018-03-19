using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class Script_MGConnectCables : MonoBehaviour
{
    //Nodes that need to be connected by puzzle
    [SerializeField] private Script_MGCableNode _startNode = null;
    [SerializeField] private Script_MGCableNode _endNode = null;

    //Puzzle "field" size
    [SerializeField] private int _width = 0;
    [SerializeField] private int _height = 0;

    //List of nodes and plugs to take into account
    [SerializeField]
    private List<Script_MGCableNode> _nodeList = new List<Script_MGCableNode>();
    [SerializeField]
    private List<GameObject> _plugs = new List<GameObject>();

    private bool _completed = false;
    public UnityEvent OnComplete;

    //TODO: FIX TEMPORARY VISUALISATION
    private List<GameObject> lrPool = new List<GameObject>();

    private uint _uniqueId = 0;

    [SerializeField]
    private Material _lineMaterial = null;

    private void Start()
    {
        //Verify game validity
        if (_width * _height != _nodeList.Count)
        {
            Debug.LogError("Width and height are different than total node list size in Connect Cable minigame: " + name);
        }

        //Link nodes to game
        _nodeList.ForEach(x => x.SetParentGame(this));

        GenerateID();

        Script_MinigameManager.Instance.RegisterPuzzle(this);

        //If start and end plugs are in dock, lock them and destroy their pick up script so you can't pull them out
        if (_startNode != null)
        {
            var plug = _startNode.GetActivePlug();
            if (plug != null)
            {
                plug.tag = "Untagged";
                Destroy(plug.GetComponent<Script_PickUpObject>());
            }
        }
        if (_endNode != null)
        {
            var plug = _endNode.GetActivePlug();
            if (plug != null)
            {
                plug.tag = "Untagged";
                Destroy(plug.GetComponent<Script_PickUpObject>());
            }
        }
    }

    private Script_MGCableNode[] GetNeighbours(Script_MGCableNode start)
    {
        var ind = _nodeList.IndexOf(start);
        return GetNeighbours(ind);
    }

    private Script_MGCableNode[] GetNeighbours(int startIndex)
    {
        List<Script_MGCableNode> neighbours = new List<Script_MGCableNode>(4);
        var row = startIndex / _height;
        var col = startIndex % _width;

        //Left
        if (col > 0)
        {
            neighbours.Add(_nodeList[startIndex - 1]);
            //Up left diagonal
            if (row > 0)
            {
                neighbours.Add(_nodeList[startIndex - _width - 1]);
            }
            //Down left diagonal
            if (row < _height - 1)
            {
                neighbours.Add(_nodeList[startIndex + _width - 1]);
            }
        }

        //Right
        if (col < _width - 1)
        {
            neighbours.Add(_nodeList[startIndex + 1]);
            //Up right diagonal
            if (row > 0)
            {
                neighbours.Add(_nodeList[startIndex - _width + 1]);
            }
            //Down right diagonal
            if (row < _height - 1)
            {
                neighbours.Add(_nodeList[startIndex + _width + 1]);
            }
        }

        //Up
        if (row > 0)
            neighbours.Add(_nodeList[startIndex - _width]);

        //Down
        if (row < _height - 1)
            neighbours.Add(_nodeList[startIndex + _width]);

        return neighbours.ToArray();
    }

    private Script_MGCableNode[] GetUsedNeighboors(Script_MGCableNode node)
    {
        if (!_nodeList.Contains(node))
        {
            Debug.LogError("Requesting neighboors from node that is not in node list");
            return null;
        }

        //Check neighbours that are in use
        var n = GetNeighbours(node);
        return n.Where(x => x.IsInUse()).ToArray();
    }

    public void UpdateConnections(Script_MGCableNode node)
    {
        //TODO: change temporary visualisation
        //Destroy previous lines
        lrPool.ForEach(Destroy);
        lrPool.Clear();

        //Create new lines
        foreach (var nd in _nodeList)
        {
            //Only check used nodes
            if (!nd.IsInUse())
                continue;

            //Get active neighbours and draw lines to them
            var n = GetUsedNeighboors(nd);
            foreach (var unb in n)
            {
                //Create object with line
                var o = new GameObject("Line");
                var lr = o.AddComponent<LineRenderer>();
                lr.sharedMaterial = _lineMaterial;
                lr.positionCount = 2;
                lr.startWidth = .1f;
                lr.endWidth = .1f;
                lr.SetPosition(0, nd.transform.position - .1f * transform.forward);
                lr.SetPosition(1, unb.transform.position - .1f * transform.forward);
                lrPool.Add(o);
            }
        }

        if (_completed)
            return;

        //No need to check completion if startnode is not active yet
        if (!_startNode.IsInUse())
            return;

        //Check if complete
        List<Script_MGCableNode> checkPath = new List<Script_MGCableNode> { _startNode };
        CheckNeighboorPath(checkPath);
        if (checkPath.Contains(_startNode) && checkPath.Contains(_endNode))
            Complete();
    }

    void CheckNeighboorPath(List<Script_MGCableNode> path)
    {
        //Add used neighbours to list
        for (int i = 0; i < path.Count; ++i)
        {
            var n = GetUsedNeighboors(path[i]);
            foreach (var o in n)
            {
                if (!path.Contains(o))
                {
                    //Save neighbours of neighbours
                    path.Add(o);
                    CheckNeighboorPath(path);
                }
            }
        }
    }

    public void Complete()
    {
        _completed = true;

        //Activate event on completion
        if (OnComplete != null)
            OnComplete.Invoke();

        //Disable colliders so player can't break solution by pulling plugs
        foreach (var plug in _plugs)
            Destroy(plug.GetComponent<BoxCollider>());

        //Register puzzle as completed
        Script_MinigameManager.Instance.SetCompleted(_uniqueId);
    }

    public List<GameObject> GetPlugList()
    {
        return _plugs;
    }

    public void LoadComplete()
    {
        //Used to load from save file
        Complete();
        //TODO: possibly implement plugs being in correct position on load if needed?
    }

    private void GenerateID()
    {
        //Hash name and position
        var objectNameHash = name.GetHashCode();
        var pos = transform.position;
        var posHash = pos.x * pos.y / (1 / (pos.z + .5f));

        //Hash based on node position
        foreach (var node in _nodeList)
        {
            pos = node.transform.position;
            posHash += pos.x * pos.y / (1 / (pos.z + .5f));
        }

        //Create ID
        _uniqueId = (uint)(objectNameHash + posHash);
    }

    public uint GetID()
    {
        if (_uniqueId == 0)
            GenerateID();
        return _uniqueId;
    }
}

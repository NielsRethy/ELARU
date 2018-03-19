using UnityEngine;

public class Script_MGCableNode : MonoBehaviour
{
    //Game that this plug is connected to
    private Script_MGConnectCables _parentGame;
    //Plug that is in node
    private GameObject _activePlug;
    private bool _docked = false;

    //Used to "break" this node
    public bool Usable = true;
    private string PlugDockSound = "PickupPressed";

    public void SetParentGame(Script_MGConnectCables g)
    {
        //Save connected game
        _parentGame = g;

        //Check if plugs already in node at start of mini game
        var ob = Physics.OverlapBox(transform.position, GetComponent<BoxCollider>().size * transform.localScale.x, transform.rotation);
        foreach (var x in ob)
        {
            if (_parentGame.GetPlugList().Contains(x.gameObject) && Usable)
            {
                _activePlug = x.gameObject;
                DockInNode(_activePlug);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Plug enters node, which is free
        if (Usable && _activePlug == null && _parentGame.GetPlugList().Contains(other.gameObject))
        {
            _activePlug = other.gameObject;
            //Action to dock plug when plug is released
            _activePlug.GetComponent<Script_PickUpObject>().OnRelease += DockInNode;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //Saved plug exits node
        if (_activePlug != null && other.gameObject == _activePlug)
        {
            //Unsubscribe from action for safety
            _activePlug.GetComponent<Script_PickUpObject>().OnRelease -= DockInNode;

            _activePlug = null;

            _parentGame.UpdateConnections(this);
        }
    }

    private void DockInNode(GameObject o)
    {
        //Set plug on dock nodde position
        _docked = true;
        _activePlug.transform.position = transform.position;
        _activePlug.transform.rotation = transform.rotation;

        //Update visual connection between used plugs
        _parentGame.UpdateConnections(this);

        //Subscribe on object grab to let plug go
        _activePlug.GetComponent<Script_PickUpObject>().OnGrab += ReleaseFromDock;

        //Make plug immoveable
        _activePlug.GetComponent<Rigidbody>().isKinematic = true;

        //Make a sound
        if (Time.timeSinceLevelLoad > 0.25f)
            Script_AudioManager.Instance.PlaySFX(PlugDockSound, _activePlug.transform.position);
    }

    private void ReleaseFromDock(GameObject o)
    {
        //Check active connections
        _docked = false;
        _parentGame.UpdateConnections(this);

        //Make plug moveable again
        _activePlug.GetComponent<Rigidbody>().isKinematic = false;

        //Unsubscribe from object grab
        _activePlug.GetComponent<Script_PickUpObject>().OnGrab -= ReleaseFromDock;
    }

    public bool IsInUse()
    {
        return _activePlug != null && Usable && _docked;
    }

    public GameObject GetActivePlug()
    {
        return _activePlug;
    }
}

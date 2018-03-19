using UnityEngine;

public class Script_ButtonMovement : MonoBehaviour
{
    [SerializeField]
    private Script_ButtonTrigger _btnTrg = null;

    private bool _handIsOnButton = false;
    private Vector3 _startPos = Vector3.zero;


    private void Start()
    {
        //Set variables
        _startPos = transform.position;
    }

    private void Update()
    {
        //Limit button to max startheight
        if (transform.position.y > _startPos.y)
            transform.position = _startPos;

        if (!_handIsOnButton)
        {
            //Reset if too low
            if (transform.position.y < _startPos.y)
                transform.position = Vector3.MoveTowards(transform.position, _startPos, Time.deltaTime);
        }
        else
        {
            //Deactivate collider if pos too low
            if (transform.position.y < _startPos.y - 0.1f)
                transform.GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.tag.Equals("Hand"))
            return;

        //Player stared pressing the button
        if (other.transform.position.y > transform.position.y)
        {
            _handIsOnButton = true;
            _btnTrg.ControlHandSide = other.transform.parent.GetComponent<Script_PickUp>().Hand;
        }

        //Make button able to be pressed
        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Collider>().enabled = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.tag.Equals("Hand"))
            return;

        //Hand has left button
        _handIsOnButton = false;
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().enabled = false;
    }
}

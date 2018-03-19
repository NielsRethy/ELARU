using UnityEngine;

public class Script_ButtonTrigger : MonoBehaviour
{
    private bool _triggered = false;
    [SerializeField]
    private int _buttonNr = 1;

    //Attached upgrade station
    [SerializeField] Script_UpgradeStation _station = null;
    
    public HandSide ControlHandSide = HandSide.None;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Button") && !_triggered)
        {
            _triggered = true;

            //Reset triggered state after .5 seconds
            Invoke("DisableTriggeredState", .5f);

            //Pass buttonpress to attached upgrade station
            _station.Pressed(_buttonNr);

            //Vibrate controller that pressed button
            Script_TactileFeedback.Instance.SendLongVibration(.2f, 1, ControlHandSide);
        }
    }

    private void DisableTriggeredState()
    {
        _triggered = false;
    }
}

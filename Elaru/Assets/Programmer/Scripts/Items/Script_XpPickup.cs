using UnityEngine;

public class Script_XpPickup : MonoBehaviour
{
    [SerializeField]
    private int XpAmount = 100;

    //Manager to send vibrations to the controllers
    private Script_TactileFeedback _feedbackMng = null;
    //Manager to get the controllers
    private Script_LocomotionBase _locoInstance = null;

    private void Start()
    {
        //Cache managers
        _feedbackMng = Script_TactileFeedback.Instance;
        _locoInstance = Script_LocomotionBase.Instance;
    }

    private void OnTriggerStay(Collider other)
    {
        //Check for player hand
        var spu = other.GetComponent<Script_PickUp>();
        if (spu == null)
            return;

        var hand = spu.Hand;

        //Send vibration
        _feedbackMng.SendShortVib(2000, hand);

        //Gain xp on grab
        if (_locoInstance.GetPressDown(_locoInstance.GripButton, hand))
        {
            Script_PlayerInformation.Instance.GainXP(XpAmount);
            Debug.Log("Gained: " + XpAmount + " Amount of xp");
            gameObject.SetActive(false);
        }
    }
}

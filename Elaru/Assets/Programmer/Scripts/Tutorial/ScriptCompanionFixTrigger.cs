using UnityEngine;

public class ScriptCompanionFixTrigger : MonoBehaviour
{
    private Script_CompanionTutorial _base = null;

    private bool _leftFixed = false;
    private bool _rightFixed = false;

    //Repaired versions of arms
    [SerializeField] private GameObject _leftArm = null;
    [SerializeField] private GameObject _rightArm = null;
    [SerializeField] private GameObject _leftArmHologram = null;
    [SerializeField] private GameObject _rightArmHologram = null;

    private const string ArmAttachSound = "CompanionRepair";
    private const string FixedSound = "CompanionWakeUp";

    private const string LeftName = "LeftArm";
    private const string RightName = "RightArm";

    private void Start()
    {
        //Cache needed components
        _base = transform.parent.GetComponent<Script_CompanionTutorial>();
    }

    private void OnTriggerStay(Collider other)
    {
        //Return if no relevant object entered trigger
        if (other.tag != "PickUp" && other.name != LeftName && other.name != RightName)
            return;

        if (other.name == LeftName && !_leftFixed)
            _leftFixed = DistanceCheck(true, other);

        if (other.name == RightName && !_rightFixed)
            _rightFixed = DistanceCheck(false, other);

        //Enable arms if fixed
        if (_leftFixed)
        {
            _leftArmHologram.SetActive(false);
            _leftArm.SetActive(true);
        }
        if (_rightFixed)
        {
            _rightArmHologram.SetActive(false);
            _rightArm.SetActive(true);
        }

        if (_leftFixed && _rightFixed)
        {
            //_base.gameObject.GetComponent<Script_SplineInterpolator>().Pauze = true;
            _base.IsFixed = true;
            // Play a sound
            Script_AudioManager.Instance.PlaySFX(FixedSound, other.transform.position);
        }
    }

    private bool DistanceCheck(bool left, Collider other)
    {
        //Check if the side the object was entered corresponds to the arm side
        if ((left && transform.position.x > other.transform.position.x) ||
            (!left && transform.position.x < other.transform.position.x))
        {
            other.gameObject.SetActive(false);
            DropItems();
            // Play a sound
            Script_AudioManager.Instance.PlaySFX(ArmAttachSound, other.transform.position);
            return true;
        }

        return false;
    }

    private void DropItems()
    {
        var loco = Script_LocomotionBase.Instance;
        //Drop items out of both hands
        // Left hand
        if (loco.LeftControllerTrObj != null)
            loco.GetPickUpFromHand(HandSide.Left).Drop();
        // Right hand
        if (loco.RightControllerTrObj != null)
            loco.GetPickUpFromHand(HandSide.Right).Drop();
    }
}

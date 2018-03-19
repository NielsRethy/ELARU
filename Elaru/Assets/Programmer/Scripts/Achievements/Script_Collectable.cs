using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Script_PickUpObject))]
public class Script_Collectable : MonoBehaviour
{
    public ItemType Type;
    private Script_PickUp _leftHand = null;
    private Script_PickUp _rightHand = null;

    private HandSide _hand = HandSide.None;

    public bool Activated = false;
    void Start()
    {
        //Update achievement when player picks up object
        var pickUpScript = GetComponent<Script_PickUpObject>();
        if (pickUpScript != null)
        {
            pickUpScript.OnGrab += TurnoffRotationFloat;
            pickUpScript.OnGrab += UpdateAchievement;
        }

        //Get hands
        _leftHand = Script_LocomotionBase.Instance.LeftControllerTrObj.GetComponent<Script_PickUp>();
        _rightHand = Script_LocomotionBase.Instance.RightControllerTrObj.GetComponent<Script_PickUp>();
    }

    private void TurnoffRotationFloat(GameObject o)
    {
        //check if has float rotation script
        var cmp = o.GetComponent<Script_FloatAndRotate>();

        //turn off script
        if (cmp != null) Destroy(o.GetComponent<Script_FloatAndRotate>());
        StartCoroutine(GetInfoDelay(o));
    }

    private IEnumerator GetInfoDelay(GameObject o)
    {
        yield return new WaitForEndOfFrame();
        var puo = o.GetComponent<Script_PickUpObject>();
        var obj = puo.ControlHandSide;
        _hand = o.GetComponent<Script_PickUpObject>().ControlHandSide;
    }

    void UpdateAchievement(GameObject o)
    {
        if (Activated)
            return;
        Script_TrophyManager.Instance.RegisterPickup(Type);

        //Destroy object and check collection achievements
        Invoke("DestroyObject", 2.0f);
        Script_AchievementManager.Instance.UpdateCollectionAchievements(o);
        Activated = true;
    }

    private void DestroyObject()
    {
        GetComponent<Script_PickUpObject>().ImpossibleToDrop = false;
        switch (_hand)
        {
            case HandSide.Left:
                _leftHand.Drop();
                break;
            case HandSide.Right:
                _rightHand.Drop();
                break;
        }
        Destroy(this.gameObject);
    }
}

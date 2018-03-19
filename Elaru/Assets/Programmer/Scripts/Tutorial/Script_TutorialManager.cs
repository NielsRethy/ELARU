using UnityEngine;
using UnityEngine.Events;

public enum TutorialType
{
    None,
    Start,
    QuestObj,
    Finish
}

public class Script_TutorialManager : MonoBehaviour
{
    //Use this event to check if the quest needs to start
    public UnityEvent StartOfQuest;
    //Use this event to check if the quest is completed
    public UnityEvent QuestObjective;
    //Use this event to do whatever needs to happen after the quest
    public UnityEvent FinishedQuest;

    public TutorialType Type = TutorialType.None;

    /// <summary>
    /// Call this function in each of the events to go to the next step.
    /// </summary>
    public void SetType(TutorialType type)
    {
        //Invoke current stage actions
        switch (type)
        {
            case TutorialType.Start:
                StartOfQuest.Invoke();
                break;
            case TutorialType.QuestObj:
                QuestObjective.Invoke();
                break;
            case TutorialType.Finish:
                FinishedQuest.Invoke();
                break;
        }

        Type = type;
    }
}

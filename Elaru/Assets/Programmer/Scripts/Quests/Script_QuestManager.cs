using System.Collections.Generic;
using System.Linq;

public class Script_QuestManager : Script_Singleton<Script_QuestManager>
{
    //List of all main quests
    public List<Script_QuestLinker> QuestLinks { get; set; }
    
    public void RegisterLink(Script_QuestLinker link)
    {
        //Register link if not in list yet
        if (QuestLinks == null)
            QuestLinks = new List<Script_QuestLinker>();
        if (!QuestLinks.Contains(link))
            QuestLinks.Add(link);
    }

    public float GetCompletion()
    {
        //Get percentage of completed main quests
        var completed = QuestLinks.Count(x => x.IsLinkCompleted);
        return completed / (float)QuestLinks.Count;
    }

    public List<QuestLinkerSaveData> GetQuestLinkerSaveDatas()
    {
        //Get save data from all main quests
        List<QuestLinkerSaveData> r = new List<QuestLinkerSaveData>();
        QuestLinks.ForEach(x => r.Add(x.GetSaveData()));
        return r;
    }

    public void LoadQuestLinksFromSaveDatas(List<QuestLinkerSaveData> lsd)
    {
        foreach (var sd in lsd)
        {
            var ql = QuestLinks.FirstOrDefault(x => x.GetLinkID() == sd.ID);
            if (ql != null)
            {
                ql.LoadFromSaveData(sd);
            }
        }
    }
}

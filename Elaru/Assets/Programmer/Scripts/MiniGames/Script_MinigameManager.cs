using System.Collections.Generic;
using System.Linq;

public class Script_MinigameManager : Script_Singleton<Script_MinigameManager>
{
    //Registered puzzles
    private List<Script_MGLeverManager> _leverPuzzles = new List<Script_MGLeverManager>();
    private List<Script_MGConnectCables> _electricPuzzles = new List<Script_MGConnectCables>();

    //unqique ids of saved/completed puzzles
    public List<uint> PuzzleIDs { get; set; }

    public virtual void Awake()
    {
        //Load into puzzles
        PuzzleIDs = new List<uint>();
    }

    public void Load()
    {
        Invoke("LoadPuzzles", 0.5f);
    }

    private void LoadPuzzles()
    {
        foreach (uint id in PuzzleIDs)
        {
            //Load puzzle that matches ID
            var ep = _electricPuzzles.FirstOrDefault(x => x.GetID() == id);
            if (ep != null)
            {
                ep.LoadComplete();
                continue;
            }

            var lp = _leverPuzzles.FirstOrDefault(x => x.GetID() == id);
            if (lp != null)
                lp.Complete();
        }
    }

    public void RegisterPuzzle(Script_MGConnectCables cablesScript)
    {
        //Adds the puzzle to manager
        _electricPuzzles.Add(cablesScript);
    }

    public void RegisterPuzzle(Script_MGLeverManager leverObj)
    {
        //Adds the puzzle to manager
        _leverPuzzles.Add(leverObj);
    }

    public void SetCompleted(uint id)
    {
        //Adds to finished list for savefile
        if (!PuzzleIDs.Contains(id))
            PuzzleIDs.Add(id);
    }
}

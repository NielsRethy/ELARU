using System;
using UnityEngine;

public abstract class Script_Lever : MonoBehaviour
{
    public abstract void TopTrigger();
    public abstract void BotTrigger();

    //Actions to call on open / close
    public Action OnOpen;
    public Action OnClose;

    public bool IsTop = false;

    public virtual void CallTrigger(bool isTopTrig)
    {
        if (isTopTrig)
            TopTrigger();
        else
            BotTrigger();
    }
}

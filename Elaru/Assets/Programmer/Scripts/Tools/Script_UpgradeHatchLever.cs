public class Script_UpgradeHatchLever : Script_Lever
{
    public override void BotTrigger()
    {
        IsTop = false;
        if (OnOpen != null)
            OnOpen.Invoke();
    }

    public override void TopTrigger()
    {
        IsTop = true;
        if (OnClose != null)
            OnClose.Invoke();
    }
}

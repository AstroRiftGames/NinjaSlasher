using UnityEngine;

public class CreditsModal : UIModalBase
{
    protected override void OnShown()
    {
        MusicEvents.OnEnterCredits?.Invoke();
    }
    
    protected override void OnHidden()
    {
        MusicEvents.OnEnterLevelSelection?.Invoke();
    }
}
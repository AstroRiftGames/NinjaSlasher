using UnityEngine;

public class DailyRewardModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();
    }

    protected override void OnHideAnimationCompleted()
    {
        UIEvents.RaiseDailyRewardModalClosed();
    }
}

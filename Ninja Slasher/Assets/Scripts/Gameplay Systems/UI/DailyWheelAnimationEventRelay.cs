using UnityEngine;

public class DailyWheelAnimationEventRelay : MonoBehaviour
{
    private DailyWheelUI _dailyWheelUI;

    private void Awake()
    {
        ResolveDailyWheelUI();
    }

    public void AnimationEvent_PlayBallBounce()
    {
        if (ResolveDailyWheelUI())
        {
            _dailyWheelUI.AnimationEvent_PlayBallBounce();
        }
    }

    private bool ResolveDailyWheelUI()
    {
        if (_dailyWheelUI != null)
        {
            return true;
        }

        _dailyWheelUI = GetComponentInParent<DailyWheelUI>();
        return _dailyWheelUI != null;
    }
}

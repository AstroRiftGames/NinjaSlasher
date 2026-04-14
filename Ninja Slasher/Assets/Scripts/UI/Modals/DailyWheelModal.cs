using UnityEngine;

public class DailyWheelModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.3f;
    [SerializeField] private DailyWheelUI _dailyWheelUI;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_dailyWheelUI == null)
        {
            _dailyWheelUI = GetComponentInChildren<DailyWheelUI>(true);
        }
    }

    protected override void OnShown()
    {
        base.OnShown();
        _dailyWheelUI?.HandleModalShown();
    }

    public void CloseRewardPopup()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.CloseRewardPopup();
            return;
        }

        Hide();
    }

    public void CloseNoSpinsPopup()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.HideNoSpinsPopup();
            return;
        }

        Hide();
    }

    public void BuyNoSpinsPopupOffer()
    {
        _dailyWheelUI?.TryPurchaseNoSpinsOffer();
    }

    public void CloseModal()
    {
        if (_dailyWheelUI != null && !_dailyWheelUI.CanCloseModal())
        {
            return;
        }

        _dailyWheelUI?.CloseRewardPopup();
        Hide();
    }
}

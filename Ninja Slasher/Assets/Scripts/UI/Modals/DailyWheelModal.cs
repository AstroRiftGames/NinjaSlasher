using UnityEngine;

public class DailyWheelModal : UIModalBase
{
    [Header("Animation")]
    [SerializeField] private float _closeAnimationDuration = 0.3f;
    [SerializeField] private DailyWheelUI _dailyWheelUI;

    protected override void Awake()
    {
        base.Awake();

        if (_dailyWheelUI == null)
        {
            _dailyWheelUI = GetComponentInChildren<DailyWheelUI>(true);
        }
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true;
        }

        NotifyPanelShown();
        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        SetPanelInputEnabled(false);

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false;
        }

        OnHidden();

        StartCoroutine(DelayedHide());
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

    private System.Collections.IEnumerator DelayedHide()
    {
        yield return new WaitForSecondsRealtime(_closeAnimationDuration);
        gameObject.SetActive(false);
    }
}

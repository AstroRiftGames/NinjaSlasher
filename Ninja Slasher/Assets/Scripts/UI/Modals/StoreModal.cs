using System.Collections;
using UnityEngine;

public class StoreModal : UIModalBase
{
    [Header("Animation")]
    [SerializeField] private Animator _panelAnimator;
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private StorePurchaseConfirmationPopUp _purchaseConfirmationPanel;

    protected override void Awake()
    {
        base.Awake();

        if (_panelAnimator == null)
        {
            _panelAnimator = GetComponentInChildren<Animator>();
        }

        if (_purchaseConfirmationPanel == null)
        {
            _purchaseConfirmationPanel = GetComponentInChildren<StorePurchaseConfirmationPopUp>(true);
        }

        if (_purchaseConfirmationPanel == null)
        {
            Debug.LogWarning("[StoreModal] StorePurchaseConfirmationPopUp instance is not assigned or not present under the StoreModal hierarchy.");
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

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Open");
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

        _purchaseConfirmationPanel?.HideImmediate();

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Close");
        }

        OnHidden();

        StartCoroutine(DelayedHide());
    }

    private IEnumerator DelayedHide()
    {
        yield return new WaitForSecondsRealtime(_closeAnimationDuration);
        gameObject.SetActive(false);
    }

    protected override void OnShown()
    {
        base.OnShown();
        AnalyticsManager.Instance?.RecordShopOpened();
    }

    public void ShowPurchaseConfirmation(string productId, RectTransform feedbackOrigin)
    {
        _purchaseConfirmationPanel?.ShowConfirmation(productId, feedbackOrigin);
    }
}

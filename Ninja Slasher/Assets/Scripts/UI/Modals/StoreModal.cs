using UnityEngine;

public class StoreModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private StorePurchaseConfirmationPopUp _purchaseConfirmationPanel;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        if (_purchaseConfirmationPanel == null)
            _purchaseConfirmationPanel = GetComponentInChildren<StorePurchaseConfirmationPopUp>(true);

        if (_purchaseConfirmationPanel == null)
        {
            Debug.LogWarning("[StoreModal] StorePurchaseConfirmationPopUp instance is not assigned or not present under the StoreModal hierarchy.");
        }
    }

    protected override void OnHidden()
    {
        _purchaseConfirmationPanel?.HideImmediate();
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

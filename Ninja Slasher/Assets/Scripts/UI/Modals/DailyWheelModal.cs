using UnityEngine;

public class DailyWheelModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.3f;
    [SerializeField] private DailyWheelUI _dailyWheelUI;
    [SerializeField] private RectTransform _contentRoot;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        if (_contentRoot == null)
        {
            Transform contentTransform = transform.Find("DailyWheelPanel");
            if (contentTransform != null)
                _contentRoot = contentTransform as RectTransform;
        }

        base.Awake();

        if (_contentRoot != null)
        {
            _animatedContentTransform = _contentRoot;
            _animatedContentCanvasGroup = _contentRoot.GetComponent<CanvasGroup>();

            if (_animatedContentCanvasGroup == null)
                _animatedContentCanvasGroup = _contentRoot.gameObject.AddComponent<CanvasGroup>();
        }

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

    protected override void OnHidden()
    {
        base.OnHidden();
        UIEvents.RaiseDailyWheelModalClosed();
    }

    public void CloseRewardPopup()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.CloseRewardPopup();
            return;
        }

        RequestCloseModal();
    }

    public void CloseNoSpinsPopup()
    {
        if (_dailyWheelUI != null)
        {
            _dailyWheelUI.HideNoSpinsPopup();
            return;
        }

        RequestCloseModal();
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
        RequestCloseModal();
    }

    private void RequestCloseModal()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseModal(this);
            return;
        }

        Hide();
    }

    protected override void RequestCloseFromOutsideClick()
    {
        if (_dailyWheelUI != null && !_dailyWheelUI.CanCloseModal())
        {
            return;
        }

        base.RequestCloseFromOutsideClick();
    }
}

public class DailyWheelLotteryPopup : UIPopupBase
{
    [SerializeField] private GameObject _rewardInfoSection;
    [SerializeField] private GameObject _noSpinsInfoSection;
    [SerializeField] private GameObject _buyButtonObject;
    [SerializeField] private GameObject _closeButtonObject;
    private bool _clearSectionsOnHide;

    protected override void Awake()
    {
        base.Awake();

        if (_canvasGroup != null)
            _canvasGroup.ignoreParentGroups = true;
    }

    public void Initialize(GameObject rewardInfoSection, GameObject noSpinsInfoSection, GameObject buyButtonObject, GameObject closeButtonObject)
    {
        _rewardInfoSection = rewardInfoSection;
        _noSpinsInfoSection = noSpinsInfoSection;
        _buyButtonObject = buyButtonObject;
        _closeButtonObject = closeButtonObject;

        ApplySectionState(showRewardInfo: false, showNoSpinsInfo: false, showBuyButton: false, showCloseButton: false);
    }

    public void ShowSection(bool showRewardInfo, bool showNoSpinsInfo, bool showBuyButton)
    {
        ApplySectionState(showRewardInfo, showNoSpinsInfo, showBuyButton, showCloseButton: showRewardInfo || showNoSpinsInfo);

        if (_isVisible)
            return;

        Show();
    }

    public override void Hide()
    {
        if (!_isVisible)
        {
            ApplySectionState(showRewardInfo: false, showNoSpinsInfo: false, showBuyButton: false, showCloseButton: false);
            return;
        }

        _clearSectionsOnHide = true;
        base.Hide();
    }

    public override void HideImmediate()
    {
        _clearSectionsOnHide = false;
        ApplySectionState(showRewardInfo: false, showNoSpinsInfo: false, showBuyButton: false, showCloseButton: false);
        base.HideImmediate();
    }

    protected override void OnHidden()
    {
        base.OnHidden();

        if (_clearSectionsOnHide)
        {
            _clearSectionsOnHide = false;
            ApplySectionState(showRewardInfo: false, showNoSpinsInfo: false, showBuyButton: false, showCloseButton: false);
        }
    }

    private void ApplySectionState(bool showRewardInfo, bool showNoSpinsInfo, bool showBuyButton, bool showCloseButton)
    {
        if (_rewardInfoSection != null)
            _rewardInfoSection.SetActive(showRewardInfo);

        if (_noSpinsInfoSection != null)
            _noSpinsInfoSection.SetActive(showNoSpinsInfo);

        if (_buyButtonObject != null)
            _buyButtonObject.SetActive(showBuyButton);

        if (_closeButtonObject != null)
            _closeButtonObject.SetActive(showCloseButton);
    }
}

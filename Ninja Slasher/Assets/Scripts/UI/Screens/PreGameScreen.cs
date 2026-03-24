using UnityEngine;

public class PreGameScreen : UIScreenBase
{
    [Header("Animation")]
    [SerializeField] private Animator _panelAnimator;
    [SerializeField] private float _closeAnimationDuration = 0.4f;

    [Header("Managers")]
    [SerializeField] private PreGameUIManager _preGameUIManager;
    [SerializeField] private ButtonManager _buttonManager;

    protected override void Awake()
    {
        base.Awake();

        if (_panelAnimator == null)
        {
            _panelAnimator = GetComponentInChildren<Animator>();
        }

        if (_preGameUIManager == null)
        {
            _preGameUIManager = UIManager.Instance?.GetComponent<PreGameUIManager>();
        }

        if (_buttonManager == null)
        {
            _buttonManager = UIManager.Instance?.GetComponent<ButtonManager>();
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

        if (_buttonManager != null)
        {
            _buttonManager.StopAllButtonAnimations();
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

        if (_preGameUIManager != null)
        {
            _preGameUIManager.StopAllAnimations();
            _preGameUIManager.HidePowerUpConfirmationImmediate();
        }

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Close");
        }

        OnHidden();

        StartCoroutine(DelayedHide());
    }

    private System.Collections.IEnumerator DelayedHide()
    {
        yield return new WaitForSecondsRealtime(_closeAnimationDuration);

        gameObject.SetActive(false);
    }
}

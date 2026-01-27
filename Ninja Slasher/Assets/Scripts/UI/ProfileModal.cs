using UnityEngine;

public class ProfileModal : UIModalBase
{
    [Header("Animation")]
    [SerializeField] private Animator _panelAnimator;
    [SerializeField] private float _closeAnimationDuration = 0.4f;

    [Header("Background")]
    [SerializeField] private GameObject _backgroundObject;

    protected override void Awake()
    {
        base.Awake();

        if (_panelAnimator == null)
        {
            _panelAnimator = GetComponentInChildren<Animator>();
        }

        if (_backgroundObject == null && _backgroundImage != null)
        {
            _backgroundObject = _backgroundImage.gameObject;
        }
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        if (_hasBackground && _backgroundObject != null)
        {
            _backgroundObject.SetActive(true);
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = true;
        }

        if (_panelAnimator != null)
        {
            _panelAnimator.SetTrigger("Open");
        }

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        if (_hasBackground && _backgroundImage != null)
        {
            _backgroundImage.raycastTarget = false;
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

        if (_hasBackground && _backgroundObject != null)
        {
            _backgroundObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    protected override void OnShown()
    {
        Debug.Log("[ProfileModal] Modal de perfil mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[ProfileModal] Modal de perfil ocultado");
    }
}
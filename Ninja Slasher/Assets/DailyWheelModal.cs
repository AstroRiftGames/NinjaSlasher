using UnityEngine;

public class DailyWheelModal : UIModalBase
{
    [Header("Animation")]
    [SerializeField] private float _closeAnimationDuration = 0.3f;

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

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

        OnHidden();

        StartCoroutine(DelayedHide());
    }

    private System.Collections.IEnumerator DelayedHide()
    {
        yield return new WaitForSecondsRealtime(_closeAnimationDuration);
        gameObject.SetActive(false);
    }

    protected override void OnShown()
    {
        Debug.Log("[DailyWheelModal] Modal de ruleta diaria mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[DailyWheelModal] Modal de ruleta diaria ocultado");
    }
}
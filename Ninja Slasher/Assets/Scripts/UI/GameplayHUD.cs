using UnityEngine;

public class GameplayHUD : UIPanel
{
    [Header("Overlay Visibility")]
    [SerializeField] private GameObject _pauseButtonObject;
    [SerializeField] private GameObject _timerIconObject;
    [SerializeField] private GameObject _timerTextObject;

    private bool _cachedPauseButtonActive;
    private bool _cachedTimerIconActive;
    private bool _cachedTimerTextActive;
    private bool _hasCachedOverlayVisibility;

    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        CacheOverlayReferences();
        _isVisible = gameObject.activeSelf;
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        OnHidden();

        gameObject.SetActive(false);
    }

    public void SetTopRightInfoVisible(bool visible)
    {
        CacheOverlayReferences();

        if (visible)
        {
            RestoreOverlayVisibility();
            return;
        }

        CacheCurrentOverlayVisibility();
        SetObjectActive(_pauseButtonObject, false);
        SetObjectActive(_timerIconObject, false);
        SetObjectActive(_timerTextObject, false);
    }

    private void CacheOverlayReferences()
    {
        if (_pauseButtonObject == null)
            _pauseButtonObject = FindChildObject("PauseButton");

        if (_timerIconObject == null)
            _timerIconObject = FindChildObject("TimerIcon");

        if (_timerTextObject == null)
            _timerTextObject = FindChildObject("TimerText");
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(includeInactive: true);
        foreach (Transform child in children)
        {
            if (child.name == objectName)
                return child.gameObject;
        }

        return null;
    }

    private void CacheCurrentOverlayVisibility()
    {
        _cachedPauseButtonActive = _pauseButtonObject != null && _pauseButtonObject.activeSelf;
        _cachedTimerIconActive = _timerIconObject != null && _timerIconObject.activeSelf;
        _cachedTimerTextActive = _timerTextObject != null && _timerTextObject.activeSelf;
        _hasCachedOverlayVisibility = true;
    }

    private void RestoreOverlayVisibility()
    {
        if (!_hasCachedOverlayVisibility)
            return;

        SetObjectActive(_pauseButtonObject, _cachedPauseButtonActive);
        SetObjectActive(_timerIconObject, _cachedTimerIconActive);
        SetObjectActive(_timerTextObject, _cachedTimerTextActive);
        _hasCachedOverlayVisibility = false;
    }

    private static void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}

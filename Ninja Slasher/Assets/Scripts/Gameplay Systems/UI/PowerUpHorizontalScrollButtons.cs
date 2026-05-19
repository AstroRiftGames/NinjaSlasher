using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PowerUpHorizontalScrollButtons : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;

    [Header("SETTINGS")]
    [SerializeField] [Min(0f)] private float _scrollStepNormalized;
    [SerializeField] [Min(0f)] private float _edgeTolerance = 0.001f;
    [SerializeField] private bool _hideButtonsWhenUnavailable = true;

    private Coroutine _refreshRoutine;

    private void Awake()
    {
        CacheReferencesIfNeeded();
    }

    private void OnEnable()
    {
        CacheReferencesIfNeeded();
        Subscribe();
        RefreshAfterLayout();
    }

    private void OnDisable()
    {
        Unsubscribe();
        StopRefreshRoutine();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    public void ResetToStart()
    {
        if (_scrollRect == null)
            return;

        _scrollRect.StopMovement();
        _scrollRect.horizontalNormalizedPosition = 0f;
        RefreshButtonsState();
    }

    public void RefreshAfterLayout()
    {
        StopRefreshRoutine();

        if (!gameObject.activeInHierarchy)
        {
            RefreshNow();
            return;
        }

        _refreshRoutine = StartCoroutine(RefreshAfterLayoutRoutine());
    }

    private void CacheReferencesIfNeeded()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponentInChildren<ScrollRect>(true);
    }

    private void Subscribe()
    {
        if (_leftButton != null)
        {
            _leftButton.onClick.RemoveListener(OnLeftButtonClicked);
            _leftButton.onClick.AddListener(OnLeftButtonClicked);
        }

        if (_rightButton != null)
        {
            _rightButton.onClick.RemoveListener(OnRightButtonClicked);
            _rightButton.onClick.AddListener(OnRightButtonClicked);
        }

        if (_scrollRect != null)
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }
    }

    private void Unsubscribe()
    {
        if (_leftButton != null)
            _leftButton.onClick.RemoveListener(OnLeftButtonClicked);

        if (_rightButton != null)
            _rightButton.onClick.RemoveListener(OnRightButtonClicked);

        if (_scrollRect != null)
            _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
    }

    private void OnLeftButtonClicked()
    {
        ScrollBy(-1f);
    }

    private void OnRightButtonClicked()
    {
        ScrollBy(1f);
    }

    private void OnScrollValueChanged(Vector2 _)
    {
        RefreshButtonsState();
    }

    private void ScrollBy(float direction)
    {
        if (_scrollRect == null || !HasHorizontalOverflow())
            return;

        float step = GetScrollStepNormalized();
        float targetPosition = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition + (step * direction));

        if (targetPosition <= _edgeTolerance)
            targetPosition = 0f;
        else if (targetPosition >= 1f - _edgeTolerance)
            targetPosition = 1f;

        _scrollRect.StopMovement();
        _scrollRect.horizontalNormalizedPosition = targetPosition;
        RefreshButtonsState();
    }

    private IEnumerator RefreshAfterLayoutRoutine()
    {
        // Wait a frame so the layout group and content fitter can resize the slots before measuring.
        yield return null;
        RefreshNow();
        _refreshRoutine = null;
    }

    private void RefreshNow()
    {
        if (_scrollRect == null)
        {
            SetButtonAvailability(_leftButton, false);
            SetButtonAvailability(_rightButton, false);
            return;
        }

        RectTransform viewport = GetViewport();
        RectTransform content = _scrollRect.content;
        if (viewport == null || content == null)
        {
            SetButtonAvailability(_leftButton, false);
            SetButtonAvailability(_rightButton, false);
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_scrollRect.transform);

        if (!HasHorizontalOverflow())
        {
            _scrollRect.StopMovement();
            _scrollRect.horizontalNormalizedPosition = 0f;
        }

        RefreshButtonsState();
    }

    private void RefreshButtonsState()
    {
        bool hasOverflow = HasHorizontalOverflow();
        if (!hasOverflow)
        {
            SetButtonAvailability(_leftButton, false);
            SetButtonAvailability(_rightButton, false);
            return;
        }

        float position = _scrollRect != null ? _scrollRect.horizontalNormalizedPosition : 0f;
        bool canScrollLeft = position > _edgeTolerance;
        bool canScrollRight = position < 1f - _edgeTolerance;

        SetButtonAvailability(_leftButton, canScrollLeft);
        SetButtonAvailability(_rightButton, canScrollRight);
    }

    private bool HasHorizontalOverflow()
    {
        if (_scrollRect == null || _scrollRect.content == null)
            return false;

        RectTransform viewport = GetViewport();
        if (viewport == null)
            return false;

        return _scrollRect.content.rect.width > viewport.rect.width + _edgeTolerance;
    }

    private RectTransform GetViewport()
    {
        if (_scrollRect == null)
            return null;

        if (_scrollRect.viewport != null)
            return _scrollRect.viewport;

        return _scrollRect.transform as RectTransform;
    }

    private float GetScrollStepNormalized()
    {
        if (_scrollStepNormalized > 0f)
            return Mathf.Clamp01(_scrollStepNormalized);

        if (_scrollRect == null || _scrollRect.content == null)
            return 1f;

        RectTransform viewport = GetViewport();
        if (viewport == null)
            return 1f;

        float scrollableWidth = _scrollRect.content.rect.width - viewport.rect.width;
        if (scrollableWidth <= 0f)
            return 1f;

        return Mathf.Clamp01(viewport.rect.width / scrollableWidth);
    }

    private void SetButtonAvailability(Button button, bool available)
    {
        if (button == null)
            return;

        if (_hideButtonsWhenUnavailable)
            button.gameObject.SetActive(available);
        else if (!button.gameObject.activeSelf)
            button.gameObject.SetActive(true);

        button.interactable = available;
    }

    private void StopRefreshRoutine()
    {
        if (_refreshRoutine == null)
            return;

        StopCoroutine(_refreshRoutine);
        _refreshRoutine = null;
    }
}

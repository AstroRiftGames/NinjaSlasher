using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class HUDPlayerOcclusionFade : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private Camera _gameplayCamera;
    [SerializeField] private Canvas _rootCanvas;

    [Header("Opacity")]
    [SerializeField, Range(0f, 1f)] private float _normalOpacity = 1f;
    [SerializeField, Range(0f, 1f)] private float _occludedOpacity = 0.35f;
    [SerializeField, Min(0f)] private float _fadeDuration = 0.15f;

    [Header("Detection")]
    [SerializeField] private Vector2 _padding = new Vector2(20f, 20f);
    [SerializeField] private bool _autoResolveReferences = true;
    [SerializeField, Min(0.05f)] private float _resolveRetryInterval = 0.25f;

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private PlayerController _playerController;
    private float _targetOpacity;
    private float _nextResolveTime;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _targetOpacity = _normalOpacity;

        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        ResolvePlayerControllerFromTransform();
        ApplyCanvasGroupInteractionState();
        SetOpacity(_normalOpacity);
    }

    private void OnEnable()
    {
        ResolveMissingReferences();
        ApplyCanvasGroupInteractionState();
        SetOpacity(_normalOpacity);
    }

    private void OnDisable()
    {
        if (_canvasGroup != null)
            SetOpacity(_normalOpacity);
    }

    private void Update()
    {
        if (_canvasGroup == null)
            return;

        if (_autoResolveReferences)
            ResolveMissingReferencesThrottled();

        bool shouldFade = ShouldFadeForPlayer();
        _targetOpacity = shouldFade ? _occludedOpacity : _normalOpacity;

        if (Mathf.Approximately(_canvasGroup.alpha, _targetOpacity))
            return;

        float nextOpacity;
        if (_fadeDuration <= 0f)
        {
            nextOpacity = _targetOpacity;
        }
        else
        {
            float maxDelta = Time.unscaledDeltaTime / _fadeDuration;
            nextOpacity = Mathf.MoveTowards(_canvasGroup.alpha, _targetOpacity, maxDelta);
        }

        SetOpacity(nextOpacity);
    }

    private void ResolveMissingReferencesThrottled()
    {
        if (_playerTransform != null && _gameplayCamera != null && _rootCanvas != null)
            return;

        if (Time.unscaledTime < _nextResolveTime)
            return;

        _nextResolveTime = Time.unscaledTime + _resolveRetryInterval;
        ResolveMissingReferences();
    }

    private void ResolveMissingReferences()
    {
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        if (_gameplayCamera == null)
            _gameplayCamera = Camera.main;

        if (_playerTransform == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                _playerController = player;
                _playerTransform = player.transform;
            }
        }
        else if (_playerController == null)
        {
            ResolvePlayerControllerFromTransform();
        }
    }

    private void ResolvePlayerControllerFromTransform()
    {
        if (_playerTransform == null)
        {
            _playerController = null;
            return;
        }

        _playerController = _playerTransform.GetComponent<PlayerController>();
        if (_playerController == null)
            _playerController = _playerTransform.GetComponentInParent<PlayerController>();
    }

    private bool ShouldFadeForPlayer()
    {
        if (_canvasGroup == null || _rectTransform == null)
            return false;

        if (_playerTransform == null || _gameplayCamera == null)
            return false;

        if (!_playerTransform.gameObject.activeInHierarchy)
            return false;

        if (_playerController != null)
        {
            if (_playerController.IsDeadOrDying)
                return false;

            PlayerView view = _playerController.View;
            if (view != null && view.SpriteContainer != null && !view.SpriteContainer.activeInHierarchy)
                return false;
        }

        Vector3 screenPosition = _gameplayCamera.WorldToScreenPoint(_playerTransform.position);
        if (screenPosition.z <= 0f)
            return false;

        Camera uiCamera = GetUICamera();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                screenPosition,
                uiCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = _rectTransform.rect;
        rect.xMin -= _padding.x;
        rect.xMax += _padding.x;
        rect.yMin -= _padding.y;
        rect.yMax += _padding.y;

        return rect.Contains(localPoint);
    }

    private Camera GetUICamera()
    {
        if (_rootCanvas == null || _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (_rootCanvas.worldCamera != null)
            return _rootCanvas.worldCamera;

        return _gameplayCamera;
    }

    private void SetOpacity(float opacity)
    {
        _canvasGroup.alpha = opacity;
    }

    private void ApplyCanvasGroupInteractionState()
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _normalOpacity = Mathf.Clamp01(_normalOpacity);
        _occludedOpacity = Mathf.Clamp01(_occludedOpacity);
        _fadeDuration = Mathf.Max(0f, _fadeDuration);
        _resolveRetryInterval = Mathf.Max(0.05f, _resolveRetryInterval);
    }
#endif
}

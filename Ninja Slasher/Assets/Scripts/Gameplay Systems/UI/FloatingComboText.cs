using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class FloatingComboText : MonoBehaviour, IPoolable
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float _duration = 1.2f;
    [SerializeField] private float _moveDistance = 150f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _startScale = 0.3f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _finalScale = 1f;
    [SerializeField] private Vector2 _positionOffset = new(0f, 50f);
    [SerializeField] private Vector2 _screenEdgePadding = new(32f, 24f);

    [Header("Effects")]
    [SerializeField] private bool _addPunchEffect = true;
    [SerializeField] private float _punchStrength = 0.3f;

    private RectTransform _rectTransform;
    private Sequence _animationSequence;
    private Vector3 _baseScale;
    private bool _baseScaleCached;
    private Vector2 _minimumSize;

    public event Action<FloatingComboText> OnRequestDespawn;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _rectTransform = transform as RectTransform;
        _minimumSize = _rectTransform != null ? _rectTransform.rect.size : Vector2.zero;
        CacheBaseScaleIfNeeded();
        ResetVisualState();
    }

    public void Show(string message, Vector3 worldPosition, Color color, Canvas canvas, RectTransform positionReference)
    {
        CacheBaseScaleIfNeeded();
        ResetVisualState();

        if (_text == null || _rectTransform == null || _canvasGroup == null || canvas == null || positionReference == null)
        {
            RequestDespawn();
            return;
        }

        _text.text = message;
        _text.color = color;
        ResizeToFitText();

        Camera worldCamera = ResolveWorldCamera(canvas);
        if (worldCamera == null)
        {
            RequestDespawn();
            return;
        }

        Vector3 viewportPosition = worldCamera.WorldToViewportPoint(worldPosition);
        if (viewportPosition.z <= 0f)
        {
            RequestDespawn();
            return;
        }

        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(worldCamera, worldPosition);
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : (canvas.worldCamera != null ? canvas.worldCamera : worldCamera);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            positionReference,
            screenPosition,
            uiCamera,
            out Vector2 localPoint
        );

        localPoint += _positionOffset;
        localPoint = ClampToReference(localPoint, positionReference);

        _rectTransform.localPosition = localPoint;

        PlayAnimation();
    }

    private void ResizeToFitText()
    {
        _text.ForceMeshUpdate();

        float width = Mathf.Max(_minimumSize.x, _text.preferredWidth);
        float height = Mathf.Max(_minimumSize.y, _text.preferredHeight);

        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    private Vector2 ClampToReference(Vector2 localPoint, RectTransform referenceRect)
    {
        Rect containerRect = referenceRect.rect;
        Rect ownRect = _rectTransform.rect;
        Vector2 pivot = _rectTransform.pivot;

        float minX = containerRect.xMin + ownRect.width * pivot.x + _screenEdgePadding.x;
        float maxX = containerRect.xMax - ownRect.width * (1f - pivot.x) - _screenEdgePadding.x;
        float minY = containerRect.yMin + ownRect.height * pivot.y + _screenEdgePadding.y;
        float maxY = containerRect.yMax - ownRect.height * (1f - pivot.y) - _screenEdgePadding.y;

        if (minX > maxX)
        {
            float centerX = (containerRect.xMin + containerRect.xMax) * 0.5f;
            minX = centerX;
            maxX = centerX;
        }

        if (minY > maxY)
        {
            float centerY = (containerRect.yMin + containerRect.yMax) * 0.5f;
            minY = centerY;
            maxY = centerY;
        }

        localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);
        localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);
        return localPoint;
    }

    private static Camera ResolveWorldCamera(Canvas canvas)
    {
        Camera fallbackCamera = Camera.main;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return fallbackCamera;

        return canvas.worldCamera != null ? canvas.worldCamera : fallbackCamera;
    }

    private void PlayAnimation()
    {
        ResetVisualState();

        Vector3 startScale = _baseScale * _startScale;
        Vector3 maxScale = _baseScale * _maxScale;
        Vector3 finalScale = _baseScale * _finalScale;

        _rectTransform.localScale = startScale;

        Vector3 startPos = _rectTransform.localPosition;

        _animationSequence = DOTween.Sequence();

        _animationSequence.Append(_rectTransform.DOScale(maxScale, _duration * 0.15f)
            .SetEase(Ease.OutBack, 1.5f));

        if (_addPunchEffect)
        {
            _animationSequence.Join(_rectTransform.DOPunchScale(_baseScale * _punchStrength, _duration * 0.15f, 5, 0.5f));
        }

        _animationSequence.Append(_rectTransform.DOScale(finalScale, _duration * 0.1f)
            .SetEase(Ease.OutQuad));

        _animationSequence.Join(_rectTransform.DOLocalMoveY(startPos.y + _moveDistance, _duration)
            .SetEase(_moveCurve));

        _animationSequence.Append(_canvasGroup.DOFade(0f, _duration * 0.35f)
            .SetEase(Ease.InQuad));

        _animationSequence.OnComplete(RequestDespawn);
    }

    private void CacheBaseScaleIfNeeded()
    {
        if (_baseScaleCached)
            return;

        _baseScale = _rectTransform != null ? _rectTransform.localScale : transform.localScale;
        _baseScaleCached = true;
    }

    private void ResetVisualState()
    {
        KillActiveTweens();

        if (_rectTransform != null)
        {
            _rectTransform.localScale = _baseScaleCached ? _baseScale : _rectTransform.localScale;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }
    }

    private void KillActiveTweens()
    {
        if (_animationSequence != null)
        {
            if (_animationSequence.IsActive())
                _animationSequence.Kill();

            _animationSequence = null;
        }

        if (_rectTransform != null)
        {
            DOTween.Kill(_rectTransform);
        }

        if (_canvasGroup != null)
        {
            DOTween.Kill(_canvasGroup);
        }
    }

    private void RequestDespawn()
    {
        OnRequestDespawn?.Invoke(this);
    }

    private void OnDestroy()
    {
        KillActiveTweens();
    }

    public void OnSpawn()
    {
        CacheBaseScaleIfNeeded();
        ResetVisualState();
    }

    public void OnDespawn()
    {
        CacheBaseScaleIfNeeded();
        ResetVisualState();
    }
}

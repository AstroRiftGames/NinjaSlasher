using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

public class FloatingComboText : MonoBehaviour, IPoolable
{
    [Header("REFERENCES")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float _duration = 1.2f;
    [SerializeField] private float _moveDistance = 150f;
    [SerializeField] private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _startScale = 0.3f;
    [SerializeField] private float _maxScale = 1.5f;
    [SerializeField] private float _finalScale = 1f;
    [SerializeField] private Vector2 _positionOffset = new Vector2(0, 50f);

    [Header("EFFECTS")]
    [SerializeField] private bool _addPunchEffect = true;
    [SerializeField] private float _punchStrength = 0.3f;

    private RectTransform _rectTransform;
    private Sequence _animationSequence;
    private Vector3 _baseScale;
    private bool _baseScaleCached;

    public event Action<FloatingComboText> OnRequestDespawn;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        _rectTransform = transform as RectTransform;
        CacheBaseScaleIfNeeded();
        ResetVisualState();
    }

    public void Show(string message, Vector3 worldPosition, Color color)
    {
        CacheBaseScaleIfNeeded();
        ResetVisualState();

        _text.text = message;
        _text.color = color;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            RequestDespawn();
            return;
        }

        bool isOverlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        Camera worldCam = isOverlay
            ? Camera.main
            : (canvas.worldCamera != null ? canvas.worldCamera : Camera.main);

        if (worldCam == null)
        {
            RequestDespawn();
            return;
        }

        Vector3 viewportPosition = worldCam.WorldToViewportPoint(worldPosition);

        if (viewportPosition.z < 0f)
            viewportPosition = new Vector3(0.5f, 0.5f, 1f);

        Rect cameraRect = worldCam.rect;
        viewportPosition.x = (viewportPosition.x - cameraRect.x) / cameraRect.width;
        viewportPosition.y = (viewportPosition.y - cameraRect.y) / cameraRect.height;

        Vector2 screenPosition = new Vector2(
            viewportPosition.x * Screen.width,
            viewportPosition.y * Screen.height
        );

        Camera localCam = isOverlay ? null : worldCam;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            localCam,
            out Vector2 localPoint
        );

        localPoint += _positionOffset;

        RectTransform canvasRect = canvas.transform as RectTransform;
        float halfW = canvasRect.rect.width * 0.5f;
        float halfH = canvasRect.rect.height * 0.5f;
        const float marginX = 90f;
        const float marginY = 80f;
        localPoint.x = Mathf.Clamp(localPoint.x, -halfW + marginX, halfW - marginX);
        localPoint.y = Mathf.Clamp(localPoint.y, -halfH + marginY, halfH - marginY);

        _rectTransform.localPosition = localPoint;

        PlayAnimation();
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

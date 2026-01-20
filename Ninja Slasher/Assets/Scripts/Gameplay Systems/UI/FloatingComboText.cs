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

    private Sequence _animationSequence;

    public event Action<FloatingComboText> OnRequestDespawn;

    private void Awake()
    {
        if (_text == null)
            _text = GetComponent<TextMeshProUGUI>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Show(string message, Vector3 worldPosition, Color color)
    {
        _text.text = message;
        _text.color = color;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            RequestDespawn();
            return;
        }

        Camera canvasCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        if (canvasCamera == null)
        {
            RequestDespawn();
            return;
        }

        Vector3 viewportPosition = canvasCamera.WorldToViewportPoint(worldPosition);

        Rect cameraRect = canvasCamera.rect;
        viewportPosition.x = (viewportPosition.x - cameraRect.x) / cameraRect.width;
        viewportPosition.y = (viewportPosition.y - cameraRect.y) / cameraRect.height;

        Vector2 screenPosition = new Vector2(
            viewportPosition.x * Screen.width,
            viewportPosition.y * Screen.height
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPosition,
            canvasCamera,
            out Vector2 localPoint
        );

        localPoint += _positionOffset;

        transform.localPosition = localPoint;

        PlayAnimation();
    }

    private void PlayAnimation()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
            _animationSequence.Kill();

        _canvasGroup.alpha = 1f;

        Vector3 startScale = Vector3.one * _startScale;
        Vector3 maxScale = Vector3.one * _maxScale;
        Vector3 finalScale = Vector3.one * _finalScale;

        transform.localScale = startScale;

        Vector3 startPos = transform.localPosition;

        _animationSequence = DOTween.Sequence();

        _animationSequence.Append(transform.DOScale(maxScale, _duration * 0.15f)
            .SetEase(Ease.OutBack, 1.5f));

        if (_addPunchEffect)
        {
            _animationSequence.Join(transform.DOPunchScale(Vector3.one * _punchStrength, _duration * 0.15f, 5, 0.5f));
        }

        _animationSequence.Append(transform.DOScale(finalScale, _duration * 0.1f)
            .SetEase(Ease.OutQuad));

        _animationSequence.Join(transform.DOLocalMoveY(startPos.y + _moveDistance, _duration)
            .SetEase(_moveCurve));

        _animationSequence.Append(_canvasGroup.DOFade(0f, _duration * 0.35f)
            .SetEase(Ease.InQuad));

        _animationSequence.OnComplete(RequestDespawn);
    }

    private void RequestDespawn()
    {
        OnRequestDespawn?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
            _animationSequence.Kill();
    }

    public void OnSpawn()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
            _animationSequence.Kill();

        _canvasGroup.alpha = 1f;
    }

    public void OnDespawn()
    {
        if (_animationSequence != null && _animationSequence.IsActive())
            _animationSequence.Kill();
    }
}
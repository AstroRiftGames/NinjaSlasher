using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class UIPunchScaleFeedback : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private RectTransform _target;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("BEHAVIOR")]
    [SerializeField] private bool _useFade = true;
    [SerializeField] private bool _hideGameObjectOnComplete = true;
    [SerializeField] private bool _activateGameObjectOnPlay = true;
    [SerializeField] private bool _useUnscaledTime = false;

    [Header("TIMING")]
    [SerializeField] private float _scaleInDuration = 0.2f;
    [SerializeField] private float _punchDuration = 0.15f;
    [SerializeField] private float _visibleDuration = 1.2f;
    [SerializeField] private float _fadeDuration = 0.3f;

    [Header("SCALE")]
    [SerializeField] private float _initialScaleMultiplier = 0.5f;
    [SerializeField] private Vector3 _punchStrength = new Vector3(0.3f, 0.3f, 0f);
    [SerializeField] private int _vibrato = 5;
    [SerializeField] private float _elasticity = 0.5f;

    private Sequence _playbackSequence;
    private Vector3 _baseScale = Vector3.one;
    private bool _baseScaleCached;

    private void Awake()
    {
        CacheReferences();
        CacheBaseScale();
        RestoreBaseState();
    }

    private void OnDisable()
    {
        KillActiveTweens();
        RestoreBaseState();
    }

    public void Play()
    {
        if (_activateGameObjectOnPlay && !gameObject.activeSelf)
            gameObject.SetActive(true);

        CacheReferences();
        CacheBaseScale();

        if (_target == null)
            return;

        KillActiveTweens();
        RestoreBaseState();

        _target.localScale = _baseScale * _initialScaleMultiplier;
        SetCanvasAlpha(1f);

        _playbackSequence = DOTween.Sequence();
        _playbackSequence.SetUpdate(_useUnscaledTime);
        _playbackSequence.Append(_target.DOScale(_baseScale, _scaleInDuration).SetEase(Ease.OutBack));
        _playbackSequence.AppendCallback(() => _target.localScale = _baseScale);

        if (_punchDuration > 0f && _punchStrength != Vector3.zero)
        {
            _playbackSequence.Append(_target.DOPunchScale(_punchStrength, _punchDuration, _vibrato, _elasticity));
            _playbackSequence.AppendCallback(() => _target.localScale = _baseScale);
        }

        if (_visibleDuration > 0f)
            _playbackSequence.AppendInterval(_visibleDuration);

        if (_useFade && _canvasGroup != null && _fadeDuration > 0f)
            _playbackSequence.Append(_canvasGroup.DOFade(0f, _fadeDuration).SetEase(Ease.InQuad));

        _playbackSequence.OnComplete(HandlePlaybackCompleted);
    }

    public void HideImmediate()
    {
        ResetImmediate();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public void ResetImmediate()
    {
        CacheReferences();
        CacheBaseScale();
        KillActiveTweens();
        RestoreBaseState();
    }

    public void Configure(
        bool useFade,
        bool hideGameObjectOnComplete,
        bool activateGameObjectOnPlay,
        bool useUnscaledTime,
        float scaleInDuration,
        float punchDuration,
        float visibleDuration,
        float fadeDuration,
        float initialScaleMultiplier,
        Vector3 punchStrength,
        int vibrato,
        float elasticity)
    {
        _useFade = useFade;
        _hideGameObjectOnComplete = hideGameObjectOnComplete;
        _activateGameObjectOnPlay = activateGameObjectOnPlay;
        _useUnscaledTime = useUnscaledTime;
        _scaleInDuration = scaleInDuration;
        _punchDuration = punchDuration;
        _visibleDuration = visibleDuration;
        _fadeDuration = fadeDuration;
        _initialScaleMultiplier = initialScaleMultiplier;
        _punchStrength = punchStrength;
        _vibrato = vibrato;
        _elasticity = elasticity;
    }

    private void HandlePlaybackCompleted()
    {
        _playbackSequence = null;
        RestoreBaseState();

        if (_hideGameObjectOnComplete && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void CacheReferences()
    {
        if (_target == null)
            _target = transform as RectTransform;

        if (_useFade && _canvasGroup == null)
        {
            if (_target != null && !_target.TryGetComponent(out _canvasGroup))
                _canvasGroup = _target.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void CacheBaseScale()
    {
        if (_baseScaleCached || _target == null)
            return;

        _baseScale = _target.localScale;
        _baseScaleCached = true;
    }

    private void RestoreBaseState()
    {
        if (_target != null && _baseScaleCached)
            _target.localScale = _baseScale;

        SetCanvasAlpha(1f);
    }

    private void KillActiveTweens()
    {
        if (_playbackSequence != null)
        {
            if (_playbackSequence.IsActive())
                _playbackSequence.Kill();

            _playbackSequence = null;
        }

        if (_target != null)
            DOTween.Kill(_target);

        if (_canvasGroup != null)
            DOTween.Kill(_canvasGroup);
    }

    private void SetCanvasAlpha(float alpha)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = alpha;
    }
}

using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class KatanaTransitionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RawImage _slashOverlay;
    [SerializeField] private Image _blackOverlay;

    [Header("Settings")]
    [SerializeField] private float _fadeToBlackDuration = 0.4f;
    [SerializeField] private float _slashDuration = 0.56f;
    [SerializeField] private float _delayBeforeSlash = 0.11f;
    [SerializeField] private float _cutAngle = -45f;
    [SerializeField] private float _slashDirection = 45f;
    [SerializeField] private float _openFollowDelay = 0.4f;

    [Header("Animation Curves")]
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _slashCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3.6f),
        new Keyframe(0.22f, 0.58f, 2.1f, 0.9f),
        new Keyframe(1f, 1f, 0.18f, 0f));

    [Header("Slash Look")]
    [SerializeField] private float _edgeSoftness = 0.019f;
    [SerializeField] private float _cutFeather = 0.0025f;
    [SerializeField] private float _lineThickness = 0.011f;
    [SerializeField] private float _glowThickness = 0.045f;
    [SerializeField] private Color _highlightColor = new Color(1f, 0.96f, 0.82f, 0.9f);
    [SerializeField] private Color _coreLineColor = Color.white;

    [Header("Split Motion")]
    [SerializeField] private float _halfSeparation = 1.7f;
    [SerializeField] private float _halfDrop = 0.18f;
    [SerializeField] private float _halfDropExponent = 1.45f;

    [Header("Audio")]
    [SerializeField] private UIAudioContext _audioContext;

    private bool _isTransitioning;

    public bool IsTransitioning => _isTransitioning;
    public bool IsReady => _slashOverlay != null && _slashOverlay.material != null;

    private void Awake()
    {
        _audioContext ??= GetComponentInParent<UIAudioContext>();
    }

    private void Start()
    {
        if (_slashOverlay != null)
            _slashOverlay.enabled = false;
        if (_blackOverlay != null)
            _blackOverlay.color = new Color(0, 0, 0, 0);
    }

    public void PlayEnterLevelTransition(System.Action onComplete = null)
    {
        if (_isTransitioning || !IsReady) return;
        _isTransitioning = true;
        _slashOverlay.enabled = false;
        ResetMaterialValues();
        StartCoroutine(CoverSequence(onComplete));
    }

    public void PlayExitLevelTransition(System.Action onComplete = null)
    {
        if (_isTransitioning || !IsReady) return;
        _isTransitioning = true;

        if (_audioContext?.Audio?.transitionSlash != null && AudioService.Instance != null)
        {
            AudioService.Instance.PlaySFX(_audioContext.Audio.transitionSlash);
        }

        ApplyMaterialValues(1f, 0f, 1f);
        _slashOverlay.enabled = true;

        StartCoroutine(RevealSequence(onComplete));
    }

    private System.Collections.IEnumerator CoverSequence(System.Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < _fadeToBlackDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = _fadeCurve.Evaluate(Mathf.Clamp01(elapsed / _fadeToBlackDuration));
            if (_blackOverlay != null)
            {
                Color c = _blackOverlay.color;
                c.a = t;
                _blackOverlay.color = c;
            }
            yield return null;
        }
        if (_blackOverlay != null)
        {
            Color c = _blackOverlay.color;
            c.a = 1f;
            _blackOverlay.color = c;
        }

        _isTransitioning = false;
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator RevealSequence(System.Action onComplete)
    {
        // Let the closed katana mask take over before removing the flat black overlay.
        yield return null;

        if (_blackOverlay != null)
        {
            Color c = _blackOverlay.color;
            c.a = 0f;
            _blackOverlay.color = c;
        }

        if (_delayBeforeSlash > 0f)
            yield return new WaitForSecondsRealtime(_delayBeforeSlash);

        yield return AnimateSlash();

        ResetMaterialValues();
        _slashOverlay.enabled = false;
        _isTransitioning = false;
        onComplete?.Invoke();
    }

    private System.Collections.IEnumerator AnimateSlash()
    {
        float elapsed = 0f;
        while (elapsed < _slashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = _slashCurve.Evaluate(Mathf.Clamp01(elapsed / _slashDuration));
            ApplyMaterialValues(1f, t, 1f);
            yield return null;
        }
        ApplyMaterialValues(1f, 1f, 1f);
    }

    private void ResetMaterialValues()
    {
        ApplyMaterialValues(0f, 0f, 0f);
    }

    private void ApplyMaterialValues(float opacity, float travel, float energy)
    {
        if (_slashOverlay == null || _slashOverlay.material == null) return;

        Material mat = _slashOverlay.material;
        mat.SetFloat("_Opacity", opacity);
        mat.SetFloat("_Travel", travel);
        mat.SetFloat("_TravelPosition", Mathf.Lerp(2.2f, -2.2f, travel));
        mat.SetFloat("_OpenProgress", EvaluateOpenProgress(travel));
        mat.SetFloat("_TravelDirection", _slashDirection);
        mat.SetFloat("_Angle", _cutAngle);
        mat.SetFloat("_CutPosition", 0f);
        mat.SetFloat("_EdgeSoftness", _edgeSoftness);
        mat.SetFloat("_CutFeather", _cutFeather);
        mat.SetFloat("_LineThickness", _lineThickness);
        mat.SetFloat("_GlowThickness", _glowThickness);
        mat.SetFloat("_Energy", energy);
        mat.SetColor("_TintColor", Color.black);
        mat.SetColor("_HighlightColor", _highlightColor);
        mat.SetColor("_CoreLineColor", _coreLineColor);
        mat.SetFloat("_MaxAperture", 2.35f);
        mat.SetFloat("_HalfSeparation", _halfSeparation);
        mat.SetFloat("_HalfDrop", _halfDrop);
        mat.SetFloat("_HalfDropExponent", _halfDropExponent);
    }

    private float EvaluateOpenProgress(float travel)
    {
        float delay = Mathf.Clamp(_openFollowDelay, 0f, 0.95f);
        if (delay <= 0f)
        {
            return Mathf.Clamp01(travel);
        }

        float normalized = Mathf.Clamp01((travel - delay) / (1f - delay));
        return normalized * normalized;
    }
}

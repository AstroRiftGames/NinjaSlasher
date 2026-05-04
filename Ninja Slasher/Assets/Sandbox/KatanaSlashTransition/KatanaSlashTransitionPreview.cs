using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public sealed class KatanaSlashTransitionPreview : MonoBehaviour
{
    private const string IntroBackdropName = "IntroBackdrop";
    private const string IntroPanelName = "IntroPanel";
    private const string IntroAccentName = "IntroAccent";
    private const string BackdropName = "DemoBackdrop";
    private const string HorizonName = "DemoHorizon";
    private const string FloorName = "DemoFloor";
    private const string PlatformLeftName = "DemoPlatformLeft";
    private const string PlatformRightName = "DemoPlatformRight";
    private const string GoalName = "DemoGoal";
    private const string OverlayName = "SlashOverlay";
    private const float MaxAperture = 2.35f;

    [Header("References")]
    [SerializeField] private Material _baseMaterial;

    [Header("Playback")]
    [SerializeField] private bool _autoPlayOnStart = true;
    [SerializeField] private float _fadeToBlackDuration = 0.4f;
    [SerializeField] private float _delayBeforeSlash = 0.11f;
    [SerializeField] private float _slashTravelDuration = 0.56f;
    [SerializeField] private float _postSlashTrailDuration = 0.08f;
    [SerializeField] private float _delayAfterSlash = 0f;
    [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _slashTravelCurve = new AnimationCurve(
        new Keyframe(0f, 0f, 0f, 3.6f),
        new Keyframe(0.22f, 0.58f, 2.1f, 0.9f),
        new Keyframe(1f, 1f, 0.18f, 0f));

    [Header("Slash Path")]
    [SerializeField, Range(-180f, 180f)] private float _slashTravelDirection = 45f;
    [SerializeField, Range(-180f, 180f)] private float _cutAngle = -45f;
    [SerializeField, Range(-1.25f, 1.25f)] private float _cutPosition = 0f;
    [SerializeField, Min(1.5f)] private float _travelOverscan = 2.2f;
    [SerializeField, Min(0f)] private float _slashFrontWidth = 0.055f;
    [SerializeField, Min(0f)] private float _trailLength = 0.22f;
    [SerializeField, Range(0f, 0.6f)] private float _openFollowDelay = 0.28f;
    [SerializeField, Min(0f)] private float _openWidthBehindSlash = 1.1f;

    [Header("Slash Look")]
    [SerializeField, Min(0.0005f)] private float _edgeSoftness = 0.019f;
    [SerializeField, Min(0.00025f)] private float _cutFeather = 0.0025f;
    [SerializeField, Min(0f)] private float _slashLineThickness = 0.011f;
    [SerializeField, Min(0f)] private float _coreLineIntensity = 1.2f;
    [SerializeField, Min(0f)] private float _glowThickness = 0.045f;
    [SerializeField, Min(0f)] private float _sweepIntensity = 0.72f;
    [SerializeField, Min(0f)] private float _lineIrregularity = 0.002f;
    [SerializeField, Min(0f)] private float _irregularityFrequency = 8f;
    [SerializeField] private Color _highlightColor = new Color(1f, 0.96f, 0.82f, 0.9f);
    [SerializeField] private Color _coreLineColor = Color.white;

    [Header("Afterglow")]
    [SerializeField, Min(0f)] private float _postTraceIntensity = 0.22f;

    [Header("Split Motion")]
    [SerializeField, Min(0f)] private float _halfSeparation = 1.7f;
    [SerializeField, Min(0f)] private float _halfDrop = 0.18f;
    [SerializeField, Min(0.01f)] private float _halfDropExponent = 1.45f;

    [Header("Cut Curvature")]
    [SerializeField, Min(0f)] private float _cutCurvatureAmount = 0.045f;
    [SerializeField, Range(0f, 1f)] private float _cutCurvatureBias = 0.58f;
    [SerializeField, Min(0.01f)] private float _cutCurvatureFalloff = 1.8f;

    [Header("Cut Sharpness")]
    [SerializeField, Min(0.05f)] private float _cutBodyWidthScale = 1f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTipTaper = 0.2f;
    [SerializeField, Range(0.05f, 1f)] private float _cutTailTaper = 0.45f;
    [SerializeField, Min(0.01f)] private float _cutTaperSharpness = 1.35f;

    [Header("Sandbox")]
    [SerializeField] private bool _showDemoBackdrop = true;
    [SerializeField] private Color _backdropColor = new Color(0.11f, 0.13f, 0.16f, 1f);
    [SerializeField] private int _sortingOrder = 5000;

    [Header("Editor Preview")]
    [SerializeField, Range(0f, 1f)] private float _editorBlackAmount = 1f;
    [SerializeField, Range(0f, 1f)] private float _editorSlashAmount;

    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private Image _introBackdrop;
    private Image _introPanel;
    private Image _introAccent;
    private Image _demoBackdrop;
    private Image _demoHorizon;
    private Image _demoFloor;
    private Image _demoPlatformLeft;
    private Image _demoPlatformRight;
    private Image _demoGoal;
    private RawImage _slashOverlay;
    private Material _runtimeMaterial;
    private Coroutine _previewRoutine;
    private float _currentBlackAmount = 1f;
    private float _currentSlashTravel;
    private bool _showLevelContent = true;

    public float FadeToBlackDuration => _fadeToBlackDuration;
    public float SlashTravelDuration => _slashTravelDuration;
    public float CutAngle => _cutAngle;
    public float CutPosition => _cutPosition;
    public float EdgeSoftness => _edgeSoftness;
    public float CutFeather => _cutFeather;
    public float SlashLineThickness => _slashLineThickness;
    public float HalfSeparation => _halfSeparation;
    public float HalfDrop => _halfDrop;
    public float DelayBeforeSlash => _delayBeforeSlash;
    public float DelayAfterSlash => _delayAfterSlash;

    private void Reset()
    {
        EnsureSetup(forceMaterialRefresh: true);
        PreviewCovered();
    }

    private void Awake()
    {
        EnsureSetup(forceMaterialRefresh: true);
        PreviewCovered();
    }

    private void OnEnable()
    {
        EnsureSetup(forceMaterialRefresh: true);
        PreviewCovered();

        if (Application.isPlaying && _autoPlayOnStart)
        {
            PlayPreview();
        }
    }

    private void OnDisable()
    {
        StopPreview();
        DestroyRuntimeMaterial();
    }

    private void OnValidate()
    {
        ClampSerializedValues();
        EnsureSetup();

        if (Application.isPlaying)
        {
            ApplyCurrentPose();
            return;
        }

        bool showLevel = _editorBlackAmount > 0f || _editorSlashAmount > 0f;
        if (showLevel)
        {
            ShowLevelContent();
        }
        else
        {
            ShowIntroContent();
        }

        ApplyMaterialProperties(_editorBlackAmount, _editorSlashAmount, Mathf.Clamp01(_editorSlashAmount));
    }

    [ContextMenu("Play Preview")]
    public void PlayPreview()
    {
        if (!Application.isPlaying)
        {
            ShowLevelContent();
            SetEditorPose(1f, 0.5f);
            return;
        }

        StopPreview();
        _previewRoutine = StartCoroutine(PlayRevealRoutine());
    }

    [ContextMenu("Play Full Sequence")]
    public void PlayFullSequence()
    {
        if (!Application.isPlaying)
        {
            PreviewSource();
            return;
        }

        StopPreview();
        _previewRoutine = StartCoroutine(PlayFullPreviewRoutine());
    }

    [ContextMenu("Stop Preview")]
    public void StopPreview()
    {
        if (_previewRoutine == null)
        {
            return;
        }

        StopCoroutine(_previewRoutine);
        _previewRoutine = null;
    }

    [ContextMenu("Preview Source")]
    public void PreviewSource()
    {
        StopPreview();
        ShowIntroContent();
        SetEditorPose(0f, 0f);
    }

    [ContextMenu("Preview Covered")]
    public void PreviewCovered()
    {
        StopPreview();
        ShowLevelContent();
        SetEditorPose(1f, 0f);
    }

    [ContextMenu("Preview Mid Slash")]
    public void PreviewMidSlash()
    {
        StopPreview();
        ShowLevelContent();
        SetEditorPose(1f, 0.45f);
    }

    [ContextMenu("Preview Open")]
    public void PreviewOpen()
    {
        StopPreview();
        ShowLevelContent();
        SetEditorPose(1f, 1f);
    }

    [ContextMenu("Rebuild Sandbox")]
    public void RebuildSandbox()
    {
        EnsureSetup(forceMaterialRefresh: true);
        ApplyCurrentPose();
    }

    private IEnumerator PlayFullPreviewRoutine()
    {
        ShowIntroContent();
        SetRuntimePose(0f, 0f);
        yield return AnimatePhase(0f, 1f, _fadeToBlackDuration, _fadeCurve, blackOnly: true);

        if (_delayBeforeSlash > 0f)
        {
            yield return new WaitForSecondsRealtime(_delayBeforeSlash);
        }

        ShowLevelContent();
        SetRuntimePose(1f, 0f);
        yield return AnimatePhase(0f, 1f, _slashTravelDuration, _slashTravelCurve, blackOnly: false);
        yield return FinishRevealRoutine();
    }

    private IEnumerator PlayRevealRoutine()
    {
        ShowLevelContent();
        SetRuntimePose(1f, 0f);
        yield return AnimatePhase(0f, 1f, _slashTravelDuration, _slashTravelCurve, blackOnly: false);
        yield return FinishRevealRoutine();
    }

    private IEnumerator FinishRevealRoutine()
    {
        if (_postSlashTrailDuration > 0f)
        {
            yield return AnimatePostSlashTrail();
        }

        if (_delayAfterSlash > 0f)
        {
            yield return new WaitForSecondsRealtime(_delayAfterSlash);
        }

        _previewRoutine = null;
    }

    private IEnumerator AnimatePhase(float from, float to, float duration, AnimationCurve curve, bool blackOnly)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = curve != null ? curve.Evaluate(normalized) : normalized;
            float value = Mathf.LerpUnclamped(from, to, eased);

            if (blackOnly)
            {
                SetRuntimePose(value, 0f);
            }
            else
            {
                SetRuntimePose(1f, value);
            }

            yield return null;
        }

        if (blackOnly)
        {
            SetRuntimePose(to, 0f);
        }
        else
        {
            SetRuntimePose(1f, to);
        }
    }

    private IEnumerator AnimatePostSlashTrail()
    {
        float elapsed = 0f;

        while (elapsed < _postSlashTrailDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / _postSlashTrailDuration);
            ApplyMaterialProperties(1f, 1f, 1f - normalized);
            yield return null;
        }

        ApplyMaterialProperties(1f, 1f, 0f);
    }

    private void SetRuntimePose(float blackAmount, float slashTravel)
    {
        _currentBlackAmount = Mathf.Clamp01(blackAmount);
        _currentSlashTravel = Mathf.Clamp01(slashTravel);
        ApplyMaterialProperties(_currentBlackAmount, _currentSlashTravel, 1f);
    }

    private void SetEditorPose(float blackAmount, float slashTravel)
    {
        _editorBlackAmount = Mathf.Clamp01(blackAmount);
        _editorSlashAmount = Mathf.Clamp01(slashTravel);
        ApplyMaterialProperties(_editorBlackAmount, _editorSlashAmount, Mathf.Clamp01(_editorSlashAmount));
    }

    private void ApplyCurrentPose()
    {
        if (Application.isPlaying)
        {
            ApplyMaterialProperties(_currentBlackAmount, _currentSlashTravel, 1f);
            return;
        }

        ApplyMaterialProperties(_editorBlackAmount, _editorSlashAmount, Mathf.Clamp01(_editorSlashAmount));
    }

    private void ApplyMaterialProperties(float blackAmount, float slashTravel, float energyAmount)
    {
        EnsureSetup();
        if (_runtimeMaterial == null)
        {
            return;
        }

        _runtimeMaterial.SetFloat("_Opacity", Mathf.Clamp01(blackAmount));
        _runtimeMaterial.SetFloat("_Travel", Mathf.Clamp01(slashTravel));
        _runtimeMaterial.SetFloat("_TravelPosition", Mathf.Lerp(_travelOverscan, -_travelOverscan, Mathf.Clamp01(slashTravel)));
        _runtimeMaterial.SetFloat("_OpenProgress", EvaluateOpenProgress(slashTravel));
        _runtimeMaterial.SetFloat("_TravelDirection", _slashTravelDirection);
        _runtimeMaterial.SetFloat("_Angle", _cutAngle);
        _runtimeMaterial.SetFloat("_CutPosition", _cutPosition);
        _runtimeMaterial.SetFloat("_FrontWidth", _slashFrontWidth);
        _runtimeMaterial.SetFloat("_TrailLength", _trailLength);
        _runtimeMaterial.SetFloat("_OpenWidth", _openWidthBehindSlash);
        _runtimeMaterial.SetFloat("_EdgeSoftness", _edgeSoftness);
        _runtimeMaterial.SetFloat("_CutFeather", _cutFeather);
        _runtimeMaterial.SetFloat("_LineThickness", _slashLineThickness);
        _runtimeMaterial.SetFloat("_CoreLineIntensity", _coreLineIntensity);
        _runtimeMaterial.SetFloat("_GlowThickness", _glowThickness);
        _runtimeMaterial.SetFloat("_LineIrregularity", _lineIrregularity);
        _runtimeMaterial.SetFloat("_IrregularityFrequency", _irregularityFrequency);
        _runtimeMaterial.SetFloat("_SweepIntensity", _sweepIntensity);
        _runtimeMaterial.SetFloat("_Energy", Mathf.Clamp01(energyAmount));
        _runtimeMaterial.SetFloat("_PostTraceIntensity", _postTraceIntensity);
        _runtimeMaterial.SetFloat("_MaxAperture", MaxAperture);
        _runtimeMaterial.SetFloat("_HalfSeparation", _halfSeparation);
        _runtimeMaterial.SetFloat("_HalfDrop", _halfDrop);
        _runtimeMaterial.SetFloat("_HalfDropExponent", _halfDropExponent);
        _runtimeMaterial.SetFloat("_CutCurvatureAmount", _cutCurvatureAmount);
        _runtimeMaterial.SetFloat("_CutCurvatureBias", _cutCurvatureBias);
        _runtimeMaterial.SetFloat("_CutCurvatureFalloff", _cutCurvatureFalloff);
        _runtimeMaterial.SetFloat("_CutBodyWidthScale", _cutBodyWidthScale);
        _runtimeMaterial.SetFloat("_CutTipTaper", _cutTipTaper);
        _runtimeMaterial.SetFloat("_CutTailTaper", _cutTailTaper);
        _runtimeMaterial.SetFloat("_CutTaperSharpness", _cutTaperSharpness);
        _runtimeMaterial.SetColor("_TintColor", Color.black);
        _runtimeMaterial.SetColor("_HighlightColor", _highlightColor);
        _runtimeMaterial.SetColor("_CoreLineColor", _coreLineColor);

        ConfigureCanvas();
        ApplyBackdropColors();
        UpdateDemoVisibility();
    }

    private float EvaluateOpenProgress(float travel)
    {
        float delay = Mathf.Clamp(_openFollowDelay, 0f, 0.95f);
        if (delay <= 0f)
        {
            return Mathf.Clamp01(travel);
        }

        return Mathf.Clamp01((travel - delay) / (1f - delay));
    }

    private void EnsureSetup(bool forceMaterialRefresh = false)
    {
        ResolveReferences();
        ConfigureCanvas();

        if (forceMaterialRefresh)
        {
            DestroyRuntimeMaterial();
        }

        if (_runtimeMaterial == null || (_slashOverlay != null && _slashOverlay.material != _runtimeMaterial))
        {
            RecreateRuntimeMaterial();
        }
    }

    private void ResolveReferences()
    {
        _canvas = GetComponent<Canvas>();
        _canvasScaler = GetComponent<CanvasScaler>();
        _introBackdrop = FindChildImage(IntroBackdropName);
        _introPanel = FindChildImage(IntroPanelName);
        _introAccent = FindChildImage(IntroAccentName);
        _demoBackdrop = FindChildImage(BackdropName);
        _demoHorizon = FindChildImage(HorizonName);
        _demoFloor = FindChildImage(FloorName);
        _demoPlatformLeft = FindChildImage(PlatformLeftName);
        _demoPlatformRight = FindChildImage(PlatformRightName);
        _demoGoal = FindChildImage(GoalName);
        _slashOverlay = FindChildRawImage(OverlayName);
    }

    private void ConfigureCanvas()
    {
        if (_canvas != null)
        {
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.pixelPerfect = false;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = _sortingOrder;
        }

        if (_canvasScaler != null)
        {
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f;
        }

        if (_slashOverlay != null)
        {
            _slashOverlay.raycastTarget = false;
            _slashOverlay.color = Color.white;
        }
    }

    private void ApplyBackdropColors()
    {
        if (_introBackdrop != null) _introBackdrop.color = new Color(0.08f, 0.1f, 0.16f, 1f);
        if (_introPanel != null) _introPanel.color = new Color(0.16f, 0.2f, 0.34f, 1f);
        if (_introAccent != null) _introAccent.color = new Color(0.93f, 0.35f, 0.2f, 1f);
        if (_demoBackdrop != null) _demoBackdrop.color = GetPreviewBackdropColor();
        if (_demoHorizon != null) _demoHorizon.color = new Color(0.42f, 0.74f, 0.93f, 0.18f);
        if (_demoFloor != null) _demoFloor.color = new Color(0.08f, 0.16f, 0.18f, 1f);
        if (_demoPlatformLeft != null) _demoPlatformLeft.color = new Color(0.95f, 0.45f, 0.2f, 1f);
        if (_demoPlatformRight != null) _demoPlatformRight.color = new Color(0.18f, 0.82f, 0.72f, 1f);
        if (_demoGoal != null) _demoGoal.color = new Color(1f, 0.9f, 0.42f, 1f);
    }

    private Color GetPreviewBackdropColor()
    {
        if (!_showDemoBackdrop)
        {
            return _backdropColor;
        }

        float luminance =
            (_backdropColor.r * 0.2126f) +
            (_backdropColor.g * 0.7152f) +
            (_backdropColor.b * 0.0722f);

        if (luminance < 0.12f)
        {
            return new Color(0.12f, 0.2f, 0.28f, 1f);
        }

        return _backdropColor;
    }

    private void ShowIntroContent()
    {
        _showLevelContent = false;
        UpdateDemoVisibility();
    }

    private void ShowLevelContent()
    {
        _showLevelContent = true;
        UpdateDemoVisibility();
    }

    private void UpdateDemoVisibility()
    {
        bool showIntro = _showDemoBackdrop && !_showLevelContent;
        bool showLevel = _showDemoBackdrop && _showLevelContent;

        SetImageEnabled(_introBackdrop, showIntro);
        SetImageEnabled(_introPanel, showIntro);
        SetImageEnabled(_introAccent, showIntro);
        SetImageEnabled(_demoBackdrop, showLevel);
        SetImageEnabled(_demoHorizon, showLevel);
        SetImageEnabled(_demoFloor, showLevel);
        SetImageEnabled(_demoPlatformLeft, showLevel);
        SetImageEnabled(_demoPlatformRight, showLevel);
        SetImageEnabled(_demoGoal, showLevel);
    }

    private void RecreateRuntimeMaterial()
    {
        DestroyRuntimeMaterial();
        if (_slashOverlay == null)
        {
            return;
        }

        Material sourceMaterial = _baseMaterial;
        if (sourceMaterial == null)
        {
            Shader shader = Shader.Find("UI/Katana Slash Transition Preview");
            if (shader != null)
            {
                sourceMaterial = new Material(shader);
            }
        }

        if (sourceMaterial == null)
        {
            return;
        }

        _runtimeMaterial = new Material(sourceMaterial)
        {
            name = sourceMaterial.name + " (Preview Instance)",
            hideFlags = HideFlags.HideAndDontSave
        };

        _slashOverlay.material = _runtimeMaterial;
    }

    private void DestroyRuntimeMaterial()
    {
        if (_runtimeMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(_runtimeMaterial);
        }

        _runtimeMaterial = null;
    }

    private void ClampSerializedValues()
    {
        _fadeToBlackDuration = Mathf.Max(0.01f, _fadeToBlackDuration);
        _delayBeforeSlash = Mathf.Max(0f, _delayBeforeSlash);
        _slashTravelDuration = Mathf.Max(0.01f, _slashTravelDuration);
        _postSlashTrailDuration = Mathf.Max(0f, _postSlashTrailDuration);
        _delayAfterSlash = Mathf.Max(0f, _delayAfterSlash);
        _travelOverscan = Mathf.Max(1.5f, _travelOverscan);
        _slashFrontWidth = Mathf.Max(0.0001f, _slashFrontWidth);
        _trailLength = Mathf.Max(0f, _trailLength);
        _openWidthBehindSlash = Mathf.Max(0f, _openWidthBehindSlash);
        _edgeSoftness = Mathf.Max(0.0005f, _edgeSoftness);
        _cutFeather = Mathf.Max(0.00025f, _cutFeather);
        _slashLineThickness = Mathf.Max(0f, _slashLineThickness);
        _coreLineIntensity = Mathf.Max(0f, _coreLineIntensity);
        _glowThickness = Mathf.Max(0f, _glowThickness);
        _sweepIntensity = Mathf.Max(0f, _sweepIntensity);
        _lineIrregularity = Mathf.Max(0f, _lineIrregularity);
        _irregularityFrequency = Mathf.Max(0f, _irregularityFrequency);
        _postTraceIntensity = Mathf.Max(0f, _postTraceIntensity);
        _halfSeparation = Mathf.Max(0f, _halfSeparation);
        _halfDrop = Mathf.Max(0f, _halfDrop);
        _halfDropExponent = Mathf.Max(0.01f, _halfDropExponent);
        _cutCurvatureAmount = Mathf.Max(0f, _cutCurvatureAmount);
        _cutCurvatureFalloff = Mathf.Max(0.01f, _cutCurvatureFalloff);
        _cutBodyWidthScale = Mathf.Max(0.05f, _cutBodyWidthScale);
        _cutTaperSharpness = Mathf.Max(0.01f, _cutTaperSharpness);
        _sortingOrder = Mathf.Max(0, _sortingOrder);
    }

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private RawImage FindChildRawImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<RawImage>() : null;
    }

    private static void SetImageEnabled(Graphic image, bool enabled)
    {
        if (image != null)
        {
            image.enabled = enabled;
        }
    }
}

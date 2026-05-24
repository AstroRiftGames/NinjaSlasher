using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Action = System.Action;

public class ResultsUIManager : MonoBehaviourSingleton<ResultsUIManager>
{
    [SerializeField] private Transform _starsContainer;
    [SerializeField] private TextMeshProUGUI _primaryGoalText;
    [SerializeField] private TextMeshProUGUI[] _secondaryGoalTexts;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float _starAnimationDuration = 0.8f;
    [SerializeField] private float _starAnimationDelay = 0.2f;
    [SerializeField] private float _starOffscreenDistance = 1200f;
    [SerializeField] private Ease _starAnimationEase = Ease.Linear;

    [Header("STROKE ANIMATION SETTINGS")]
    [SerializeField] private bool useStrokeAnimations = true;
    [SerializeField] private float strokeAnimationDelay = 0.3f;
    [SerializeField] private float slashEffectDuration = 0.4f;
    [SerializeField] private float strokeStagger = 0.5f;

    private Vector2[] _originalStarPositions;

    private UIAudioContext _audioContext;
    private readonly List<Tween> _activePresentationTweens = new();
    private Coroutine _objectiveStrokeCoroutine;
    private Action _sequenceCompletedCallback;
    private int _sequenceVersion;
    private int _pendingStarAnimations;
    private int _pendingStrokeAnimations;
    private bool _isSequenceRunning;

    public override void Awake()
    {
        base.Awake();
        CacheOriginalStarPositions();

        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    private void OnDisable()
    {
        CancelResultsPresentation();
    }

    public void PrepareResultsIntro()
    {
        CancelResultsPresentation();
        Canvas.ForceUpdateCanvases();

        if (_originalStarPositions == null || _originalStarPositions.Length == 0)
            CacheOriginalStarPositions();

        SetStarsToOffscreenPosition();
        ResetStarSprites();
        ResetAllStrokesVisuals();

        if (TryGetCurrentResultsConfiguration(out LevelConfiguration config, out int levelId))
        {
            SetupGoalTextsAndStrokes(config, levelId);
            return;
        }

        ClearGoalTexts();
    }

    public void ShowResultsPanel()
    {
        ShowResultsPanel(null);
    }

    public void ShowResultsPanel(Action onSequenceCompleted)
    {
        BeginResultsPresentation(onSequenceCompleted);

        if (!TryGetCurrentResultsConfiguration(out LevelConfiguration config, out int levelId))
        {
            ClearGoalTexts();

            if (_primaryGoalText)
                _primaryGoalText.text = "Objetivos no configurados.";

            TryCompleteResultsPresentation(_sequenceVersion);
            return;
        }

        SetupGoalTextsAndStrokes(config, levelId);

        _pendingStarAnimations = AnimateStars(config, levelId, _sequenceVersion);
        _pendingStrokeAnimations = 0;

        if (useStrokeAnimations)
        {
            List<Image> completedStrokeImages = BuildCompletedStrokeImages(config, levelId);
            _pendingStrokeAnimations = completedStrokeImages.Count;

            if (_pendingStrokeAnimations > 0)
                _objectiveStrokeCoroutine = StartCoroutine(AnimateObjectiveStrokes(completedStrokeImages, _sequenceVersion));
        }

        TryCompleteResultsPresentation(_sequenceVersion);
    }

    public void CancelResultsPresentation()
    {
        _sequenceVersion++;
        _isSequenceRunning = false;
        _sequenceCompletedCallback = null;
        _pendingStarAnimations = 0;
        _pendingStrokeAnimations = 0;

        if (_objectiveStrokeCoroutine != null)
        {
            StopCoroutine(_objectiveStrokeCoroutine);
            _objectiveStrokeCoroutine = null;
        }

        for (int i = _activePresentationTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = _activePresentationTweens[i];
            if (tween != null && tween.IsActive())
                tween.Kill();
        }

        _activePresentationTweens.Clear();
        KillAllStarTweens();
        KillAllStrokeTweens();
        StopPresentationSfx();
    }

    private void SetupGoalTextsAndStrokes(LevelConfiguration config, int levelId)
    {
        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText)
            _primaryGoalText.text = primary != null ? primary.description : "-";

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
                _secondaryGoalTexts[i].text = secondaries[i].description;
            else
                _secondaryGoalTexts[i].text = string.Empty;
        }

        if (_primaryGoalText && primary != null)
        {
            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
            var img = GetSlashImage(_primaryGoalText);
            if (img)
            {
                img.enabled = completed;
                if (completed) HideStrokeVisual(img);
            }
        }

        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;
            if (i >= secondaries.Length || secondaries[i] == null) continue;

            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            var img = GetSlashImage(_secondaryGoalTexts[i]);
            if (img)
            {
                img.enabled = completed;
                if (completed) HideStrokeVisual(img);
            }
        }
    }

    private IEnumerator AnimateObjectiveStrokes(List<Image> strokeImages, int sequenceVersion)
    {
        yield return new WaitForSecondsRealtime(strokeAnimationDelay);

        for (int i = 0; i < strokeImages.Count; i++)
        {
            if (!IsSequenceValid(sequenceVersion))
                yield break;

            AnimateStrokeImage(strokeImages[i], sequenceVersion);

            if (i < strokeImages.Count - 1)
                yield return new WaitForSecondsRealtime(strokeStagger);
        }

        if (sequenceVersion == _sequenceVersion)
            _objectiveStrokeCoroutine = null;
    }

    private void AnimateStrokeImage(Image slashImage, int sequenceVersion)
    {
        if (!slashImage) return;

        HideStrokeVisual(slashImage);
        DOTween.Kill(slashImage.transform, false);
        DOTween.Kill(slashImage, false);

        var seq = DOTween.Sequence().SetUpdate(true);
        TrackPresentationTween(seq);
        seq.Append(slashImage.transform.DOScaleX(1f, slashEffectDuration * 1.5f).SetEase(Ease.OutQuart));
        seq.Join(slashImage.DOFade(1f, slashEffectDuration * 1.2f));
        seq.AppendCallback(() =>
        {
            if (!IsSequenceValid(sequenceVersion))
                return;

            Tween punchTween = slashImage.transform
                .DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.8f)
                .SetUpdate(true);
            TrackPresentationTween(punchTween);
            punchTween.OnComplete(() => NotifyStrokeAnimationCompleted(sequenceVersion));
            AudioService.Instance?.PlaySFX(_audioContext.Audio.tapSplash);
        });
    }

    private void HideStrokeVisual(Image slashImage)
    {
        var c = slashImage.color;
        slashImage.color = new Color(c.r, c.g, c.b, 0f);
        slashImage.transform.localScale = new Vector3(0f, 1f, 1f);
    }

    private void ResetAllStrokesVisuals()
    {
        if (_primaryGoalText)
        {
            var img = GetSlashImage(_primaryGoalText);
            if (img) HideStrokeVisual(img);
        }

        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;
            var img = GetSlashImage(_secondaryGoalTexts[i]);
            if (img) HideStrokeVisual(img);
        }
    }

    private Image GetSlashImage(TextMeshProUGUI text)
    {
        return text ? text.GetComponentInChildren<Image>(true) : null;
    }

    private bool TryGetCurrentResultsConfiguration(out LevelConfiguration config, out int levelId)
    {
        levelId = GetLevelIdFromSceneName(SceneManager.GetActiveScene().name);
        var cfgMgr = LevelConfigurationManager.Instance;
        config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;
        return config != null;
    }

    private void ClearGoalTexts()
    {
        if (_primaryGoalText)
            _primaryGoalText.text = string.Empty;

        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (_secondaryGoalTexts[i])
                _secondaryGoalTexts[i].text = string.Empty;
        }
    }

    private void CacheOriginalStarPositions()
    {
        if (_starsContainer == null) return;
        int n = _starsContainer.childCount;
        _originalStarPositions = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            if (_starsContainer.GetChild(i) is RectTransform rt)
                _originalStarPositions[i] = rt.anchoredPosition;
        }
    }

    private void SetStarsToOffscreenPosition()
    {
        if (_starsContainer == null || _originalStarPositions == null) return;

        for (int i = 0; i < _starsContainer.childCount && i < _originalStarPositions.Length; i++)
        {
            if (!(_starsContainer.GetChild(i) is RectTransform rt)) continue;
            rt.anchoredPosition = _originalStarPositions[i] + new Vector2(_starOffscreenDistance, 0f);
            rt.rotation = Quaternion.identity;
            rt.localScale = Vector3.one;
        }
    }

    private void ResetStarSprites()
    {
        if (_starsContainer == null) return;
        for (int i = 0; i < _starsContainer.childCount; i++)
        {
            var img = _starsContainer.GetChild(i).GetComponent<Image>();
            if (img) img.sprite = _starNotAcquiredSprite;
        }
    }

    private int AnimateStars(LevelConfiguration config, int levelId, int sequenceVersion)
    {
        int animatedStars = 0;
        var primary = config.GetPrimaryObjective();
        bool primaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
        if (AnimateStar(0, primaryCompleted, 0f, sequenceVersion))
            animatedStars++;

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < secondaries.Length && i < _starsContainer.childCount - 1; i++)
        {
            bool secondaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            if (AnimateStar(i + 1, secondaryCompleted, (i + 1) * _starAnimationDelay, sequenceVersion))
                animatedStars++;
        }

        return animatedStars;
    }

    private bool AnimateStar(int starIndex, bool isCompleted, float delay, int sequenceVersion)
    {
        if (starIndex >= _starsContainer.childCount) return false;
        if (!(_starsContainer.GetChild(starIndex) is RectTransform rt)) return false;

        var targetPos = _originalStarPositions[starIndex];

        rt.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        DOTween.Kill(rt, false);

        var seq = DOTween.Sequence().SetUpdate(true);
        TrackPresentationTween(seq);
        seq.Append(rt.DOAnchorPos(targetPos, _starAnimationDuration).SetEase(_starAnimationEase).SetDelay(delay));
        seq.Join(rt.DORotate(new Vector3(0, 0, 360 * 3), _starAnimationDuration, RotateMode.FastBeyond360)
                 .SetEase(Ease.Linear).SetDelay(delay));
        seq.OnComplete(() =>
        {
            if (!IsSequenceValid(sequenceVersion))
                return;

            SetStarSprite(starIndex, isCompleted);
            rt.rotation = Quaternion.identity;
            Tween punchTween = rt
                .DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 0.5f)
                .SetUpdate(true);
            TrackPresentationTween(punchTween);
            punchTween.OnComplete(() => NotifyStarAnimationCompleted(sequenceVersion));
        });

        return true;
    }

    private void SetStarSprite(int idx, bool acquired)
    {
        var img = _starsContainer.GetChild(idx).GetComponent<Image>();
        if (!img) return;
        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
    }

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null && name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }

    private void KillAllStarTweens()
    {
        if (_starsContainer == null) return;
        for (int i = 0; i < _starsContainer.childCount; i++)
        {
            if (_starsContainer.GetChild(i) is RectTransform rt)
                DOTween.Kill(rt, false);
        }
    }

    private void BeginResultsPresentation(Action onSequenceCompleted)
    {
        CancelResultsPresentation();

        _sequenceCompletedCallback = onSequenceCompleted;
        _pendingStarAnimations = 0;
        _pendingStrokeAnimations = 0;
        _isSequenceRunning = true;
    }

    private List<Image> BuildCompletedStrokeImages(LevelConfiguration config, int levelId)
    {
        List<Image> completedStrokeImages = new List<Image>();

        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText && primary != null)
        {
            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
            var img = GetSlashImage(_primaryGoalText);
            if (completed && img && img.enabled)
                completedStrokeImages.Add(img);
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length && i < secondaries.Length; i++)
        {
            if (!_secondaryGoalTexts[i] || secondaries[i] == null)
                continue;

            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            var img = GetSlashImage(_secondaryGoalTexts[i]);
            if (completed && img && img.enabled)
                completedStrokeImages.Add(img);
        }

        return completedStrokeImages;
    }

    private void NotifyStarAnimationCompleted(int sequenceVersion)
    {
        if (!IsSequenceValid(sequenceVersion))
            return;

        _pendingStarAnimations = Mathf.Max(0, _pendingStarAnimations - 1);
        TryCompleteResultsPresentation(sequenceVersion);
    }

    private void NotifyStrokeAnimationCompleted(int sequenceVersion)
    {
        if (!IsSequenceValid(sequenceVersion))
            return;

        _pendingStrokeAnimations = Mathf.Max(0, _pendingStrokeAnimations - 1);
        TryCompleteResultsPresentation(sequenceVersion);
    }

    private void TryCompleteResultsPresentation(int sequenceVersion)
    {
        if (!IsSequenceValid(sequenceVersion))
            return;

        if (_pendingStarAnimations > 0 || _pendingStrokeAnimations > 0)
            return;

        _isSequenceRunning = false;

        Action callback = _sequenceCompletedCallback;
        _sequenceCompletedCallback = null;
        callback?.Invoke();
    }

    private bool IsSequenceValid(int sequenceVersion)
    {
        return _isSequenceRunning &&
               sequenceVersion == _sequenceVersion &&
               gameObject.activeInHierarchy;
    }

    private void TrackPresentationTween(Tween tween)
    {
        if (tween == null)
            return;

        _activePresentationTweens.Add(tween);
        tween.OnKill(() => _activePresentationTweens.Remove(tween));
    }

    private void KillAllStrokeTweens()
    {
        if (_primaryGoalText)
        {
            Image image = GetSlashImage(_primaryGoalText);
            if (image)
            {
                DOTween.Kill(image.transform, false);
                DOTween.Kill(image, false);
            }
        }

        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i])
                continue;

            Image image = GetSlashImage(_secondaryGoalTexts[i]);
            if (!image)
                continue;

            DOTween.Kill(image.transform, false);
            DOTween.Kill(image, false);
        }
    }

    private void StopPresentationSfx()
    {
        if (AudioService.Instance == null || _audioContext == null || _audioContext.Audio == null)
            return;

        if (_audioContext.Audio.tapSplash != null)
            AudioService.Instance.StopSFX(_audioContext.Audio.tapSplash);
    }

    protected override void OnDestroy()
    {
        CancelResultsPresentation();
        base.OnDestroy();
    }
}

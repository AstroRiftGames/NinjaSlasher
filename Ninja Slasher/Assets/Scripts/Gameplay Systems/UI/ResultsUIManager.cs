using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

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

    public override void Awake()
    {
        base.Awake();
        CacheOriginalStarPositions();
    }

    public void PrepareResultsIntro()
    {
        KillAllStarTweens();
        Canvas.ForceUpdateCanvases();

        if (_originalStarPositions == null || _originalStarPositions.Length == 0)
            CacheOriginalStarPositions();

        SetStarsToOffscreenPosition();
        ResetStarSprites();
        ResetAllStrokesVisuals();
    }

    public void ShowResultsPanel()
    {
        int levelId = GetLevelIdFromSceneName(SceneManager.GetActiveScene().name);
        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config == null)
        {
            if (_primaryGoalText) _primaryGoalText.text = "Goals not configured.";
            foreach (var t in _secondaryGoalTexts) if (t) t.text = string.Empty;
            return;
        }

        SetupGoalTextsAndStrokes(config, levelId);

        AnimateStars(config, levelId);

        if (useStrokeAnimations)
            StartCoroutine(AnimateObjectiveStrokes(config, levelId));
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

    private IEnumerator AnimateObjectiveStrokes(LevelConfiguration config, int levelId)
    {
        yield return new WaitForSeconds(strokeAnimationDelay);

        var toAnimate = new List<Image>();

        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText && primary != null)
        {
            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
            var img = GetSlashImage(_primaryGoalText);
            if (completed && img && img.enabled) toAnimate.Add(img);
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length && i < secondaries.Length; i++)
        {
            if (!_secondaryGoalTexts[i] || secondaries[i] == null) continue;
            bool completed = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            var img = GetSlashImage(_secondaryGoalTexts[i]);
            if (completed && img && img.enabled) toAnimate.Add(img);
        }

        for (int i = 0; i < toAnimate.Count; i++)
        {
            AnimateStrokeImage(toAnimate[i]);
            if (i < toAnimate.Count - 1) yield return new WaitForSeconds(strokeStagger);
        }
    }

    private void AnimateStrokeImage(Image slashImage)
    {
        if (!slashImage) return;

        HideStrokeVisual(slashImage);
        DOTween.Kill(slashImage.transform, false);
        DOTween.Kill(slashImage, false);

        var seq = DOTween.Sequence();
        seq.Append(slashImage.transform.DOScaleX(1f, slashEffectDuration * 1.5f).SetEase(Ease.OutQuart));
        seq.Join(slashImage.DOFade(1f, slashEffectDuration * 1.2f));
        seq.AppendCallback(() =>
        {
            slashImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.8f);
            AudioManager.Instance?.PlaySFX(SFXClip.UI_TapSplashScreen);
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

    private void AnimateStars(LevelConfiguration config, int levelId)
    {
        var primary = config.GetPrimaryObjective();
        bool primaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
        AnimateStar(0, primaryCompleted, 0f);

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < secondaries.Length && i < _starsContainer.childCount - 1; i++)
        {
            bool secondaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            AnimateStar(i + 1, secondaryCompleted, (i + 1) * _starAnimationDelay);
        }
    }

    private void AnimateStar(int starIndex, bool isCompleted, float delay)
    {
        if (starIndex >= _starsContainer.childCount) return;
        if (!(_starsContainer.GetChild(starIndex) is RectTransform rt)) return;

        var targetPos = _originalStarPositions[starIndex];

        rt.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        DOTween.Kill(rt, false);

        var seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPos(targetPos, _starAnimationDuration).SetEase(_starAnimationEase).SetDelay(delay));
        seq.Join(rt.DORotate(new Vector3(0, 0, 360 * 3), _starAnimationDuration, RotateMode.FastBeyond360)
                 .SetEase(Ease.Linear).SetDelay(delay));
        seq.OnComplete(() =>
        {
            SetStarSprite(starIndex, isCompleted);
            rt.rotation = Quaternion.identity;
            rt.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 0.5f);
            AudioManager.Instance?.PlaySFX(SFXClip.UI_Select);
        });
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
}

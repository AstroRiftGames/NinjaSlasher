using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private Ease _starAnimationEase = Ease.OutBounce;

    [Header("STROKE ANIMATION SETTINGS")]
    [SerializeField] private bool useStrokeAnimations = true;
    [SerializeField] private float strokeAnimationDelay = 0.3f;
    [SerializeField] private float slashEffectDuration = 0.4f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float strokeStagger = 0.5f;

    private bool isObjectiveComplete = false;
    private Vector2[] _originalStarPositions;

    public override void Awake()
    {
        base.Awake();
        CacheOriginalStarPositions();
    }

    public void ShowResultsPanel()
    {
        int levelId = GetLevelIdFromSceneName(SceneManager.GetActiveScene().name);

        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config == null)
        {
            _primaryGoalText.text = "Goals not configured.";
            foreach (var t in _secondaryGoalTexts) if (t) t.text = string.Empty;
            return;
        }

        SetupGoalTexts(config, levelId);
        AnimateStars(config, levelId);

        if (useStrokeAnimations)
        {
            StartCoroutine(AnimateObjectiveStrokes(config, levelId));
        }
    }

    private void SetupGoalTexts(LevelConfiguration config, int levelId)
    {
        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText)
        {
            _primaryGoalText.text = primary != null ? primary.description : "-";
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                _secondaryGoalTexts[i].text = secondaries[i].description;
            }
            else
            {
                _secondaryGoalTexts[i].text = string.Empty;
            }
        }

        SetupObjectiveCheckmarks(config, levelId);
    }

    private void SetupObjectiveCheckmarks(LevelConfiguration config, int levelId)
    {
        var primary = config.GetPrimaryObjective();
        isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;

        if (_primaryGoalText)
        {
            Image slashImage = _primaryGoalText.GetComponentInChildren<Image>();
            if (slashImage != null)
            {
                slashImage.enabled = isObjectiveComplete;
                if (useStrokeAnimations && isObjectiveComplete)
                {
                    HideStroke(slashImage);
                }
            }
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                Image slashImage = _secondaryGoalTexts[i].GetComponentInChildren<Image>();
                if (slashImage != null)
                {
                    slashImage.enabled = isObjectiveComplete;
                    if (useStrokeAnimations && isObjectiveComplete)
                    {
                        HideStroke(slashImage);
                    }
                }
            }
        }
    }

    private void HideStroke(Image slashImage)
    {
        Color originalColor = slashImage.color;
        slashImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        slashImage.transform.localScale = new Vector3(0f, 1f, 1f);
    }

    private IEnumerator AnimateObjectiveStrokes(LevelConfiguration config, int levelId)
    {
        yield return new WaitForSeconds(strokeAnimationDelay);

        List<(TextMeshProUGUI objective, bool completed)> objectiveList = new List<(TextMeshProUGUI, bool)>();

        var primary = config.GetPrimaryObjective();
        bool primaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
        if (_primaryGoalText != null)
        {
            objectiveList.Add((_primaryGoalText, primaryCompleted));
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length && i < secondaries.Length; i++)
        {
            if (_secondaryGoalTexts[i] != null && secondaries[i] != null)
            {
                bool secondaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                objectiveList.Add((_secondaryGoalTexts[i], secondaryCompleted));
            }
        }

        int strokeIndex = 0;
        foreach (var (objective, completed) in objectiveList)
        {
            if (completed)
            {
                AnimateStrokeForObjective(objective, strokeIndex);
                strokeIndex++;
                yield return new WaitForSeconds(strokeStagger);
            }
        }
    }

    private void AnimateStrokeForObjective(TextMeshProUGUI objectiveText, int strokeIndex)
    {
        Image slashImage = objectiveText.GetComponentInChildren<Image>();

        if (slashImage != null && slashImage.enabled)
        {
            Sequence slashSequence = DOTween.Sequence();

            slashSequence.Append(slashImage.transform.DOScaleX(1f, slashEffectDuration * 1.5f)
                .SetEase(Ease.OutQuart));

            slashSequence.Join(slashImage.DOFade(1f, slashEffectDuration * 1.2f));

            slashSequence.AppendCallback(() => {
                slashImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.8f);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(SFXClip.UI_TapSplashScreen);
                }
            });
        }
    }

    private void CacheOriginalStarPositions()
    {
        if (_starsContainer == null) return;

        int starCount = _starsContainer.childCount;
        _originalStarPositions = new Vector2[starCount];

        for (int i = 0; i < starCount; i++)
        {
            var rectTransform = _starsContainer.GetChild(i) as RectTransform;
            if (rectTransform != null)
            {
                _originalStarPositions[i] = rectTransform.anchoredPosition;
            }
        }
    }

    private void AnimateStars(LevelConfiguration config, int levelId)
    {
        SetStarsToOffscreenPosition();

        var primary = config.GetPrimaryObjective();
        bool primaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
        AnimateStar(0, primaryCompleted, 0f);

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < secondaries.Length && i < _starsContainer.childCount - 1; i++)
        {
            bool secondaryCompleted = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
            float delay = (i + 1) * _starAnimationDelay;
            AnimateStar(i + 1, secondaryCompleted, delay);
        }
    }

    private void SetStarsToOffscreenPosition()
    {
        for (int i = 0; i < _starsContainer.childCount; i++)
        {
            var rectTransform = _starsContainer.GetChild(i) as RectTransform;
            if (rectTransform != null)
            {
                Vector2 offscreenPos = _originalStarPositions[i] + new Vector2(_starOffscreenDistance, 0);
                rectTransform.anchoredPosition = offscreenPos;
            }
        }
    }

    private void AnimateStar(int starIndex, bool isCompleted, float delay)
    {
        if (starIndex >= _starsContainer.childCount) return;

        var rectTransform = _starsContainer.GetChild(starIndex) as RectTransform;
        if (rectTransform == null) return;

        var targetPosition = _originalStarPositions[starIndex];

        rectTransform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));

        var sequence = DOTween.Sequence();

        sequence.Append(rectTransform.DOAnchorPos(targetPosition, _starAnimationDuration)
            .SetEase(_starAnimationEase)
            .SetDelay(delay));

        sequence.Join(rectTransform.DORotate(new Vector3(0, 0, 360 * 3), _starAnimationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetDelay(delay));

        sequence.OnComplete(() => {
            SetStar(starIndex, isCompleted);

            rectTransform.rotation = Quaternion.identity;

            rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 10, 0.5f);
        });
    }

    private void SetStar(int idx, bool acquired)
    {
        var img = _starsContainer.GetChild(idx).GetComponent<Image>();
        if (!img) return;
        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
    }

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

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
            _primaryGoalText.GetComponentInChildren<Image>().enabled = isObjectiveComplete;
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = isObjectiveComplete;
            }
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
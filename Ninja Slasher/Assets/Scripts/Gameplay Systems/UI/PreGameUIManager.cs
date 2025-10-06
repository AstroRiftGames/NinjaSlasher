using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PreGameUIManager : MonoBehaviour
{
    [Header("PREGAME")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _pendingSceneName;
    [SerializeField] private Transform _powerUpsContainer;
    [SerializeField] private PowerUpSlotUI _powerUpSlotPrefab;
    [SerializeField] private PowerUpBase[] allPowerUpBases;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private Transform _starsContainer;
    [SerializeField] private TextMeshProUGUI _primaryGoalText;
    [SerializeField] private TextMeshProUGUI[] _secondaryGoalTexts;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    [Header("NINJA TEXT ANIMATIONS")]
    [SerializeField] private bool useNinjaAnimations = true;
    [SerializeField] private float titleAnimationDelay = 0.6f;
    [SerializeField] private float characterDelay = 0.05f;
    [SerializeField] private float titleAnimationDuration = 0.8f;
    [SerializeField] private float objectiveDelay = 0.3f;
    [SerializeField] private float slashEffectDuration = 0.4f;
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private float objectiveStagger = 0.1f;

    private bool isObjectiveComplete = false;
    private List<PowerUpSlotUI> _slots = new();
    private Dictionary<TextMeshProUGUI, string> originalTexts = new Dictionary<TextMeshProUGUI, string>();
    private List<Sequence> activeSequences = new List<Sequence>();

    private void Awake()
    {
        CacheOriginalTexts();
    }

    private void CacheOriginalTexts()
    {
        if (_title != null) originalTexts[_title] = _title.text;
        if (_primaryGoalText != null) originalTexts[_primaryGoalText] = _primaryGoalText.text;

        foreach (var txt in _secondaryGoalTexts)
        {
            if (txt != null) originalTexts[txt] = txt.text;
        }
    }

    public void ShowConfirmationPanel(string sceneName)
    {
        _pendingSceneName = sceneName;
        UIManager.Instance.ShowHidePreGameCanvas();

        _playButton.onClick.RemoveAllListeners();
        _playButton.onClick.AddListener(OnConfirmLevelSelection);

        _closeButton.onClick.RemoveAllListeners();
        _closeButton.onClick.AddListener(CancelLevelSelection);

        ShowPreGameTitle();
        SetGoals();
        ShowPreGamePowerUps();

        if (useNinjaAnimations)
        {
            StartCoroutine(DelayedAnimations());
        }
    }

    private IEnumerator DelayedAnimations()
    {
        if (_title != null)
        {
            Color titleColor = _title.color;
            _title.color = new Color(titleColor.r, titleColor.g, titleColor.b, 0f);
        }

        HideAllObjectiveStrokes();

        yield return new WaitForSeconds(titleAnimationDelay);

        StartCoroutine(AnimateTitle());

        yield return new WaitForSeconds(0.15f);
        StartCoroutine(AnimateObjectives());
    }

    private IEnumerator AnimateTitle()
    {
        if (_title == null) yield break;

        Color originalColor = _title.color;
        originalColor.a = 1f;

        _title.DOColor(originalColor, flashDuration * 0.5f);
        _title.transform.DOPunchScale(Vector3.one * 0.2f, flashDuration, 1, 0.8f);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
        }

        yield return new WaitForSeconds(flashDuration);
    }

    private IEnumerator AnimateObjectives()
    {
        List<TextMeshProUGUI> objectiveList = new List<TextMeshProUGUI>();

        if (_primaryGoalText != null) objectiveList.Add(_primaryGoalText);
        objectiveList.AddRange(_secondaryGoalTexts.Where(t => t != null && !string.IsNullOrEmpty(t.text)));

        foreach (var obj in objectiveList)
        {
            Color color = obj.color;
            obj.color = new Color(color.r, color.g, color.b, 0f);
            obj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        }

        for (int i = 0; i < objectiveList.Count; i++)
        {
            var objective = objectiveList[i];
            Color originalColor = objective.color;
            originalColor.a = 1f;

            objective.DOColor(originalColor, flashDuration * 0.7f);
            objective.transform.DOScale(Vector3.one, flashDuration * 0.7f)
                .SetEase(Ease.OutBack);
            objective.transform.DOPunchPosition(new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, 0), flashDuration, 1, 0.5f);

            AnimateObjectiveIfCompleted(objective, i);

            if (AudioManager.Instance != null && i % 2 == 0)
            {
                AudioManager.Instance.PlaySFX(SFXClip.UI_Select);
            }

            yield return new WaitForSeconds(objectiveStagger);
        }
    }

    private void AnimateObjectiveIfCompleted(TextMeshProUGUI objectiveText, int strokeIndex = 0)
    {
        Image slashImage = objectiveText.GetComponentInChildren<Image>();

        if (slashImage != null && slashImage.enabled)
        {
            Sequence slashSequence = DOTween.Sequence();
            activeSequences.Add(slashSequence);

            float incrementalDelay = flashDuration * 2.5f + (strokeIndex * 0.5f);
            slashSequence.AppendInterval(incrementalDelay);

            slashSequence.Append(slashImage.transform.DOScaleX(1f, flashDuration * 1.5f)
                .SetEase(Ease.OutQuart));

            slashSequence.Join(slashImage.DOFade(1f, flashDuration * 1.2f));

            slashSequence.AppendCallback(() => {
                if (slashImage != null && slashImage.gameObject != null)
                {
                    slashImage.transform.DOPunchScale(Vector3.one * 0.1f, 0.1f, 1, 0.8f);

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX(SFXClip.UI_TapSplashScreen);
                    }
                }
            });

            slashSequence.OnKill(() => {
                activeSequences.Remove(slashSequence);
            });
        }
    }

    private void HideAllObjectiveStrokes()
    {
        List<TextMeshProUGUI> allObjectives = new List<TextMeshProUGUI>();

        if (_primaryGoalText != null) allObjectives.Add(_primaryGoalText);
        allObjectives.AddRange(_secondaryGoalTexts.Where(t => t != null));

        foreach (var objective in allObjectives)
        {
            Image slashImage = objective.GetComponentInChildren<Image>();
            if (slashImage != null)
            {
                Color originalColor = slashImage.color;
                slashImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                slashImage.transform.localScale = new Vector3(0f, 1f, 1f);
            }
        }
    }

    public void ResetTexts()
    {
        foreach (var kvp in originalTexts)
        {
            if (kvp.Key != null)
            {
                kvp.Key.text = kvp.Value;
                kvp.Key.transform.localScale = Vector3.one;
                kvp.Key.transform.rotation = Quaternion.identity;
                Color color = kvp.Key.color;
                kvp.Key.color = new Color(color.r, color.g, color.b, 1f);

                Image slashImage = kvp.Key.GetComponentInChildren<Image>();
                if (slashImage != null)
                {
                    slashImage.transform.localScale = Vector3.one;
                    Color slashColor = slashImage.color;
                    float alpha = slashImage.enabled ? 1f : 0f;
                    slashImage.color = new Color(slashColor.r, slashColor.g, slashColor.b, alpha);
                }
            }
        }
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();

        foreach (var sequence in activeSequences.ToList())
        {
            if (sequence != null && sequence.IsActive())
            {
                sequence.Kill(false);
            }
        }
        activeSequences.Clear();

        if (_title != null)
        {
            DOTween.Kill(_title);
            DOTween.Kill(_title.transform);
        }

        List<TextMeshProUGUI> allObjectives = new List<TextMeshProUGUI>();
        if (_primaryGoalText != null) allObjectives.Add(_primaryGoalText);
        allObjectives.AddRange(_secondaryGoalTexts.Where(t => t != null));

        foreach (var objective in allObjectives)
        {
            DOTween.Kill(objective);
            DOTween.Kill(objective.transform);

            Image slashImage = objective.GetComponentInChildren<Image>();
            if (slashImage != null)
            {
                DOTween.Kill(slashImage);
                DOTween.Kill(slashImage.transform);
            }
        }
    }

    private void OnEnable()
    {
        DailyRewardSystem.OnRewardClaimed += OnDailyRewardClaimedRefresh;
    }

    private void OnDisable()
    {
        DailyRewardSystem.OnRewardClaimed -= OnDailyRewardClaimedRefresh;
        StopAllAnimations();
    }

    private void OnDailyRewardClaimedRefresh(DailyReward _)
    {
        ShowPreGamePowerUps();
    }

    private void OnConfirmLevelSelection()
    {
        if (!LifeManager.Instance.CanPlay())
        {
            GetComponent<GameplayUIManager>().ShowNoLivesPanel();
            return;
        }

        StopAllAnimations();

        UIManager.Instance.ShowHidePreGameCanvas();
        UIManager.Instance.LoadLevelScene(_pendingSceneName);
        PlayLevelMusic();
    }

    private void CancelLevelSelection()
    {
        StopAllAnimations();
        UIManager.Instance.ShowHidePreGameCanvas();
        _pendingSceneName = null;
    }

    void ShowPreGameTitle()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);

        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;
        _title.text = config.levelName;

        if (originalTexts.ContainsKey(_title))
            originalTexts[_title] = _title.text;
    }

    public void ShowPreGamePowerUps()
    {
        foreach (var slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();

        var inventory = SaveManager.Instance.GetGameData().powerUpInventory;

        foreach (var powerUpBase in allPowerUpBases)
        {
            var item = inventory.Find(i => i.type == powerUpBase.powerUpType);
            if (item == null)
                item = new PowerUpInventoryItem(powerUpBase.powerUpType, 0);

            var slot = Instantiate(_powerUpSlotPrefab, _powerUpsContainer);
            slot.Setup(item, powerUpBase, OnPowerUpActivateClicked);
            _slots.Add(slot);
        }
    }

    private void OnPowerUpActivateClicked(PowerUpInventoryItem item)
    {
        if (item.quantity <= 0) return;

        var powerUpBase = GetPowerUpBaseByType(item.type);
        float duration = powerUpBase != null ? powerUpBase.duration : 3600f;

        if (PowerUpManager.Instance.ActivatePowerUpFromInventory(item.type, duration))
        {
            ShowPreGamePowerUps();
        }
    }

    private PowerUpBase GetPowerUpBaseByType(PowerUpType type)
    {
        foreach (var pu in allPowerUpBases)
            if (pu.powerUpType == type)
                return pu;
        return null;
    }

    private void SetGoals()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);

        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config == null)
        {
            _primaryGoalText.text = "Goals not configured.";
            foreach (var t in _secondaryGoalTexts) if (t) t.text = string.Empty;
            return;
        }

        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText)
        {
            _primaryGoalText.text = primary != null ? primary.description : "-";
            if (originalTexts.ContainsKey(_primaryGoalText))
                originalTexts[_primaryGoalText] = _primaryGoalText.text;
        }

        isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;

        if (isObjectiveComplete)
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = true;
            SetStar(0, true);
        }
        else
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = false;
            SetStar(0, false);
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                _secondaryGoalTexts[i].text = secondaries[i].description;
                if (originalTexts.ContainsKey(_secondaryGoalTexts[i]))
                    originalTexts[_secondaryGoalTexts[i]] = _secondaryGoalTexts[i].text;

                isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                if (isObjectiveComplete)
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = true;
                    SetStar(i + 1, true);
                }
                else
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = false;
                    SetStar(i + 1, false);
                }
            }
            else
            {
                _secondaryGoalTexts[i].text = string.Empty;
            }
        }
    }

    private void SetStar(int idx, bool acquired)
    {
        var img = _starsContainer.GetChild(idx).GetComponent<Image>();
        if (!img) return;

        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
    }

    void PlayLevelMusic()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);
        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config.unlockRequirements.isBossLevel)
        {
            AudioManager.Instance.PlayMusic(MusicClip.BossLevel);
            return;
        }

        switch (config.unlockRequirements.areaId)
        {
            case 1:
                AudioManager.Instance.PlayMusic(MusicClip.Area1);
                break;
            case 2:
                AudioManager.Instance.PlayMusic(MusicClip.Area2);
                break;
            case 3:
                AudioManager.Instance.PlayMusic(MusicClip.Area3);
                break;
            case 4:
                AudioManager.Instance.PlayMusic(MusicClip.Area4);
                break;
            case 5:
                AudioManager.Instance.PlayMusic(MusicClip.Area5);
                break;
        }
    }

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}
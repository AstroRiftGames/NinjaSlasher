using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private bool isObjectiveComplete = false;
    private List<PowerUpSlotUI> _slots = new();

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
    }

    private void OnEnable()
    {
        DailyRewardSystem.OnRewardClaimed += OnDailyRewardClaimedRefresh;
    }

    private void OnDisable()
    {
        DailyRewardSystem.OnRewardClaimed -= OnDailyRewardClaimedRefresh;
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
        UIManager.Instance.ShowHidePreGameCanvas();
        UIManager.Instance.LoadLevelScene(_pendingSceneName);
        PlayLevelMusic();        
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

    private void CancelLevelSelection()
    {
        UIManager.Instance.ShowHidePreGameCanvas();
        _pendingSceneName = null;
    }

    void ShowPreGameTitle()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);

        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;
        _title.text = config.levelName;
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

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}

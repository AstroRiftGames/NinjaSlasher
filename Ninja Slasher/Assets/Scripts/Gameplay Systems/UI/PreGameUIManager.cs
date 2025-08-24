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
    [SerializeField] private Color _starNotAcquired = Color.black;
    [SerializeField] private Color _starAcquired = Color.yellow;

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
            _starsContainer.GetChild(0).GetComponent<Image>().color = _starAcquired;
        }
        else
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = false;
            _starsContainer.GetChild(0).GetComponent<Image>().color = _starNotAcquired;
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
                    _starsContainer.GetChild(i + 1).GetComponent<Image>().color = _starAcquired;
                }
                else
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = false;
                    _starsContainer.GetChild(i + 1).GetComponent<Image>().color = _starNotAcquired;
                }
            }
            else
            {
                _secondaryGoalTexts[i].text = string.Empty;
            }
        }
    }

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}

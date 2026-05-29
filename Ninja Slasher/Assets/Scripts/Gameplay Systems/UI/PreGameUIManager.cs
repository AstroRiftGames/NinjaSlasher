using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreGameUIManager : MonoBehaviour
{
    [Header("PREGAME")]
    [SerializeField] private Button _playButton;
    [SerializeField] private string _pendingSceneName;
    [SerializeField] private Transform _powerUpsContainer;
    [SerializeField] private PowerUpSlotUI _powerUpSlotPrefab;
    [SerializeField] private PowerUpBase[] allPowerUpBases;
    [SerializeField] private PowerUpConfirmationPopUp _powerUpConfirmationPopUp;
    [SerializeField] private TextMeshProUGUI _title;
    [SerializeField] private TextMeshProUGUI _primaryGoalText;
    [SerializeField] private TextMeshProUGUI[] _secondaryGoalTexts;
    [SerializeField] private TextMeshProUGUI _bossLoreText;
    [SerializeField] private Transform _goalsContainer;
    [SerializeField] private Transform _bossLoreContainer;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    [Header("OBJECTIVE REFERENCES")]
    [SerializeField] private Transform _objective1Container;
    [SerializeField] private Transform _objective2Container;
    [SerializeField] private Transform _objective3Container;

    [Header("NINJA TEXT ANIMATIONS")]
    [SerializeField] private bool useNinjaAnimations = true;
    [SerializeField] private float objectiveDelay = 0.3f;
    [SerializeField] private float objectiveStagger = 0.1f;
    [SerializeField] private float objectiveBaseStepDelay = 0.025f;
    [SerializeField] private float objectiveWordPauseDelay = 0.055f;
    [SerializeField] private float objectiveFlashDuration = 0.08f;
    [SerializeField] private float slashEffectDuration = 0.4f;
    [SerializeField] private int objectiveBurstSize = 2;

    private readonly Dictionary<TextMeshProUGUI, Color> _baseTextColors = new();
    private readonly Dictionary<TextMeshProUGUI, bool> _objectiveCompletionStates = new();
    private readonly Dictionary<TextMeshProUGUI, Image> _objectiveSlashImages = new();
    private readonly Dictionary<Image, Vector2> _objectiveSlashBasePositions = new();
    private readonly Dictionary<Image, Color> _objectiveSlashBaseColors = new();
    private readonly Dictionary<Image, bool> _objectiveSlashBaseEnabled = new();
    private readonly List<Sequence> _activeSequences = new();
    private readonly List<PowerUpSlotUI> _powerUpSlots = new();

    private UIAudioContext _audioContext;
    private PowerUpHorizontalScrollButtons _powerUpScrollButtons;
    private bool _isLevelSelected;
    private bool _isInPreGameSelection;
    private bool _isPurchasing;
    private bool _isPlaying;
    private readonly PregamePowerUpSelection _pregameSelection = new PregamePowerUpSelection();

    private void Awake()
    {
        if (_powerUpConfirmationPopUp == null)
            _powerUpConfirmationPopUp = GetComponentInChildren<PowerUpConfirmationPopUp>(true);

        _audioContext = GetComponentInParent<UIAudioContext>();

        ResolveGoalTextReferencesIfNeeded();
        CacheBaseVisualState();
        SetupButtonListeners();
        ResetVisualState();
    }

    private void OnEnable()
    {
        GameEvents.OnRewardClaimed += OnDailyRewardClaimedRefresh;
        GameEvents.OnLivesChanged += OnLivesChangedRefresh;
    }

    private void OnDisable()
    {
        GameEvents.OnRewardClaimed -= OnDailyRewardClaimedRefresh;
        GameEvents.OnLivesChanged -= OnLivesChangedRefresh;
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();
        _isPlaying = false;
    }

    private void SetupButtonListeners()
    {
        _playButton.onClick.RemoveListener(OnPlayButtonClicked);
        _playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void EnsurePlayButtonBound()
    {
        if (_playButton == null)
            return;

        _playButton.onClick.RemoveListener(OnPlayButtonClicked);
        _playButton.onClick.AddListener(OnPlayButtonClicked);
        _isInPreGameSelection = _isLevelSelected;
    }

    private void CacheBaseVisualState()
    {
        if (_primaryGoalText != null)
        {
            _baseTextColors[_primaryGoalText] = _primaryGoalText.color;
            CacheObjectiveSlashBaseline(_primaryGoalText);
        }

        foreach (TextMeshProUGUI goalText in _secondaryGoalTexts)
        {
            if (goalText == null)
                continue;

            _baseTextColors[goalText] = goalText.color;
            CacheObjectiveSlashBaseline(goalText);
        }
    }

    private void ResolveGoalTextReferencesIfNeeded()
    {
        PregameModal pregameModal = GetComponentInChildren<PregameModal>(true);
        if (pregameModal == null)
            return;

        if (_title == null)
            _title = FindNamedText(pregameModal.transform, "LevelTitle");

        if (_objective1Container == null)
            _objective1Container = FindDescendantByName(pregameModal.transform, "Objective 1");
        if (_objective2Container == null)
            _objective2Container = FindDescendantByName(pregameModal.transform, "Objective 2");
        if (_objective3Container == null)
            _objective3Container = FindDescendantByName(pregameModal.transform, "Objective 3");

        Transform[] objectiveContainers = new Transform[] { _objective1Container, _objective2Container, _objective3Container };
        List<TextMeshProUGUI> foundTexts = new List<TextMeshProUGUI>();

        foreach (Transform container in objectiveContainers)
        {
            if (container == null)
                continue;

            TextMeshProUGUI text = container.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                foundTexts.Add(text);
        }

        if (foundTexts.Count == 0)
            return;

        if (_primaryGoalText == null)
            _primaryGoalText = foundTexts[0];

        int secondaryCount = Mathf.Max(0, foundTexts.Count - 1);
        if (_secondaryGoalTexts == null || _secondaryGoalTexts.Length != secondaryCount)
            _secondaryGoalTexts = new TextMeshProUGUI[secondaryCount];

        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (_secondaryGoalTexts[i] == null && i + 1 < foundTexts.Count)
                _secondaryGoalTexts[i] = foundTexts[i + 1];
        }
    }

    private static TextMeshProUGUI FindNamedText(Transform root, string objectName)
    {
        Transform target = FindDescendantByName(root, objectName);
        return target != null ? target.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Transform FindDescendantByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    private void CacheObjectiveSlashBaseline(TextMeshProUGUI objectiveText)
    {
        if (objectiveText == null)
            return;

        Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
        if (slashImage == null)
            return;

        _objectiveSlashImages[objectiveText] = slashImage;
        _objectiveSlashBasePositions[slashImage] = slashImage.rectTransform.anchoredPosition;
        _objectiveSlashBaseColors[slashImage] = slashImage.color;
        _objectiveSlashBaseEnabled[slashImage] = slashImage.enabled;
    }

    private RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public void ShowConfirmationPanel(string sceneName)
    {
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();

        _pregameSelection.Clear();
        _isPlaying = false;

        ResolveGoalTextReferencesIfNeeded();

        _pendingSceneName = sceneName;
        _isLevelSelected = true;
        _isInPreGameSelection = true;

        UIEvents.RequestShowPregameModal();

        ShowPreGameTitle();
        SetGoals();
        RefreshObjectiveSlashBaselines();
        ResetVisualState();
        ShowPreGamePowerUps(true);
        ApplyObjectiveCompletionVisuals();
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();

        for (int i = _activeSequences.Count - 1; i >= 0; i--)
        {
            Sequence sequence = _activeSequences[i];
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }
        _activeSequences.Clear();

        if (_title != null)
            DOTween.Kill(_title);

        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            DOTween.Kill(objectiveText);

            Image slashImage = GetObjectiveSlashImage(objectiveText);
            if (slashImage != null)
            {
                DOTween.Kill(slashImage);
                DOTween.Kill(slashImage.rectTransform);
            }
        }

        ResetVisualState();
    }

    private void ResetVisualState()
    {
        ResetObjectiveVisualState();
    }

    private void ResetObjectiveVisualState()
    {
        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            objectiveText.maxVisibleCharacters = int.MaxValue;

            if (_baseTextColors.TryGetValue(objectiveText, out Color baseColor))
                objectiveText.color = baseColor;

            Image slashImage = GetObjectiveSlashImage(objectiveText);
            if (slashImage != null)
            {
                RestoreObjectiveSlashBaseline(slashImage);
                slashImage.enabled = false;
            }
        }
    }

    private void ApplyObjectiveCompletionVisuals()
    {
        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            Image slashImage = GetObjectiveSlashImage(objectiveText);
            ApplyObjectiveSlashCompletionState(objectiveText, slashImage);
        }
    }

    private void ApplyObjectiveSlashCompletionState(TextMeshProUGUI objectiveText, Image slashImage)
    {
        if (slashImage == null)
            return;

        RestoreObjectiveSlashBaseline(slashImage);
        bool shouldShowSlash = _objectiveCompletionStates.TryGetValue(objectiveText, out bool isCompleted) && isCompleted;
        slashImage.enabled = shouldShowSlash;
    }

    private void RestoreObjectiveSlashBaseline(Image slashImage)
    {
        if (slashImage == null)
            return;

        if (_objectiveSlashBasePositions.TryGetValue(slashImage, out Vector2 basePosition))
            slashImage.rectTransform.anchoredPosition = basePosition;

        if (_objectiveSlashBaseColors.TryGetValue(slashImage, out Color baseColor))
            slashImage.color = baseColor;

        if (_objectiveSlashBaseEnabled.TryGetValue(slashImage, out bool baseEnabled))
            slashImage.enabled = baseEnabled;
    }

    private IEnumerable<TextMeshProUGUI> GetObjectiveTexts()
    {
        if (_primaryGoalText != null)
            yield return _primaryGoalText;

        foreach (TextMeshProUGUI objectiveText in _secondaryGoalTexts)
        {
            if (objectiveText != null)
                yield return objectiveText;
        }
    }

    private void OnPlayButtonClicked()
    {
        if (_isInPreGameSelection && _isLevelSelected)
        {
            OnConfirmLevelSelection();
            return;
        }

        UIEvents.RequestHidePregameModal();
    }

    private void OnConfirmLevelSelection()
    {
        if (_isPlaying)
            return;

        bool canStartLevel = LevelSessionManager.Instance != null
            ? LevelSessionManager.Instance.TryAuthorizeLevelAttempt()
            : LifeManager.Instance != null && LifeManager.Instance.CanPlay();

        if (!canStartLevel)
            return;

        _isPlaying = true;

        if (!TryConsumeSelectedPowerUps())
        {
            _isPlaying = false;
            return;
        }

        StopAllAnimations();
        _isInPreGameSelection = false;
        UIEvents.RequestSceneTransition(_pendingSceneName);
    }

    private void AbortPendingLevelSelectionForLifeWall()
    {
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();
    }

    private void OnDailyRewardClaimedRefresh(DailyReward _)
    {
        ShowPreGamePowerUps();
    }

    public void RefreshPreGameAfterAdClaim()
    {
        if (!_isLevelSelected)
            return;

        _isInPreGameSelection = true;
        ShowPreGamePowerUps();
        ApplyObjectiveCompletionVisuals();
        EnsurePlayButtonBound();
    }

    private void OnLivesChangedRefresh(int lives)
    {
        if (!_isLevelSelected)
            return;

        ShowPreGamePowerUps();
        EnsurePlayButtonBound();
    }

    private void ShowPreGameTitle()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);
        LevelConfigurationManager cfgMgr = LevelConfigurationManager.Instance;
        LevelConfiguration config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        bool isBossLevel = config?.unlockRequirements?.isBossLevel ?? false;
        BossPreGameData bossData = config?.bossPreGameData;
        bool showBossLore = isBossLevel && bossData != null && !string.IsNullOrEmpty(bossData.loreDescription);

        if (showBossLore)
        {
            if (_title != null)
                _title.text = bossData.bossName;

            if (_bossLoreText != null)
                _bossLoreText.text = bossData.loreDescription;

            if (_goalsContainer != null)
                _goalsContainer.gameObject.SetActive(false);

            if (_bossLoreContainer != null)
                _bossLoreContainer.gameObject.SetActive(true);
        }
        else
        {
            if (_title != null)
                _title.text = config != null ? config.levelName : string.Empty;

            if (_bossLoreText != null)
                _bossLoreText.text = string.Empty;

            if (_goalsContainer != null)
                _goalsContainer.gameObject.SetActive(true);

            if (_bossLoreContainer != null)
                _bossLoreContainer.gameObject.SetActive(false);
        }
    }

    public void ShowPreGamePowerUps(bool resetScrollToStart = false)
    {
        PowerUpHorizontalScrollButtons powerUpScrollButtons = ResolvePowerUpScrollButtonsIfNeeded();

        if (_powerUpsContainer == null)
        {
            Debug.LogWarning("[PreGameUIManager] Power ups container not assigned.");
            DisableUnusedPowerUpSlots(0);
            FinalizePowerUpScrollRefresh(powerUpScrollButtons, resetScrollToStart);
            return;
        }

        if (_powerUpSlotPrefab == null)
        {
            Debug.LogWarning("[PreGameUIManager] Power up slot prefab not assigned.");
            DisableUnusedPowerUpSlots(0);
            FinalizePowerUpScrollRefresh(powerUpScrollButtons, resetScrollToStart);
            return;
        }

        if (allPowerUpBases == null || allPowerUpBases.Length == 0)
        {
            DisableUnusedPowerUpSlots(0);
            FinalizePowerUpScrollRefresh(powerUpScrollButtons, resetScrollToStart);
            return;
        }

        List<PowerUpInventoryItem> inventory = SaveManager.Instance?.GetGameData()?.powerUpInventory;
        int visibleSlotCount = 0;

        foreach (PowerUpBase powerUpBase in allPowerUpBases)
        {
            if (powerUpBase == null)
            {
                Debug.LogWarning("[PreGameUIManager] Null power up base found in pregame list.");
                continue;
            }

            PowerUpSlotUI slot = GetOrCreatePowerUpSlot(visibleSlotCount);
            if (slot == null)
                break;

            PowerUpInventoryItem item = null;
            if (inventory != null)
            {
                for (int j = 0; j < inventory.Count; j++)
                {
                    if (inventory[j].type == powerUpBase.powerUpType)
                    {
                        item = inventory[j];
                        break;
                    }
                }
            }
            if (item == null)
                item = new PowerUpInventoryItem(powerUpBase.powerUpType, 0);

            slot.gameObject.SetActive(true);
            slot.transform.SetSiblingIndex(visibleSlotCount);
            slot.Setup(item, powerUpBase, OnPowerUpInteractClicked);
            slot.SetPregameSelected(_pregameSelection.IsSelected(powerUpBase.powerUpType));
            visibleSlotCount++;
        }

        DisableUnusedPowerUpSlots(visibleSlotCount);
        FinalizePowerUpScrollRefresh(powerUpScrollButtons, resetScrollToStart);
    }

    private PowerUpHorizontalScrollButtons ResolvePowerUpScrollButtonsIfNeeded()
    {
        if (_powerUpScrollButtons != null)
            return _powerUpScrollButtons;

        if (_powerUpsContainer != null)
            _powerUpScrollButtons = _powerUpsContainer.GetComponentInParent<PowerUpHorizontalScrollButtons>();

        if (_powerUpScrollButtons == null)
        {
            PregameModal pregameModal = GetComponentInChildren<PregameModal>(true);
            if (pregameModal != null)
                _powerUpScrollButtons = pregameModal.GetComponentInChildren<PowerUpHorizontalScrollButtons>(true);
        }

        return _powerUpScrollButtons;
    }

    private static void FinalizePowerUpScrollRefresh(PowerUpHorizontalScrollButtons powerUpScrollButtons, bool resetScrollToStart)
    {
        if (powerUpScrollButtons == null)
            return;

        if (resetScrollToStart)
            powerUpScrollButtons.ResetToStart();

        powerUpScrollButtons.RefreshAfterLayout();
    }

    private PowerUpSlotUI GetOrCreatePowerUpSlot(int index)
    {
        while (_powerUpSlots.Count <= index)
        {
            PowerUpSlotUI newSlot = Instantiate(_powerUpSlotPrefab, _powerUpsContainer);
            if (newSlot == null)
            {
                Debug.LogWarning("[PreGameUIManager] Failed to instantiate power up slot.");
                return null;
            }

            newSlot.gameObject.SetActive(false);
            _powerUpSlots.Add(newSlot);
        }

        PowerUpSlotUI slot = _powerUpSlots[index];
        if (slot != null)
            return slot;

        PowerUpSlotUI replacementSlot = Instantiate(_powerUpSlotPrefab, _powerUpsContainer);
        if (replacementSlot == null)
        {
            Debug.LogWarning("[PreGameUIManager] Failed to replace missing power up slot.");
            return null;
        }

        replacementSlot.gameObject.SetActive(false);
        _powerUpSlots[index] = replacementSlot;
        return replacementSlot;
    }

    private void DisableUnusedPowerUpSlots(int usedSlotCount)
    {
        for (int i = usedSlotCount; i < _powerUpSlots.Count; i++)
        {
            PowerUpSlotUI slot = _powerUpSlots[i];
            if (slot == null)
                continue;

            slot.Clear();
            slot.gameObject.SetActive(false);
        }
    }

    private void OnPowerUpInteractClicked(PowerUpInventoryItem item, PowerUpBase powerUpBase)
    {
        if (item == null || powerUpBase == null)
            return;

        if (_powerUpConfirmationPopUp == null)
        {
            Debug.LogWarning("[PreGameUIManager] PowerUpConfirmationPopUp not assigned. Falling back to direct activation.");
            HandlePowerUpPrimaryAction(item);
            return;
        }

        _powerUpConfirmationPopUp.ShowConfirmation(BuildPowerUpConfirmationRequest(item, powerUpBase));
    }

    private void OnPowerUpPurchaseClicked(PowerUpInventoryItem item)
    {
        if (_isPurchasing) return;
        _isPurchasing = true;

        int cost = GetCostForType(item.type);
        PurchaseResult result = PowerUpPurchaseService.Purchase(item.type, cost);

        if (result == PurchaseResult.Success)
        {
            GameEvents.RaisePowerUpPurchased(item.type);
            ShowPreGamePowerUps();
        }

        _isPurchasing = false;
    }

    private int GetCostForType(PowerUpType type)
    {
        for (int i = 0; i < allPowerUpBases.Length; i++)
        {
            if (allPowerUpBases[i].powerUpType == type)
                return allPowerUpBases[i].cost;
        }
        return 0;
    }

    private void SetGoals()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);
        LevelConfigurationManager cfgMgr = LevelConfigurationManager.Instance;
        LevelConfiguration config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config == null)
        {
            if (_primaryGoalText != null)
            {
                _primaryGoalText.text = "Objetivos no configurados.";
                SetObjectiveCompletionVisual(_primaryGoalText, 0, false);
            }

            for (int i = 0; i < _secondaryGoalTexts.Length; i++)
            {
                if (_secondaryGoalTexts[i] == null)
                    continue;

                _secondaryGoalTexts[i].text = string.Empty;
                SetObjectiveCompletionVisual(_secondaryGoalTexts[i], i + 1, false);
            }
            return;
        }

        ObjectiveData primary = config.GetPrimaryObjective();
        if (_primaryGoalText != null)
        {
            _primaryGoalText.text = primary != null ? primary.description : "-";
            bool isPrimaryComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;
            SetObjectiveCompletionVisual(_primaryGoalText, 0, isPrimaryComplete);
        }

        ObjectiveData[] secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            TextMeshProUGUI objectiveText = _secondaryGoalTexts[i];
            if (objectiveText == null)
                continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                objectiveText.text = secondaries[i].description;
                bool isSecondaryComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                SetObjectiveCompletionVisual(objectiveText, i + 1, isSecondaryComplete);
            }
            else
            {
                objectiveText.text = string.Empty;
                SetObjectiveCompletionVisual(objectiveText, i + 1, false);
            }
        }
    }

    private void RefreshObjectiveSlashBaselines()
    {
        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
            CacheObjectiveSlashBaseline(objectiveText);
    }

    private void SetObjectiveCompletionVisual(TextMeshProUGUI objectiveText, int starIndex, bool isComplete)
    {
        if (objectiveText == null)
            return;

        _objectiveCompletionStates[objectiveText] = isComplete;

        SetGoalStar(starIndex, isComplete);
    }

    private void SetGoalStar(int objectiveIndex, bool acquired)
    {
        Transform container = objectiveIndex switch
        {
            0 => _objective1Container,
            1 => _objective2Container,
            2 => _objective3Container,
            _ => null
        };

        if (container == null)
            return;

        Image img = container.GetComponentInChildren<Image>(true);
        if (img == null)
            return;

        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
    }

    private int GetLevelIdFromSceneName(string sceneName)
    {
        return sceneName != null &&
               sceneName.Contains("Level") &&
               int.TryParse(sceneName.Substring(5), out int id)
            ? id
            : 1;
    }

    public void HidePowerUpConfirmationImmediate()
    {
        _powerUpConfirmationPopUp?.HideImmediate();
    }

    public void ClearPregameSelection()
    {
        _pregameSelection.Clear();
    }

    private PowerUpConfirmationRequest BuildPowerUpConfirmationRequest(PowerUpInventoryItem item, PowerUpBase powerUpBase)
    {
        bool hasStock = item.quantity > 0;

        if (hasStock)
        {
            PowerUpType capturedType = item.type;
            return new PowerUpConfirmationRequest(
                powerUpBase,
                _powerUpConfirmationPopUp.GetActivateButtonLabel(),
                true,
                null,
                () => TogglePowerUpSelection(capturedType));
        }

        return new PowerUpConfirmationRequest(
            powerUpBase,
            _powerUpConfirmationPopUp.GetBuyButtonLabel(powerUpBase.cost),
            PowerUpPurchaseService.CanAfford(powerUpBase.cost),
            CoinCounterUI.Instance != null ? CoinCounterUI.Instance.GetCoinSprite() : null,
            () => OnPowerUpPurchaseClicked(item));
    }

    private void HandlePowerUpPrimaryAction(PowerUpInventoryItem item)
    {
        if (item.quantity > 0)
            TogglePowerUpSelection(item.type);
        else
            OnPowerUpPurchaseClicked(item);
    }

    private void TogglePowerUpSelection(PowerUpType powerUpType)
    {
        _pregameSelection.Toggle(powerUpType);
        ShowPreGamePowerUps();
    }

    private bool TryConsumeSelectedPowerUps()
    {
        if (_pregameSelection.Count == 0)
            return true;

        GameData gameData = SaveManager.Instance?.GetGameData();
        if (gameData == null)
            return false;

        for (int i = 0; i < _pregameSelection.Count; i++)
        {
            PowerUpType type = _pregameSelection.GetSelection(i);
            bool found = false;
            for (int j = 0; j < gameData.powerUpInventory.Count; j++)
            {
                if (gameData.powerUpInventory[j].type == type && gameData.powerUpInventory[j].quantity > 0)
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Debug.LogWarning($"[PreGameUIManager] Cannot consume {type}: insufficient inventory.");
                _pregameSelection.Clear();
                ShowPreGamePowerUps();
                return false;
            }
        }

        for (int i = 0; i < _pregameSelection.Count; i++)
        {
            if (PowerUpManager.Instance == null)
            {
                Debug.LogError("[PreGameUIManager] PowerUpManager unavailable during consumption. Aborting.");
                _pregameSelection.Clear();
                ShowPreGamePowerUps();
                return false;
            }
            PowerUpManager.Instance.ActivatePowerUpFromInventory(_pregameSelection.GetSelection(i));
        }

        _pregameSelection.Clear();
        return true;
    }

    private Image GetObjectiveSlashImage(TextMeshProUGUI objectiveText)
    {
        if (objectiveText == null)
            return null;

        if (_objectiveSlashImages.TryGetValue(objectiveText, out Image slashImage) && slashImage != null)
            return slashImage;

        slashImage = objectiveText.GetComponentInChildren<Image>(true);
        if (slashImage != null)
            _objectiveSlashImages[objectiveText] = slashImage;

        return slashImage;
    }
}

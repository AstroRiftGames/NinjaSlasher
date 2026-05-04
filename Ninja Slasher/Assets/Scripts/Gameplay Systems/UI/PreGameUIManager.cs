using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private readonly Dictionary<Image, Vector2> _objectiveSlashBasePositions = new();
    private readonly Dictionary<Image, Color> _objectiveSlashBaseColors = new();
    private readonly Dictionary<Image, bool> _objectiveSlashBaseEnabled = new();
    private readonly List<Sequence> _activeSequences = new();
    private readonly List<PowerUpSlotUI> _slots = new();

    private UIAudioContext _audioContext;
    private bool _isLevelSelected;

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
    }

    private void OnDisable()
    {
        GameEvents.OnRewardClaimed -= OnDailyRewardClaimedRefresh;
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();
    }

    private void SetupButtonListeners()
    {
        _playButton.onClick.AddListener(OnPlayButtonClicked);
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

        ResolveGoalTextReferencesIfNeeded();

        _pendingSceneName = sceneName;
        _isLevelSelected = true;

        UIEvents.RequestShowPregameModal();

        ShowPreGameTitle();
        SetGoals();
        RefreshObjectiveSlashBaselines();
        ResetVisualState();
        ShowPreGamePowerUps();
        ApplyObjectiveCompletionVisuals();
    }

    public void StopAllAnimations()
    {
        StopAllCoroutines();

        foreach (Sequence sequence in _activeSequences.ToList())
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }
        _activeSequences.Clear();

        if (_title != null)
            DOTween.Kill(_title);

        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            DOTween.Kill(objectiveText);

            Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
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

            Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
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
            Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
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
        if (_isLevelSelected)
        {
            OnConfirmLevelSelection();
            return;
        }

        UIEvents.RequestHidePregameModal();
    }

    private void OnConfirmLevelSelection()
    {
        if (!LifeManager.Instance.CanPlay())
        {
            AbortPendingLevelSelectionForLifeWall();
            UIEvents.RequestShowNoLivesModal();
            return;
        }

        StopAllAnimations();
        _isLevelSelected = false;
        UIEvents.RequestHidePregameModal();
        UIEvents.RequestSceneTransition(_pendingSceneName);
    }

    private void AbortPendingLevelSelectionForLifeWall()
    {
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();
        _isLevelSelected = false;
        _pendingSceneName = null;
        UIEvents.RequestHidePregameModal();
    }

    private void OnDailyRewardClaimedRefresh(DailyReward _)
    {
        ShowPreGamePowerUps();
    }

    private void ShowPreGameTitle()
    {
        int levelId = GetLevelIdFromSceneName(_pendingSceneName);
        LevelConfigurationManager cfgMgr = LevelConfigurationManager.Instance;
        LevelConfiguration config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (_title != null)
            _title.text = config != null ? config.levelName : string.Empty;
    }

    public void ShowPreGamePowerUps()
    {
        foreach (PowerUpSlotUI slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();

        List<PowerUpInventoryItem> inventory = SaveManager.Instance.GetGameData().powerUpInventory;

        foreach (PowerUpBase powerUpBase in allPowerUpBases)
        {
            PowerUpInventoryItem item = inventory.Find(i => i.type == powerUpBase.powerUpType);
            if (item == null)
                item = new PowerUpInventoryItem(powerUpBase.powerUpType, 0);

            PowerUpSlotUI slot = Instantiate(_powerUpSlotPrefab, _powerUpsContainer);
            slot.Setup(item, powerUpBase, OnPowerUpInteractClicked);
            _slots.Add(slot);
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
        int cost = GetCostForType(item.type);
        PurchaseResult result = PowerUpPurchaseService.Purchase(item.type, cost);

        if (result == PurchaseResult.Success)
        {
            GameEvents.RaisePowerUpPurchased(item.type);
            ShowPreGamePowerUps();
        }
    }

    private int GetCostForType(PowerUpType type)
    {
        PowerUpBase powerUpBase = System.Array.Find(allPowerUpBases, powerUp => powerUp.powerUpType == type);
        return powerUpBase != null ? powerUpBase.cost : 0;
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

    private PowerUpConfirmationRequest BuildPowerUpConfirmationRequest(PowerUpInventoryItem item, PowerUpBase powerUpBase)
    {
        bool hasStock = item.quantity > 0;

        if (hasStock)
        {
            return new PowerUpConfirmationRequest(
                powerUpBase,
                _powerUpConfirmationPopUp.GetActivateButtonLabel(),
                true,
                null,
                () => ConfirmPowerUpActivation(item.type));
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
            ConfirmPowerUpActivation(item.type);
        else
            OnPowerUpPurchaseClicked(item);
    }

    private void ConfirmPowerUpActivation(PowerUpType powerUpType)
    {
        if (PowerUpManager.Instance != null && PowerUpManager.Instance.ActivatePowerUpFromInventory(powerUpType))
            ShowPreGamePowerUps();
    }
}

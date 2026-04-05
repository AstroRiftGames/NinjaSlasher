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
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _pendingSceneName;
    [SerializeField] private Transform _powerUpsContainer;
    [SerializeField] private PowerUpSlotUI _powerUpSlotPrefab;
    [SerializeField] private PowerUpBase[] allPowerUpBases;
    [SerializeField] private PowerUpConfirmationPopUp _powerUpConfirmationPopUp;
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
    [SerializeField] private float titleAnimationDuration = 0.8f;
    [SerializeField] private float titleRevealPadding = 24f;
    [SerializeField] private float objectiveDelay = 0.3f;
    [SerializeField] private float objectiveStagger = 0.1f;
    [SerializeField] private float objectiveBaseStepDelay = 0.025f;
    [SerializeField] private float objectiveWordPauseDelay = 0.055f;
    [SerializeField] private float objectiveFlashDuration = 0.08f;
    [SerializeField] private float slashEffectDuration = 0.4f;
    [SerializeField] private int objectiveBurstSize = 2;

    private readonly Dictionary<TextMeshProUGUI, Color> _baseTextColors = new();
    private readonly Dictionary<Image, Vector2> _objectiveSlashBasePositions = new();
    private readonly Dictionary<Image, Color> _objectiveSlashBaseColors = new();
    private readonly Dictionary<Image, bool> _objectiveSlashBaseEnabled = new();
    private readonly List<Sequence> _activeSequences = new();
    private readonly List<PowerUpSlotUI> _slots = new();

    private RectTransform _titleRevealRoot;
    private RectTransform _titleLeftMaskRect;
    private RectTransform _titleRightMaskRect;
    private TextMeshProUGUI _titleLeftRevealText;
    private TextMeshProUGUI _titleRightRevealText;
    private Color _titleBaseColor;
    private bool _isLevelSelected;

    private void Awake()
    {
        if (_powerUpConfirmationPopUp == null)
            _powerUpConfirmationPopUp = GetComponentInChildren<PowerUpConfirmationPopUp>(true);

        CacheBaseVisualState();
        SetupButtonListeners();
        InitializeTitleReveal();
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
        _closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void CacheBaseVisualState()
    {
        if (_title != null)
        {
            _titleBaseColor = _title.color;
            _baseTextColors[_title] = _title.color;
        }

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

    private void InitializeTitleReveal()
    {
        if (_title == null || _titleRevealRoot != null)
            return;

        RectTransform titleRect = _title.rectTransform;
        Transform titleParent = titleRect.parent;

        _titleRevealRoot = CreateRectTransform("TitleRevealRoot", titleParent);
        CopyRectTransform(titleRect, _titleRevealRoot);
        _titleRevealRoot.SetSiblingIndex(titleRect.GetSiblingIndex() + 1);

        _titleLeftMaskRect = CreateMaskRect("TitleRevealLeftMask", _titleRevealRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, 0.5f));
        _titleRightMaskRect = CreateMaskRect("TitleRevealRightMask", _titleRevealRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f));

        _titleLeftRevealText = CreateTitleRevealClone("TitleRevealLeftText", _titleLeftMaskRect);
        _titleRightRevealText = CreateTitleRevealClone("TitleRevealRightText", _titleRightMaskRect);

        _titleRevealRoot.gameObject.SetActive(false);
    }

    private RectTransform CreateRectTransform(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private RectTransform CreateMaskRect(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        RectTransform rect = CreateRectTransform(objectName, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.gameObject.AddComponent<RectMask2D>();
        return rect;
    }

    private TextMeshProUGUI CreateTitleRevealClone(string objectName, Transform parent)
    {
        TextMeshProUGUI clone = Instantiate(_title, parent);
        clone.name = objectName;
        clone.raycastTarget = false;

        RectTransform cloneRect = clone.rectTransform;
        cloneRect.anchorMin = new Vector2(0.5f, 0.5f);
        cloneRect.anchorMax = new Vector2(0.5f, 0.5f);
        cloneRect.pivot = new Vector2(0.5f, 0.5f);
        cloneRect.anchoredPosition = Vector2.zero;
        cloneRect.sizeDelta = _title.rectTransform.rect.size;
        cloneRect.localRotation = Quaternion.identity;
        cloneRect.localScale = Vector3.one;

        return clone;
    }

    private void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localRotation = source.localRotation;
        target.localScale = Vector3.one;
    }

    public void ShowConfirmationPanel(string sceneName)
    {
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();

        _pendingSceneName = sceneName;
        _isLevelSelected = true;

        UIEvents.RequestShowPreGameScreen();

        ShowPreGameTitle();
        SetGoals();
        ShowPreGamePowerUps();
        RefreshObjectiveSlashBaselines();
        ResetVisualState();

        if (useNinjaAnimations)
            StartCoroutine(DelayedAnimations());
    }

    private IEnumerator DelayedAnimations()
    {
        PrepareTitleReveal();
        PrepareObjectiveReveal();

        yield return new WaitForSeconds(titleAnimationDelay);
        yield return AnimateTitleReveal();

        yield return new WaitForSeconds(objectiveDelay);
        yield return AnimateObjectives();
    }

    private IEnumerator AnimateTitleReveal()
    {
        if (_title == null || _titleRevealRoot == null)
            yield break;

        float targetHeight = Mathf.Max(_title.rectTransform.rect.height, _title.preferredHeight) + titleRevealPadding;
        float halfWidth = Mathf.Max(0f, (_title.preferredWidth * 0.5f) + titleRevealPadding);

        SetTitleMaskDimensions(_titleLeftMaskRect, 0f, targetHeight);
        SetTitleMaskDimensions(_titleRightMaskRect, 0f, targetHeight);

        Sequence revealSequence = DOTween.Sequence();
        revealSequence.Append(
            DOTween.To(
                    () => _titleLeftMaskRect.sizeDelta.x,
                    width => SetTitleMaskDimensions(_titleLeftMaskRect, width, targetHeight),
                    halfWidth,
                    titleAnimationDuration)
                .SetEase(Ease.OutCubic));
        revealSequence.Join(
            DOTween.To(
                    () => _titleRightMaskRect.sizeDelta.x,
                    width => SetTitleMaskDimensions(_titleRightMaskRect, width, targetHeight),
                    halfWidth,
                    titleAnimationDuration)
                .SetEase(Ease.OutCubic));
        revealSequence.OnComplete(FinishTitleReveal);
        _activeSequences.Add(revealSequence);

        yield return revealSequence.WaitForCompletion();
    }

    private void SetTitleMaskDimensions(RectTransform maskRect, float width, float height)
    {
        if (maskRect == null)
            return;

        maskRect.sizeDelta = new Vector2(width, height);
    }

    private IEnumerator AnimateObjectives()
    {
        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            yield return RevealObjectiveText(objectiveText);
            yield return new WaitForSeconds(objectiveStagger);
        }
    }

    private IEnumerator RevealObjectiveText(TextMeshProUGUI objectiveText)
    {
        if (objectiveText == null || string.IsNullOrEmpty(objectiveText.text))
            yield break;

        objectiveText.ForceMeshUpdate();
        int totalCharacters = objectiveText.textInfo.characterCount;
        if (totalCharacters <= 0)
            yield break;

        objectiveText.maxVisibleCharacters = 0;

        Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
        Sequence flashSequence = BuildObjectiveFlashSequence(objectiveText, slashImage);
        if (flashSequence != null)
            _activeSequences.Add(flashSequence);

        int visibleCharacters = 0;
        while (visibleCharacters < totalCharacters)
        {
            visibleCharacters = Mathf.Min(totalCharacters, visibleCharacters + GetNextRevealBurst(objectiveText.text, visibleCharacters));
            objectiveText.maxVisibleCharacters = visibleCharacters;

            yield return new WaitForSeconds(GetRevealDelay(objectiveText.text, visibleCharacters));
        }
    }

    private Sequence BuildObjectiveFlashSequence(TextMeshProUGUI objectiveText, Image slashImage)
    {
        if (objectiveText == null)
            return null;

        if (!_baseTextColors.TryGetValue(objectiveText, out Color baseColor))
            baseColor = objectiveText.color;

        objectiveText.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.78f);

        Sequence flashSequence = DOTween.Sequence();
        flashSequence.Append(objectiveText.DOFade(baseColor.a, objectiveFlashDuration).SetEase(Ease.OutQuad));

        if (slashImage != null && slashImage.enabled)
        {
            if (!_objectiveSlashBasePositions.TryGetValue(slashImage, out Vector2 basePosition))
                basePosition = slashImage.rectTransform.anchoredPosition;

            if (!_objectiveSlashBaseColors.TryGetValue(slashImage, out Color baseSlashColor))
                baseSlashColor = slashImage.color;

            slashImage.rectTransform.anchoredPosition = basePosition + new Vector2(-18f, 0f);
            slashImage.color = new Color(baseSlashColor.r, baseSlashColor.g, baseSlashColor.b, 0f);

            flashSequence.Join(slashImage.DOFade(baseSlashColor.a, slashEffectDuration * 0.45f).SetEase(Ease.OutQuad));
            flashSequence.Join(slashImage.rectTransform.DOAnchorPos(basePosition, slashEffectDuration).SetEase(Ease.OutCubic));
            flashSequence.OnKill(() => RestoreObjectiveSlashBaseline(slashImage));
            flashSequence.OnComplete(() => RestoreObjectiveSlashBaseline(slashImage));
        }

        return flashSequence;
    }

    private int GetNextRevealBurst(string text, int currentVisibleCharacters)
    {
        if (currentVisibleCharacters >= text.Length)
            return 0;

        int burst = 0;
        while (currentVisibleCharacters + burst < text.Length && burst < Mathf.Max(1, objectiveBurstSize))
        {
            burst++;
            char nextChar = text[currentVisibleCharacters + burst - 1];
            if (char.IsWhiteSpace(nextChar) || IsPunctuation(nextChar))
                break;
        }

        return Mathf.Max(1, burst);
    }

    private float GetRevealDelay(string text, int visibleCharacters)
    {
        if (visibleCharacters <= 0 || visibleCharacters > text.Length)
            return objectiveBaseStepDelay;

        char lastVisibleChar = text[visibleCharacters - 1];
        return char.IsWhiteSpace(lastVisibleChar) || IsPunctuation(lastVisibleChar)
            ? objectiveWordPauseDelay
            : objectiveBaseStepDelay;
    }

    private static bool IsPunctuation(char value)
    {
        return value == '.' || value == ',' || value == ':' || value == ';' || value == '!' || value == '?';
    }

    private void PrepareTitleReveal()
    {
        if (_title == null || _titleRevealRoot == null)
            return;

        _title.ForceMeshUpdate();
        CopyRectTransform(_title.rectTransform, _titleRevealRoot);

        if (_titleLeftRevealText != null)
        {
            _titleLeftRevealText.text = _title.text;
            _titleLeftRevealText.color = _titleBaseColor;
            _titleLeftRevealText.alpha = 1f;
            _titleLeftRevealText.rectTransform.sizeDelta = _titleRevealRoot.rect.size;
        }

        if (_titleRightRevealText != null)
        {
            _titleRightRevealText.text = _title.text;
            _titleRightRevealText.color = _titleBaseColor;
            _titleRightRevealText.alpha = 1f;
            _titleRightRevealText.rectTransform.sizeDelta = _titleRevealRoot.rect.size;
        }

        _title.color = new Color(_titleBaseColor.r, _titleBaseColor.g, _titleBaseColor.b, 0f);
        _titleRevealRoot.gameObject.SetActive(true);
    }

    private void FinishTitleReveal()
    {
        if (_title != null)
            _title.color = _titleBaseColor;

        if (_titleRevealRoot != null)
            _titleRevealRoot.gameObject.SetActive(false);
    }

    private void PrepareObjectiveReveal()
    {
        foreach (TextMeshProUGUI objectiveText in GetObjectiveTexts())
        {
            objectiveText.ForceMeshUpdate();
            objectiveText.maxVisibleCharacters = 0;

            if (_baseTextColors.TryGetValue(objectiveText, out Color baseColor))
                objectiveText.color = baseColor;

            Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
            if (slashImage != null)
                RestoreObjectiveSlashBaseline(slashImage);
        }
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
        ResetTitleVisualState();
        ResetObjectiveVisualState();
    }

    private void ResetTitleVisualState()
    {
        if (_title != null)
            _title.color = _titleBaseColor;

        if (_titleRevealRoot != null)
            _titleRevealRoot.gameObject.SetActive(false);

        if (_titleLeftMaskRect != null)
            _titleLeftMaskRect.sizeDelta = Vector2.zero;

        if (_titleRightMaskRect != null)
            _titleRightMaskRect.sizeDelta = Vector2.zero;

        if (_titleLeftRevealText != null)
        {
            _titleLeftRevealText.text = _title != null ? _title.text : string.Empty;
            _titleLeftRevealText.color = _titleBaseColor;
            _titleLeftRevealText.alpha = 1f;
        }

        if (_titleRightRevealText != null)
        {
            _titleRightRevealText.text = _title != null ? _title.text : string.Empty;
            _titleRightRevealText.color = _titleBaseColor;
            _titleRightRevealText.alpha = 1f;
        }
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
                RestoreObjectiveSlashBaseline(slashImage);
        }
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

        UIEvents.RequestHidePreGameScreen();
    }

    private void OnCloseButtonClicked()
    {
        if (_isLevelSelected)
        {
            CancelLevelSelection();
            return;
        }

        UIEvents.RequestHidePreGameScreen();
    }

    private void OnConfirmLevelSelection()
    {
        if (!LifeManager.Instance.CanPlay())
        {
            AbortPendingLevelSelectionForLifeWall();
            UIEvents.RequestShowNoLivesOverlay();
            return;
        }

        StopAllAnimations();
        _isLevelSelected = false;
        UIEvents.RequestHidePreGameScreen();
        UIEvents.RequestSceneTransition(_pendingSceneName);
    }

    private void CancelLevelSelection()
    {
        StopAllAnimations();
        _isLevelSelected = false;
        _pendingSceneName = null;
        UIEvents.RequestHidePreGameScreen();
    }

    private void AbortPendingLevelSelectionForLifeWall()
    {
        StopAllAnimations();
        HidePowerUpConfirmationImmediate();
        _isLevelSelected = false;
        _pendingSceneName = null;
        UIEvents.RequestHidePreGameScreen();
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

        Image slashImage = objectiveText.GetComponentInChildren<Image>(true);
        if (slashImage != null)
            slashImage.enabled = isComplete;

        SetStar(starIndex, isComplete);
    }

    private void SetStar(int idx, bool acquired)
    {
        Image img = _starsContainer.GetChild(idx).GetComponent<Image>();
        if (!img)
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

using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ButtonManager : MonoBehaviourSingleton<ButtonManager>
{
    [Header("LEVEL SELECTOR BUTTONS")]
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private Button _configDropdownButton;
    [SerializeField] private Button _calendarButton;
    [SerializeField] private Button _storeButton;

    [Header("CALENDAR BUTTON ICONS")]
    [SerializeField] private Image _calendarButtonImage;
    [SerializeField] private Sprite _calendarAvailableIcon;
    [SerializeField] private Sprite _calendarClaimedIcon;

    [Header("CONFIG DROPDOWN BUTTONS")]
    [SerializeField] private Button _musicButton;
    [SerializeField] private Button _sfxButton;
    [SerializeField] private Button _profileButton;
    [SerializeField] private Image _profileButtonImage;
    [SerializeField] private Button _hapticButton;

    [Header("PROFILE BUTTONS")]
    [SerializeField] private Button _userIconButton;
    [SerializeField] private Button _userNicknameButton;
    [SerializeField] private TextMeshProUGUI _userNicknameButtonText;
    [SerializeField] private Button _closeProfileButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _closeCreditsButton;
    [SerializeField] private Image _userIconImagePanel;
    [SerializeField] private Button[] _userIconButtonGroup;

    [Header("DAILY REWARDS BUTTONS")]
    [SerializeField] private Button _closeCalendarButton;

    [Header("STORE BUTTONS")]
    [SerializeField] private Button _closeStoreButton;

    [Header("PREGAME BUTTONS")]
    [SerializeField] private Button _closePregameButton;
    [SerializeField] private Button _playButton;

    [Header("GAMEPLAY BUTTONS")]
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _quitButton;
    [SerializeField] private Button _musicPausePanelButton;
    [SerializeField] private Button _sfxPausePanelButton;
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _backToSelectionButton;
    [SerializeField] private Button _continueButton;

    [Header("NO LIVES PANEL BUTTONS")]
    [SerializeField] private Button _closeNoLivesPanelButton;
    [SerializeField] private Button _adForMoreLifeButton;
    [SerializeField] private Button _claimButton;

    [Header("PROGRESSION UI")]
    [SerializeField] private Image[] levelButtonImages;
    [SerializeField] private Color lockedButtonColor = Color.gray;
    [SerializeField] private Color unlockedButtonColor = Color.white;

    [SerializeField] private string[] sceneNames;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float fallDistance = 800f;
    [SerializeField] private float waveDelay = 0.08f;
    [SerializeField] private bool addRotationEffect = true;
    [SerializeField] private bool addImpactEffect = true;

#if UNITY_EDITOR
    [SerializeField] private Button deleteSaveButton;
#endif

    private AudioSettingsUI _configToggles;
    private ConfigDropdown _configPanelManager;

    private List<Sequence> activeButtonSequences = new List<Sequence>();
    private Dictionary<Button, Vector2> savedButtonPositions = new Dictionary<Button, Vector2>();

    private UIAudioContext _audioContext;

    private void OnSaveDataLoaded(GameData _) => RefreshLevelProgression();

    [Header("POP UPS")]
    [SerializeField] private UserIconsPopUp _userIconsPanel;
    [SerializeField] private UserNicknameEditPopUp _userNicknameEditPanel;
    [SerializeField] private ConfirmationPopUp _confirmationPanel;

    public override void Awake()
    {
        base.Awake();
        _audioContext = GetComponentInParent<UIAudioContext>();
        _configToggles = GetComponent<AudioSettingsUI>();
        _configPanelManager = GetComponent<ConfigDropdown>();

        if (_calendarButtonImage == null && _calendarButton != null)
        {
            _calendarButtonImage = _calendarButton.GetComponent<Image>();
        }

        SaveButtonPositions();
    }

    private void Start()
    {
        StartCoroutine(InitializeCalendarIcon());
    }

    private void OnEnable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated += RefreshLevelProgression;
        }
        else
        {
            StartCoroutine(DelayedSubscription());
        }

        GameEvents.OnRewardClaimed += OnRewardClaimed;
        GameEvents.OnRewardAvailabilityChanged += UpdateCalendarButtonIcon;
        UIEvents.OnNicknameChanged += OnNicknameChanged;

        SaveManager.OnDataLoaded += OnSaveDataLoaded;
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated -= RefreshLevelProgression;
        }

        GameEvents.OnRewardClaimed -= OnRewardClaimed;
        GameEvents.OnRewardAvailabilityChanged -= UpdateCalendarButtonIcon;

        UIEvents.OnNicknameChanged -= OnNicknameChanged;

        SaveManager.OnDataLoaded -= OnSaveDataLoaded;
    }

    private IEnumerator DelayedSubscription()
    {
        yield return null;

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated += RefreshLevelProgression;
        }
    }

    public void SetupButtons()
    {
        SetupLevelSelectorButtons();
        SetupGameplayButtons();
        SetupLevelProgression();
        UpdateButtonProgression();

#if UNITY_EDITOR
        SetupDebugButtons();
#endif
    }

    private void SetupGameplayButtons()
    {
        _pauseButton.onClick.AddListener(() => UIEvents.RequestTogglePauseOverlay());
        _resumeButton.onClick.AddListener(() => UIEvents.RequestTogglePauseOverlay());
        _restartButton.onClick.AddListener(OnRestartPressed);

        _musicPausePanelButton.onClick.AddListener(_configToggles.MusicButtonPushed);
        _sfxPausePanelButton.onClick.AddListener(_configToggles.SFXButtonPushed);

        _retryButton.onClick.AddListener(() => UIEvents.RequestRestartLevel());
        _backToSelectionButton.onClick.AddListener(UIEvents.RaiseQuitToMenuPressed);
        _continueButton.onClick.AddListener(UIEvents.RaiseQuitToMenuPressed);
    }

    private void OnNicknameChanged(string newNickname)
    {
        if (_userNicknameButtonText != null)
            _userNicknameButtonText.text = newNickname;

        Debug.Log($"[ButtonManager] Nickname actualizado a: {newNickname}");
    }

    private IEnumerator InitializeCalendarIcon()
    {
        while (DailyRewardSystem.Instance == null || SaveManager.Instance == null || !SaveManager.Instance.IsDataLoaded)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        UpdateCalendarButtonIcon(DailyRewardSystem.Instance.CanClaimToday());
    }

    private void OnRewardClaimed(DailyReward reward)
    {
        UpdateCalendarButtonIcon(false);
    }

    public void UpdateCalendarButtonIcon(bool isAvailable)
    {
        if (_calendarButtonImage == null || _calendarAvailableIcon == null || _calendarClaimedIcon == null)
        {
            return;
        }

        _calendarButtonImage.sprite = isAvailable ? _calendarAvailableIcon : _calendarClaimedIcon;
    }

    private void SetupLevelProgression()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            bool isUnlocked = IsLevelUnlocked(levelId);

            levelButtons[i].interactable = isUnlocked;

            if (levelButtonImages != null && i < levelButtonImages.Length && levelButtonImages[i] != null)
            {
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : lockedButtonColor;
            }
        }
    }

    private void SaveButtonPositions()
    {
        savedButtonPositions.Clear();
        foreach (var btn in levelButtons)
        {
            if (btn != null)
            {
                RectTransform rt = btn.GetComponent<RectTransform>();
                savedButtonPositions[btn] = rt.anchoredPosition;
            }
        }
    }

    public void OpenURLButtonClicked(string url)
    {
        Application.OpenURL(url);
    }

    private void SetupLevelSelectorButtons()
    {        
        _musicButton.onClick.AddListener(_configToggles.MusicButtonPushed);
        _sfxButton.onClick.AddListener(_configToggles.SFXButtonPushed);
        _profileButton.onClick.AddListener(UIEvents.RequestShowProfileModal);

        if (_hapticButton != null)
        {
            _hapticButton.onClick.AddListener(_configToggles.HapticFeedbackPushed);
        }

        _closeNoLivesPanelButton.onClick.AddListener(UIEvents.RequestHideNoLivesOverlay);

        _userIconButton.onClick.AddListener(ToggleUserIconsPanel);

        _claimButton.onClick.AddListener(OnClaimLifeButtonPressed);

        _userNicknameButton.onClick.AddListener(ShowNicknameEditPopup);

        _creditsButton.onClick.AddListener(UIEvents.RequestShowCreditsModal);
        _closeProfileButton.onClick.AddListener(UIEvents.RequestHideProfileModal);
        _closeCreditsButton.onClick.AddListener(UIEvents.RequestHideCreditsModal);

        foreach (var img in _userIconButtonGroup)
        {
            img.onClick.AddListener(() =>
            {
                Image icon = img.transform.GetChild(0).GetComponent<Image>();
                _userIconImagePanel.sprite = icon.sprite;
                _userIconImagePanel.color = icon.color;
                _profileButtonImage.sprite = icon.sprite;
                _profileButtonImage.color = icon.color;
                if (_userIconsPanel != null)
                    _userIconsPanel.Hide();
            });
        }

        _storeButton.onClick.AddListener(UIEvents.RequestShowStoreModal);
        _closeStoreButton.onClick.AddListener(UIEvents.RequestHideStoreModal);
        _calendarButton.onClick.AddListener(UIEvents.RequestShowDailyRewardModal);
        _configDropdownButton.onClick.AddListener(_configPanelManager.OpenCloseConfigPanel);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            string sceneName = sceneNames[i];

            levelButtons[i].onClick.AddListener(() =>
            {
                if (IsLevelUnlocked(levelId))
                {
                    UIEvents.RequestLevelPreview(sceneName);
                }
                else
                {
                    ShowLevelLockedMessage(levelId);
                }
            });
        }
    }

    private void ToggleUserIconsPanel()
    {
        if (_userIconsPanel == null)
        {
            Debug.LogWarning("[ButtonManager] UserIconsPanel no asignado");
            return;
        }

        if (_userIconsPanel.IsVisible)
            _userIconsPanel.Hide();
        else
            _userIconsPanel.Show();
    }

    private void ShowNicknameEditPopup()
    {
        if (_userNicknameEditPanel == null)
        {
            Debug.LogWarning("[ButtonManager] UserNicknameEditPanel no asignado");
            return;
        }

        _userNicknameEditPanel.SetNickname(_userNicknameButtonText.text);
        _userNicknameEditPanel.Show();
    }

    public void OnClaimLifeButtonPressed()
    {
        if (LifeManager.Instance != null)
        {
            LifeManager.Instance.AddLife();
            Debug.Log("Vida reclamada exitosamente");

            if (UIManager.Instance != null)
            {    
                UIEvents.RequestHideNoLivesOverlay();
            }
        }
    }

    void UpdateStars(Button levelButton, int levelId)
    {
        Transform buttonTransform = levelButton.transform;
        Transform starsContainer = buttonTransform.Find("Stars");
        int starsEarned = GetStars(levelId);
        bool isLevelUnlocked = IsLevelUnlocked(levelId);

        for (int i = 0; i < 3; i++)
        {
            Transform star = starsContainer.GetChild(i);
            if (star != null)
            {
                Image starImage = star.GetComponent<Image>();
                if (starImage != null)
                {
                    star.gameObject.SetActive(isLevelUnlocked);
                    if (isLevelUnlocked)
                    {
                        bool isEarned = i < starsEarned;
                        SetStar(starImage, isEarned);
                    }
                }
            }
        }
    }

    private void SetStar(Image img, bool acquired)
    {
        if (!img) return;
        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
        img.color = Color.white;
    }

    private int GetStars(int levelId)
    {
        var gameData = SaveManager.Instance.GetGameData();
        if (gameData.levelStars.TryGetValue(levelId, out int stars))
        {
            return stars;
        }
        return 0;
    }

    void UpdateButtonProgression()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            UpdateStars(levelButtons[i], levelId);
        }
    }

    private void ShowLevelLockedMessage(int levelId)
    {
        Debug.Log($"[ButtonManager] Nivel {levelId} esta bloqueado");
    }

    public void RefreshLevelProgression()
    {
        var progressionInfo = LevelProgressionManager.Instance?.GetProgressionInfo();
        if (progressionInfo == null)
        {
            return;
        }

        int unlockedCount = 0;
        int lockedCount = 0;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelId = i + 1;
            bool isUnlocked = IsLevelUnlocked(levelId);

            levelButtons[i].interactable = isUnlocked;

            if (levelButtonImages != null && i < levelButtonImages.Length && levelButtonImages[i] != null)
            {
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : lockedButtonColor;
            }

            if (isUnlocked) unlockedCount++;
            else lockedCount++;
        }

        UpdateButtonProgression();
    }

    private void OnRestartPressed()
    {
        UIEvents.RequestRestartLevel();
        UIEvents.RequestTogglePauseOverlay();
    }

    public void ShowConfirmation(
    string message,
    Action onConfirm,
    Action onCancel = null,
    string title = "Confirmation",
    string confirmText = "Confirm",
    string cancelText = "Cancel")
    {
        if (_confirmationPanel == null)
        {
            Debug.LogWarning("[UIManager] ConfirmationPanel no asignado");
            return;
        }

        _confirmationPanel.ShowConfirmation(message, onConfirm, onCancel, title, confirmText, cancelText);
    }

    private void OnQuitButtonPressed()
    {
        ShowConfirmation(
            message: "Are you sure you want to exit? You will lose your progress in this level?",
            onConfirm: () => {
                UIEvents.RaiseQuitToMenuPressed();
            },
            onCancel: () => Debug.Log("Cancelled"),
            title: "Exit level",
            confirmText: "Exit",
            cancelText: "Continue"
        );
    }

    private void OnBuyItemPressed(string itemName, int cost)
    {
        ShowConfirmation(
            message: $"Buy {itemName} for {cost} coins?",
            onConfirm: () =>
            {
                Debug.Log($"Buying {itemName}");
            },
            title: "Confirm Purchase",
            confirmText: "Buy",
            cancelText: "Cancel"
        );
    }

    private bool IsLevelUnlocked(int levelId)
    {
        if (LevelProgressionManager.Instance == null)
        {
            return levelId == 1;
        }
        return LevelProgressionManager.Instance.IsLevelUnlocked(levelId);
    }

    public void AnimateButtons(IEnumerable<Button> buttons)
    {
        if (buttons == null) return;

        int sequenceIndex = 0;
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            btn.gameObject.SetActive(true);
            btn.transform.DOKill();
            AnimateHeavySingleButton(btn, sequenceIndex);
            sequenceIndex++;
        }
    }

    public void ShowButtonsInstantly(IEnumerable<Button> buttons)
    {
        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.gameObject.SetActive(true);

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                DOTween.Kill(rt);

                if (savedButtonPositions.TryGetValue(btn, out var saved))
                    rt.anchoredPosition = saved;

                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
                DOTween.Kill(img);
        }
    }

    private void AnimateHeavySingleButton(Button button, int sequenceIndex)
    {
        if (button == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();

        Vector2 finalPosition = savedButtonPositions.TryGetValue(button, out var saved)
            ? saved
            : rectTransform.anchoredPosition;

        rectTransform.anchoredPosition = finalPosition + Vector2.up * (fallDistance * 1.5f);

        if (addRotationEffect)
        {
            float heavyRotation = UnityEngine.Random.Range(-90f, 90f);
            rectTransform.rotation = Quaternion.Euler(0, 0, heavyRotation);
        }

        float heavyDelay = sequenceIndex * (waveDelay * 3f);

        Sequence heavySequence = DOTween.Sequence();
        activeButtonSequences.Add(heavySequence);

        heavySequence.AppendInterval(heavyDelay);

        heavySequence.Append(
            rectTransform.DOAnchorPos(finalPosition, 0.35f)
                .SetEase(Ease.InQuart)
        );

        if (addRotationEffect)
        {
            heavySequence.Join(
                rectTransform.DORotate(Vector3.zero, 0.25f)
                    .SetEase(Ease.InOutSine)
            );
        }

        heavySequence.AppendCallback(() => {
            if (button != null && button.gameObject != null)
                CreateHeavyImpactEffect(button);
        });

        heavySequence.OnKill(() => {
            activeButtonSequences.Remove(heavySequence);
        });
    }

    private void CreateHeavyImpactEffect(Button button)
    {
        if (button == null || button.gameObject == null) return;

        RectTransform rectTransform = button.GetComponent<RectTransform>();

        Vector2 sideShake = new Vector2(UnityEngine.Random.Range(-15f, 15f), 0f);
        rectTransform.DOPunchAnchorPos(sideShake, 0.4f, 8, 1f);

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            buttonImage.DOColor(Color.white, 0.05f)
                .OnComplete(() => {
                    if (buttonImage != null && buttonImage.gameObject != null)
                    {
                        buttonImage.DOColor(originalColor, 0.2f);
                    }
                });
        }

        if (button.interactable && _audioContext != null && _audioContext.Audio != null)
        {
            AudioService.Instance?.PlaySFX(_audioContext.Audio.select);
        }
    }

    public void StopAllButtonAnimations()
    {
        foreach (var sequence in activeButtonSequences.ToList())
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }
        activeButtonSequences.Clear();

        foreach (var kvp in savedButtonPositions)
        {
            var btn = kvp.Key;
            if (btn == null) continue;

            var rt = btn.GetComponent<RectTransform>();
            if (rt != null)
            {
                DOTween.Kill(rt);
                rt.anchoredPosition = kvp.Value;
                rt.localRotation = Quaternion.identity;
            }

            var img = btn.GetComponent<Image>();
            if (img != null)
                DOTween.Kill(img);
        }
    }

    public void SetWaveParameters(float distance, float delay, bool rotation, bool impact)
    {
        fallDistance = distance;
        waveDelay = delay;
        addRotationEffect = rotation;
        addImpactEffect = impact;
    }

    public Button[] GetLevelButtons()
    {
        return levelButtons;
    }

#if UNITY_EDITOR
    private void SetupDebugButtons()
    {
        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.AddListener(() =>
            {
                GetComponent<DebugUIManager>().DeleteSaveDataFromUI();
                RefreshLevelProgression();
            });
        }
    }
#endif
}

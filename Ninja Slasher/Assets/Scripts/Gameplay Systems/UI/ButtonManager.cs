using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ButtonManager : MonoBehaviourSingleton<ButtonManager>
{
    [Header("LEVEL SELECTOR BUTTONS")]
    [SerializeField] private Button[] levelButtons;

    [Header("PROGRESSION UI")]
    [SerializeField] private Image[] levelButtonImages;
    [SerializeField] private Color lockedButtonColor = Color.gray;
    [SerializeField] private Color unlockedButtonColor = Color.white;
    [SerializeField] private Color _bossLockedButtonColor = new Color(0.8f, 0.6f, 0.2f);

    [SerializeField] private string[] sceneNames;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    [Header("STAR FEEDBACK")]
    [SerializeField] private float _starRevealStagger = 0.14f;
    [SerializeField] private float _starPunchDuration = 0.18f;
    [SerializeField] private Vector3 _starPunchStrength = new Vector3(0.16f, 0.16f, 0f);
    [SerializeField] private StarRevealCelebrationEffect _starRevealCelebrationEffectPrefab;
    [SerializeField] private float _starRevealLevelGap = 0.08f;
    [SerializeField, Min(1f)] private float _starRevealCelebrationReferenceButtonSize = 180f;
    [SerializeField, Min(0.01f)] private float _starRevealCelebrationMinScale = 0.06f;
    [SerializeField, Min(0.01f)] private float _starRevealCelebrationMaxScale = 0.125f;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float fallDistance = 800f;
    [SerializeField] private float waveDelay = 0.08f;
    [SerializeField] private bool addRotationEffect = true;
    [SerializeField] private bool addImpactEffect = true;

    private List<Sequence> activeButtonSequences = new List<Sequence>();
    private List<Sequence> _activeStarSequences = new List<Sequence>();
    private Dictionary<Button, Vector2> savedButtonPositions = new Dictionary<Button, Vector2>();
    private readonly Dictionary<Button, RectTransform> _buttonRectTransforms = new();
    private readonly Dictionary<Button, Image> _buttonImages = new();
    private readonly Dictionary<Button, RectTransform> _buttonStarsContainers = new();
    private readonly Dictionary<Button, BossLevelProgressUI> _bossProgressByButton = new();
    private readonly Stack<StarRevealCelebrationEffect> _availableCelebrationEffects = new();
    private readonly HashSet<StarRevealCelebrationEffect> _activeCelebrationEffects = new();

    private UIAudioContext _audioContext;
    private Coroutine _pendingStarRevealPlaybackRoutine;

    private readonly struct PendingStarRevealPresentation
    {
        public readonly Button Button;
        public readonly RectTransform StarsContainer;
        public readonly RectTransform FeedbackFxAnchor;
        public readonly int PreviousStars;
        public readonly int NewStars;
        public readonly int LevelId;
        public readonly bool AnimateStars;

        public PendingStarRevealPresentation(
            Button button,
            RectTransform starsContainer,
            RectTransform feedbackFxAnchor,
            int previousStars,
            int newStars,
            int levelId,
            bool animateStars)
        {
            Button = button;
            StarsContainer = starsContainer;
            FeedbackFxAnchor = feedbackFxAnchor;
            PreviousStars = previousStars;
            NewStars = newStars;
            LevelId = levelId;
            AnimateStars = animateStars;
        }
    }

    private void OnSaveDataLoaded(GameData _) => RefreshLevelProgression();

    public override void Awake()
    {
        base.Awake();
        _audioContext = GetComponentInParent<UIAudioContext>();
        SaveButtonPositions();
        HideAllStarContainers();
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

        SaveManager.OnDataLoaded += OnSaveDataLoaded;
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnProgressionUpdated -= RefreshLevelProgression;
        }

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
        SetupLevelProgression();
        UpdateButtonProgression();
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
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : GetLockedColor(levelId);
            }
        }
    }

    private Color GetLockedColor(int levelId)
    {
        if (LevelProgressionManager.Instance != null &&
            LevelProgressionManager.Instance.GetRequiredStarsForBoss(levelId) > 0)
            return _bossLockedButtonColor;
        return lockedButtonColor;
    }

    private void SaveButtonPositions()
    {
        savedButtonPositions.Clear();
        _buttonRectTransforms.Clear();
        _buttonImages.Clear();
        _buttonStarsContainers.Clear();
        _bossProgressByButton.Clear();
        foreach (var btn in levelButtons)
        {
            if (btn != null)
            {
                RectTransform rt = btn.GetComponent<RectTransform>();
                _buttonRectTransforms[btn] = rt;
                _buttonImages[btn] = btn.GetComponent<Image>();
                savedButtonPositions[btn] = rt.anchoredPosition;

                Transform starsContainer = btn.transform.Find("Stars");
                if (starsContainer is RectTransform starsRect)
                    _buttonStarsContainers[btn] = starsRect;

                BossLevelProgressUI bossProgress = btn.GetComponentInChildren<BossLevelProgressUI>(true);
                if (bossProgress != null)
                    _bossProgressByButton[btn] = bossProgress;
            }
        }
    }

    private void SetupLevelSelectorButtons()
    {
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

    void UpdateStars(Button levelButton, int levelId)
    {
        if (levelButton == null)
            return;

        if (!TryGetStarsContainer(levelButton, out RectTransform starsContainer))
            return;

        if (!IsSaveDataReady())
        {
            starsContainer.gameObject.SetActive(false);
            return;
        }

        int starsEarned = GetStars(levelId);
        bool hasRecordedLevelProgress = HasRecordedLevelProgress(levelId);
        int previousStars = 0;
        int pendingNewStars = 0;
        bool hasPendingStarReveal = LevelProgressionManager.Instance != null
            && LevelProgressionManager.Instance.TryGetPendingStarReveal(levelId, out previousStars, out pendingNewStars);

        starsContainer.gameObject.SetActive(hasRecordedLevelProgress);

        if (!hasRecordedLevelProgress)
            return;

        for (int i = 0; i < starsContainer.childCount; i++)
        {
            Transform star = starsContainer.GetChild(i);
            if (star != null)
            {
                Image starImage = star.GetComponent<Image>();
                if (starImage != null)
                {
                    bool isEarned = i < starsEarned;
                    SetStar(starImage, isEarned);
                    bool shouldHideForPendingReveal = hasPendingStarReveal
                        && i >= previousStars;

                    star.gameObject.SetActive(!shouldHideForPendingReveal);
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

    private bool HasRecordedLevelProgress(int levelId)
    {
        if (!IsSaveDataReady())
            return false;

        GameData gameData = SaveManager.Instance.GetGameData();
        if (gameData == null)
            return false;

        if (gameData.levelStars != null && gameData.levelStars.ContainsKey(levelId))
            return true;

        if (gameData.levelProgressData != null
            && gameData.levelProgressData.TryGetValue(levelId, out LevelProgressData progress))
        {
            return progress != null
                && (progress.totalAttempts > 0
                    || progress.isCompleted
                    || progress.maxStarsEarned > 0
                    || progress.completedObjectiveIds != null && progress.completedObjectiveIds.Count > 0
                    || progress.bestTimeSeconds < float.MaxValue
                    || progress.bestMoves < int.MaxValue);
        }

        return false;
    }

    private bool IsSaveDataReady()
    {
        return SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded;
    }

    private void HideAllStarContainers()
    {
        foreach (Button levelButton in levelButtons)
        {
            if (levelButton == null)
                continue;

            if (TryGetStarsContainer(levelButton, out RectTransform starsContainer))
                starsContainer.gameObject.SetActive(false);
        }
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
        if (LevelProgressionManager.Instance == null)
        {
            return;
        }

        int requiredStars = LevelProgressionManager.Instance.GetRequiredStarsForBoss(levelId);
        if (requiredStars > 0)
        {
        }
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
                levelButtonImages[i].color = isUnlocked ? unlockedButtonColor : GetLockedColor(levelId);
            }

            if (isUnlocked) unlockedCount++;
            else lockedCount++;
        }

        UpdateButtonProgression();
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

    public void HideAllLevelButtons()
    {
        foreach (var btn in levelButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    public void ShowAllLevelButtonsInstantly()
    {
        ShowButtonsInstantly(levelButtons);
    }

    public void AnimateLevelButtonsReveal(IEnumerable<Button> buttons, string reason = null)
    {
        AnimateButtons(buttons);
    }

    public void ShowButtonsInstantly(IEnumerable<Button> buttons)
    {
        if (buttons == null) return;

        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            btn.gameObject.SetActive(true);

            RectTransform rt = GetButtonRectTransform(btn);
            if (rt != null)
            {
                DOTween.Kill(rt);

                if (savedButtonPositions.TryGetValue(btn, out var saved))
                    rt.anchoredPosition = saved;

                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }

            Image img = GetButtonImage(btn);
            if (img != null)
                DOTween.Kill(img);
        }
    }

    private void AnimateHeavySingleButton(Button button, int sequenceIndex)
    {
        if (button == null) return;

        RectTransform rectTransform = GetButtonRectTransform(button);
        if (rectTransform == null)
            return;

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

        RectTransform rectTransform = GetButtonRectTransform(button);
        if (rectTransform == null)
            return;

        Vector2 sideShake = new Vector2(UnityEngine.Random.Range(-15f, 15f), 0f);
        rectTransform.DOPunchAnchorPos(sideShake, 0.4f, 8, 1f);

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 6, 0.8f);

        Image buttonImage = GetButtonImage(button);
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
        StopStarRevealPresentation();

        for (int i = activeButtonSequences.Count - 1; i >= 0; i--)
        {
            Sequence sequence = activeButtonSequences[i];
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }
        activeButtonSequences.Clear();

        foreach (var kvp in savedButtonPositions)
        {
            var btn = kvp.Key;
            if (btn == null) continue;

            RectTransform rt = GetButtonRectTransform(btn);
            if (rt != null)
            {
                DOTween.Kill(rt);
                rt.anchoredPosition = kvp.Value;
                rt.localRotation = Quaternion.identity;
            }

            Image img = GetButtonImage(btn);
            if (img != null)
                DOTween.Kill(img);
        }
    }

    public void TryPlayPendingStarRevealAnimations()
    {
        TryPlayPendingCompletionAnimations(out _);
    }

    public bool TryPlayPendingCompletionAnimations(out float estimatedDuration)
    {
        estimatedDuration = 0f;

        if (LevelProgressionManager.Instance == null || _pendingStarRevealPlaybackRoutine != null)
            return false;

        List<PendingStarRevealPresentation> pendingReveals = CollectPendingStarRevealPresentations();
        if (pendingReveals.Count == 0)
            return false;

        estimatedDuration = EstimatePendingRevealSequenceDuration(pendingReveals);
        _pendingStarRevealPlaybackRoutine = StartCoroutine(PlayPendingStarRevealSequence(pendingReveals));
        return true;
    }

    public void StopStarRevealPresentation()
    {
        if (_pendingStarRevealPlaybackRoutine != null)
        {
            StopCoroutine(_pendingStarRevealPlaybackRoutine);
            _pendingStarRevealPlaybackRoutine = null;
        }

        KillAllStarTweens();
        StopAllCelebrationEffects();
        UpdateButtonProgression();
    }

    private List<PendingStarRevealPresentation> CollectPendingStarRevealPresentations()
    {
        List<PendingStarRevealPresentation> pendingReveals = new();

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button levelButton = levelButtons[i];
            if (levelButton == null || !levelButton.gameObject.activeInHierarchy)
                continue;

            int levelId = i + 1;
            bool isBossLevel = LevelProgressionManager.Instance.IsBossLevel(levelId);

            if (isBossLevel)
            {
                if (!LevelProgressionManager.Instance.ConsumePendingBossCompletionFeedback(levelId))
                    continue;

                RectTransform feedbackFxAnchor = ResolveBossFeedbackFxAnchor(levelButton);
                pendingReveals.Add(new PendingStarRevealPresentation(
                    levelButton,
                    starsContainer: null,
                    feedbackFxAnchor,
                    previousStars: 0,
                    newStars: 0,
                    levelId,
                    animateStars: false));
                continue;
            }

            if (!TryGetStarsContainer(levelButton, out RectTransform starsContainer) || !starsContainer.gameObject.activeInHierarchy)
                continue;

            if (!LevelProgressionManager.Instance.ConsumePendingStarReveal(levelId, out int previousStars, out int newStars))
                continue;

            pendingReveals.Add(new PendingStarRevealPresentation(
                levelButton,
                starsContainer,
                starsContainer,
                previousStars,
                newStars,
                levelId,
                animateStars: true));
        }

        return pendingReveals;
    }

    private IEnumerator PlayPendingStarRevealSequence(List<PendingStarRevealPresentation> pendingReveals)
    {
        try
        {
            foreach (PendingStarRevealPresentation reveal in pendingReveals)
            {
                if (!isActiveAndEnabled)
                    yield break;

                if (reveal.Button == null || !reveal.Button.gameObject.activeInHierarchy)
                    continue;

                PlayStarRevealCelebration(reveal, out float celebrationLeadDelay, out float celebrationWaitDuration);

                if (celebrationLeadDelay > 0f)
                    yield return new WaitForSecondsRealtime(celebrationLeadDelay);

                float revealDuration = reveal.AnimateStars
                    ? AnimateNewStars(reveal.StarsContainer, reveal.PreviousStars, reveal.NewStars, reveal.LevelId)
                    : 0f;

                float waitDuration = reveal.AnimateStars ? revealDuration : celebrationWaitDuration;
                if (waitDuration > 0f)
                    yield return new WaitForSecondsRealtime(waitDuration + _starRevealLevelGap);
            }
        }
        finally
        {
            _pendingStarRevealPlaybackRoutine = null;
        }
    }

    private float AnimateNewStars(RectTransform starsContainer, int previousStars, int newStars, int levelId)
    {
        if (starsContainer == null)
            return 0f;

        int clampedPrevious = Mathf.Clamp(previousStars, 0, starsContainer.childCount);
        int clampedNew = Mathf.Clamp(newStars, clampedPrevious, starsContainer.childCount);
        if (clampedNew <= clampedPrevious)
            return 0f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ButtonManager] StarReveal -> Level={levelId} | Previous={clampedPrevious} | New={clampedNew}");
#endif

        float revealSpacing = Mathf.Max(_starRevealStagger, _starPunchDuration + 0.06f);
        int revealEnd = starsContainer.childCount;

        for (int i = clampedPrevious; i < revealEnd; i++)
        {
            if (!(starsContainer.GetChild(i) is RectTransform starRect))
                continue;

            Image starImage = starRect.GetComponent<Image>();
            if (starImage == null)
                continue;

            DOTween.Kill(starRect);
            starRect.localScale = Vector3.one;
            starRect.gameObject.SetActive(false);

            Sequence starSequence = DOTween.Sequence();
            _activeStarSequences.Add(starSequence);

            float delay = (i - clampedPrevious) * revealSpacing;
            starSequence.AppendInterval(delay);
            starSequence.AppendCallback(() =>
            {
                if (starRect == null)
                    return;

                starRect.gameObject.SetActive(true);
                starRect.localScale = Vector3.one;
                starRect.DOPunchScale(_starPunchStrength, _starPunchDuration, vibrato: 1, elasticity: 0.4f);
            });
            starSequence.AppendInterval(_starPunchDuration);
            starSequence.OnKill(() =>
            {
                _activeStarSequences.Remove(starSequence);
                if (starRect != null)
                {
                    starRect.gameObject.SetActive(true);
                    starRect.localScale = Vector3.one;
                }
            });
        }

        return ((revealEnd - clampedPrevious - 1) * revealSpacing) + _starPunchDuration;
    }

    private float EstimatePendingRevealSequenceDuration(List<PendingStarRevealPresentation> pendingReveals)
    {
        float totalDuration = 0f;
        float celebrationLeadDelay = GetCelebrationLeadDelay();
        float celebrationWaitDuration = GetCelebrationWaitDurationAfterLead();

        for (int i = 0; i < pendingReveals.Count; i++)
        {
            PendingStarRevealPresentation reveal = pendingReveals[i];
            totalDuration += celebrationLeadDelay;

            float presentationDuration = reveal.AnimateStars
                ? CalculateStarRevealDuration(reveal.StarsContainer, reveal.PreviousStars, reveal.NewStars)
                : celebrationWaitDuration;

            if (presentationDuration > 0f)
                totalDuration += presentationDuration + _starRevealLevelGap;
        }

        return totalDuration;
    }

    private void PlayStarRevealCelebration(PendingStarRevealPresentation reveal, out float leadDelay, out float waitDurationAfterLead)
    {
        leadDelay = 0f;
        waitDurationAfterLead = 0f;

        if (_starRevealCelebrationEffectPrefab == null || reveal.Button == null)
            return;

        StarRevealCelebrationEffect effect = GetCelebrationEffect();
        if (effect == null)
            return;

        RectTransform anchor = reveal.FeedbackFxAnchor != null
            ? reveal.FeedbackFxAnchor
            : (reveal.StarsContainer != null ? reveal.StarsContainer : GetButtonRectTransform(reveal.Button));
        Transform effectParent = reveal.Button.transform;
        Vector3 localPosition = Vector3.zero;
        if (anchor != null && anchor != effectParent)
            localPosition = effectParent.InverseTransformPoint(anchor.position);

        effect.transform.SetParent(effectParent, false);
        effect.transform.localPosition = localPosition;
        effect.transform.localRotation = Quaternion.identity;
        effect.transform.localScale = Vector3.one * CalculateCelebrationEffectScale(reveal.Button, anchor);
        effect.Play();
        leadDelay = effect.LeadDelayBeforeStarReveal;
        waitDurationAfterLead = Mathf.Max(0f, effect.TotalDuration - leadDelay);
    }

    private float CalculateCelebrationEffectScale(Button button, RectTransform anchor)
    {
        float referenceSize = 0f;

        RectTransform buttonRectTransform = GetButtonRectTransform(button);
        if (buttonRectTransform != null)
            referenceSize = Mathf.Max(referenceSize, buttonRectTransform.rect.width, buttonRectTransform.rect.height);

        if (anchor != null)
            referenceSize = Mathf.Max(referenceSize, anchor.rect.width, anchor.rect.height);

        if (referenceSize <= 0f)
            return _starRevealCelebrationMinScale;

        float rawScale = referenceSize / _starRevealCelebrationReferenceButtonSize;
        return Mathf.Clamp(rawScale, _starRevealCelebrationMinScale, _starRevealCelebrationMaxScale);
    }

    private float CalculateStarRevealDuration(RectTransform starsContainer, int previousStars, int newStars)
    {
        if (starsContainer == null)
            return 0f;

        int clampedPrevious = Mathf.Clamp(previousStars, 0, starsContainer.childCount);
        int clampedNew = Mathf.Clamp(newStars, clampedPrevious, starsContainer.childCount);
        if (clampedNew <= clampedPrevious)
            return 0f;

        float revealSpacing = Mathf.Max(_starRevealStagger, _starPunchDuration + 0.06f);
        return ((clampedNew - clampedPrevious - 1) * revealSpacing) + _starPunchDuration;
    }

    private float GetCelebrationLeadDelay()
    {
        return _starRevealCelebrationEffectPrefab != null
            ? _starRevealCelebrationEffectPrefab.LeadDelayBeforeStarReveal
            : 0f;
    }

    private float GetCelebrationWaitDurationAfterLead()
    {
        if (_starRevealCelebrationEffectPrefab == null)
            return 0f;

        return Mathf.Max(0f, _starRevealCelebrationEffectPrefab.TotalDuration - _starRevealCelebrationEffectPrefab.LeadDelayBeforeStarReveal);
    }

    private RectTransform ResolveBossFeedbackFxAnchor(Button button)
    {
        BossLevelProgressUI bossProgress = GetBossProgress(button);
        if (bossProgress != null)
        {
            RectTransform configuredAnchor = bossProgress.GetCompletionFeedbackAnchor();
            if (configuredAnchor != null)
                return configuredAnchor;
        }

        if (TryGetStarsContainer(button, out RectTransform starsContainer))
            return starsContainer;

        return GetButtonRectTransform(button);
    }

    private BossLevelProgressUI GetBossProgress(Button button)
    {
        if (button == null)
            return null;

        if (_bossProgressByButton.TryGetValue(button, out BossLevelProgressUI bossProgress) && bossProgress != null)
            return bossProgress;

        bossProgress = button.GetComponentInChildren<BossLevelProgressUI>(true);
        if (bossProgress != null)
            _bossProgressByButton[button] = bossProgress;

        return bossProgress;
    }

    private StarRevealCelebrationEffect GetCelebrationEffect()
    {
        while (_availableCelebrationEffects.Count > 0)
        {
            StarRevealCelebrationEffect pooledEffect = _availableCelebrationEffects.Pop();
            if (pooledEffect == null)
                continue;

            _activeCelebrationEffects.Add(pooledEffect);
            return pooledEffect;
        }

        if (_starRevealCelebrationEffectPrefab == null)
            return null;

        StarRevealCelebrationEffect createdEffect = Instantiate(_starRevealCelebrationEffectPrefab, transform);
        createdEffect.Initialize(ReturnCelebrationEffectToPool);
        _activeCelebrationEffects.Add(createdEffect);
        return createdEffect;
    }

    private void ReturnCelebrationEffectToPool(StarRevealCelebrationEffect effect)
    {
        if (effect == null)
            return;

        effect.transform.SetParent(transform, false);
        _activeCelebrationEffects.Remove(effect);

        if (!_availableCelebrationEffects.Contains(effect))
            _availableCelebrationEffects.Push(effect);
    }

    private void StopAllCelebrationEffects()
    {
        while (_activeCelebrationEffects.Count > 0)
        {
            StarRevealCelebrationEffect effect = null;
            foreach (StarRevealCelebrationEffect activeEffect in _activeCelebrationEffects)
            {
                effect = activeEffect;
                break;
            }

            if (effect == null)
                break;

            effect.StopAndRelease();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Play Star Reveal Celebration Test")]
    private void ContextMenuPlayStarRevealCelebrationTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[ButtonManager] Star reveal celebration test requires Play Mode.", this);
            return;
        }

        if (_starRevealCelebrationEffectPrefab == null)
        {
            Debug.LogWarning("[ButtonManager] Star reveal celebration effect prefab is not assigned.", this);
            return;
        }

        Button targetButton = ResolveCelebrationTestButton();
        if (targetButton == null)
        {
            Debug.LogWarning("[ButtonManager] No level button is available for star reveal celebration test.", this);
            return;
        }

        StopAllCelebrationEffects();

        RectTransform starsContainer = null;
        TryGetStarsContainer(targetButton, out starsContainer);

        PendingStarRevealPresentation reveal = new PendingStarRevealPresentation(
            targetButton,
            starsContainer,
            ResolveBossFeedbackFxAnchor(targetButton),
            previousStars: 0,
            newStars: 3,
            levelId: 0,
            animateStars: false);

        PlayStarRevealCelebration(reveal, out _, out _);
    }

    [ContextMenu("Debug/Stop Star Reveal Celebration Test")]
    private void ContextMenuStopStarRevealCelebrationTest()
    {
        StopAllCelebrationEffects();
    }

    private Button ResolveCelebrationTestButton()
    {
        GameObject selectedObject = Selection.activeGameObject;
        if (selectedObject != null)
        {
            Button selectedButton = selectedObject.GetComponentInParent<Button>();
            if (selectedButton != null && selectedButton.gameObject.activeInHierarchy)
                return selectedButton;
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null && levelButtons[i].gameObject.activeInHierarchy)
                return levelButtons[i];
        }

        return null;
    }
#endif

    private void KillAllStarTweens()
    {
        for (int i = _activeStarSequences.Count - 1; i >= 0; i--)
        {
            Sequence sequence = _activeStarSequences[i];
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }

        _activeStarSequences.Clear();

        foreach (Button levelButton in levelButtons)
        {
            if (levelButton == null)
                continue;

            if (!TryGetStarsContainer(levelButton, out RectTransform starsContainer))
                continue;

            for (int i = 0; i < starsContainer.childCount; i++)
            {
                if (starsContainer.GetChild(i) is RectTransform starRect)
                {
                    DOTween.Kill(starRect);
                    starRect.localScale = Vector3.one;
                }
            }
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

    private RectTransform GetButtonRectTransform(Button button)
    {
        if (button == null)
            return null;

        if (_buttonRectTransforms.TryGetValue(button, out RectTransform rectTransform))
            return rectTransform;

        rectTransform = button.GetComponent<RectTransform>();
        _buttonRectTransforms[button] = rectTransform;
        return rectTransform;
    }

    private Image GetButtonImage(Button button)
    {
        if (button == null)
            return null;

        if (_buttonImages.TryGetValue(button, out Image image))
            return image;

        image = button.GetComponent<Image>();
        _buttonImages[button] = image;
        return image;
    }

    private bool TryGetStarsContainer(Button button, out RectTransform starsContainer)
    {
        if (button != null && _buttonStarsContainers.TryGetValue(button, out starsContainer) && starsContainer != null)
            return true;

        starsContainer = button != null ? button.transform.Find("Stars") as RectTransform : null;
        if (button != null && starsContainer != null)
            _buttonStarsContainers[button] = starsContainer;

        return starsContainer != null;
    }
}

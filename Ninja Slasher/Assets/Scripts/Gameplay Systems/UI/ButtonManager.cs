using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float fallDistance = 800f;
    [SerializeField] private float waveDelay = 0.08f;
    [SerializeField] private bool addRotationEffect = true;
    [SerializeField] private bool addImpactEffect = true;

    private List<Sequence> activeButtonSequences = new List<Sequence>();
    private List<Sequence> _activeStarSequences = new List<Sequence>();
    private Dictionary<Button, Vector2> savedButtonPositions = new Dictionary<Button, Vector2>();

    private UIAudioContext _audioContext;

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
        foreach (var btn in levelButtons)
        {
            if (btn != null)
            {
                RectTransform rt = btn.GetComponent<RectTransform>();
                savedButtonPositions[btn] = rt.anchoredPosition;
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

        Transform buttonTransform = levelButton.transform;
        Transform starsContainer = buttonTransform.Find("Stars");
        if (starsContainer == null)
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

            Transform starsContainer = levelButton.transform.Find("Stars");
            if (starsContainer != null)
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
            Debug.Log($"[ButtonManager] Nivel {levelId} esta bloqueado");
            return;
        }

        int requiredStars = LevelProgressionManager.Instance.GetRequiredStarsForBoss(levelId);
        if (requiredStars > 0)
        {
            var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
            int deficit = requiredStars - totalStars;
            Debug.Log($"[ButtonManager] Nivel {levelId} es un nivel jefe. Necesitas {requiredStars} estrellas (te faltan {Mathf.Max(0, deficit)}).");
        }
        else
        {
            Debug.Log($"[ButtonManager] Nivel {levelId} esta bloqueado");
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
        Debug.Log("[ButtonManager] Render -> HideAllLevelButtons");

        foreach (var btn in levelButtons)
        {
            if (btn != null)
                btn.gameObject.SetActive(false);
        }
    }

    public void ShowAllLevelButtonsInstantly()
    {
        Debug.Log("[ButtonManager] Render -> ShowAllLevelButtonsInstantly");
        ShowButtonsInstantly(levelButtons);
    }

    public void AnimateLevelButtonsReveal(IEnumerable<Button> buttons, string reason = null)
    {
        Debug.Log($"[ButtonManager] Render -> AnimateLevelButtonsReveal | Reason={reason ?? "Unspecified"}");
        AnimateButtons(buttons);
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
        KillAllStarTweens();

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

    public void TryPlayPendingStarRevealAnimations()
    {
        if (LevelProgressionManager.Instance == null)
            return;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Button levelButton = levelButtons[i];
            if (levelButton == null || !levelButton.gameObject.activeInHierarchy)
                continue;

            Transform starsContainer = levelButton.transform.Find("Stars");
            if (starsContainer == null || !starsContainer.gameObject.activeInHierarchy)
                continue;

            int levelId = i + 1;
            if (!LevelProgressionManager.Instance.ConsumePendingStarReveal(levelId, out int previousStars, out int newStars))
                continue;

            AnimateNewStars(starsContainer, previousStars, newStars, levelId);
        }
    }

    private void AnimateNewStars(Transform starsContainer, int previousStars, int newStars, int levelId)
    {
        if (starsContainer == null)
            return;

        int clampedPrevious = Mathf.Clamp(previousStars, 0, starsContainer.childCount);
        int clampedNew = Mathf.Clamp(newStars, clampedPrevious, starsContainer.childCount);
        if (clampedNew <= clampedPrevious)
            return;

        Debug.Log($"[ButtonManager] StarReveal -> Level={levelId} | Previous={clampedPrevious} | New={clampedNew}");

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
    }

    private void KillAllStarTweens()
    {
        foreach (Sequence sequence in _activeStarSequences.ToList())
        {
            if (sequence != null && sequence.IsActive())
                sequence.Kill(false);
        }

        _activeStarSequences.Clear();

        foreach (Button levelButton in levelButtons)
        {
            if (levelButton == null)
                continue;

            Transform starsContainer = levelButton.transform.Find("Stars");
            if (starsContainer == null)
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
}

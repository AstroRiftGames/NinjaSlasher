using System.Collections;
using TMPro;
using UnityEngine;

public class BossLevelProgressUI : MonoBehaviour
{
    [Header("SETTINGS")]
    [Tooltip("ID del nivel jefe al que pertenece este botón")]
    [SerializeField] private int _levelId;

    [Header("UI REFERENCES")]
    [Tooltip("Raíz del bloque visual (ícono + texto). Se activa solo cuando el boss está bloqueado")]
    [SerializeField] private GameObject _feedbackRoot;

    [Tooltip("Texto del progreso: '12/15'")]
    [SerializeField] private TextMeshProUGUI _progressLabel;

    [Tooltip("Visual alternativo mostrado cuando el boss ya fue completado")]
    [SerializeField] private GameObject _completedStarVisual;

    [Tooltip("Anchor opcional para fireworks/completion FX. Si no está asignado, usa la estrella completada o este mismo bloque.")]
    [SerializeField] private Transform _feedbackFxAnchor;

    private void OnEnable()
    {
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnProgressionUpdated += Refresh;
        else
            StartCoroutine(DelayedSubscription());

        SaveManager.OnDataLoaded += OnDataLoaded;
        Refresh();
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnProgressionUpdated -= Refresh;

        SaveManager.OnDataLoaded -= OnDataLoaded;
    }

    private IEnumerator DelayedSubscription()
    {
        yield return null;
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnProgressionUpdated += Refresh;
        Refresh();
    }

    private void OnDataLoaded(GameData _) => Refresh();

    public void Refresh()
    {
        RefreshBossLevelStatusVisual();
    }

    private void RefreshBossLevelStatusVisual()
    {
        if (LevelProgressionManager.Instance == null || SaveManager.Instance == null)
        {
            RefreshBossRequirementVisual(isBossLevel: false, isBossCompleted: false, totalStars: 0, requiredStars: 0);
            RefreshBossCompletionVisual(isBossLevel: false, isBossCompleted: false);
            return;
        }

        bool isBossLevel = LevelProgressionManager.Instance.IsBossLevel(_levelId);
        bool isBossCompleted = isBossLevel && LevelProgressionManager.Instance.IsLevelCompleted(_levelId);
        int requiredStars = isBossLevel ? LevelProgressionManager.Instance.GetRequiredStarsForBoss(_levelId) : 0;
        int totalStars = 0;

        if (isBossLevel && !isBossCompleted)
        {
            (_, _, totalStars) = SaveManager.Instance.GetProgressionData();
        }

        RefreshBossRequirementVisual(isBossLevel, isBossCompleted, totalStars, requiredStars);
        RefreshBossCompletionVisual(isBossLevel, isBossCompleted);
    }

    private void RefreshBossRequirementVisual(bool isBossLevel, bool isBossCompleted, int totalStars, int requiredStars)
    {
        if (_feedbackRoot == null)
            return;

        bool shouldShowRequirement = isBossLevel && !isBossCompleted;
        _feedbackRoot.SetActive(shouldShowRequirement);

        if (!shouldShowRequirement || _progressLabel == null)
            return;

        _progressLabel.text = $"{totalStars}/{requiredStars}";
    }

    private void RefreshBossCompletionVisual(bool isBossLevel, bool isBossCompleted)
    {
        if (_completedStarVisual == null)
        {
            return;
        }

        _completedStarVisual.SetActive(isBossLevel && isBossCompleted);
    }

    public RectTransform GetCompletionFeedbackAnchor()
    {
        if (_feedbackFxAnchor is RectTransform configuredAnchor)
            return configuredAnchor;

        if (_completedStarVisual != null && _completedStarVisual.transform is RectTransform completedStarRect)
            return completedStarRect;

        if (_feedbackRoot != null && _feedbackRoot.transform is RectTransform feedbackRootRect)
            return feedbackRootRect;

        return transform as RectTransform;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Forzar Refresh")]
    private void DebugRefresh() => Refresh();
#endif
}

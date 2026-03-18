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
        if (_feedbackRoot == null || _progressLabel == null) return;
        if (LevelProgressionManager.Instance == null || SaveManager.Instance == null) return;

        int required = LevelProgressionManager.Instance.GetRequiredStarsForBoss(_levelId);

        if (required <= 0)
        {
            _feedbackRoot.SetActive(false);
            return;
        }

        bool isUnlocked = LevelProgressionManager.Instance.IsLevelUnlocked(_levelId);

        if (isUnlocked)
        {
            _feedbackRoot.SetActive(false);
        }
        else
        {
            var (_, _, totalStars) = SaveManager.Instance.GetProgressionData();
            _progressLabel.text = $"{totalStars}/{required}";
            _feedbackRoot.SetActive(true);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Forzar Refresh")]
    private void DebugRefresh() => Refresh();
#endif
}

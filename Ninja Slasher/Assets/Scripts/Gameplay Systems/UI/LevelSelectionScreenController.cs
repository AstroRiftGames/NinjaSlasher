using UnityEngine;
using TMPro;

public class LevelSelectionScreenController : MonoBehaviour
{
    [Header("Areas")]
    [SerializeField] private AreaSectionController[] _areas;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _totalStarsText;

    private void OnEnable()
    {
        RefreshAll();
        UpdateTotalStarsDisplay();

        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnNewAreaUnlocked += HandleNewAreaUnlocked;
            LevelProgressionManager.Instance.OnProgressionUpdated += UpdateTotalStarsDisplay;
        }

        foreach (var area in _areas)
        {
            if (area != null)
                area.OnUnlocked += OnAreaUnlockAnimationComplete;
        }
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
        {
            LevelProgressionManager.Instance.OnNewAreaUnlocked -= HandleNewAreaUnlocked;
            LevelProgressionManager.Instance.OnProgressionUpdated -= UpdateTotalStarsDisplay;
        }

        foreach (var area in _areas)
        {
            if (area != null)
                area.OnUnlocked -= OnAreaUnlockAnimationComplete;
        }
    }

    private void RefreshAll()
    {
        foreach (var area in _areas)
        {
            if (area != null)
                area.Refresh();
        }
    }

    private void UpdateTotalStarsDisplay()
    {
        if (_totalStarsText == null) return;

        // Obtiene las estrellas totales acumuladas del jugador
        var (_, _, totalStars) = SaveManager.Instance?.GetProgressionData() ?? (1, 1, 0);
        _totalStarsText.text = totalStars.ToString();
    }

    private void HandleNewAreaUnlocked(int areaId)
    {
        Debug.Log($"[LevelSelectionScreenController] Area {areaId} unlocked.");
    }

    private void OnAreaUnlockAnimationComplete(AreaSectionController area)
    {
        // Refresca los botones de niveles tras completar la animación de nubes
        ButtonManager.Instance?.RefreshLevelProgression();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Refresh Todas las Áreas")]
    private void ContextMenuRefreshAll() => RefreshAll();
#endif
}

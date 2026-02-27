using UnityEngine;

public class LevelSelectionScreenController : MonoBehaviour
{
    [Header("Areas")]
    [SerializeField] private AreaSectionController[] _areas;

    private void OnEnable()
    {
        RefreshAll();

        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnNewAreaUnlocked += HandleNewAreaUnlocked;
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnNewAreaUnlocked -= HandleNewAreaUnlocked;
    }

    private void RefreshAll()
    {
        foreach (var area in _areas)
        {
            if (area != null)
                area.Refresh();
        }
    }

    private void HandleNewAreaUnlocked(int areaId)
    {
        Debug.Log($"[LevelSelectionScreenController] Area {areaId} unlocked.");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Refresh Todas las Áreas")]
    private void ContextMenuRefreshAll() => RefreshAll();
#endif
}

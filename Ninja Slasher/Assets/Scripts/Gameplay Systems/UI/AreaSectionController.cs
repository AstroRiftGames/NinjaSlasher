using UnityEngine;

public class AreaSectionController : MonoBehaviour
{
    [Header("SETTINGS")]
    [SerializeField] private AreaData _areaData;

    [Header("REFERENCES")]
    [SerializeField] private AreaCloudLockVisual _lockOverlay;
    [SerializeField] private CanvasGroup _levelButtonsGroup;

    private void OnEnable()
    {
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnProgressionUpdated += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (LevelProgressionManager.Instance != null)
            LevelProgressionManager.Instance.OnProgressionUpdated -= Refresh;
    }

    public void Refresh()
    {
        if (_areaData == null)
        {
            Debug.LogWarning($"[AreaSectionController] '{gameObject.name}' no tiene AreaData asignada.", this);
            return;
        }

        bool unlocked = IsAreaUnlocked();
        SetLocked(!unlocked);
    }

    private bool IsAreaUnlocked()
    {
        if (LevelProgressionManager.Instance != null)
            return LevelProgressionManager.Instance.IsAreaUnlocked(_areaData.areaId);

        return _areaData.areaId == 1;
    }

    private void SetLocked(bool locked)
    {
        SetLockOverlay(locked);
        SetButtonsInteractable(!locked);
    }

    private void SetLockOverlay(bool visible)
    {
        if (_lockOverlay == null) return;

        _lockOverlay.gameObject.SetActive(visible);

        if (visible)
            _lockOverlay.Rebuild();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (_levelButtonsGroup == null) return;

        _levelButtonsGroup.interactable    = interactable;
        _levelButtonsGroup.blocksRaycasts  = interactable;
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Refresh")]
    private void ContextMenuRefresh() => Refresh();

    [ContextMenu("Debug/Forzar Bloqueado")]
    private void ContextMenuLock() => SetLocked(true);

    [ContextMenu("Debug/Forzar Desbloqueado")]
    private void ContextMenuUnlock() => SetLocked(false);
#endif
}

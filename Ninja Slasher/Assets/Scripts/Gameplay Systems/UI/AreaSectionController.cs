using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AreaSectionController : MonoBehaviour
{
    public event Action<AreaSectionController> OnUnlocked;

    [Header("SETTINGS")]
    [SerializeField] private AreaData _areaData;

    [Header("REFERENCES")]
    [SerializeField] private AreaCloudLockVisual _lockOverlay;
    [SerializeField] private CanvasGroup _levelButtonsGroup;
    [SerializeField] private ScrollRect _scrollRect;

    private bool _lockStateInitialized;
    private bool _lastKnownLockState = true;

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

        bool locked = !IsAreaUnlocked();

        if (LevelProgressionManager.Instance != null &&
            LevelProgressionManager.Instance.ConsumePendingAreaUnlock(_areaData.areaId))
        {
            SetLockOverlay(true);
            SetButtonsInteractable(false);
            _lastKnownLockState = true;
            _lockStateInitialized = true;
            PlayUnlock();
            return;
        }

        if (!_lockStateInitialized)
        {
            SetLockOverlay(locked);
            SetButtonsInteractable(!locked);
            _lastKnownLockState  = locked;
            _lockStateInitialized = true;
            return;
        }

        if (locked == _lastKnownLockState) return;

        _lastKnownLockState = locked;

        if (!locked)
            PlayUnlock();
        else
            SetLocked(true);
    }

    private void PlayUnlock()
    {
        StartCoroutine(UnlockSequence());
    }

    private IEnumerator UnlockSequence()
    {
        if (_scrollRect != null)
            yield return StartCoroutine(ScrollToArea());

        if (_lockOverlay != null)
        {
            _lockOverlay.PlayUnlockAnimation(onComplete: () =>
            {
                SetButtonsInteractable(true);
                OnUnlocked?.Invoke(this);
            });
        }
        else
        {
            SetButtonsInteractable(true);
            OnUnlocked?.Invoke(this);
        }
    }

    private IEnumerator ScrollToArea(float duration = 0.7f)
    {
        float start  = _scrollRect.horizontalNormalizedPosition;
        float target = GetTargetNormalizedX();

        if (Mathf.Approximately(start, target)) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _scrollRect.horizontalNormalizedPosition = Mathf.Lerp(start, target, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _scrollRect.horizontalNormalizedPosition = target;
    }

    private float GetTargetNormalizedX()
    {
        RectTransform content  = _scrollRect.content;
        RectTransform viewport = _scrollRect.viewport != null
            ? _scrollRect.viewport
            : (RectTransform)_scrollRect.transform;
        RectTransform self = GetComponent<RectTransform>();

        Vector3 worldCenter = self.TransformPoint(self.rect.center);
        float   areaLocalX  = content.InverseTransformPoint(worldCenter).x;

        float scrollRange = content.rect.width - viewport.rect.width;
        if (scrollRange <= 0f) return 0f;

        float normalized = (areaLocalX - viewport.rect.width * 0.5f) / scrollRange;
        return Mathf.Clamp01(normalized);
    }

    public bool IsUnlocked() => IsAreaUnlocked();

    public Button[] GetAreaButtons()
    {
        if (_levelButtonsGroup == null) return System.Array.Empty<Button>();
        return _levelButtonsGroup.GetComponentsInChildren<Button>(includeInactive: true);
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

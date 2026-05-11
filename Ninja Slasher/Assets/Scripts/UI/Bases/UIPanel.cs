using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class UIPanel : MonoBehaviour
{
    private static readonly List<UIPanel> BlockingPanels = new();
    public static event System.Action OnBlockingPanelVisibilityChanged;

    [SerializeField] protected CanvasGroup _canvasGroup;
    [SerializeField] protected RectTransform _panelTransform;

    protected bool _isVisible = false;

    protected UIAudioContext _audioContext;
    private bool _selfInputEnabled = true;
    private bool _isBlockedByHigherPanel = false;

    protected virtual bool BlocksUnderlyingUI => false;

    protected virtual void Awake()
    {
        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null && BlocksUnderlyingUI)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    public virtual void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        OnShown();
    }

    public virtual void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        OnHidden();

        gameObject.SetActive(false);
    }

    public virtual void HideImmediate()
    {
        if (!_isVisible && !gameObject.activeSelf)
            return;

        _isVisible = false;
        SetPanelInputEnabled(false);
        OnHidden();
        gameObject.SetActive(false);
    }

    public virtual IEnumerator ShowRoutine()
    {
        Show();
        yield break;
    }

    public virtual IEnumerator HideRoutine()
    {
        Hide();
        yield break;
    }

    protected virtual void OnShown()
    {
    }

    protected virtual void OnHidden()
    {
    }

    protected virtual void OnShowAnimationCompleted()
    {
    }

    protected virtual void OnHideAnimationCompleted()
    {
    }

    protected virtual void OnEnable()
    {
        if (_isVisible && BlocksUnderlyingUI)
            RegisterBlockingPanel();
    }

    protected virtual void OnDisable()
    {
        if (BlocksUnderlyingUI)
            UnregisterBlockingPanel();
    }

    protected void NotifyPanelShown()
    {
        SetPanelInputEnabled(true);

        if (BlocksUnderlyingUI)
            RegisterBlockingPanel();
    }

    protected void SetPanelInputEnabled(bool enabled)
    {
        _selfInputEnabled = enabled;
        RefreshInputState();
    }

    protected static IEnumerator WaitForSecondsUnscaled(float duration)
    {
        if (duration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    protected static IEnumerator WaitForAnimatorPlayback(Animator animator, float fallbackDuration, int layer = 0)
    {
        if (animator == null)
        {
            yield return WaitForSecondsUnscaled(fallbackDuration);
            yield break;
        }

        yield return null;

        if (animator == null || !animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
            yield break;

        float timeout = Mathf.Max(0.1f, fallbackDuration + 0.5f);
        float elapsed = 0f;
        int initialStateHash = animator.GetCurrentAnimatorStateInfo(layer).fullPathHash;
        bool observedPlayback = false;

        while (elapsed < timeout)
        {
            if (animator == null || !animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
                yield break;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            if (animator.IsInTransition(layer) || state.fullPathHash != initialStateHash)
            {
                observedPlayback = true;
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!observedPlayback)
        {
            yield return WaitForSecondsUnscaled(fallbackDuration);
            yield break;
        }

        elapsed = 0f;
        while (elapsed < timeout)
        {
            if (animator == null || !animator.isActiveAndEnabled || !animator.gameObject.activeInHierarchy)
                yield break;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
            if (!animator.IsInTransition(layer) && state.normalizedTime >= 1f)
                yield break;

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    protected static IEnumerator WaitForGameObjectToDeactivate(GameObject target, float timeout)
    {
        if (target == null)
            yield break;

        float elapsed = 0f;
        while (target.activeInHierarchy && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void SetBlockedByHigherPanel(bool blocked)
    {
        _isBlockedByHigherPanel = blocked;
        RefreshInputState();
    }

    private void RefreshInputState()
    {
        if (_canvasGroup == null)
            return;

        bool allowInput = _isVisible && _selfInputEnabled && !_isBlockedByHigherPanel;
        _canvasGroup.interactable = allowInput;
        _canvasGroup.blocksRaycasts = allowInput;
    }

    private void RegisterBlockingPanel()
    {
        if (!gameObject.activeInHierarchy)
            return;

        BlockingPanels.Remove(this);
        BlockingPanels.Add(this);
        RefreshBlockingPanels();
    }

    private void UnregisterBlockingPanel()
    {
        if (BlockingPanels.Remove(this))
            RefreshBlockingPanels();
    }

    private static void RefreshBlockingPanels()
    {
        for (int i = BlockingPanels.Count - 1; i >= 0; i--)
        {
            UIPanel panel = BlockingPanels[i];
            if (panel == null || !panel.gameObject.activeInHierarchy)
                BlockingPanels.RemoveAt(i);
        }

        int topIndex = BlockingPanels.Count - 1;
        for (int i = 0; i < BlockingPanels.Count; i++)
        {
            BlockingPanels[i].SetBlockedByHigherPanel(i != topIndex);
        }

        if (topIndex >= 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        OnBlockingPanelVisibilityChanged?.Invoke();
    }

    public bool IsVisible => _isVisible;
    public static bool HasVisibleBlockingPanel => BlockingPanels.Count > 0;
    public bool IsBlockedByHigherPanel => _isBlockedByHigherPanel;

    public static string GetBlockingPanelDebugSummary()
    {
        if (BlockingPanels.Count == 0)
            return "None";

        List<string> panelNames = new List<string>(BlockingPanels.Count);
        for (int i = 0; i < BlockingPanels.Count; i++)
        {
            UIPanel panel = BlockingPanels[i];
            panelNames.Add(panel != null ? panel.name : "NullPanel");
        }

        return string.Join(" > ", panelNames);
    }
}

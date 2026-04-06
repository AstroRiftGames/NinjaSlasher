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

    protected virtual void OnShown()
    {
    }

    protected virtual void OnHidden()
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
}

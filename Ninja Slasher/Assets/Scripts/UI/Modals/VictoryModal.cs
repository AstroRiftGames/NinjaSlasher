using UnityEngine;
using UnityEngine.UI;

public class VictoryModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private Button _continueButton;

    [Header("Variant UI")]
    [SerializeField] private GameObject _normalTitleRoot;
    [SerializeField] private GameObject _bossTitleRoot;
    [SerializeField] private GameObject _bossMessageRoot;

    private Button[] _navigationButtons;
    private bool _isObjectiveSequenceRunning;
    private bool _isPreviewAnimation;
    private VictoryContext _context;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        CacheNavigationButtons();
        SetupButtons();
        ResolveVariantRoots();
        ApplyContext(null);
    }

    public void ApplyContext(VictoryContext context)
    {
        _context = context ?? new VictoryContext();
        bool isBossClear = IsBossClear();

        SetActive(_normalTitleRoot, !isBossClear);
        SetActive(_bossTitleRoot, isBossClear);
        SetActive(_bossMessageRoot, isBossClear);
    }

    public void PreviewCompleteAnimation(bool bossClear = true)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[VictoryModal] PreviewCompleteAnimation requiere Play Mode para reproducir animaciones y tweens.");
            return;
        }

        if (_isVisible || _isOpening || _isClosing || gameObject.activeSelf)
            HideImmediate();

        VictoryModalVariant variant = bossClear ? VictoryModalVariant.BossClear : VictoryModalVariant.Normal;
        var previewContext = new VictoryContext
        {
            Variant = variant,
            IsBossLevel = bossClear,
            StarsEarned = bossClear ? 1 : 3
        };

        _isPreviewAnimation = true;
        UIEvents.SetVictoryContext(previewContext);
        ApplyContext(previewContext);
        Show();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Preview Boss Victory Animation")]
    private void PreviewBossVictoryAnimationFromInspector()
    {
        PreviewCompleteAnimation(true);
    }

    [ContextMenu("Debug/Preview Normal Victory Animation")]
    private void PreviewNormalVictoryAnimationFromInspector()
    {
        PreviewCompleteAnimation(false);
    }
#endif

    protected override void OnShown()
    {
        ApplyContext(_context);
        PlayVictoryAudio();
        BeginObjectiveSequence();

        if (ResultsUIManager.Instance != null)
        {
            if (_isPreviewAnimation)
                ResultsUIManager.Instance.PreparePreviewResultsIntro(IsBossClear());
            else
                ResultsUIManager.Instance.PrepareResultsIntro();
        }
    }

    protected override void OnShowAnimationCompleted()
    {
        if (ResultsUIManager.Instance != null)
        {
            if (_isPreviewAnimation)
                ResultsUIManager.Instance.ShowPreviewResultsPanel(IsBossClear(), HandleObjectiveSequenceCompleted);
            else
                ResultsUIManager.Instance.ShowResultsPanel(HandleObjectiveSequenceCompleted);

            return;
        }

        HandleObjectiveSequenceCompleted();
    }

    protected override void OnHidden()
    {
        CancelObjectiveSequence();
        _isPreviewAnimation = false;
    }

    protected override void OnDisable()
    {
        CancelObjectiveSequence();
        base.OnDisable();
    }

    protected override void RequestCloseFromOutsideClick()
    {
        if (_isObjectiveSequenceRunning)
            return;

        base.RequestCloseFromOutsideClick();
    }

    private void CacheNavigationButtons()
    {
        _navigationButtons = GetComponentsInChildren<Button>(true);
    }

    private void SetupButtons()
    {
        if (_continueButton == null)
        {
            Debug.LogWarning("[VictoryModal] Continue button is not assigned.");
            return;
        }

        _continueButton.onClick.RemoveListener(OnContinueClicked);
        _continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void ResolveVariantRoots()
    {
        if (_bossMessageRoot == null)
            _bossMessageRoot = FindChildObject("Boss Level Message");

        if (_bossMessageRoot == null)
            _bossMessageRoot = FindChildObject("Boss Message");
    }

    private GameObject FindChildObject(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.gameObject.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private bool IsBossClear()
    {
        return _context != null && (_context.Variant == VictoryModalVariant.BossClear || _context.IsBossLevel);
    }

    private void BeginObjectiveSequence()
    {
        _isObjectiveSequenceRunning = true;
        SetNavigationButtonsInteractable(false);
    }

    private void HandleObjectiveSequenceCompleted()
    {
        if (!isActiveAndEnabled || !_isVisible)
            return;

        _isObjectiveSequenceRunning = false;
        SetNavigationButtonsInteractable(true);
    }

    private void CancelObjectiveSequence()
    {
        _isObjectiveSequenceRunning = false;
        SetNavigationButtonsInteractable(false);
        ResultsUIManager.Instance?.CancelResultsPresentation();
    }

    private void SetNavigationButtonsInteractable(bool interactable)
    {
        if (_navigationButtons == null || _navigationButtons.Length == 0)
            CacheNavigationButtons();

        if (_navigationButtons == null)
            return;

        for (int i = 0; i < _navigationButtons.Length; i++)
        {
            Button button = _navigationButtons[i];
            if (button != null)
                button.interactable = interactable;
        }
    }

    private void OnContinueClicked()
    {
        if (_isObjectiveSequenceRunning)
            return;

        ResultsUIManager.Instance?.CancelResultsPresentation();

        if (_isPreviewAnimation)
        {
            Hide();
            return;
        }

        SetPanelInputEnabled(false);
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void PlayVictoryAudio()
    {
        if (AudioService.Instance == null || _audioContext == null || _audioContext.Audio == null)
            return;

        bool isVictory = GameManager.Instance != null && GameManager.Instance.IsVictory;
        AudioEvent clip = isVictory ? _audioContext.Audio.victory : _audioContext.Audio.defeat;

        if (clip != null)
            AudioService.Instance.PlaySFX(clip);
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(OnContinueClicked);
    }
}

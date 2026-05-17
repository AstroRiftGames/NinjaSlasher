using UnityEngine;
using UnityEngine.UI;

public class VictoryModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private Button _continueButton;

    private Button[] _navigationButtons;
    private bool _isObjectiveSequenceRunning;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        CacheNavigationButtons();
        SetupButtons();
    }

    protected override void OnShown()
    {
        PlayVictoryAudio();
        BeginObjectiveSequence();

        if (ResultsUIManager.Instance != null)
            ResultsUIManager.Instance.PrepareResultsIntro();
    }

    protected override void OnShowAnimationCompleted()
    {
        if (ResultsUIManager.Instance != null)
        {
            ResultsUIManager.Instance.ShowResultsPanel(HandleObjectiveSequenceCompleted);
            return;
        }

        HandleObjectiveSequenceCompleted();
    }

    protected override void OnHidden()
    {
        CancelObjectiveSequence();
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

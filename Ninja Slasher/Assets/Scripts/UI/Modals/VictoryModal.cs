using UnityEngine;
using UnityEngine.UI;

public class VictoryModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;
    [SerializeField] private Button _continueButton;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        SetupButtons();
    }

    protected override void OnShown()
    {
        PlayVictoryAudio();

        if (ResultsUIManager.Instance != null)
            ResultsUIManager.Instance.PrepareResultsIntro();
    }

    protected override void OnShowAnimationCompleted()
    {
        if (ResultsUIManager.Instance != null)
            ResultsUIManager.Instance.ShowResultsPanel();
    }

    protected override void OnHidden()
    {
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

    private void OnContinueClicked()
    {
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DefeatModal : UIModalBase
{
    [Header("Defeat UI")]
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _livesRemainingText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _quitButton;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_continueButton != null)
            _continueButton.onClick.AddListener(OnContinueClicked);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);
    }

    public void ShowLifeLost(int livesRemaining)
    {
        UpdateUI(livesRemaining);
    }

    public override void Show()
    {
        base.Show();
        PlayDefeatAudio();
    }

    protected override void OnShown()
    {
        PauseController.Instance?.RequestPause(PauseSource.Defeat);
        UIEvents.RaisePause(true);
    }

    protected override void OnHidden()
    {
    }

    protected override void OnHideAnimationCompleted()
    {
        PauseController.Instance?.ReleasePause(PauseSource.Defeat);
        UIEvents.RaisePause(false);
    }

    private void UpdateUI(int livesRemaining)
    {
        int effectiveLives = LifeManager.Instance != null ? LifeManager.Instance.GetEffectiveLivesForCurrentAttempt() : livesRemaining;
        int maxLives = LifeManager.Instance != null ? LifeManager.Instance.GetMaxLives() : GameConfigManager.Config?.maxLives ?? 5;

        if (_titleText != null)
            _titleText.text = effectiveLives > 0 ? "Has fallado" : "Sin Vidas";

        if (_livesRemainingText != null)
            _livesRemainingText.text = $"Vidas: {effectiveLives}/{maxLives}";

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(true);
            _continueButton.interactable = true;
        }

        if (_quitButton != null)
            _quitButton.gameObject.SetActive(true);
    }

    private void OnContinueClicked()
    {
        UIEvents.RaiseRetryPressed();
    }

    private void OnQuitClicked()
    {
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void PlayDefeatAudio()
    {
        if (_audioContext == null)
            return;

        if (AudioService.Instance == null)
            return;

        if (_audioContext.Audio.defeat != null)
            AudioService.Instance.PlaySFX(_audioContext.Audio.defeat);
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveAllListeners();

        if (_quitButton != null)
            _quitButton.onClick.RemoveAllListeners();
    }

    protected override void RequestCloseFromOutsideClick()
    {
        OnQuitClicked();
    }

    private void RequestClose()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.CloseModal(this);
            return;
        }

        Hide();
    }
}

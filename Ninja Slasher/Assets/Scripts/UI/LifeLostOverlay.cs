using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LifeLostOverlay : UIOverlayBase
{
    [Header("Life Lost UI")]
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
        Show();
    }

    protected override void OnShown()
    {
        Debug.Log("[LifeLostOverlay] Vida perdida");

        Time.timeScale = 0f;

        UIEvents.RaisePause(true);
    }

    protected override void OnHidden()
    {
        Debug.Log("[LifeLostOverlay] Continuando juego");

        Time.timeScale = 1f;

        UIEvents.RaisePause(false);
    }

    private void UpdateUI(int livesRemaining)
    {
        if (_titleText != null)
        {
            _titleText.text = livesRemaining > 0 ? "¡Vida Perdida!" : "Sin Vidas";
        }

        if (_livesRemainingText != null)
        {
            int maxLives = GameConfigManager.Config?.maxLives ?? 5;
            _livesRemainingText.text = $"Vidas: {livesRemaining}/{maxLives}";
        }

        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(livesRemaining > 0);
        }

        if (_quitButton != null)
        {
            _quitButton.gameObject.SetActive(true);
        }
    }

    private void OnContinueClicked()
    {
        Hide();
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnDestroy()
    {
        if (_continueButton != null)
            _continueButton.onClick.RemoveAllListeners();

        if (_quitButton != null)
            _quitButton.onClick.RemoveAllListeners();
    }
}
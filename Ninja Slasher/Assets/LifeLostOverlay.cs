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

    [Header("Auto-hide Settings")]
    [SerializeField] private bool _autoHide = true;
    [SerializeField] private float _autoHideDelay = 3f;

    private Coroutine _autoHideCoroutine;

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

        if (_autoHide)
        {
            if (_autoHideCoroutine != null)
                StopCoroutine(_autoHideCoroutine);

            _autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
        }
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

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(_autoHideDelay);

        int livesRemaining = LifeManager.Instance?.CurrentLives ?? 0;

        if (livesRemaining > 0)
        {
            Hide();
        }

        _autoHideCoroutine = null;
    }

    private void OnContinueClicked()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }

        Hide();
    }

    private void OnQuitClicked()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }

        Time.timeScale = 1f;

        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnDestroy()
    {
        if (_autoHideCoroutine != null)
            StopCoroutine(_autoHideCoroutine);

        if (_continueButton != null)
            _continueButton.onClick.RemoveAllListeners();

        if (_quitButton != null)
            _quitButton.onClick.RemoveAllListeners();
    }
}
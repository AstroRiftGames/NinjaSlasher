using System;
using TMPro;
using UnityEngine;
using DG.Tweening;

public class GameplayUIManager : MonoBehaviour
{
    [Header("GAMEPLAY UI")]
    [SerializeField] private TextMeshProUGUI _livesAmount;
    [SerializeField] private TextMeshProUGUI _livesTimerText;
    [SerializeField] private GameObject _livesTimerObj;
    [SerializeField] private TextMeshProUGUI _noLivesTimerText;
    [SerializeField] private TextMeshProUGUI _levelTimerText;
    [SerializeField] private TextMeshProUGUI _bonusTimeText;

    private UIAudioContext _audioContext;
    private RectTransform _bonusTimeRectTransform;
    private CanvasGroup _bonusTimeCanvasGroup;
    private Sequence _bonusTimeSequence;
    private Vector3 _bonusTimeBaseScale = Vector3.one;
    private bool _bonusTimeBaseScaleCached;

    private bool _noLivesActive = false;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
        CacheBonusTimeReferences();
        CacheBonusTimeBaseScale();
        ResetBonusTimeVisualState(hide: true);
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        ResetBonusTimeVisualState(hide: true);
    }

    public void Initialize()
    {
        UpdateLivesUI(LifeManager.Instance?.GetDisplayLives() ?? 0);
        ResetBonusTimeVisualState(hide: true);
    }

    public void OnSceneLoaded()
    {
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        UnsubscribeFromEvents();

        GameEvents.OnLivesChanged += OnLivesChanged;
        GameEvents.OnLevelTimeChanged += OnLevelTimeChanged;
        GameEvents.OnLevelTimeExpired += OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus += ShowBonusTimeText;
        UIEvents.OnUILivesUpdateRequested += UpdateLivesUI;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnLivesChanged -= OnLivesChanged;
        GameEvents.OnLevelTimeChanged -= OnLevelTimeChanged;
        GameEvents.OnLevelTimeExpired -= OnLevelTimeExpired;
        GameEvents.OnLevelTimeBonus -= ShowBonusTimeText;
        UIEvents.OnUILivesUpdateRequested -= UpdateLivesUI;
    }

    public void UpdateUI()
    {
        UpdateNoLivesTimer();
    }

    private void UpdateNoLivesTimer()
    {
        var lm = LifeManager.Instance;
        if (lm == null || !lm.IsInitialized) return;

        bool needsTimer = lm.GetRealLives() < 3;

        if (_livesTimerObj != null)
            _livesTimerObj.SetActive(needsTimer);

        if (!needsTimer) return;

        TimeSpan time = lm.GetTimeToNextLife();
        string formatted = $"{time.Minutes:D2}:{time.Seconds:D2}";

        if (_noLivesTimerText != null)
            _noLivesTimerText.text = formatted;

        if (_livesTimerText != null)
            _livesTimerText.text = formatted;
    }

    public void ShowNoLivesPanel()
    {
        _noLivesActive = true;
        UIEvents.RequestShowNoLivesOverlay();
    }

    public void UpdateLivesUI(int lives)
    {
        if (_livesAmount != null)
        {
            _livesAmount.text = lives.ToString();
        }
    }

    public void OnRetryPressed()
    {
        UIEvents.RaiseRetryPressed();
    }

    public void OnBackToSelectionPressed()
    {
        UIEvents.RequestShowLifeLostPanel();
        UIEvents.RequestHideVictoryModal();
        UIEvents.RaiseQuitToMenuPressed();
    }

    public void ContinueToLevelSelector()
    {
        UIEvents.RequestHideVictoryModal();
        UIEvents.RaiseQuitToMenuPressed();
    }

    private void OnLivesChanged(int lives)
    {
        UpdateLivesUI(lives);

        if (_noLivesActive && LifeManager.Instance.GetRealLives() > 0)
        {
            _noLivesActive = false;
            UIEvents.RequestHideNoLivesOverlay();
        }
    }

    private void ShowBonusTimeText(float bonus)
    {
        if (_bonusTimeText == null) return;

        _bonusTimeText.text = $"+{bonus:F0}s";
        _bonusTimeText.gameObject.SetActive(true);
        PlayBonusTimeAnimation();
    }

    private void PlayBonusTimeAnimation()
    {
        CacheBonusTimeReferences();
        CacheBonusTimeBaseScale();

        if (_bonusTimeRectTransform == null || _bonusTimeCanvasGroup == null)
            return;

        ResetBonusTimeVisualState(hide: false);

        _bonusTimeRectTransform.localScale = _bonusTimeBaseScale * 0.5f;
        _bonusTimeCanvasGroup.alpha = 1f;

        _bonusTimeSequence = DOTween.Sequence();
        _bonusTimeSequence.Append(_bonusTimeRectTransform
            .DOScale(_bonusTimeBaseScale, 0.2f)
            .SetEase(Ease.OutBack));
        _bonusTimeSequence.AppendCallback(() => _bonusTimeRectTransform.localScale = _bonusTimeBaseScale);
        _bonusTimeSequence.Append(_bonusTimeRectTransform
            .DOPunchScale(_bonusTimeBaseScale * 0.3f, 0.15f, vibrato: 5, elasticity: 0.5f));
        _bonusTimeSequence.AppendCallback(() => _bonusTimeRectTransform.localScale = _bonusTimeBaseScale);
        _bonusTimeSequence.AppendInterval(1.2f);
        _bonusTimeSequence.Append(_bonusTimeCanvasGroup
            .DOFade(0f, 0.3f)
            .SetEase(Ease.InQuad));
        _bonusTimeSequence.OnComplete(() => ResetBonusTimeVisualState(hide: true));
    }

    private void CacheBonusTimeReferences()
    {
        if (_bonusTimeText == null)
            return;

        if (_bonusTimeRectTransform == null)
            _bonusTimeRectTransform = _bonusTimeText.rectTransform;

        if (_bonusTimeCanvasGroup == null &&
            !_bonusTimeText.TryGetComponent(out _bonusTimeCanvasGroup))
        {
            _bonusTimeCanvasGroup = _bonusTimeText.gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void CacheBonusTimeBaseScale()
    {
        if (_bonusTimeBaseScaleCached || _bonusTimeRectTransform == null)
            return;

        _bonusTimeBaseScale = _bonusTimeRectTransform.localScale;
        _bonusTimeBaseScaleCached = true;
    }

    private void ResetBonusTimeVisualState(bool hide)
    {
        KillBonusTimeTweens();

        if (_bonusTimeRectTransform != null && _bonusTimeBaseScaleCached)
            _bonusTimeRectTransform.localScale = _bonusTimeBaseScale;

        if (_bonusTimeCanvasGroup != null)
            _bonusTimeCanvasGroup.alpha = hide ? 0f : 1f;

        if (hide && _bonusTimeText != null)
            _bonusTimeText.gameObject.SetActive(false);
    }

    private void KillBonusTimeTweens()
    {
        if (_bonusTimeSequence != null)
        {
            if (_bonusTimeSequence.IsActive())
                _bonusTimeSequence.Kill();

            _bonusTimeSequence = null;
        }

        KillBonusTimeScaleTweens();

        if (_bonusTimeCanvasGroup != null)
            DOTween.Kill(_bonusTimeCanvasGroup);
    }

    private void KillBonusTimeScaleTweens()
    {
        if (_bonusTimeRectTransform != null)
            DOTween.Kill(_bonusTimeRectTransform);
    }

    private void OnLevelTimeChanged(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        _levelTimerText.text = $"{minutes:D2}:{seconds:D2}";
    }

    private void OnLevelTimeExpired()
    {
        if (_levelTimerText != null)
            _levelTimerText.text = "00:00";
    }
}

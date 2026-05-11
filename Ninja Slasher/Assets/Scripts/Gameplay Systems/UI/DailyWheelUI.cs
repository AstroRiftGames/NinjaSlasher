using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyWheelUI : MonoBehaviour
{
    private const int NoSpinsPurchaseCost = 100;
    private const int NoSpinsPurchaseSpinsAmount = 3;
    private static readonly Color AvailableSpinsIncreaseColor = new Color(0.45f, 1f, 0.45f, 1f);
    private static readonly Color AvailableSpinsDecreaseColor = new Color(1f, 0.45f, 0.45f, 1f);

    [Header("Animators")]
    [SerializeField] private Animator _wheelAnimator;
    [SerializeField] private Animator _ballAnimator;

    [Header("References")]
    [SerializeField] private WheelLever _wheelLever;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI _availableSpinsText;
    [SerializeField] private UIPunchScaleFeedback _availableSpinsFeedback;
    [SerializeField] private TextMeshProUGUI _nextFreeSpinPopupText;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _noSpinsBuyButton;
    [SerializeField] private GameObject _lotteryPanel;
    [SerializeField] private GameObject _sharedRewardInfoPanel;
    [SerializeField] private GameObject _sharedNoSpinsInfoPanel;

    [Header("Ball And Reward")]
    [SerializeField] private Image ballImage;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardQuantityText;
    [SerializeField] private Image rewardIconImage;

    [Header("Sequence")]
    [SerializeField] private int _spinLoopCount = 3;
    [SerializeField] private float _noSpinsPopupOpenDelay = 0.25f;
    [SerializeField] private float _availableSpinsPunchScale = 0.22f;
    [SerializeField] private float _availableSpinsAnimationDuration = 0.25f;

    [Header("Spin Audio")]
    [SerializeField] private float _tickEveryDegrees = 30f;
    [SerializeField] private Vector2 _ballHitIntervalRange = new Vector2(0.08f, 0.2f);

    [Header("Wheel Controller")]
    [SerializeField] private string _wheelSpinTrigger = "Spin";
    [SerializeField] private string _wheelIdleState = "GaraponIdle";
    [SerializeField] private string _wheelSpinState = "GaraponSpin";

    [Header("Ball Controller")]
    [SerializeField] private string _ballEjectTrigger = "Eject";
    [SerializeField] private string _ballHiddenState = "BallHidden";
    [SerializeField] private string _ballRevealState = "BallReveal";
    [SerializeField] private string _ballOpenState = "OpenBall";

    private bool _isSpinning;
    private Coroutine _timerCoroutine;
    private Coroutine _sequenceCoroutine;
    private Coroutine _spinAudioCoroutine;
    private Coroutine _delayedNoSpinsPopupCoroutine;
    private WheelReward _currentReward;
    private UIAudioContext _audioContext;
    private float _lastWheelAngle;
    private float _accumulatedSpinDegrees;
    private int _lastAvailableSpins = -1;
    private Color _availableSpinsBaseColor = Color.white;
    private Button _sharedNoSpinsBuyButton;
    private Button _lotteryCloseButton;
    private DailyWheelLotteryPopup _lotteryPopup;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
        CacheReferences();
    }

    private void OnEnable()
    {
        DailyWheelSystem.OnBootstrapped += OnWheelSystemBootstrapped;
        if (_wheelLever != null) _wheelLever.OnLeverActivated += OnLeverPulled;
        GameEvents.OnCoinsChanged += OnCoinsChanged;
        StartTimerUpdate();
        TryRefreshWheelState("OnEnable");
    }

    private void OnDisable()
    {
        DailyWheelSystem.OnBootstrapped -= OnWheelSystemBootstrapped;
        if (_wheelLever != null) _wheelLever.OnLeverActivated -= OnLeverPulled;
        GameEvents.OnCoinsChanged -= OnCoinsChanged;
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        StopSpinAudio(false);

        _timerCoroutine = null;
        _sequenceCoroutine = null;
        _delayedNoSpinsPopupCoroutine = null;
        _isSpinning = false;

        HideLotteryInfo();

        if (_availableSpinsText != null)
        {
            DOTween.Kill(_availableSpinsText);
            _availableSpinsText.color = _availableSpinsBaseColor;

            if (_availableSpinsFeedback != null)
                _availableSpinsFeedback.ResetImmediate();
            else
                _availableSpinsText.transform.localScale = Vector3.one;
        }
    }

    private void Start()
    {
        HideLotteryInfo();
        ResetVisualState();
    }

    public void HandleModalShown()
    {
        TryRefreshWheelState("HandleModalShown");

        if (_delayedNoSpinsPopupCoroutine != null)
        {
            StopCoroutine(_delayedNoSpinsPopupCoroutine);
            _delayedNoSpinsPopupCoroutine = null;
        }

        if (!IsWheelSystemReady() || DailyWheelSystem.Instance.GetAvailabilitySnapshot().AvailableCount > 0)
            return;

        _delayedNoSpinsPopupCoroutine = StartCoroutine(ShowNoSpinsPopupWithDelay());
    }

    private void OnLeverPulled()
    {
        if (_isSpinning) return;
        if (!IsWheelSystemReady()) return;

        if (!DailyWheelSystem.Instance.SpinWheel(out _currentReward))
        {
            ShowNoSpinsPopup();
            return;
        }

        RefreshWheelState();

        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
        }

        _sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _isSpinning = true;
        PrepareRewardVisuals();
        UpdateUIState(false);
        HideLotteryInfo();

        StartSpinAudio();

        int spinCount = Mathf.Max(1, _spinLoopCount);
        for (int i = 0; i < spinCount; i++)
        {
            TriggerWheelSpin();
            yield return WaitForStateToStart(_wheelAnimator, _wheelSpinState);
            yield return WaitForStateToExit(_wheelAnimator, _wheelSpinState);
        }

        yield return WaitForState(_wheelAnimator, _wheelIdleState);
        StopSpinAudio(true);

        TriggerBallReveal();
        yield return WaitForStateToStart(_ballAnimator, _ballRevealState);
        yield return WaitForStateToExit(_ballAnimator, _ballRevealState);
        yield return WaitForStateToStart(_ballAnimator, _ballOpenState);
        yield return WaitForStateToExit(_ballAnimator, _ballOpenState);

        ShowRewardPopup();
        _isSpinning = false;
        _sequenceCoroutine = null;
        RefreshWheelState();
    }

    private void TriggerWheelSpin()
    {
        if (_wheelAnimator == null || string.IsNullOrEmpty(_wheelSpinTrigger)) return;

        _wheelAnimator.ResetTrigger(_wheelSpinTrigger);
        _wheelAnimator.SetTrigger(_wheelSpinTrigger);
    }

    private void TriggerBallReveal()
    {
        if (_ballAnimator == null || string.IsNullOrEmpty(_ballEjectTrigger)) return;

        _ballAnimator.ResetTrigger(_ballEjectTrigger);
        _ballAnimator.SetTrigger(_ballEjectTrigger);
    }

    private void PrepareRewardVisuals()
    {
        if (_currentReward == null) return;

        if (ballImage != null)
        {
            ballImage.color = GetBallColorForReward(_currentReward);
        }

        if (rewardNameText != null) rewardNameText.text = _currentReward.displayName;
        if (rewardQuantityText != null) rewardQuantityText.text = $"x{_currentReward.quantity}";
        if (rewardIconImage != null) rewardIconImage.sprite = _currentReward.icon;
    }

    private void ShowRewardPopup()
    {
        if (_currentReward == null) return;

        SetSharedPopupSection(showRewardInfo: true, showNoSpinsInfo: false);
        PlayUIAudio(_audioContext?.Audio?._rewardPopupAudio);
    }

    public void CloseRewardPopup()
    {
        if (IsBusy())
            return;

        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        StopSpinAudio(false);

        _isSpinning = false;

        HideLotteryInfo();
        HideNoSpinsPopup();

        ResetVisualState();
        RefreshWheelState();
        UIEvents.RaiseWheelSequenceCompleted();
    }

    public void ResetVisualState()
    {
        ForceState(_wheelAnimator, _wheelIdleState);
        ForceState(_ballAnimator, _ballHiddenState);
    }

    private void ForceState(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        animator.Play(stateName, 0, 0f);
        animator.Update(0f);
    }

    private IEnumerator WaitForState(Animator animator, string stateName, float timeout = 2f)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < timeout)
        {
            if (animator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForStateToStart(Animator animator, string stateName, float timeout = 2f)
    {
        yield return WaitForState(animator, stateName, timeout);
    }

    private IEnumerator WaitForStateToExit(Animator animator, string stateName, float timeout = 5f)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < timeout)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(stateName))
            {
                yield break;
            }

            if (state.normalizedTime >= 1f && !animator.IsInTransition(0))
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void StartSpinAudio()
    {
        StopSpinAudio(false);

        if (!HasWheelSpinAudio() && !HasBallHitAudio())
        {
            return;
        }

        _lastWheelAngle = GetWheelAngle();
        _accumulatedSpinDegrees = 0f;
        _spinAudioCoroutine = StartCoroutine(SpinAudioRoutine());
    }

    private void StopSpinAudio(bool playFinishHit)
    {
        if (_spinAudioCoroutine != null)
        {
            StopCoroutine(_spinAudioCoroutine);
            _spinAudioCoroutine = null;
        }

        _accumulatedSpinDegrees = 0f;
        _lastWheelAngle = GetWheelAngle();

        if (playFinishHit)
        {
            PlayUIAudio(_audioContext?.Audio?.finishBallHit);
        }
    }

    private IEnumerator SpinAudioRoutine()
    {
        float nextBallHitDelay = GetNextBallHitDelay();
        float ballHitTimer = 0f;

        while (_isSpinning)
        {
            UpdateWheelTickAudio();
            bool isWheelSpinning = IsAnimatorInState(_wheelAnimator, _wheelSpinState);

            if (isWheelSpinning)
            {
                ballHitTimer += Time.deltaTime;

                if (ballHitTimer >= nextBallHitDelay)
                {
                    PlayUIAudio(_audioContext?.Audio?.ballHit);
                    ballHitTimer = 0f;
                    nextBallHitDelay = GetNextBallHitDelay();
                }
            }
            else
            {
                _lastWheelAngle = GetWheelAngle();
            }

            yield return null;
        }
    }

    private void UpdateWheelTickAudio()
    {
        float currentAngle = GetWheelAngle();
        float delta = Mathf.Abs(Mathf.DeltaAngle(_lastWheelAngle, currentAngle));
        _lastWheelAngle = currentAngle;

        if (delta <= 0f || _tickEveryDegrees <= 0f)
        {
            return;
        }

        _accumulatedSpinDegrees += delta;
        while (_accumulatedSpinDegrees >= _tickEveryDegrees)
        {
            PlayUIAudio(_audioContext?.Audio?.wheelSpin);
            _accumulatedSpinDegrees -= _tickEveryDegrees;
        }
    }

    private bool IsAnimatorInState(Animator animator, string stateName)
    {
        return animator != null
            && !string.IsNullOrEmpty(stateName)
            && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    private float GetWheelAngle()
    {
        if (_wheelAnimator == null)
        {
            return 0f;
        }

        return _wheelAnimator.transform.localEulerAngles.z;
    }

    private float GetNextBallHitDelay()
    {
        float min = Mathf.Max(0.01f, Mathf.Min(_ballHitIntervalRange.x, _ballHitIntervalRange.y));
        float max = Mathf.Max(min, Mathf.Max(_ballHitIntervalRange.x, _ballHitIntervalRange.y));
        return UnityEngine.Random.Range(min, max);
    }

    private bool HasWheelSpinAudio()
    {
        return _audioContext != null && _audioContext.Audio != null && _audioContext.Audio.wheelSpin != null;
    }

    private bool HasBallHitAudio()
    {
        return _audioContext != null
            && _audioContext.Audio != null
            && (_audioContext.Audio.ballHit != null || _audioContext.Audio.finishBallHit != null);
    }

    private void PlayUIAudio(AudioEvent audioEvent)
    {
        if (audioEvent == null || AudioService.Instance == null)
        {
            return;
        }

        AudioService.Instance.PlaySFX(audioEvent);
    }

    public void AnimationEvent_PlayBallBounce()
    {
        PlayUIAudio(_audioContext.Audio._ballBounceAudio);
    }

    private void StartTimerUpdate()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(UpdateTimerRoutine());
    }

    private IEnumerator UpdateTimerRoutine()
    {
        var wait = new WaitForSeconds(1f);

        while (true)
        {
            if (IsWheelSystemReady())
            {
                DailyAvailabilitySnapshot availability = DailyWheelSystem.Instance.GetAvailabilitySnapshot();
                UpdateSpinCountdownTexts(availability.IsAvailable, availability.AvailableCount, availability.NextAvailabilityUtc);
                UpdateAvailableSpinsText(availability.AvailableCount);
            }

            yield return wait;
        }
    }

    private DateTime GetNextSpinAvailabilityUtc()
    {
        if (!IsWheelSystemReady())
            return DateTime.UtcNow;

        return DailyWheelSystem.Instance.GetAvailabilitySnapshot().NextAvailabilityUtc;
    }

    private void RefreshWheelState()
    {
        if (!IsWheelSystemReady())
        {
            UpdateUIState(false);
            return;
        }

        DailyAvailabilitySnapshot availability = DailyWheelSystem.Instance.GetAvailabilitySnapshot();
        UpdateSpinCountdownTexts(availability.IsAvailable, availability.AvailableCount, availability.NextAvailabilityUtc);
        UpdateAvailableSpinsText(availability.AvailableCount);
        UpdateUIState(availability.IsAvailable);
    }

    private void UpdateUIState(bool canSpin)
    {
        UpdateCloseButtonState();

        if (_wheelLever != null)
        {
            bool popupOpen = IsAnyPopupOpen();
            _wheelLever.SetInteractable(canSpin && !_isSpinning && !popupOpen);
        }
    }

    private Color GetBallColorForReward(WheelReward reward)
    {
        var allRewards = DailyWheelSystem.Instance.WheelRewards;
        int index = Array.IndexOf(allRewards, reward);
        Color[] colors = { Color.white, Color.cyan, Color.green, Color.red, Color.yellow };
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }

    private void CacheReferences()
    {
        if (_availableSpinsText != null)
            _availableSpinsBaseColor = _availableSpinsText.color;

        if (_availableSpinsFeedback == null && _availableSpinsText != null)
        {
            _availableSpinsFeedback = GetOrCreateAvailableSpinsFeedback(_availableSpinsText.rectTransform);
        }

        if (_lotteryPanel == null)
        {
            Transform rewardParent = _sharedRewardInfoPanel != null ? _sharedRewardInfoPanel.transform.parent : null;
            Transform noSpinsParent = _sharedNoSpinsInfoPanel != null ? _sharedNoSpinsInfoPanel.transform.parent : null;

            if (rewardParent != null && rewardParent == noSpinsParent)
                _lotteryPanel = rewardParent.gameObject;
        }

        _sharedNoSpinsBuyButton = _noSpinsBuyButton;
        _lotteryCloseButton = ResolveLotteryCloseButton();

        if (_lotteryPanel != null)
        {
            _lotteryPopup = _lotteryPanel.GetComponent<DailyWheelLotteryPopup>();
            if (_lotteryPopup == null)
                _lotteryPopup = _lotteryPanel.AddComponent<DailyWheelLotteryPopup>();

            _lotteryPopup.Initialize(_sharedRewardInfoPanel, _sharedNoSpinsInfoPanel, _sharedNoSpinsBuyButton != null
                ? _sharedNoSpinsBuyButton.gameObject
                : null, _lotteryCloseButton != null ? _lotteryCloseButton.gameObject : null);
        }

    }

    private Button ResolveLotteryCloseButton()
    {
        if (_lotteryPanel == null)
            return null;

        Button[] buttons = _lotteryPanel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == "CancelButton")
                return buttons[i];
        }

        return null;
    }

    private void UpdateAvailableSpinsText(int availableSpins)
    {
        if (_availableSpinsText == null)
            return;

        int clampedAvailableSpins = Mathf.Max(0, availableSpins);
        _availableSpinsText.text = clampedAvailableSpins.ToString();

        if (_lastAvailableSpins >= 0 && clampedAvailableSpins != _lastAvailableSpins)
            AnimateAvailableSpinsChange(clampedAvailableSpins > _lastAvailableSpins);

        _lastAvailableSpins = clampedAvailableSpins;
    }

    private void UpdateSpinCountdownTexts(bool canSpin, int availableSpins, DateTime nextSpinAvailabilityUtc)
    {
        string nextSpinText = DailyAvailabilityUIFormatter.FormatLockedAvailability(
            "Proximo giro en ",
            nextSpinAvailabilityUtc,
            "Giro disponible ahora");
        string nextSpinPopupText = DailyAvailabilityUIFormatter.FormatLockedAvailability(
            "Proximo giro en: ",
            nextSpinAvailabilityUtc,
            "Giro disponible ahora");

        if (timerText != null)
        {
            if (!canSpin)
            {
                timerText.text = nextSpinText;
            }
            else
            {
                timerText.text = availableSpins > 1
                    ? $"Buena suerte!"
                    : "Buena suerte!";
            }
        }

        if (_nextFreeSpinPopupText != null)
            _nextFreeSpinPopupText.text = canSpin ? "Giro disponible ahora" : nextSpinPopupText;
    }

    public void ShowNoSpinsPopup()
    {
        if (_sharedNoSpinsInfoPanel == null)
            return;

        if (_delayedNoSpinsPopupCoroutine != null)
        {
            StopCoroutine(_delayedNoSpinsPopupCoroutine);
            _delayedNoSpinsPopupCoroutine = null;
        }

        HideLotteryInfo();
        RefreshNoSpinsPopupState();
        SetSharedPopupSection(showRewardInfo: false, showNoSpinsInfo: true);
        UpdateUIState(false);
    }

    public void HideNoSpinsPopup()
    {
        if (_delayedNoSpinsPopupCoroutine != null)
        {
            StopCoroutine(_delayedNoSpinsPopupCoroutine);
            _delayedNoSpinsPopupCoroutine = null;
        }

        HideLotteryInfo();

        RefreshWheelState();
    }

    public void TryPurchaseNoSpinsOffer()
    {
        if (SaveManager.Instance == null || !IsWheelSystemReady())
            return;

        if (!SaveManager.Instance.SpendCoins(NoSpinsPurchaseCost))
        {
            RefreshNoSpinsPopupState();
            return;
        }

        DailyWheelSystem.Instance.GrantFreeSpins(NoSpinsPurchaseSpinsAmount);
        HideNoSpinsPopup();
        RefreshWheelState();
        RefreshNoSpinsPopupState();
    }

    private void RefreshNoSpinsPopupState()
    {
        if (SaveManager.Instance == null)
            return;

        bool canAfford = SaveManager.Instance.GetCoins() >= NoSpinsPurchaseCost;

        if (_noSpinsBuyButton != null)
            _noSpinsBuyButton.interactable = canAfford;

        if (_sharedNoSpinsBuyButton != null && _sharedNoSpinsBuyButton != _noSpinsBuyButton)
            _sharedNoSpinsBuyButton.interactable = canAfford;
    }

    private IEnumerator ShowNoSpinsPopupWithDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _noSpinsPopupOpenDelay));
        _delayedNoSpinsPopupCoroutine = null;

        if (!isActiveAndEnabled || !IsWheelSystemReady() || DailyWheelSystem.Instance.GetAvailabilitySnapshot().AvailableCount > 0)
            yield break;

        ShowNoSpinsPopup();
    }

    private void HideRewardPopupVisuals()
    {
        HideLotteryInfo();
    }

    private void OnCoinsChanged(int _)
    {
        RefreshNoSpinsPopupState();
    }

    private void AnimateAvailableSpinsChange(bool increased)
    {
        if (_availableSpinsText == null)
            return;

        if (_availableSpinsFeedback == null)
            _availableSpinsFeedback = GetOrCreateAvailableSpinsFeedback(_availableSpinsText.rectTransform);

        DOTween.Kill(_availableSpinsText);

        _availableSpinsText.color = _availableSpinsBaseColor;

        if (_availableSpinsFeedback != null)
            _availableSpinsFeedback.ResetImmediate();
        else
            _availableSpinsText.transform.localScale = Vector3.one;

        Color targetColor = increased ? AvailableSpinsIncreaseColor : AvailableSpinsDecreaseColor;
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(_availableSpinsText);
        sequence.Join(_availableSpinsText.DOColor(targetColor, _availableSpinsAnimationDuration * 0.45f));

        if (_availableSpinsFeedback != null)
            _availableSpinsFeedback.Play();
        else
            sequence.Join(_availableSpinsText.transform.DOPunchScale(Vector3.one * _availableSpinsPunchScale, _availableSpinsAnimationDuration, vibrato: 1, elasticity: 0.6f));

        sequence.Append(_availableSpinsText.DOColor(_availableSpinsBaseColor, _availableSpinsAnimationDuration * 0.55f));
    }

    private UIPunchScaleFeedback GetOrCreateAvailableSpinsFeedback(RectTransform target)
    {
        UIPunchScaleFeedback feedback = target.GetComponent<UIPunchScaleFeedback>();
        if (feedback == null)
            feedback = target.gameObject.AddComponent<UIPunchScaleFeedback>();

        feedback.Configure(
            useFade: false,
            hideGameObjectOnComplete: false,
            activateGameObjectOnPlay: false,
            useUnscaledTime: false,
            scaleInDuration: 0f,
            punchDuration: _availableSpinsAnimationDuration,
            visibleDuration: 0f,
            fadeDuration: 0f,
            initialScaleMultiplier: 1f,
            punchStrength: Vector3.one * _availableSpinsPunchScale,
            vibrato: 1,
            elasticity: 0.6f);

        feedback.ResetImmediate();
        return feedback;
    }

    private void HideLotteryInfo()
    {
        if (_lotteryPopup != null)
        {
            _lotteryPopup.HideImmediate();
            return;
        }

        if (_sharedRewardInfoPanel != null)
            _sharedRewardInfoPanel.SetActive(false);

        if (_sharedNoSpinsInfoPanel != null)
            _sharedNoSpinsInfoPanel.SetActive(false);

        if (_lotteryPanel != null)
            _lotteryPanel.SetActive(false);
    }

    private void SetSharedPopupSection(bool showRewardInfo, bool showNoSpinsInfo)
    {
        bool showBuyButton = showNoSpinsInfo && !showRewardInfo;

        if (_lotteryPopup != null)
        {
            _lotteryPopup.ShowSection(showRewardInfo, showNoSpinsInfo, showBuyButton);
            UpdateBuyButtonInteractable(showBuyButton);
            return;
        }

        if (_lotteryPanel != null)
            _lotteryPanel.SetActive(showRewardInfo || showNoSpinsInfo);

        if (_sharedRewardInfoPanel != null)
            _sharedRewardInfoPanel.SetActive(showRewardInfo);

        if (_sharedNoSpinsInfoPanel != null)
            _sharedNoSpinsInfoPanel.SetActive(showNoSpinsInfo);

        UpdateBuyButtonInteractable(showBuyButton);
    }

    private bool IsAnyPopupOpen()
    {
        return (_sharedRewardInfoPanel != null && _sharedRewardInfoPanel.activeSelf)
            || (_sharedNoSpinsInfoPanel != null && _sharedNoSpinsInfoPanel.activeSelf);
    }

    public bool CanCloseModal()
    {
        return !IsBusy();
    }

    private bool IsBusy()
    {
        return _isSpinning || _sequenceCoroutine != null;
    }

    private void UpdateCloseButtonState()
    {
        if (_closeButton == null)
            return;

        if (!_closeButton.gameObject.activeSelf)
            _closeButton.gameObject.SetActive(true);

        _closeButton.interactable = !IsBusy();
    }

    private void UpdateBuyButtonVisibility(bool shouldBeInteractable)
    {
        if (_sharedNoSpinsBuyButton == null)
            return;

        _sharedNoSpinsBuyButton.gameObject.SetActive(shouldBeInteractable);
        UpdateBuyButtonInteractable(shouldBeInteractable);
    }

    private void UpdateBuyButtonInteractable(bool shouldBeInteractable)
    {
        if (_sharedNoSpinsBuyButton == null)
            return;

        _sharedNoSpinsBuyButton.interactable = shouldBeInteractable && SaveManager.Instance != null && SaveManager.Instance.GetCoins() >= NoSpinsPurchaseCost;
    }

    private void OnWheelSystemBootstrapped()
    {
        Debug.Log("[DailyWheelUI] Signal -> DailyWheelSystem.OnBootstrapped");
        TryRefreshWheelState("DailyWheelSystem.OnBootstrapped");
    }

    private bool TryRefreshWheelState(string reason)
    {
        if (!IsWheelSystemReady())
        {
            bool saveLoaded = SaveManager.Instance != null && SaveManager.Instance.IsDataLoaded;
            bool systemExists = DailyWheelSystem.Instance != null;
            bool wheelBootstrapped = systemExists && DailyWheelSystem.Instance.IsBootstrapped;
            Debug.Log($"[DailyWheelUI] Bootstrap -> Waiting | Reason={reason} | systemExists={systemExists} | saveLoaded={saveLoaded} | wheelBootstrapped={wheelBootstrapped}");
            return false;
        }

        RefreshWheelState();
        RefreshNoSpinsPopupState();
        Debug.Log($"[DailyWheelUI] Bootstrap -> Ready | Reason={reason}");
        return true;
    }

    private bool IsWheelSystemReady()
    {
        return DailyWheelSystem.Instance != null
            && SaveManager.Instance != null
            && SaveManager.Instance.IsDataLoaded
            && DailyWheelSystem.Instance.IsBootstrapped;
    }
}

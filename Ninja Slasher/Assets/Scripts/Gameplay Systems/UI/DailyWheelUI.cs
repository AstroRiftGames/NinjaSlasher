using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyWheelUI : MonoBehaviour
{
    private enum SequenceStage
    {
        Idle,
        WheelStarting,
        WheelLooping,
        WheelStopping,
        BallRevealing,
        BallOpening,
        RewardVisible
    }

    [Header("Animators")]
    [SerializeField] private Animator _wheelAnimator;
    [SerializeField] private Animator _ballAnimator;

    [Header("References")]
    [SerializeField] private WheelLever _wheelLever;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private Button _closeButton;

    [Header("Ball And Reward")]
    [SerializeField] private Image ballImage;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardQuantityText;
    [SerializeField] private Image rewardIconImage;

    [Header("Sequence")]
    [SerializeField] private int _spinLoopCount = 3;
    [SerializeField] private float _rewardPopupScaleDuration = 0.4f;

    [Header("Wheel Animator Parameters")]
    [SerializeField] private string _wheelStartTrigger = "StartSpin";
    [SerializeField] private string _wheelLoopTrigger = "SpinLoop";
    [SerializeField] private string _wheelStopTrigger = "StopSpin";
    [SerializeField] private string _wheelResetTrigger = "Reset";

    [Header("Ball Animator Parameters")]
    [SerializeField] private string _ballRevealTrigger = "Reveal";
    [SerializeField] private string _ballOpenTrigger = "Open";
    [SerializeField] private string _ballResetTrigger = "Reset";

    private bool _isSpinning;
    private int _remainingSpinLoops;
    private SequenceStage _stage = SequenceStage.Idle;
    private Coroutine _timerCoroutine;
    private UIAudioContext _audioContext;
    private WheelReward _currentReward;

    private void Awake()
    {
        _audioContext = GetComponentInParent<UIAudioContext>();
    }

    private void OnEnable()
    {
        if (_wheelLever != null) _wheelLever.OnLeverActivated += OnLeverPulled;
        StartTimerUpdate();
    }

    private void OnDisable()
    {
        if (_wheelLever != null) _wheelLever.OnLeverActivated -= OnLeverPulled;
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = null;
        _isSpinning = false;
        _stage = SequenceStage.Idle;
    }

    private void Start()
    {
        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);

        ResetVisualState();
        UpdateUIState(DailyWheelSystem.Instance.CanSpinToday());
    }

    private void OnLeverPulled()
    {
        if (_isSpinning) return;
        if (DailyWheelSystem.Instance == null) return;

        if (!DailyWheelSystem.Instance.SpinWheel(out _currentReward))
        {
            return;
        }

        BeginSequence();
    }

    private void BeginSequence()
    {
        _isSpinning = true;
        _remainingSpinLoops = Mathf.Max(1, _spinLoopCount);
        _stage = SequenceStage.WheelStarting;

        PrepareRewardVisuals();
        UpdateUIState(false);

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);

        TriggerAnimator(_wheelAnimator, _wheelResetTrigger, _wheelStartTrigger);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.wheelSpin);
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

    public void AnimationEvent_OnWheelLoopComplete()
    {
        if (_stage != SequenceStage.WheelStarting && _stage != SequenceStage.WheelLooping) return;

        _remainingSpinLoops--;
        if (_remainingSpinLoops > 0)
        {
            _stage = SequenceStage.WheelLooping;
            TriggerAnimator(_wheelAnimator, _wheelStopTrigger, _wheelLoopTrigger);
            return;
        }

        _stage = SequenceStage.WheelStopping;
        TriggerAnimator(_wheelAnimator, _wheelLoopTrigger, _wheelStopTrigger);
    }

    public void AnimationEvent_OnWheelStopped()
    {
        if (_stage != SequenceStage.WheelStopping) return;

        _stage = SequenceStage.BallRevealing;
        TriggerAnimator(_ballAnimator, _ballOpenTrigger, _ballRevealTrigger);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.leverPull);
    }

    public void AnimationEvent_OnBallRevealFinished()
    {
        if (_stage != SequenceStage.BallRevealing) return;

        _stage = SequenceStage.BallOpening;
        TriggerAnimator(_ballAnimator, _ballRevealTrigger, _ballOpenTrigger);
        AudioService.Instance?.PlaySFX(_audioContext.Audio.rewardPrize);
        CandyCoded.HapticFeedback.HapticFeedback.HeavyFeedback();
    }

    public void AnimationEvent_OnBallOpenFinished()
    {
        if (_stage != SequenceStage.BallOpening) return;

        ShowRewardPopup();
    }

    public void AnimationEvent_PlayBallHit()
    {
        if (!_isSpinning) return;

        AudioService.Instance?.PlaySFX(_audioContext.Audio.ballHit);
        CandyCoded.HapticFeedback.HapticFeedback.LightFeedback();
    }

    public void AnimationEvent_PlayFinishBallHit()
    {
        AudioService.Instance?.PlaySFX(_audioContext.Audio.finishBallHit);
        CandyCoded.HapticFeedback.HapticFeedback.MediumFeedback();
    }

    public void AnimationEvent_PlayLeverPull()
    {
        AudioService.Instance?.PlaySFX(_audioContext.Audio.leverPull);
    }

    private void ShowRewardPopup()
    {
        if (_currentReward == null || rewardPopup == null) return;

        _stage = SequenceStage.RewardVisible;

        rewardPopup.SetActive(true);
        rewardPopup.transform.localScale = Vector3.zero;
        rewardPopup.transform.DOScale(1f, _rewardPopupScaleDuration).SetEase(Ease.OutBack);

        if (_closeButton != null) _closeButton.gameObject.SetActive(true);
    }

    public void CloseRewardPopup()
    {
        _isSpinning = false;
        _stage = SequenceStage.Idle;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);

        ResetVisualState();
        UpdateUIState(DailyWheelSystem.Instance != null && DailyWheelSystem.Instance.CanSpinToday());
        UIEvents.RaiseWheelSequenceCompleted();
    }

    public void ResetVisualState()
    {
        TriggerAnimator(_wheelAnimator, _wheelStartTrigger, _wheelResetTrigger, _wheelLoopTrigger, _wheelStopTrigger);
        TriggerAnimator(_ballAnimator, _ballRevealTrigger, _ballResetTrigger, _ballOpenTrigger);
    }

    private void TriggerAnimator(Animator animator, params string[] triggerNames)
    {
        if (animator == null) return;

        for (int i = 0; i < triggerNames.Length; i++)
        {
            string triggerName = triggerNames[i];
            if (string.IsNullOrEmpty(triggerName)) continue;
            animator.ResetTrigger(triggerName);
        }

        if (triggerNames.Length == 0) return;

        string finalTrigger = triggerNames[triggerNames.Length - 1];
        if (!string.IsNullOrEmpty(finalTrigger))
        {
            animator.SetTrigger(finalTrigger);
        }
    }

    private void StartTimerUpdate()
    {
        if (_timerCoroutine != null) StopCoroutine(_timerCoroutine);
        _timerCoroutine = StartCoroutine(UpdateTimerRoutine());
    }

    private System.Collections.IEnumerator UpdateTimerRoutine()
    {
        var wait = new WaitForSeconds(1f);

        while (true)
        {
            if (DailyWheelSystem.Instance != null && timerText != null)
            {
                bool canSpin = DailyWheelSystem.Instance.CanSpinToday();
                if (!canSpin)
                {
                    TimeSpan time = GetTimeUntilNextSpin();
                    timerText.text = $"Next spin in {time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
                }
                else
                {
                    timerText.text = "Ready to spin!";
                }
            }

            yield return wait;
        }
    }

    private TimeSpan GetTimeUntilNextSpin()
    {
        DateTime now = DateTime.UtcNow;
        return now.Date.AddDays(1) - now;
    }

    private void UpdateUIState(bool canSpin)
    {
        if (_wheelLever != null)
        {
            _wheelLever.SetInteractable(canSpin && !_isSpinning);
        }
    }

    private Color GetBallColorForReward(WheelReward reward)
    {
        var allRewards = DailyWheelSystem.Instance.WheelRewards;
        int index = Array.IndexOf(allRewards, reward);
        Color[] colors = { Color.white, Color.cyan, Color.green, Color.red, Color.yellow };
        return colors[Mathf.Clamp(index, 0, colors.Length - 1)];
    }
}

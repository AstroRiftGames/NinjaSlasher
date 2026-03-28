using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyWheelUI : MonoBehaviour
{
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
    private WheelReward _currentReward;
    private UIAudioContext _audioContext;
    private float _lastWheelAngle;
    private float _accumulatedSpinDegrees;

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
        if (_sequenceCoroutine != null) StopCoroutine(_sequenceCoroutine);
        StopSpinAudio(false);

        _timerCoroutine = null;
        _sequenceCoroutine = null;
        _isSpinning = false;
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

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);

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
        _sequenceCoroutine = null;
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
        if (_currentReward == null || rewardPopup == null) return;
        PlayUIAudio(_audioContext?.Audio?._rewardPopupAudio);
        rewardPopup.SetActive(true);
        rewardPopup.transform.localScale = Vector3.zero;
        rewardPopup.transform.DOScale(1f, _rewardPopupScaleDuration).SetEase(Ease.OutBack);

        if (_closeButton != null) _closeButton.gameObject.SetActive(true);
    }

    public void CloseRewardPopup()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        StopSpinAudio(false);

        _isSpinning = false;

        if (rewardPopup != null) rewardPopup.SetActive(false);
        if (_closeButton != null) _closeButton.gameObject.SetActive(false);

        ResetVisualState();
        UpdateUIState(DailyWheelSystem.Instance != null && DailyWheelSystem.Instance.CanSpinToday());
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

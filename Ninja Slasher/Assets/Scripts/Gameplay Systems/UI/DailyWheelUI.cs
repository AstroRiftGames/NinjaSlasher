using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyWheelUI : MonoBehaviour
{
    [Header("GARAPON VISUALS")]
    [SerializeField] private RectTransform wheelBody;
    [SerializeField] private Image ballDisplayImage;
    [SerializeField] private Transform ballExitPoint;
    [SerializeField] private Transform ballRevealPoint;

    [Header("UI CONTROLS")]
    [SerializeField] private WheelLever _wheelLever;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("REWARD POPUP")]
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardQuantityText;
    [SerializeField] private Image rewardIconImage;
    [SerializeField] private Image rewardPopupBackground;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float crankDuration = 1.5f;
    [SerializeField] private int crankRotations = 3;
    [SerializeField] private float ballMoveDuration = 0.5f;
    [SerializeField] private Ease crankEase = Ease.InOutBack;

    [Header("BALL ANIMATION SETTINGS")]
    [SerializeField] private Image _ballImage;
    [SerializeField] private Sprite _ballClosedSprite;
    [SerializeField] private Sprite _ballOpenSprite;
    [SerializeField] private float _delayBeforeOpening = 0.5f;
    [SerializeField] private float _delayAfterOpening = 0.2f;

    private bool _isSpinning = false;
    private Coroutine _timerCoroutine;

    public event Action OnWheelProcessComplete;

    private readonly Color[] _ballColors = new Color[]
    {
        Color.white, new Color(0.4f, 0.7f, 1f), Color.green, new Color(1f, 0.3f, 0.3f), new Color(1f, 0.84f, 0f)
    };

    private void OnEnable()
    {
        GameEvents.OnWheelAvailabilityChanged += HandleAvailabilityChanged;
        GameEvents.OnWheelSpun += HandleRewardSpun;

        // DEPRECATED
        //DailyWheelSystem.OnWheelAvailabilityChanged += HandleAvailabilityChanged;
        //DailyWheelSystem.OnRewardSpun += HandleRewardSpun;
    }

    private void OnDisable()
    {
        GameEvents.OnWheelAvailabilityChanged -= HandleAvailabilityChanged;
        GameEvents.OnWheelSpun -= HandleRewardSpun;

        // DEPRECATED
        //DailyWheelSystem.OnWheelAvailabilityChanged -= HandleAvailabilityChanged;
        //DailyWheelSystem.OnRewardSpun -= HandleRewardSpun;
    }

    private void Start()
    {
        if (ballDisplayImage != null)
            ballDisplayImage.gameObject.SetActive(false);

        if (rewardPopup != null)
            rewardPopup.SetActive(false);

        if (_wheelLever != null)
        {
            _wheelLever.OnLeverActivated += OnLeverPulled;
        }
        
        UpdateUIState(DailyWheelSystem.Instance.CanSpinToday());
        StartTimerUpdate();
    }

    private void OnDestroy()
    {
        if (_wheelLever != null)
        {
            _wheelLever.OnLeverActivated -= OnLeverPulled;
        }
    }

    private void OnLeverPulled()
    {
        if (_isSpinning) return;

        if (DailyWheelSystem.Instance.SpinWheel(out WheelReward reward))
        {
            StartCoroutine(GaraponSequence(reward));
        }
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
                    timerText.text = "Come back tomorrow";
                }
                else
                {
                    timerText.text = "Ready to spin!";
                }
            }
            yield return wait;
        }
    }

    private void HandleAvailabilityChanged(bool isAvailable)
    {
        UpdateUIState(isAvailable);
    }

    private void HandleRewardSpun(WheelReward reward)
    {
    }

    private void UpdateUIState(bool canSpin)
    {
        if (_isSpinning) return;

        if (_wheelLever != null)
        {
            _wheelLever.SetInteractable(canSpin);
        }
    }

    private IEnumerator GaraponSequence(WheelReward reward)
    {
        _isSpinning = true;
        AudioManager.Instance?.PlaySFX(SFXClip.DW_Spin);

        if (wheelBody != null)
        {
            wheelBody.DORotate(new Vector3(0, 0, -360 * crankRotations), crankDuration, RotateMode.FastBeyond360)
                .SetEase(crankEase);
        }


        yield return new WaitForSeconds(crankDuration/2);

        AudioManager.Instance?.PlaySFX(SFXClip.DW_Prize);

        yield return new WaitForSeconds(crankDuration/2);

        Color ballColor = GetBallColorForReward(reward);

        Vector3 popupSpawnPosition = Vector3.zero;

        if (ballDisplayImage != null)
        {
            ballDisplayImage.gameObject.SetActive(true);
            ballDisplayImage.color = ballColor;
            ballDisplayImage.transform.position = ballExitPoint.position;
            ballDisplayImage.transform.localScale = Vector3.zero;

            if (_ballClosedSprite != null) ballDisplayImage.sprite = _ballClosedSprite;

            Sequence ballSeq = DOTween.Sequence();
            ballSeq.Append(ballDisplayImage.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

            if (ballRevealPoint != null)
            {
                ballSeq.Join(ballDisplayImage.transform.DOMove(ballRevealPoint.position, ballMoveDuration).SetEase(Ease.InQuad));
            }

            ballSeq.Append(ballDisplayImage.transform.DOShakeScale(0.3f, 0.2f));

            yield return ballSeq.WaitForCompletion();

            yield return new WaitForSeconds(0.2f);

            if (_ballOpenSprite != null)
            {
                ballDisplayImage.sprite = _ballOpenSprite;
            }

            yield return new WaitForSeconds(0.2f);

            popupSpawnPosition = ballDisplayImage.transform.position;

            ballDisplayImage.gameObject.SetActive(false);
        }

        ShowRewardPopup(reward, ballColor, popupSpawnPosition);

        _isSpinning = false;
        UpdateUIState(DailyWheelSystem.Instance.CanSpinToday());
    }

    private void ShowRewardPopup(WheelReward reward, Color themeColor, Vector3 startPosition = default)
    {
        if (rewardNameText != null) rewardNameText.text = reward.displayName;
        if (rewardQuantityText != null) rewardQuantityText.text = $"x{reward.quantity}";
        if (rewardIconImage != null) rewardIconImage.sprite = reward.icon;

        rewardPopup.SetActive(true);

        rewardPopup.transform.localScale = Vector3.zero;

        if (startPosition != Vector3.zero)
        {
            rewardPopup.transform.position = startPosition;
        }
        else
        {
            rewardPopup.transform.localPosition = Vector3.zero;
        }

        Sequence popupSeq = DOTween.Sequence();

        popupSeq.Append(rewardPopup.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

        if (startPosition != Vector3.zero)
        {
            popupSeq.Join(rewardPopup.transform.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.OutBack));
        }
    }

    public void CloseRewardPopup()
    {
        if (rewardPopup == null) return;

        rewardPopup.transform.DOScale(0f, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                rewardPopup.SetActive(false);

                OnWheelProcessComplete?.Invoke();
            });
    }

    public void SkipOrCloseWheel()
    {
        OnWheelProcessComplete?.Invoke();
    }

    private Color GetBallColorForReward(WheelReward reward)
    {
        var allRewards = DailyWheelSystem.Instance.WheelRewards;
        int index = System.Array.IndexOf(allRewards, reward);

        if (index >= 0 && index < _ballColors.Length)
            return _ballColors[index];

        return _ballColors[index % _ballColors.Length];
    }

    public void PlayBallRevealSequence(Action onSequenceComplete)
    {
        StartCoroutine(AnimateBallSequence(onSequenceComplete));
    }

    private IEnumerator AnimateBallSequence(Action onComplete)
    {
        if (_ballImage != null && _ballClosedSprite != null)
        {
            _ballImage.sprite = _ballClosedSprite;
        }

        yield return new WaitForSeconds(_delayBeforeOpening);

        if (_ballImage != null && _ballOpenSprite != null)
        {
            _ballImage.sprite = _ballOpenSprite;

            // SFX
        }

        yield return new WaitForSeconds(_delayAfterOpening);

        onComplete?.Invoke();
    }
}
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
    [SerializeField] private Button spinButton;
    [SerializeField] private TextMeshProUGUI spinButtonText;
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

    private bool _isSpinning = false;
    private Coroutine _timerCoroutine;

    public event Action OnWheelProcessComplete;

    private readonly Color[] _ballColors = new Color[]
    {
        Color.white, new Color(0.4f, 0.7f, 1f), Color.green, new Color(1f, 0.3f, 0.3f), new Color(1f, 0.84f, 0f)
    };

    private void OnEnable()
    {
        DailyWheelSystem.OnWheelAvailabilityChanged += HandleAvailabilityChanged;
        DailyWheelSystem.OnRewardSpun += HandleRewardSpun;
    }

    private void OnDisable()
    {
        DailyWheelSystem.OnWheelAvailabilityChanged -= HandleAvailabilityChanged;
        DailyWheelSystem.OnRewardSpun -= HandleRewardSpun;
    }

    private void Start()
    {
        if (ballDisplayImage != null)
            ballDisplayImage.gameObject.SetActive(false);

        if (rewardPopup != null)
            rewardPopup.SetActive(false);

        spinButton.onClick.AddListener(OnSpinClicked);

        UpdateUIState(DailyWheelSystem.Instance.CanSpinToday());
        StartTimerUpdate();
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

        spinButton.interactable = canSpin;
        if (spinButtonText != null)
            spinButtonText.text = canSpin ? "SPIN" : "CLAIMED";
    }

    private void OnSpinClicked()
    {
        if (_isSpinning) return;

        if (DailyWheelSystem.Instance.SpinWheel(out WheelReward reward))
        {
            StartCoroutine(GaraponSequence(reward));
        }
    }

    private IEnumerator GaraponSequence(WheelReward reward)
    {
        _isSpinning = true;
        spinButton.interactable = false;
        AudioManager.Instance?.PlaySFX(SFXClip.UI_Claim);

        if (wheelBody != null)
        {
            wheelBody.DORotate(new Vector3(0, 0, -360 * crankRotations), crankDuration, RotateMode.FastBeyond360)
                .SetEase(crankEase);
        }

        yield return new WaitForSeconds(crankDuration);

        Color ballColor = GetBallColorForReward(reward);

        if (ballDisplayImage != null)
        {
            ballDisplayImage.gameObject.SetActive(true);
            ballDisplayImage.color = ballColor;
            ballDisplayImage.transform.position = ballExitPoint.position;
            ballDisplayImage.transform.localScale = Vector3.zero;

            Sequence ballSeq = DOTween.Sequence();

            ballSeq.Append(ballDisplayImage.transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack));

            if (ballRevealPoint != null)
            {
                ballSeq.Join(ballDisplayImage.transform.DOMove(ballRevealPoint.position, ballMoveDuration).SetEase(Ease.InQuad));
            }

            ballSeq.Append(ballDisplayImage.transform.DOShakeScale(0.3f, 0.2f));

            yield return ballSeq.WaitForCompletion();

            ballDisplayImage.gameObject.SetActive(false);
        }

        ShowRewardPopup(reward, ballColor);

        _isSpinning = false;

        UpdateUIState(DailyWheelSystem.Instance.CanSpinToday());
    }

    private void ShowRewardPopup(WheelReward reward, Color themeColor)
    {
        if (rewardNameText != null) rewardNameText.text = reward.displayName;
        if (rewardQuantityText != null) rewardQuantityText.text = $"x{reward.quantity}";
        if (rewardIconImage != null) rewardIconImage.sprite = reward.icon;

        rewardPopup.SetActive(true);
        rewardPopup.transform.localScale = Vector3.zero;

        rewardPopup.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
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
}
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DailyWheelUI : MonoBehaviour
{
    [Header("WHEEL REFERENCES")]
    [SerializeField] private RectTransform wheelContainer;
    [SerializeField] private RectTransform wheelPointer;
    [SerializeField] private Button spinButton;
    [SerializeField] private TextMeshProUGUI spinButtonText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject rewardPopup;
    [SerializeField] private TextMeshProUGUI rewardNameText;
    [SerializeField] private TextMeshProUGUI rewardQuantityText;
    [SerializeField] private Image rewardIconImage;

    [Header("WHEEL SEGMENTS")]
    [SerializeField] private Transform segmentsParent;
    [SerializeField] private GameObject wheelSegmentPrefab;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float minSpinDuration = 3f;
    [SerializeField] private float maxSpinDuration = 5f;
    [SerializeField] private int minExtraRotations = 3;
    [SerializeField] private int maxExtraRotations = 6;
    [SerializeField] private Ease spinEase = Ease.OutQuart;
    [SerializeField] private float popupAnimationDuration = 0.3f;

    private bool isSpinning = false;
    private WheelReward[] rewards;

    private void Start()
    {
        if (DailyWheelSystem.Instance != null)
        {
            rewards = DailyWheelSystem.Instance.WheelRewards;
            GenerateWheelSegments();
        }

        spinButton.onClick.AddListener(OnSpinButtonClicked);

        if (rewardPopup != null)
            rewardPopup.SetActive(false);

        //UpdateWheelState();

        //DailyWheelSystem.OnWheelAvailabilityChanged += OnWheelAvailabilityChanged;
    }

    private void OnDestroy()
    {
        spinButton.onClick.RemoveListener(OnSpinButtonClicked);
        //DailyWheelSystem.OnWheelAvailabilityChanged -= OnWheelAvailabilityChanged;

        DOTween.Kill(wheelContainer);
        DOTween.Kill(rewardPopup?.transform);
    }

    //private void Update()
    //{
    //    if (!DailyWheelSystem.Instance.CanSpinToday() && timerText != null)
    //    {
    //        var timeRemaining = DailyWheelSystem.Instance.GetTimeUntilNextSpin();
    //        timerText.text = $"Próximo giro en: {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
    //    }
    //}

    void GenerateWheelSegments()
    {
        if (rewards == null || rewards.Length == 0 || segmentsParent == null || wheelSegmentPrefab == null)
            return;

        foreach (Transform child in segmentsParent)
        {
            Destroy(child.gameObject);
        }

        float anglePerSegment = 360f / rewards.Length;

        for (int i = 0; i < rewards.Length; i++)
        {
            GameObject segment = Instantiate(wheelSegmentPrefab, segmentsParent);
            RectTransform segmentRect = segment.GetComponent<RectTransform>();

            float angle = i * anglePerSegment;
            segmentRect.localRotation = Quaternion.Euler(0, 0, -angle);

            Image segmentImage = segment.GetComponent<Image>();
            if (segmentImage != null && rewards[i].backgroundColor != null)
            {
                segmentImage.color = rewards[i].backgroundColor;
            }

            Image iconImage = segment.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && rewards[i].icon != null)
            {
                iconImage.sprite = rewards[i].icon;
            }

            TextMeshProUGUI quantityText = segment.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
            if (quantityText != null)
            {
                quantityText.text = $"x{rewards[i].quantity}";
            }
        }
    }

    void OnSpinButtonClicked()
    {
        //if (isSpinning || !DailyWheelSystem.Instance.CanSpinToday())
        //    return;

        StartCoroutine(SpinWheelRoutine());
    }

    IEnumerator SpinWheelRoutine()
    {
        isSpinning = true;
        spinButton.interactable = false;

        if (DailyWheelSystem.Instance.SpinWheel(out WheelReward reward))
        {
            yield return StartCoroutine(AnimateWheelSpin(reward));

            yield return new WaitForSeconds(0.5f);

            ShowRewardPopup(reward);
        }

        isSpinning = false;
        //UpdateWheelState();
    }

    IEnumerator AnimateWheelSpin(WheelReward targetReward)
    {
        int rewardIndex = System.Array.IndexOf(rewards, targetReward);

        if (rewardIndex == -1)
            rewardIndex = 0;

        float anglePerSegment = 360f / rewards.Length;
        float targetAngle = rewardIndex * anglePerSegment;

        targetAngle += anglePerSegment / 2f;

        int extraRotations = Random.Range(minExtraRotations, maxExtraRotations + 1);
        float totalRotation = (extraRotations * 360f) + targetAngle;

        float duration = Random.Range(minSpinDuration, maxSpinDuration);

        float currentRotation = wheelContainer.localEulerAngles.z;
        float finalRotation = currentRotation + totalRotation;

        wheelContainer.DORotate(new Vector3(0, 0, -finalRotation), duration, RotateMode.FastBeyond360)
            .SetEase(spinEase);

        yield return new WaitForSeconds(duration);
    }

    void ShowRewardPopup(WheelReward reward)
    {
        if (rewardPopup == null)
            return;

        if (rewardNameText != null)
            rewardNameText.text = reward.displayName;

        if (rewardQuantityText != null)
            rewardQuantityText.text = $"x{reward.quantity}";

        if (rewardIconImage != null && reward.icon != null)
            rewardIconImage.sprite = reward.icon;

        rewardPopup.SetActive(true);
        rewardPopup.transform.localScale = Vector3.zero;

        Sequence popupSequence = DOTween.Sequence();
        popupSequence.Append(rewardPopup.transform.DOScale(1.1f, popupAnimationDuration * 0.6f)
            .SetEase(Ease.OutBack));
        popupSequence.Append(rewardPopup.transform.DOScale(1f, popupAnimationDuration * 0.4f)
            .SetEase(Ease.InOutQuad));
    }

    public void CloseRewardPopup()
    {
        if (rewardPopup == null)
            return;

        rewardPopup.transform.DOScale(0f, popupAnimationDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => rewardPopup.SetActive(false));
    }

    //void UpdateWheelState()
    //{
    //    bool canSpin = DailyWheelSystem.Instance.CanSpinToday();

    //    spinButton.interactable = canSpin && !isSpinning;

    //    if (spinButtonText != null)
    //    {
    //        spinButtonText.text = canSpin ? "GIRAR" : "VUELVE MAÑANA";
    //    }

    //    if (timerText != null)
    //    {
    //        timerText.gameObject.SetActive(!canSpin);
    //    }
    //}

    //void OnWheelAvailabilityChanged(bool isAvailable)
    //{
    //    UpdateWheelState();
    //}
}
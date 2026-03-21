using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StoreRewardFeedbackController : MonoBehaviour
{
    private const int InitialPoolSize = 16;
    private const float DefaultIconSize = 36f;
    private const float RewardGapSeconds = 0.12f;

    private static StoreRewardFeedbackController _instance;

    private readonly Queue<StorePurchaseFeedbackRequest> _pendingRequests = new();

    private CoinCounterUI _coinCounter;
    private LevelSelectionScreenController _levelSelectionScreen;
    private Canvas _rootCanvas;
    private RectTransform _container;
    private StoreRewardFeedbackIcon _iconPrefab;
    private ObjectPool<StoreRewardFeedbackIcon> _pool;
    private Sprite _fallbackSprite;
    private bool _isProcessing;

    public static void EnsureFor(CoinCounterUI counter)
    {
        if (counter == null)
            return;

        StoreRewardFeedbackController controller = GetOrCreate(counter.GetRootCanvas());
        if (controller == null)
            return;

        controller.Bind(counter);
    }

    public static void EnsureFor(LevelSelectionScreenController levelSelectionScreen)
    {
        if (levelSelectionScreen == null)
            return;

        StoreRewardFeedbackController controller = GetOrCreate(levelSelectionScreen.GetRootCanvas());
        if (controller == null)
            return;

        controller.Bind(levelSelectionScreen);
    }

    private static StoreRewardFeedbackController GetOrCreate(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (_instance == null)
        {
            GameObject root = new GameObject("StoreRewardFeedbackController", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            _instance = root.AddComponent<StoreRewardFeedbackController>();
        }

        _instance.SetCanvas(canvas);
        return _instance;
    }

    private void OnEnable()
    {
        GameEvents.OnStorePurchaseFeedbackRequested += HandleStorePurchaseFeedback;
    }

    private void OnDisable()
    {
        GameEvents.OnStorePurchaseFeedbackRequested -= HandleStorePurchaseFeedback;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Bind(CoinCounterUI counter)
    {
        _coinCounter = counter;
        SetCanvas(counter.GetRootCanvas());
    }

    private void Bind(LevelSelectionScreenController levelSelectionScreen)
    {
        _levelSelectionScreen = levelSelectionScreen;
        SetCanvas(levelSelectionScreen.GetRootCanvas());
    }

    private void SetCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        _rootCanvas = canvas;

        RectTransform ownRect = transform as RectTransform;
        if (transform.parent != canvas.transform)
            transform.SetParent(canvas.transform, false);

        ownRect.anchorMin = Vector2.zero;
        ownRect.anchorMax = Vector2.one;
        ownRect.offsetMin = Vector2.zero;
        ownRect.offsetMax = Vector2.zero;
        ownRect.SetAsLastSibling();
        _container = ownRect;

        if (_iconPrefab == null)
            CreatePool();
    }

    private void CreatePool()
    {
        GameObject iconObject = new GameObject(
            "StoreRewardFeedbackIconPrefab",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(Image),
            typeof(StoreRewardFeedbackIcon));

        iconObject.transform.SetParent(transform, false);
        iconObject.SetActive(false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(DefaultIconSize, DefaultIconSize);

        _iconPrefab = iconObject.GetComponent<StoreRewardFeedbackIcon>();
        _pool = new ObjectPool<StoreRewardFeedbackIcon>(_iconPrefab, InitialPoolSize, transform);
    }

    private void HandleStorePurchaseFeedback(StorePurchaseFeedbackRequest request)
    {
        if (request == null || request.SourceTransform == null || !request.HasRewards)
            return;

        _pendingRequests.Enqueue(request);
        if (!_isProcessing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        _isProcessing = true;

        while (_pendingRequests.Count > 0)
        {
            StorePurchaseFeedbackRequest request = _pendingRequests.Dequeue();
            yield return PlayRequest(request);
        }

        _isProcessing = false;
    }

    private IEnumerator PlayRequest(StorePurchaseFeedbackRequest request)
    {
        if (_container == null || request == null || request.SourceTransform == null)
            yield break;

        foreach (StoreRewardFeedbackEntry reward in request.Rewards)
        {
            if (reward == null)
                continue;

            yield return PlayRewardFeedback(request.SourceTransform, reward);
            yield return new WaitForSecondsRealtime(RewardGapSeconds);
        }
    }

    private IEnumerator PlayRewardFeedback(RectTransform sourceTransform, StoreRewardFeedbackEntry reward)
    {
        if (reward != null && reward.RewardType == StoreRewardFeedbackType.UnlimitedLives)
        {
            LevelSelectionScreenController levelSelectionTarget = _levelSelectionScreen != null ? _levelSelectionScreen : LevelSelectionScreenController.Instance;
            if (levelSelectionTarget != null && levelSelectionTarget.GetUnlimitedLivesFeedbackTarget() != null)
            {
                levelSelectionTarget.PlayUnlimitedLivesArrivalFeedback();
                yield return new WaitForSecondsRealtime(0.18f);
            }

            yield break;
        }

        if (!TryResolveRewardVisuals(reward, out StoreRewardVisuals visuals))
            yield break;

        if (!TryGetAnchoredPosition(sourceTransform, out Vector2 sourcePosition) ||
            !TryGetAnchoredPosition(visuals.Target, out Vector2 targetPosition))
        {
            yield break;
        }

        int iconCount = GetVisualCount(reward);
        if (iconCount <= 0)
            yield break;

        int completed = 0;
        bool finished = false;

        for (int i = 0; i < iconCount; i++)
        {
            StoreRewardFeedbackIcon icon = _pool.Get();
            icon.Configure(visuals.Sprite, visuals.Color, visuals.Size);
            PlayIconTween(icon, sourcePosition, targetPosition, reward.RewardType, i, iconCount, () =>
            {
                completed++;
                if (completed >= iconCount)
                {
                    visuals.OnArrival?.Invoke();
                    finished = true;
                }
            });
        }

        while (!finished)
            yield return null;
    }

    private void PlayIconTween(StoreRewardFeedbackIcon icon, Vector2 sourcePosition, Vector2 targetPosition, StoreRewardFeedbackType rewardType, int index, int totalCount, System.Action onComplete)
    {
        float delay = index * (rewardType == StoreRewardFeedbackType.Coins ? 0.03f : 0.06f);
        float spread = rewardType == StoreRewardFeedbackType.Coins ? Random.Range(40f, 110f) : Random.Range(55f, 85f);
        Vector2 burstDirection = Quaternion.Euler(0f, 0f, Random.Range(-75f, 75f)) * Vector2.up;
        Vector2 burstPosition = sourcePosition + burstDirection * spread;
        float launchDuration = rewardType == StoreRewardFeedbackType.Coins ? Random.Range(0.12f, 0.2f) : Random.Range(0.14f, 0.2f);
        float flyDuration = rewardType == StoreRewardFeedbackType.Coins ? Random.Range(0.38f, 0.55f) : Random.Range(0.46f, 0.62f);
        float endScale = rewardType == StoreRewardFeedbackType.Coins ? 0.55f : 0.7f;

        icon.RectTransform.anchoredPosition = sourcePosition;
        icon.RectTransform.localScale = Vector3.one * Random.Range(0.78f, 0.98f);
        icon.transform.SetAsLastSibling();

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        sequence.SetDelay(delay);
        sequence.Append(icon.RectTransform.DOAnchorPos(burstPosition, launchDuration).SetEase(Ease.OutQuad));
        sequence.Join(icon.RectTransform.DOScale(1f, launchDuration).SetEase(Ease.OutBack));
        sequence.Append(icon.RectTransform.DOAnchorPos(targetPosition, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(icon.RectTransform.DOScale(endScale, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(icon.CanvasGroup.DOFade(0.9f, flyDuration * 0.85f).SetEase(Ease.Linear));
        sequence.OnComplete(() =>
        {
            _pool.Release(icon);
            onComplete?.Invoke();
        });
    }

    private bool TryResolveRewardVisuals(StoreRewardFeedbackEntry reward, out StoreRewardVisuals visuals)
    {
        visuals = default;

        switch (reward.RewardType)
        {
            case StoreRewardFeedbackType.Coins:
                CoinCounterUI coinTarget = _coinCounter != null ? _coinCounter : CoinCounterUI.Instance;
                if (coinTarget == null)
                    return false;

                Sprite coinSprite = coinTarget.GetCoinSprite();
                visuals = new StoreRewardVisuals(
                    coinTarget.GetFeedbackTarget(),
                    coinSprite != null ? coinSprite : GetFallbackSprite(),
                    coinTarget.GetCoinColor(),
                    new Vector2(DefaultIconSize, DefaultIconSize),
                    coinTarget.PlayArrivalFeedback);
                return visuals.Target != null;
        }

        return false;
    }

    private bool TryGetAnchoredPosition(RectTransform target, out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (_container == null || target == null)
            return false;

        Camera uiCamera = _rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? _rootCanvas.worldCamera
            : null;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_container, screenPoint, uiCamera, out anchoredPosition);
    }

    private int GetVisualCount(StoreRewardFeedbackEntry reward)
    {
        if (reward == null)
            return 0;

        return reward.RewardType switch
        {
            StoreRewardFeedbackType.Coins => GetCoinVisualCount(reward.Amount),
            StoreRewardFeedbackType.UnlimitedLives => 0,
            _ => 0
        };
    }

    private int GetCoinVisualCount(int coinAmount)
    {
        if (coinAmount <= 0) return 0;
        if (coinAmount < 250) return 6;
        if (coinAmount < 1000) return 8;
        if (coinAmount < 5000) return 10;
        return 12;
    }

    private Sprite GetFallbackSprite()
    {
        if (_fallbackSprite == null)
            _fallbackSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");

        return _fallbackSprite;
    }

    private readonly struct StoreRewardVisuals
    {
        public readonly RectTransform Target;
        public readonly Sprite Sprite;
        public readonly Color Color;
        public readonly Vector2 Size;
        public readonly System.Action OnArrival;

        public StoreRewardVisuals(RectTransform target, Sprite sprite, Color color, Vector2 size, System.Action onArrival)
        {
            Target = target;
            Sprite = sprite;
            Color = color;
            Size = size;
            OnArrival = onArrival;
        }
    }
}

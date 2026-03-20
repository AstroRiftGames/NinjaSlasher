using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CoinFlyFeedbackController : MonoBehaviour
{
    private const int InitialPoolSize = 12;
    private const float CoinSize = 36f;

    private static CoinFlyFeedbackController _instance;

    private CoinCounterUI _targetCounter;
    private Canvas _rootCanvas;
    private RectTransform _container;
    private CoinFlyFeedbackCoin _coinPrefab;
    private ObjectPool<CoinFlyFeedbackCoin> _pool;

    public static void EnsureFor(CoinCounterUI counter)
    {
        if (counter == null)
            return;

        if (_instance == null)
        {
            Canvas canvas = counter.GetRootCanvas();
            if (canvas == null)
                return;

            GameObject root = new GameObject("CoinFlyFeedbackController", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            _instance = root.AddComponent<CoinFlyFeedbackController>();
        }

        _instance.Bind(counter);
    }

    private void OnEnable()
    {
        GameEvents.OnCoinPackPurchaseFeedbackRequested += HandleCoinPackPurchaseFeedback;
    }

    private void OnDisable()
    {
        GameEvents.OnCoinPackPurchaseFeedbackRequested -= HandleCoinPackPurchaseFeedback;
    }

    private void Bind(CoinCounterUI counter)
    {
        _targetCounter = counter;
        _rootCanvas = counter.GetRootCanvas();

        RectTransform ownRect = transform as RectTransform;
        ownRect.anchorMin = Vector2.zero;
        ownRect.anchorMax = Vector2.one;
        ownRect.offsetMin = Vector2.zero;
        ownRect.offsetMax = Vector2.zero;
        ownRect.SetAsLastSibling();

        _container = ownRect;

        if (_coinPrefab == null)
            CreatePool();
    }

    private void CreatePool()
    {
        GameObject coinObject = new GameObject("CoinFlyFeedbackCoinPrefab", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(CoinFlyFeedbackCoin));
        coinObject.transform.SetParent(transform, false);
        coinObject.SetActive(false);

        RectTransform coinRect = coinObject.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0.5f, 0.5f);
        coinRect.anchorMax = new Vector2(0.5f, 0.5f);
        coinRect.sizeDelta = new Vector2(CoinSize, CoinSize);

        _coinPrefab = coinObject.GetComponent<CoinFlyFeedbackCoin>();
        _pool = new ObjectPool<CoinFlyFeedbackCoin>(_coinPrefab, InitialPoolSize, transform);
    }

    private void HandleCoinPackPurchaseFeedback(int coinAmount, RectTransform sourceTransform)
    {
        if (_targetCounter == null || _container == null || sourceTransform == null)
            return;

        RectTransform targetTransform = _targetCounter.GetFeedbackTarget();
        if (targetTransform == null)
            return;

        if (!TryGetAnchoredPosition(sourceTransform, out Vector2 sourcePosition) ||
            !TryGetAnchoredPosition(targetTransform, out Vector2 targetPosition))
        {
            return;
        }

        Sprite coinSprite = _targetCounter.GetCoinSprite();
        int coinCount = GetCoinVisualCount(coinAmount);
        int completed = 0;

        for (int i = 0; i < coinCount; i++)
        {
            CoinFlyFeedbackCoin coin = _pool.Get();
            coin.Configure(coinSprite, _targetCounter.GetCoinColor(), new Vector2(CoinSize, CoinSize));
            PlayCoinTween(coin, sourcePosition, targetPosition, i, coinCount, () =>
            {
                completed++;
                if (completed == coinCount)
                    _targetCounter.PlayArrivalFeedback();
            });
        }
    }

    private void PlayCoinTween(CoinFlyFeedbackCoin coin, Vector2 sourcePosition, Vector2 targetPosition, int index, int totalCount, System.Action onComplete)
    {
        float spread = Random.Range(40f, 110f);
        Vector2 burstDirection = Quaternion.Euler(0f, 0f, Random.Range(-70f, 70f)) * Vector2.up;
        Vector2 burstPosition = sourcePosition + burstDirection * spread;
        float launchDuration = Random.Range(0.12f, 0.2f);
        float flyDuration = Random.Range(0.38f, 0.55f);
        float delay = index * 0.03f;

        coin.RectTransform.anchoredPosition = sourcePosition;
        coin.RectTransform.localScale = Vector3.one * Random.Range(0.75f, 0.95f);
        coin.transform.SetAsLastSibling();

        Sequence sequence = DOTween.Sequence();
        sequence.SetUpdate(true);
        sequence.SetDelay(delay);
        sequence.Append(coin.RectTransform.DOAnchorPos(burstPosition, launchDuration).SetEase(Ease.OutQuad));
        sequence.Join(coin.RectTransform.DOScale(1f, launchDuration).SetEase(Ease.OutBack));
        sequence.Append(coin.RectTransform.DOAnchorPos(targetPosition, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(coin.RectTransform.DOScale(0.55f, flyDuration).SetEase(Ease.InQuad));
        sequence.Join(coin.CanvasGroup.DOFade(0.9f, flyDuration * 0.85f).SetEase(Ease.Linear));
        sequence.OnComplete(() =>
        {
            _pool.Release(coin);
            onComplete?.Invoke();
        });
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

    private int GetCoinVisualCount(int coinAmount)
    {
        if (coinAmount <= 0) return 0;
        if (coinAmount < 250) return 6;
        if (coinAmount < 1000) return 8;
        if (coinAmount < 5000) return 10;
        return 12;
    }
}

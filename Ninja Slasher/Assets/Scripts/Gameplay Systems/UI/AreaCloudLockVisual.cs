using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class AreaCloudLockVisual : MonoBehaviour
{
    [Header("Cloud Sprites")]
    [Tooltip("Sprites de nube")]
    [SerializeField] private Sprite[] cloudSprites;

    [Tooltip("Tamano base de cada nube")]
    [SerializeField] private Vector2 cloudBaseSize = new Vector2(220f, 160f);

    [Header("Grid")]
    [SerializeField, Range(0.1f, 0.65f)] private float overlapFactor = 0.35f;
    [SerializeField, Range(0f, 0.45f)] private float jitterFactor = 0.25f;
    [SerializeField, Range(0f, 1f)] private float edgeExtendFactor = 0.5f;

    [Header("Cloud Transform")]
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.2f;
    [SerializeField] private float minRotation = -5f;
    [SerializeField] private float maxRotation = 5f;

    [Header("Movement")]
    [SerializeField] private float driftSpeed = 0.25f;

    [Tooltip("Distancia maxima de deriva en pixeles")]
    [SerializeField] private float driftAmplitude = 15f;

    [Tooltip("Amplitud de oscilacion vertical")]
    [SerializeField] private float oscillationAmplitude = 8f;

    private sealed class CloudData
    {
        public RectTransform Rect;
        public Image Image;
        public Vector2 Origin;
        public Vector2 DriftDir;
        public float DriftPhase;
        public float OscPhase;
        public float SpeedMultiplier;
    }

    private readonly List<CloudData> _clouds = new List<CloudData>(8);

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private RectTransform _container;
    private Coroutine _unlockRoutine;
    private Vector2 _lastRectSize = new Vector2(-1f, -1f);
    private int _activeCloudCount;
    private int _gridCols;
    private int _gridRows;
    private int _lastSettingsHash;
    private bool _started;
    private bool _dispersing;
    private bool _cloudsVisible;
    private bool _layoutDirty = true;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ConfigureCanvasGroup();
    }

    private void Start()
    {
        _started = true;
        RefreshLockState();
    }

    private void OnEnable()
    {
        if (!_started) return;
        RefreshLockState();
    }

    private void OnDisable()
    {
        StopUnlockAnimation(resetPositions: true);
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!_started) return;

        _layoutDirty = true;

        if (isActiveAndEnabled)
            BuildCloudsIfNeeded();
    }

    private void Update()
    {
        if (!_cloudsVisible || _dispersing || _activeCloudCount == 0)
            return;

        AnimateClouds(Time.deltaTime);
    }

    private void ConfigureCanvasGroup()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = false;
        _canvasGroup.alpha = 1f;
    }

    private void BuildCloudsIfNeeded()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            Debug.LogWarning($"[AreaCloudLockVisual] No hay sprites asignados en '{gameObject.name}'.");
            SetCloudsVisible(false);
            _activeCloudCount = 0;
            return;
        }

        Rect bounds = _rectTransform.rect;
        if (bounds.width <= 0f || bounds.height <= 0f)
            return;

        int settingsHash = ComputeSettingsHash();
        int cols = CalculateCols(bounds.width);
        int rows = CalculateRows(bounds.height);
        int requiredCloudCount = cols * rows;
        Vector2 rectSize = bounds.size;

        bool needsRebuild =
            _layoutDirty ||
            _container == null ||
            requiredCloudCount != _activeCloudCount ||
            cols != _gridCols ||
            rows != _gridRows ||
            !Approximately(_lastRectSize, rectSize) ||
            _lastSettingsHash != settingsHash;

        _container = GetOrCreateContainer();

        if (!needsRebuild)
        {
            SetCloudsVisible(true);
            return;
        }

        float stepX = cloudBaseSize.x * (1f - overlapFactor);
        float stepY = cloudBaseSize.y * (1f - overlapFactor);
        float startX = -(cols - 1) * stepX * 0.5f;
        float startY = -(rows - 1) * stepY * 0.5f;
        float maxJitterX = stepX * jitterFactor;
        float maxJitterY = stepY * jitterFactor;

        EnsureCloudPool(requiredCloudCount);

        _activeCloudCount = requiredCloudCount;
        _gridCols = cols;
        _gridRows = rows;
        _lastRectSize = rectSize;
        _lastSettingsHash = settingsHash;
        _layoutDirty = false;

        for (int col = 0, index = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++, index++)
            {
                float x = startX + col * stepX + UnityEngine.Random.Range(-maxJitterX, maxJitterX);
                float y = startY + row * stepY + UnityEngine.Random.Range(-maxJitterY, maxJitterY);
                ConfigureCloud(_clouds[index], index, new Vector2(x, y));
            }
        }

        for (int i = requiredCloudCount; i < _clouds.Count; i++)
            SetCloudActive(_clouds[i], false);

        SetCloudsVisible(true);
    }

    private void RefreshLockState()
    {
        ConfigureCanvasGroup();
        StopUnlockAnimation(resetPositions: false);
        BuildCloudsIfNeeded();
        SetCloudsVisible(_activeCloudCount > 0);
    }

    private void SetCloudsVisible(bool visible)
    {
        _cloudsVisible = visible && _activeCloudCount > 0;

        if (_container == null) return;

        GameObject containerGameObject = _container.gameObject;
        if (containerGameObject.activeSelf != _cloudsVisible)
            containerGameObject.SetActive(_cloudsVisible);
    }

    private void AnimateClouds(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        float t = Time.time;

        for (int i = 0; i < _activeCloudCount; i++)
        {
            CloudData cloud = _clouds[i];
            if (cloud.Rect == null) continue;

            float speed = driftSpeed * cloud.SpeedMultiplier;
            float drift = Mathf.Sin(t * speed + cloud.DriftPhase) * driftAmplitude;
            float osc = Mathf.Sin(t * speed * 1.3f + cloud.OscPhase) * oscillationAmplitude;

            cloud.Rect.anchoredPosition = cloud.Origin
                + cloud.DriftDir * drift
                + new Vector2(0f, osc);
        }
    }

    private RectTransform GetOrCreateContainer()
    {
        if (_container != null)
        {
            if (_container.transform.parent != transform)
                _container.SetParent(transform, false);

            if (_container.GetSiblingIndex() != 0)
                _container.SetSiblingIndex(0);

            return _container;
        }

        Transform existing = transform.Find("CloudsContainer");
        GameObject go = existing != null
            ? existing.gameObject
            : new GameObject("CloudsContainer", typeof(RectTransform));

        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0);

        _container = go.GetComponent<RectTransform>();
        _container.anchorMin = Vector2.zero;
        _container.anchorMax = Vector2.one;
        _container.offsetMin = Vector2.zero;
        _container.offsetMax = Vector2.zero;

        return _container;
    }

    private void EnsureCloudPool(int requiredCloudCount)
    {
        for (int i = _clouds.Count; i < requiredCloudCount; i++)
            _clouds.Add(CreateCloud(i));
    }

    private CloudData CreateCloud(int index)
    {
        GameObject go = new GameObject($"Cloud_{index + 1}", typeof(RectTransform));
        go.transform.SetParent(_container, false);

        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = cloudBaseSize;

        return new CloudData
        {
            Rect = rect,
            Image = image,
        };
    }

    private void ConfigureCloud(CloudData cloud, int index, Vector2 origin)
    {
        cloud.Rect.gameObject.name = $"Cloud_{index + 1}";
        cloud.Image.sprite = cloudSprites[UnityEngine.Random.Range(0, cloudSprites.Length)];
        cloud.Rect.sizeDelta = cloudBaseSize;

        float scale = UnityEngine.Random.Range(minScale, maxScale);
        cloud.Rect.localScale = Vector3.one * scale;
        cloud.Rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(minRotation, maxRotation));
        cloud.Rect.anchoredPosition = origin;

        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

        cloud.Origin = origin;
        cloud.DriftDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        cloud.DriftPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        cloud.OscPhase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        cloud.SpeedMultiplier = UnityEngine.Random.Range(0.7f, 1.3f);

        SetCloudActive(cloud, true);
    }

    private static void SetCloudActive(CloudData cloud, bool active)
    {
        if (cloud?.Rect == null) return;

        GameObject cloudGameObject = cloud.Rect.gameObject;
        if (cloudGameObject.activeSelf != active)
            cloudGameObject.SetActive(active);
    }

    private void ResetCloudPositions()
    {
        for (int i = 0; i < _activeCloudCount; i++)
        {
            CloudData cloud = _clouds[i];
            if (cloud.Rect == null) continue;
            cloud.Rect.anchoredPosition = cloud.Origin;
        }
    }

    private int CalculateCols(float width)
    {
        float stepX = cloudBaseSize.x * (1f - overlapFactor);
        float extX = cloudBaseSize.x * edgeExtendFactor;
        return Mathf.CeilToInt((width + extX * 2f) / stepX) + 1;
    }

    private int CalculateRows(float height)
    {
        float stepY = cloudBaseSize.y * (1f - overlapFactor);
        float extY = cloudBaseSize.y * edgeExtendFactor;
        return Mathf.CeilToInt((height + extY * 2f) / stepY) + 1;
    }

    private int ComputeSettingsHash()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + cloudSprites.Length;
            hash = hash * 31 + cloudBaseSize.GetHashCode();
            hash = hash * 31 + overlapFactor.GetHashCode();
            hash = hash * 31 + jitterFactor.GetHashCode();
            hash = hash * 31 + edgeExtendFactor.GetHashCode();
            hash = hash * 31 + minScale.GetHashCode();
            hash = hash * 31 + maxScale.GetHashCode();
            hash = hash * 31 + minRotation.GetHashCode();
            hash = hash * 31 + maxRotation.GetHashCode();
            hash = hash * 31 + driftSpeed.GetHashCode();
            hash = hash * 31 + driftAmplitude.GetHashCode();
            hash = hash * 31 + oscillationAmplitude.GetHashCode();
            return hash;
        }
    }

    private static bool Approximately(Vector2 a, Vector2 b)
    {
        return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    }

    public void Rebuild()
    {
        if (!_started) return;
        RefreshLockState();
    }

    public void PlayUnlockAnimation(Action onComplete = null)
    {
        if (!_started)
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        RefreshLockState();

        if (_activeCloudCount == 0)
        {
            gameObject.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        StopUnlockAnimation(resetPositions: true);
        _unlockRoutine = StartCoroutine(DispersionAndFade(onComplete));
    }

    private IEnumerator DispersionAndFade(Action onComplete, float duration = 2.5f)
    {
        SetCloudsVisible(true);
        _dispersing = true;

        Vector2[] startPositions = new Vector2[_activeCloudCount];
        Vector2[] targets = new Vector2[_activeCloudCount];
        float disperseDistance = _rectTransform.rect.width * 0.75f;

        for (int i = 0; i < _activeCloudCount; i++)
        {
            CloudData cloud = _clouds[i];

            startPositions[i] = cloud.Rect != null
                ? cloud.Rect.anchoredPosition
                : cloud.Origin;

            int col = i / _gridRows;
            int row = i % _gridRows;
            float sign = ((col + row) % 2 == 0) ? 1f : -1f;

            targets[i] = new Vector2(cloud.Origin.x + sign * disperseDistance, cloud.Origin.y);
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < _activeCloudCount; i++)
            {
                CloudData cloud = _clouds[i];
                if (cloud.Rect == null) continue;
                cloud.Rect.anchoredPosition = Vector2.Lerp(startPositions[i], targets[i], eased);
            }

            _canvasGroup.alpha = 1f - t;

            elapsed += Time.deltaTime;
            yield return null;
        }

        _canvasGroup.alpha = 1f;
        _dispersing = false;
        _unlockRoutine = null;

        onComplete?.Invoke();
        gameObject.SetActive(false);
    }

    private void StopUnlockAnimation(bool resetPositions)
    {
        if (_unlockRoutine != null)
        {
            StopCoroutine(_unlockRoutine);
            _unlockRoutine = null;
        }

        _dispersing = false;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        if (resetPositions)
            ResetCloudPositions();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Clouds")]
    private void ContextMenuRebuild()
    {
        if (!Application.isPlaying) return;
        _layoutDirty = true;
        RefreshLockState();
    }
#endif
}

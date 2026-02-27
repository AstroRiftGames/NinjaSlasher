using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class AreaCloudLockVisual : MonoBehaviour
{
    [Header("Cloud Sprites")]
    [Tooltip("Arrastra aquí los sprites de nube")]
    [SerializeField] private Sprite[] cloudSprites;

    [Tooltip("Tamaño base de cada nube en unidades de canvas.")]
    [SerializeField] private Vector2 cloudBaseSize = new Vector2(220f, 160f);

    [Header("Grid")]

    [SerializeField, Range(0.1f, 0.65f)] private float overlapFactor = 0.35f;

    [SerializeField, Range(0f, 0.45f)] private float jitterFactor = 0.25f;

    [SerializeField, Range(0f, 1f)] private float edgeExtendFactor = 0.5f;

    [Header("Cloud Transform")]
    [SerializeField] private float minScale    = 0.8f;
    [SerializeField] private float maxScale    = 1.2f;
    [SerializeField] private float minRotation = -5f;
    [SerializeField] private float maxRotation =  5f;

    [Header("Movement")]
    [Tooltip("Frecuencia angular de la deriva (rad/s).")]
    [SerializeField] private float driftSpeed = 0.25f;

    [Tooltip("Distancia máxima de deriva en píxeles de canvas.")]
    [SerializeField] private float driftAmplitude = 15f;

    [Tooltip("Amplitud de oscilación vertical adicional.")]
    [SerializeField] private float oscillationAmplitude = 8f;

    private struct CloudData
    {
        public RectTransform Rect;
        public Vector2       Origin;
        public Vector2       DriftDir;
        public float         DriftPhase;
        public float         OscPhase;
        public float         SpeedMultiplier;
    }

    private CanvasGroup   _canvasGroup;
    private RectTransform _rectTransform;
    private CloudData[]   _clouds;
    private bool          _started;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        ConfigureCanvasGroup();
    }

    private void Start()
    {
        _started = true;
        BuildClouds();
    }

    private void Update()
    {
        if (_clouds == null) return;

        float t = Time.time;

        for (int i = 0; i < _clouds.Length; i++)
        {
            ref CloudData c = ref _clouds[i];

            if (c.Rect == null) continue;

            float speed = driftSpeed * c.SpeedMultiplier;
            float drift = Mathf.Sin(t * speed        + c.DriftPhase) * driftAmplitude;
            float osc   = Mathf.Sin(t * speed * 1.3f + c.OscPhase)   * oscillationAmplitude;

            c.Rect.anchoredPosition = c.Origin
                + c.DriftDir * drift
                + new Vector2(0f, osc);
        }
    }

    private void ConfigureCanvasGroup()
    {
        _canvasGroup                = GetComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable   = false;
        _canvasGroup.alpha          = 1f;
    }

    private void BuildClouds()
    {
        if (cloudSprites == null || cloudSprites.Length == 0)
        {
            Debug.LogWarning($"[AreaCloudLockVisual] No hay sprites asignados en '{gameObject.name}'.");
            return;
        }

        Rect bounds = _rectTransform.rect;

        if (bounds.width <= 0f || bounds.height <= 0f)
            return;

        RectTransform container = GetOrCreateContainer();
        ClearChildren(container);

        float W = bounds.width;
        float H = bounds.height;

        float stepX = cloudBaseSize.x * (1f - overlapFactor);
        float stepY = cloudBaseSize.y * (1f - overlapFactor);

        float extX = cloudBaseSize.x * edgeExtendFactor;
        float extY = cloudBaseSize.y * edgeExtendFactor;

        int cols = Mathf.CeilToInt((W + extX * 2f) / stepX) + 1;
        int rows = Mathf.CeilToInt((H + extY * 2f) / stepY) + 1;

        float startX = -(cols - 1) * stepX * 0.5f;
        float startY = -(rows - 1) * stepY * 0.5f;

        float maxJitterX = stepX * jitterFactor;
        float maxJitterY = stepY * jitterFactor;

        _clouds = new CloudData[cols * rows];
        int index = 0;

        for (int col = 0; col < cols; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                float x = startX + col * stepX + Random.Range(-maxJitterX, maxJitterX);
                float y = startY + row * stepY + Random.Range(-maxJitterY, maxJitterY);

                _clouds[index] = SpawnCloud(container, index, new Vector2(x, y));
                index++;
            }
        }
    }

    private RectTransform GetOrCreateContainer()
    {
        Transform existing = transform.Find("CloudsContainer");
        GameObject go      = existing != null
            ? existing.gameObject
            : new GameObject("CloudsContainer", typeof(RectTransform));

        go.transform.SetParent(transform, false);
        go.transform.SetSiblingIndex(0);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return rt;
    }

    private CloudData SpawnCloud(RectTransform container, int index, Vector2 origin)
    {
        GameObject go = new GameObject($"Cloud_{index + 1}", typeof(RectTransform));
        go.transform.SetParent(container, false);

        Image img         = go.AddComponent<Image>();
        img.sprite        = cloudSprites[Random.Range(0, cloudSprites.Length)];
        img.raycastTarget = false;
        img.preserveAspect = true;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = cloudBaseSize;

        float scale = Random.Range(minScale, maxScale);
        rt.localScale    = Vector3.one * scale;
        rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(minRotation, maxRotation));

        rt.anchoredPosition = origin;

        float angle = Random.Range(0f, Mathf.PI * 2f);

        return new CloudData
        {
            Rect            = rt,
            Origin          = origin,
            DriftDir        = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
            DriftPhase      = Random.Range(0f, Mathf.PI * 2f),
            OscPhase        = Random.Range(0f, Mathf.PI * 2f),
            SpeedMultiplier = Random.Range(0.7f, 1.3f),
        };
    }

    private static void ClearChildren(RectTransform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    public void Rebuild()
    {
        if (!_started) return;
        BuildClouds();
    }

#if UNITY_EDITOR
    [ContextMenu("Rebuild Clouds")]
    private void ContextMenuRebuild()
    {
        if (!Application.isPlaying) return;
        BuildClouds();
    }
#endif
}

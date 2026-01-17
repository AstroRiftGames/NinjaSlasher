using UnityEngine;
using UnityEngine.UI;

public class BackgroundScroller : MonoBehaviour
{
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private RectTransform _backgroundContainer;

    [Range(0f, 1f)]
    [SerializeField] private float _scrollFactor = 1f;

    [SerializeField] private bool _syncWithContent = true;

    private RectTransform _content;
    private Vector2 _lastContentPosition;

    private void Awake()
    {
        if (_scrollRect == null)
        {
            _scrollRect = GetComponent<ScrollRect>();
        }

        if (_scrollRect != null)
        {
            _content = _scrollRect.content;
        }
    }

    private void Start()
    {
        if (_content != null)
        {
            _lastContentPosition = _content.anchoredPosition;
        }
    }

    private void LateUpdate()
    {
        UpdateBackgroundPosition();
    }

    private void UpdateBackgroundPosition()
    {
        if (_content == null || _backgroundContainer == null) return;

        Vector2 currentContentPosition = _content.anchoredPosition;
        Vector2 deltaMovement = currentContentPosition - _lastContentPosition;

        float movementFactor = _syncWithContent ? 1f : _scrollFactor;

        Vector2 newBackgroundPosition = _backgroundContainer.anchoredPosition;
        newBackgroundPosition.x += deltaMovement.x * movementFactor;

        _backgroundContainer.anchoredPosition = newBackgroundPosition;

        _lastContentPosition = currentContentPosition;
    }

    public void ResetBackgroundPosition()
    {
        if (_backgroundContainer != null)
        {
            _backgroundContainer.anchoredPosition = Vector2.zero;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_syncWithContent)
        {
            _scrollFactor = 1f;
        }
    }
#endif
}
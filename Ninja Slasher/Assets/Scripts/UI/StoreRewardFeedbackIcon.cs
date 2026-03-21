using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class StoreRewardFeedbackIcon : MonoBehaviour, IPoolable
{
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Image _image;

    public RectTransform RectTransform => _rectTransform;
    public CanvasGroup CanvasGroup => _canvasGroup;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _image = GetComponent<Image>();
    }

    public void Configure(Sprite sprite, Color color, Vector2 size)
    {
        _image.sprite = sprite;
        _image.color = color;
        _rectTransform.sizeDelta = size;
    }

    public void OnSpawn()
    {
        DOTween.Kill(_rectTransform);
        DOTween.Kill(_canvasGroup);
        _rectTransform.localScale = Vector3.one;
        _canvasGroup.alpha = 1f;
    }

    public void OnDespawn()
    {
        DOTween.Kill(_rectTransform);
        DOTween.Kill(_canvasGroup);
        _rectTransform.localScale = Vector3.one;
        _canvasGroup.alpha = 1f;
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HandSwipeAnimation : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private RectTransform handTransform;
    [SerializeField] private Image handImage;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private Vector2 startPosition = new Vector2(-100, 0);
    [SerializeField] private Vector2 endPosition = new Vector2(100, 0);
    [SerializeField] private float swipeDuration = 1f;
    [SerializeField] private float pauseBetweenSwipes = 0.5f;
    [SerializeField] private AnimationCurve swipeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("VISUAL SETTINGS")]
    [SerializeField] private bool fadeInOut = true;
    [SerializeField] private float fadeSpeed = 2f;

    private bool isAnimating = false;
    private Coroutine animationCoroutine;

    private void OnEnable()
    {
        if (handTransform == null)
        {
            handTransform = GetComponent<RectTransform>();
        }

        StartAnimation();
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    public void StartAnimation()
    {
        if (isAnimating) return;

        isAnimating = true;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(SwipeAnimationLoop());
    }

    public void StopAnimation()
    {
        isAnimating = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator SwipeAnimationLoop()
    {
        while (isAnimating)
        {
            yield return StartCoroutine(PerformSwipe());

            yield return new WaitForSecondsRealtime(pauseBetweenSwipes);
        }
    }

    private IEnumerator PerformSwipe()
    {
        float elapsedTime = 0f;

        if (fadeInOut && handImage != null)
        {
            yield return StartCoroutine(FadeHand(0f, 1f, 1f / fadeSpeed));
        }

        while (elapsedTime < swipeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / swipeDuration);
            float curveValue = swipeCurve.Evaluate(t);

            Vector2 currentPos = Vector2.Lerp(startPosition, endPosition, curveValue);
            handTransform.anchoredPosition = currentPos;

            yield return null;
        }

        handTransform.anchoredPosition = endPosition;

        if (fadeInOut && handImage != null)
        {
            yield return StartCoroutine(FadeHand(1f, 0f, 1f / fadeSpeed));
        }

        handTransform.anchoredPosition = startPosition;
    }

    private IEnumerator FadeHand(float fromAlpha, float toAlpha, float duration)
    {
        if (handImage == null) yield break;

        float elapsedTime = 0f;
        Color color = handImage.color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            handImage.color = color;

            yield return null;
        }

        color.a = toAlpha;
        handImage.color = color;
    }

    public void SetSwipePositions(Vector2 start, Vector2 end)
    {
        startPosition = start;
        endPosition = end;

        if (isAnimating)
        {
            StopAnimation();
            StartAnimation();
        }
    }

    public void SetSwipeDuration(float duration)
    {
        swipeDuration = duration;
    }
}
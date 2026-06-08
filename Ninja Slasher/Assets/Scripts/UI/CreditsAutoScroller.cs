using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class CreditsAutoScroller : MonoBehaviour
{
    [SerializeField] private float _initialDelay = 0.5f;
    [SerializeField] private float _scrollDuration = 30f;

    private ScrollRect _scrollRect;
    private Coroutine _autoScrollCoroutine;

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        ConfigureScrollRect();
        ResetScrollPosition();
    }

    private void OnDisable()
    {
        StopAutoScroll();
    }

    public void BeginScroll(System.Action onComplete = null)
    {
        StopAutoScroll();
        _autoScrollCoroutine = StartCoroutine(AutoScrollRoutine(onComplete));
    }

    private void ConfigureScrollRect()
    {
        if (_scrollRect == null) return;
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.inertia = false;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = 0f;
    }

    private void ResetScrollPosition()
    {
        if (_scrollRect == null) return;
        Canvas.ForceUpdateCanvases();
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void StopAutoScroll()
    {
        if (_autoScrollCoroutine != null)
        {
            StopCoroutine(_autoScrollCoroutine);
            _autoScrollCoroutine = null;
        }
    }

    private IEnumerator AutoScrollRoutine(System.Action onComplete = null)
    {
        yield return new WaitForSecondsRealtime(_initialDelay);

        float elapsed = 0f;
        while (elapsed < _scrollDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / _scrollDuration;
            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        _scrollRect.verticalNormalizedPosition = 0f;
        _autoScrollCoroutine = null;
        onComplete?.Invoke();
    }
}

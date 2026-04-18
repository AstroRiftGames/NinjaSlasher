using UnityEngine;
using UnityEngine.UI;

public class InputFeedbackUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform currentPoint;
    [SerializeField] private RectTransform trail;

    [Header("Settings")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private float trailThickness = 20f;
    [SerializeField] private float maxDistance = 300f;
    [SerializeField] private float resumeThreshold = 10f;

    private InputDetection swipeDetection;
    private Camera cam;
    private Vector2 startScreenPos;
    private bool isCanceled;

    private void Awake()
    {
        cam = Camera.main;
    }

    public void SetSwipeDetection(InputDetection newSwipeDetection)
    {
        if (swipeDetection != null)
        {
            swipeDetection.OnInputStart -= HandleInputStart;
            swipeDetection.OnSwipeCanceled -= HandleSwipeCanceled;
        }

        swipeDetection = newSwipeDetection;

        swipeDetection.OnInputStart += HandleInputStart;
        swipeDetection.OnSwipeCanceled += HandleSwipeCanceled;
    }

    private void HandleSwipeCanceled()
    {
        isCanceled = true;
    }

    private void Start()
    {
        SetActive(false);
    }

    private void Update()
    {
        if (swipeDetection == null) return;

        if (!swipeDetection.IsPressing || GameManager.Instance.IsVictory || GameManager.Instance.PlayerHasDied)
        {
            SetActive(false);
            return;
        }

        Vector2 currentScreenPos = swipeDetection.CurrentPosition;
        UpdateVisual(startScreenPos, currentScreenPos);
    }

    private void HandleInputStart(Vector2 position)
    {
        isCanceled = false;

        startScreenPos = position;
        SetActive(true);
        UpdateVisual(startScreenPos, startScreenPos);
    }

    private void UpdateVisual(Vector2 start, Vector2 current)
    {
        Vector2 startUI = ScreenToCanvasPosition(start);
        Vector2 currentUI = ScreenToCanvasPosition(current);

        startPoint.anchoredPosition = startUI;

        Vector2 direction = currentUI - startUI;
        float distance = direction.magnitude;

        if (isCanceled && distance > resumeThreshold)
        {
            isCanceled = false;
        }

        if (isCanceled)
        {
            trail.gameObject.SetActive(false);
            currentPoint.gameObject.SetActive(false);
            return;
        }
        else
        {
            trail.gameObject.SetActive(true);
            currentPoint.gameObject.SetActive(true);
        }

        if (distance > maxDistance)
        {
            direction = direction.normalized * maxDistance;
            distance = maxDistance;
        }

        Vector2 clampedCurrent = startUI + direction;

        currentPoint.anchoredPosition = clampedCurrent;

        trail.sizeDelta = new Vector2(distance, trailThickness);
        trail.anchoredPosition = startUI + direction * 0.5f;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        trail.rotation = Quaternion.Euler(0, 0, angle);
        startPoint.rotation = Quaternion.Euler(0, 0, angle + 180);
    }

    private Vector2 ScreenToCanvasPosition(Vector2 screenPos)
    {
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 localPoint
        );

        return localPoint;
    }

    private Vector2 GetCurrentPointerPosition()
    {
        return UnityEngine.InputSystem.Pointer.current.position.ReadValue();
    }

    private void SetActive(bool state)
    {
        startPoint.gameObject.SetActive(state);
        currentPoint.gameObject.SetActive(state);
        trail.gameObject.SetActive(state);
    }

    private bool IsSetupValid()
    {
        return startPoint != null &&
               currentPoint != null &&
               trail != null &&
               canvas != null;
    }
}
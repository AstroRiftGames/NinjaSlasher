using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuAspectRatioManager : MonoBehaviour
{
    private const float TargetAspect = 16f / 9f;
    private const float MonitorIntervalSeconds = 0.25f;

    [SerializeField] private Camera menuCamera;

    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;
    private int _lastCanvasSignature;
    private Coroutine _monitorRoutine;

    private void Start()
    {
        RefreshLayout(force: true);
    }

    private void OnEnable()
    {
        RefreshLayout(force: true);
        StartMonitor();
    }

    private void OnDisable()
    {
        StopMonitor();
    }

    private void StartMonitor()
    {
        if (_monitorRoutine != null)
        {
            return;
        }

        _monitorRoutine = StartCoroutine(MonitorLayoutChanges());
    }

    private void StopMonitor()
    {
        if (_monitorRoutine == null)
        {
            return;
        }

        StopCoroutine(_monitorRoutine);
        _monitorRoutine = null;
    }

    private IEnumerator MonitorLayoutChanges()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(MonitorIntervalSeconds);

        while (enabled)
        {
            yield return wait;
            RefreshLayout();
        }
    }

    private void RefreshLayout(bool force = false)
    {
        EnsureCamera();

        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        int currentCanvasSignature = GetCanvasSignature();

        bool screenChanged = force || currentWidth != _lastScreenWidth || currentHeight != _lastScreenHeight;
        bool canvasesChanged = force || currentCanvasSignature != _lastCanvasSignature;

        if (canvasesChanged)
        {
            SetupUICanvases();
            _lastCanvasSignature = currentCanvasSignature;
        }

        if (screenChanged)
        {
            AdjustAspectRatio(currentWidth, currentHeight);
            _lastScreenWidth = currentWidth;
            _lastScreenHeight = currentHeight;
        }
    }

    private void EnsureCamera()
    {
        if (menuCamera == null)
        {
            menuCamera = GetComponent<Camera>();
        }
    }

    private int GetCanvasSignature()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int signature = canvases.Length;

        foreach (Canvas canvas in canvases)
        {
            signature = unchecked((signature * 397) ^ canvas.GetInstanceID());
        }

        return signature;
    }

    private void SetupUICanvases()
    {
        Canvas[] uiCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Canvas canvas in uiCanvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = menuCamera;
            canvas.planeDistance = 10f;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
            }
        }
    }

    private void AdjustAspectRatio(int screenWidth, int screenHeight)
    {
        if (menuCamera == null || screenWidth <= 0 || screenHeight <= 0) return;

        float windowAspect = (float)screenWidth / screenHeight;
        float scaleHeight = windowAspect / TargetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = menuCamera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            menuCamera.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = menuCamera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            menuCamera.rect = rect;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class AspectCamera : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;

    private float targetAspect = 16f / 9f;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }

        SetupCanvases();
        AdjustAspectRatio();
    }

    void Update()
    {
        AdjustAspectRatio();
    }

    void SetupCanvases()
    {
        Canvas[] allCanvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in allCanvases)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

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

    void AdjustAspectRatio()
    {
        if (mainCamera == null) return;

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1.0f)
        {
            Rect rect = mainCamera.rect;

            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;

            mainCamera.rect = rect;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;

            Rect rect = mainCamera.rect;

            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;

            mainCamera.rect = rect;
        }
    }
}
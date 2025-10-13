using UnityEngine;
using UnityEngine.UI;

public class MenuAspectRatioManager : MonoBehaviour
{
    [SerializeField] private Camera menuCamera;

    private float targetAspect = 16f / 9f;
    private Canvas[] uiCanvases;

    void Start()
    {
        if (menuCamera == null)
        {
            menuCamera = GetComponent<Camera>();
        }

        SetupUICanvases();
        AdjustAspectRatio();
    }

    void Update()
    {
        AdjustAspectRatio();
    }

    void SetupUICanvases()
    {
        uiCanvases = FindObjectsOfType<Canvas>(true);

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

    void AdjustAspectRatio()
    {
        if (menuCamera == null) return;

        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;

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
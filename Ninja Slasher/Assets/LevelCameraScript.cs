using UnityEngine;

public class LandscapeCameraScaler : MonoBehaviour
{
    [SerializeField] float targetHorizontalSize = 21f;

    void Start()
    {
        Camera cam = Camera.main;
        float screenAspect = (float)Screen.width / Screen.height;
        float orthographicSize = targetHorizontalSize / screenAspect / 2f;
        cam.orthographicSize = orthographicSize;
    }
}

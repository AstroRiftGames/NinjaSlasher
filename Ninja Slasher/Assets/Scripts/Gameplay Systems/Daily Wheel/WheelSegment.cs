using UnityEngine;
using UnityEngine.UI;

public class WheelSegment : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;

    public void Setup(Sprite icon, Color backgroundColor)
    {
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }
    }
}
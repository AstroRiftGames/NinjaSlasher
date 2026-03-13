using UnityEngine;
using UnityEngine.UI;

public class PowerUpUIController : MonoBehaviour
{
    [SerializeField] private Image activePowerUpIcon;

    public void ShowPowerUp(Sprite icon)
    {
        if (activePowerUpIcon == null) return;

        activePowerUpIcon.enabled = true;
        activePowerUpIcon.sprite = icon;
    }

    public void HidePowerUp()
    {
        if (activePowerUpIcon == null) return;

        activePowerUpIcon.enabled = false;
    }
}

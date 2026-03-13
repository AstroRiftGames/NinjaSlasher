using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpIconView : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text usesText;

    public void SetIcon(Sprite sprite)
    {
        icon.sprite = sprite;
    }

    public void SetUses(int uses)
    {
        if (usesText == null) return;

        usesText.text = uses.ToString();
    }
}

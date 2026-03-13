using System.Collections.Generic;
using UnityEngine;

public class PowerUpUIController : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private PowerUpIconView iconPrefab;

    private Dictionary<PowerUpType, PowerUpIconView> activeIcons;
    private Queue<PowerUpIconView> pool;

    void Awake()
    {
        activeIcons = new Dictionary<PowerUpType, PowerUpIconView>();
        pool = new Queue<PowerUpIconView>();
    }

    public void ShowPowerUp(PowerUpType type, Sprite icon, int initialUses)
    {
        if (activeIcons.ContainsKey(type)) return;

        PowerUpIconView view = pool.Count > 0 ? pool.Dequeue() : Instantiate(iconPrefab, container);
        view.SetIcon(icon);
        view.SetUses(initialUses);
        view.gameObject.SetActive(true);
        activeIcons[type] = view;
    }

    public void HidePowerUp(PowerUpType type)
    {
        if (!activeIcons.TryGetValue(type, out PowerUpIconView view)) return;

        view.gameObject.SetActive(false);
        activeIcons.Remove(type);
        pool.Enqueue(view);
    }

    public void UpdateUses(PowerUpType type, int usesRemaining)
    {
        if (!activeIcons.TryGetValue(type, out PowerUpIconView view)) return;

        view.SetUses(usesRemaining);
    }
}

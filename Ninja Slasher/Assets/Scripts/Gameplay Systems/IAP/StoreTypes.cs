using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PowerUpRewardEntry
{
    public PowerUpType type;
    [Min(1)] public int quantity;
}

[Serializable]
public class BundleRewardData
{
    [Header("Lives")]
    [Tooltip("Si true, activa vidas ilimitadas por unlimitedLivesDurationMinutes. Si false, suma vidas regulares.")]
    public bool unlimitedLives;

    [Tooltip("Duracion en minutos de las vidas ilimitadas (solo si unlimitedLives = true).")]
    [Min(0.5f)] public float unlimitedLivesDurationMinutes = 5f;

    [Tooltip("Vidas regulares a añadir (solo si unlimitedLives = false).")]
    [Min(0)] public int regularLivesCount;

    [Header("Power-ups")]
    public List<PowerUpRewardEntry> powerUps = new();

    [Header("Currency")]
    [Min(0)] public int coins;
}

[Serializable]
public class BundleDisplayData
{
    [Tooltip("Nombre visible del bundle en overlays")]
    public string bundleName;

    [Tooltip("Icono del bundle.")]
    public Sprite icon;
}

public enum BundleTier { Small, Medium, Large }

public enum RewardType { Bundle, Coins, RemoveAds }

public enum PurchaseState { Idle, Processing, Completed, Failed }

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Purchasing;

[CreateAssetMenu(fileName = "NewProduct", menuName = "Store/Product Definition")]
public class StoreProductDefinition : ScriptableObject
{
    [Tooltip("Todos los IAP product IDs que mapean a este producto. Índice 0 = SKU primario.")]
    public string[] productIds = new string[0];

    [Tooltip("Tipo de producto IAP registrado en Google Play / App Store.")]
    public ProductType productType = ProductType.Consumable;

    public ProductCategory category;
    public RewardType      rewardType;

    [Header("Display (para overlays de emergencia)")]
    public BundleDisplayData display;

    [Header("Confirmation Popup")]
    [TextArea(2, 6)]
    public string confirmationDescription;

    [Header("Bundle Reward — rewardType = Bundle")]
    public BundleRewardData bundleReward;

    [Header("Coin Reward — rewardType = Coins")]
    [Min(0)] public int coinAmount;

    public string PrimaryProductId
        => productIds != null && productIds.Length > 0 ? productIds[0] : string.Empty;

    public string ConfirmationTitle
    {
        get
        {
            if (display != null && !string.IsNullOrWhiteSpace(display.bundleName))
                return display.bundleName;

            return Humanize(PrimaryProductId);
        }
    }

    public Sprite ConfirmationIcon => display != null ? display.icon : null;

    public string ConfirmationDescription
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(confirmationDescription))
                return confirmationDescription;

            List<string> lines = new List<string>();

            switch (rewardType)
            {
                case RewardType.Coins:
                    if (coinAmount > 0)
                        lines.Add($"Incluye {coinAmount} monedas.");
                    break;

                case RewardType.Bundle:
                    if (bundleReward != null)
                    {
                        if (bundleReward.coins > 0)
                            lines.Add($"Incluye {bundleReward.coins} monedas.");

                        if (bundleReward.unlimitedLives && bundleReward.unlimitedLivesDurationMinutes > 0f)
                            lines.Add($"Incluye vidas ilimitadas por {FormatDurationMinutes(bundleReward.unlimitedLivesDurationMinutes)}.");
                        else if (bundleReward.regularLivesCount > 0)
                            lines.Add($"Incluye {bundleReward.regularLivesCount} {(bundleReward.regularLivesCount == 1 ? "vida" : "vidas")}.");

                        if (bundleReward.powerUps != null)
                        {
                            foreach (PowerUpRewardEntry entry in bundleReward.powerUps)
                            {
                                if (entry.quantity <= 0)
                                    continue;

                                lines.Add($"Incluye {entry.quantity} {GetPowerUpDisplayName(entry.type)}.");
                            }
                        }
                    }
                    break;

                case RewardType.RemoveAds:
                    lines.Add("Elimina los anuncios intersticiales de forma permanente.");
                    lines.Add("Los anuncios recompensados siguen disponibles.");
                    break;
            }

            if (lines.Count == 0)
                return "Confirma la compra de este producto de la tienda.";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0)
                    builder.AppendLine();

                builder.Append("- ");
                builder.Append(lines[i]);
            }

            return builder.ToString();
        }
    }

    public bool MatchesProductId(string id)
        => productIds != null && Array.Exists(productIds, pid => pid == id);

    private static string FormatDurationMinutes(float minutes)
    {
        int roundedMinutes = Mathf.RoundToInt(minutes);
        return roundedMinutes == 1 ? "1 minuto" : $"{roundedMinutes} minutos";
    }

    private static string GetPowerUpDisplayName(PowerUpType type)
    {
        switch (type)
        {
            case PowerUpType.ExtraTime:
                return "Tiempo Extra";
            case PowerUpType.DashTurbo:
                return "Turbo de Dash";
            case PowerUpType.ParryPerfect:
                return "Parry Perfecto";
            case PowerUpType.ComboMaster:
                return "Maestro del Combo";
            case PowerUpType.SecondChance:
                return "Segunda Oportunidad";
            case PowerUpType.HawkVision:
                return "Ojo de Halcon";
            case PowerUpType.EnhancedParry:
                return "Parry Potenciado";
            default:
                return Humanize(type.ToString());
        }
    }

    private static string Humanize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string value = raw.Replace("_", " ").Trim();
        StringBuilder builder = new StringBuilder(value.Length + 8);

        for (int i = 0; i < value.Length; i++)
        {
            char current = value[i];
            if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[i - 1]))
                builder.Append(' ');

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }
}

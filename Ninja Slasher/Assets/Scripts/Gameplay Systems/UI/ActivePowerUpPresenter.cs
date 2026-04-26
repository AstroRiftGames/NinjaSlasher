using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivePowerUpPresenter : MonoBehaviour
{
    [Header("UI REFERENCES")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text usesText;

    private void Awake()
    {
        if (root == null)
            root = gameObject;
    }

    private void OnEnable()
    {
        GameEvents.OnPowerUpActivated += OnPowerUpActivated;
        GameEvents.OnPowerUpUsesUpdated += OnPowerUpUsesUpdated;
        GameEvents.OnPowerUpExpired += OnPowerUpExpired;

        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnPowerUpActivated -= OnPowerUpActivated;
        GameEvents.OnPowerUpUsesUpdated -= OnPowerUpUsesUpdated;
        GameEvents.OnPowerUpExpired -= OnPowerUpExpired;
    }

    public void Refresh()
    {
        EnsureReferences();

        GameObject targetRoot = root != null ? root : gameObject;

        if (PowerUpManager.Instance == null ||
            !PowerUpManager.Instance.TryGetAnyActivePowerUp(out PowerUpBase powerUp, out int remainingUses))
        {
            targetRoot.SetActive(false);
            return;
        }

        targetRoot.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = powerUp.icon;
            iconImage.enabled = powerUp.icon != null;
        }

        if (usesText != null)
        {
            usesText.text = remainingUses.ToString();
        }
    }

    private void EnsureReferences()
    {
        if (root == null)
            root = gameObject;
    }

    private void OnPowerUpActivated(PowerUpType _, int __)
    {
        Refresh();
    }

    private void OnPowerUpUsesUpdated(PowerUpType _, int __)
    {
        Refresh();
    }

    private void OnPowerUpExpired(PowerUpType _)
    {
        Refresh();
    }
}

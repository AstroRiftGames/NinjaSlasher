using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreGameUIManager : MonoBehaviour
{
    [Header("PREGAME")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _playButton;
    [SerializeField] private string _pendingSceneName;
    [SerializeField] private Transform _powerUpsContainer;
    [SerializeField] private PowerUpSlotUI _powerUpSlotPrefab;
    [SerializeField] private PowerUpBase[] allPowerUpBases;

    private List<PowerUpSlotUI> _slots = new List<PowerUpSlotUI>();

    public void ShowConfirmationPanel(string sceneName)
    {
        _pendingSceneName = sceneName;
        UIManager.Instance.ShowHidePreGameCanvas();

        _playButton.onClick.RemoveAllListeners();
        _playButton.onClick.AddListener(OnConfirmLevelSelection);

        _closeButton.onClick.RemoveAllListeners();
        _closeButton.onClick.AddListener(CancelLevelSelection);
    }

    private void OnConfirmLevelSelection()
    {
        UIManager.Instance.ShowHidePreGameCanvas();
        UIManager.Instance.LoadLevelScene(_pendingSceneName);
    }

    private void CancelLevelSelection()
    {
        UIManager.Instance.ShowHidePreGameCanvas();
        _pendingSceneName = null;
    }

    public void ShowPreGamePowerUps()
    {
        foreach (var slot in _slots)
            Destroy(slot.gameObject);
        _slots.Clear();

        var inventory = SaveManager.Instance.GetGameData().powerUpInventory;

        foreach (var powerUpBase in allPowerUpBases)
        {
            var item = inventory.Find(i => i.type == powerUpBase.powerUpType);
            if (item == null)
                item = new PowerUpInventoryItem(powerUpBase.powerUpType, 0);

            var slot = Instantiate(_powerUpSlotPrefab, _powerUpsContainer);
            slot.Setup(item, powerUpBase, OnPowerUpActivateClicked);
            _slots.Add(slot);
        }
    }

    private void OnPowerUpActivateClicked(PowerUpInventoryItem item)
    {
        if (item.quantity > 0)
        {
            item.quantity--;
            var powerUpBase = GetPowerUpBaseByType(item.type);

            if (powerUpBase != null)
                PowerUpManager.Instance.ActivatePowerUp(powerUpBase);

            var powerUpData = new PowerUpData
            {
                type = item.type,
                activationTime = DateTime.Now,
                duration = powerUpBase != null ? powerUpBase.duration : 3600f
            };
            SaveManager.Instance.GetGameData().activePowerUps.Add(powerUpData);
            SaveManager.Instance.SaveData();
            ShowPreGamePowerUps();
        }
    }

    private PowerUpBase GetPowerUpBaseByType(PowerUpType type)
    {
        foreach (var pu in allPowerUpBases)
            if (pu.powerUpType == type)
                return pu;
        return null;
    }
}

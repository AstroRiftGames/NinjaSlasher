using UnityEngine;

public class LevelSelectorUIRefresher : MonoBehaviour
{
    [SerializeField] private ButtonManager _buttonManager;

    void Awake()
    {
        _buttonManager ??= GetComponentInParent<ButtonManager>();
    }

    void OnEnable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnGameDataChanged += OnGameDataChanged;
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.CloudDataApplied += OnCloudDataApplied;
    }

    void OnDisable()
    {
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnGameDataChanged -= OnGameDataChanged;
        if (CloudSaveManager.Instance != null)
            CloudSaveManager.Instance.CloudDataApplied -= OnCloudDataApplied;
    }

    private void OnGameDataChanged(GameData _)
    {
        _buttonManager?.SetupButtons();
    }

    private void OnCloudDataApplied(GameData _)
    {
        _buttonManager?.SetupButtons();
    }
}

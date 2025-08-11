using TMPro;
using UnityEngine;

public class DebugUIManager : MonoBehaviour
{
    [Header("DEBUG")]
    [SerializeField] private TextMeshProUGUI debugStarsText;
    [SerializeField] private TextMeshProUGUI _mainCurrentStarsText;
    [SerializeField] private TextMeshProUGUI _profileCurrentStarsText;

    public void ShowStarsDebug()
    {
        if (debugStarsText == null) return;

        var data = SaveManager.Instance.GetGameData();
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total estrellas: {data.totalStars}");

        foreach (var kvp in data.levelStars)
        {
            sb.AppendLine($"Nivel {kvp.Key}: {kvp.Value} estrellas");
        }
        _mainCurrentStarsText.text = $"{data.totalStars}";
        _profileCurrentStarsText.text = $"{data.totalStars}";
        debugStarsText.text = sb.ToString();
    }

#if UNITY_EDITOR
    public void DeleteSaveDataFromUI()
    {
        SaveManager.Instance.DeleteSaveData();
        ShowStarsDebug();
        Debug.Log("[UIManager] Progreso borrado.");
    }
#endif
}
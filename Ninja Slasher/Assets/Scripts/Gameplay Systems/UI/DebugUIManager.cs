using TMPro;
using UnityEngine;

public class DebugUIManager : MonoBehaviour
{
    [Header("DEBUG")]
    [SerializeField] private TextMeshProUGUI debugStarsText;

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
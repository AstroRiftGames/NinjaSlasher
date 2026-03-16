#if UNITY_EDITOR
using UnityEngine;

public class ProgressionDebugTool : MonoBehaviour
{
    [Header("Simulación de Nivel")]
    [SerializeField] private int _levelIdToSimulate = 1;
    [SerializeField, Range(1, 3)] private int _starsToSimulate = 3;

    [ContextMenu("Debug/Simular Victoria de Nivel")]
    public void DebugSimulateLevelWin()
    {
        if (!ValidateDependencies()) return;

        Debug.Log($"[ProgressionDebugTool] Simulando victoria — Nivel {_levelIdToSimulate} | {_starsToSimulate} estrella(s).");

        LevelProgressionManager.Instance.HandleLevelCompletion(_levelIdToSimulate, _starsToSimulate);

        var (highestLevel, highestArea, totalStars) = SaveManager.Instance.GetProgressionData();
        Debug.Log($"[ProgressionDebugTool] Resultado — Nivel más alto: {highestLevel} | Área más alta: {highestArea} | Estrellas totales: {totalStars}");
    }

    [ContextMenu("Debug/Resetear Save Data")]
    public void DebugResetSaveData()
    {
        if (!ValidateDependencies()) return;

        SaveManager.Instance.ResetAllLocalSaves(notify: true);
        Debug.Log("[ProgressionDebugTool] Save data reseteado completamente.");
    }

    private bool ValidateDependencies()
    {
        if (LevelProgressionManager.Instance == null)
        {
            Debug.LogError("[ProgressionDebugTool] LevelProgressionManager no está disponible en escena.");
            return false;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogError("[ProgressionDebugTool] SaveManager no está disponible en escena.");
            return false;
        }

        return true;
    }
}
#endif

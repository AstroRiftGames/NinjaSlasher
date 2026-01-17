using UnityEngine;
using System;

[CreateAssetMenu(fileName = "ComboFeedbackConfig", menuName = "Game/Combo Feedback Config")]
public class ComboFeedbackConfig : ScriptableObject
{
    [Header("Pool Settings")]
    [Tooltip("Tamaño del pool de textos")]
    public int poolSize = 10;

    [Header("Combo Levels")]
    [Tooltip("Configuración visual para cada nivel de combo (índice 0 = combo x2)")]
    public ComboLevelData[] comboLevels = new ComboLevelData[]
    {
        new ComboLevelData
        {
            minComboLevel = 2,
            message = "COMBO x2!",
            color = new Color(1f, 1f, 0f)
        },
        new ComboLevelData
        {
            minComboLevel = 3,
            message = "COMBO x3!!",
            color = new Color(1f, 0.5f, 0f)
        },
        new ComboLevelData
        {
            minComboLevel = 4,
            message = "COMBO x4!!!",
            color = new Color(1f, 0.3f, 0f)
        },
        new ComboLevelData
        {
            minComboLevel = 5,
            message = "COMBO x5!!!!",
            color = new Color(1f, 0f, 0f)
        }
    };

    public ComboLevelData GetComboData(int comboLevel)
    {
        if (comboLevel < 2) return null;

        for (int i = comboLevels.Length - 1; i >= 0; i--)
        {
            if (comboLevel >= comboLevels[i].minComboLevel)
            {
                return comboLevels[i];
            }
        }

        return null;
    }

    public bool ShouldShowFeedback(int comboLevel)
    {
        return comboLevel >= 2;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (comboLevels != null)
        {
            for (int i = 0; i < comboLevels.Length; i++)
            {
                if (comboLevels[i].minComboLevel < 2)
                {
                    comboLevels[i].minComboLevel = 2;
                }
            }
        }
    }
#endif
}

[Serializable]
public class ComboLevelData
{
    [Tooltip("Nivel mínimo de combo requerido")]
    [Min(2)]
    public int minComboLevel = 2;

    [Tooltip("Mensaje a mostrar")]
    public string message = "COMBO!";

    [Tooltip("Color del texto")]
    public Color color = Color.white;
}
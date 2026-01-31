using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioSettingsSO))]
public class AudioSettingsSOEditor : Editor
{
    private AudioSettingsSO settings;

    private void OnEnable()
    {
        settings = (AudioSettingsSO)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(settings);

            if (Application.isPlaying)
            {
                ApplyRuntimePreview();
            }
        }

        EditorGUILayout.Space();
        DrawUtilityButtons();
    }

    private void DrawUtilityButtons()
    {
        EditorGUILayout.BeginVertical("box");

        if (GUILayout.Button("Save To PlayerPrefs"))
        {
            settings.Save();
            Debug.Log("AudioSettings saved.");
        }

        if (GUILayout.Button("Reset To Defaults"))
        {
            Undo.RecordObject(settings, "Reset Audio Settings");
            settings.ResetToDefaults();

            if (Application.isPlaying)
            {
                ApplyRuntimePreview();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void ApplyRuntimePreview()
    {
        if (AudioService.Instance == null)
            return;

        AudioService.Instance.RefreshVolumes();
    }
}
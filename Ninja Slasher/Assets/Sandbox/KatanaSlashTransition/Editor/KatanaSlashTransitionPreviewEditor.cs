#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KatanaSlashTransitionPreview))]
public sealed class KatanaSlashTransitionPreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sandbox Controls", EditorStyles.boldLabel);

        KatanaSlashTransitionPreview preview = (KatanaSlashTransitionPreview)target;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Closed"))
            {
                preview.PreviewClosed();
            }

            if (GUILayout.Button("Mid Slash"))
            {
                preview.PreviewMidSlash();
            }

            if (GUILayout.Button("Open"))
            {
                preview.PreviewOpen();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = EditorApplication.isPlaying;

            if (GUILayout.Button("Play Sequence"))
            {
                preview.PlayPreview();
            }

            if (GUILayout.Button("Stop"))
            {
                preview.StopPreview();
            }

            GUI.enabled = true;
        }

        if (GUILayout.Button("Rebuild Sandbox"))
        {
            preview.RebuildSandbox();
        }

        EditorGUILayout.HelpBox(
            "El prefab es aislado: no usa SceneManager, no toca el Animator productivo y solo sirve para iteracion visual.",
            MessageType.Info);
    }
}
#endif

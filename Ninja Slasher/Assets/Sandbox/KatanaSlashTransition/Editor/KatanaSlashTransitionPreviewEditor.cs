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
            if (GUILayout.Button("Source"))
            {
                preview.PreviewSource();
            }

            if (GUILayout.Button("Covered"))
            {
                preview.PreviewCovered();
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

            if (GUILayout.Button("Play Full"))
            {
                preview.PlayFullSequence();
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
            "Play Sequence corta un fondo negro ya cubierto. Play Full reproduce source -> fade to black -> hold -> cut.",
            MessageType.Info);
    }
}
#endif

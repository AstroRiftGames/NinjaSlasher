using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AudioEventBalancerWindow : EditorWindow
{
    private List<AudioEvent> audioEvents;
    private Vector2 scroll;
    private string searchQuery = string.Empty;
    private AudioChannel? channelFilter = null;

    private AudioSource previewSource;

    [MenuItem("Tools/Audio/Audio Event Balancer")]
    public static void Open()
    {
        GetWindow<AudioEventBalancerWindow>("Audio Event Balancer");
    }

    private void OnEnable()
    {
        LoadAudioEvents();
        CreatePreviewSource();
    }

    private void OnDisable()
    {
        if (previewSource != null)
        {
            DestroyImmediate(previewSource.gameObject);
        }
    }

    private void LoadAudioEvents()
    {
        audioEvents = AssetDatabase
            .FindAssets("t:AudioEvent")
            .Select(guid =>
                AssetDatabase.LoadAssetAtPath<AudioEvent>(
                    AssetDatabase.GUIDToAssetPath(guid)))
            .Where(e => e != null)
            .OrderBy(e => e.name)
            .ToList();
    }

    private void CreatePreviewSource()
    {
        GameObject go = new GameObject("AudioEventPreviewSource");
        go.hideFlags = HideFlags.HideAndDontSave;

        previewSource = go.AddComponent<AudioSource>();
        previewSource.playOnAwake = false;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Audio Event Balancer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Refresh Audio Events"))
        {
            LoadAudioEvents();
        }

        EditorGUILayout.Space();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var audioEvent in GetFilteredEvents())
        {
            DrawAudioEvent(audioEvent);
        }

        EditorGUILayout.EndScrollView();

        DrawFilters();
        EditorGUILayout.Space();
    }

    private void DrawFilters()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Search", EditorStyles.boldLabel);
        searchQuery = EditorGUILayout.TextField(searchQuery);

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Channel", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Toggle(channelFilter == null, "All", EditorStyles.miniButtonLeft))
            channelFilter = null;

        foreach (AudioChannel channel in Enum.GetValues(typeof(AudioChannel)))
        {
            bool selected = channelFilter == channel;

            if (GUILayout.Toggle(selected, channel.ToString(), EditorStyles.miniButtonMid))
            {
                channelFilter = channel;
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawAudioEvent(AudioEvent audioEvent)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(audioEvent.name, EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        audioEvent.volume = EditorGUILayout.Slider("Volume", audioEvent.volume, 0f, 1f);
        audioEvent.loop = EditorGUILayout.Toggle("Loop", audioEvent.loop);

        audioEvent.randomPitch = EditorGUILayout.Toggle("Random Pitch", audioEvent.randomPitch);

        if (audioEvent.randomPitch)
        {
            audioEvent.minPitch = EditorGUILayout.Slider("Min Pitch", audioEvent.minPitch, 0.5f, 1.5f);
            audioEvent.maxPitch = EditorGUILayout.Slider("Max Pitch", audioEvent.maxPitch, 0.5f, 1.5f);
        }

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(audioEvent);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("▶ Play"))
        {
            PlayPreview(audioEvent);
        }

        if (GUILayout.Button("■ Stop"))
        {
            previewSource.Stop();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void PlayPreview(AudioEvent audioEvent)
    {
        if (audioEvent == null || audioEvent.clip == null)
            return;

        previewSource.Stop();

        previewSource.clip = audioEvent.clip;
        previewSource.volume = audioEvent.volume;
        previewSource.loop = audioEvent.loop;
        previewSource.pitch = audioEvent.GetPitch();
        previewSource.spatialBlend = 0f;

        previewSource.Play();
    }

    private IEnumerable<AudioEvent> GetFilteredEvents()
    {
        IEnumerable<AudioEvent> result = audioEvents;

        if (!string.IsNullOrEmpty(searchQuery))
        {
            string lower = searchQuery.ToLowerInvariant();
            result = result.Where(e => e.name.ToLowerInvariant().Contains(lower));
        }

        if (channelFilter.HasValue)
        {
            result = result.Where(e => e.channel == channelFilter.Value);
        }

        return result;
    }
}
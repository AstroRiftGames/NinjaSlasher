using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Event", menuName = "Game/Audio/Audio Event")]
public class AudioEvent : ScriptableObject
{
    [Header("Audio Clip")]
    public AudioClip clip;

    [Header("Playback")]
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop;

    [Header("Pitch")]
    public bool randomPitch;
    [Range(0.5f, 1.5f)] public float minPitch = 0.95f;
    [Range(0.5f, 1.5f)] public float maxPitch = 1.05f;

    [Header("Spatial")]
    public bool spatial;
    [Range(0f, 1f)] public float spatialBlend = 1f;
    [Range(1f, 100f)] public float maxDistance = 20f;

    [Header("Channel")]
    public AudioChannel channel = AudioChannel.SFX;

    public AudioClip GetClip() => clip;

    public float GetPitch()
    {
        if (randomPitch)
        {
            return Random.Range(minPitch, maxPitch);
        }
        return 1f;
    }

    private void OnValidate()
    {
        if (clip == null)
        {
            Debug.LogWarning($"AudioEvent '{name}' no tiene clip asignado.", this);
        }

        if (randomPitch && minPitch > maxPitch)
        {
            Debug.LogWarning($"AudioEvent '{name}': minPitch > maxPitch", this);
        }
    }
}
using UnityEngine;

public class ElementAudioContext : MonoBehaviour
{
    private ElementAudioSet _audioSet;

    public ElementAudioSet Audio => _audioSet;

    public void Initialize(AudioSet audioSet)
    {
        _audioSet = audioSet as ElementAudioSet;
    }
}

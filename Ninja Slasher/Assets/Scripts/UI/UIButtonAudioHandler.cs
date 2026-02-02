using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonAudioHandler : MonoBehaviour
{
    private Button _button;
    private UIAudioContext _audioContext;

    private void Awake()
    {
        _button = GetComponent<Button>();

        _audioContext = GetComponentInParent<UIAudioContext>();

        if (_button != null)
        {
            _button.onClick.AddListener(PlayClickSound);
        }
    }

    private void OnDestroy()
    {
        if (_button != null)
        {
            _button.onClick.RemoveListener(PlayClickSound);
        }
    }

    private void PlayClickSound()
    {
        if (_button != null && !_button.interactable)
            return;

        if (AudioService.Instance != null)
        {
            AudioService.Instance.PlaySFX(_audioContext.Audio.select);
        }
    }
}
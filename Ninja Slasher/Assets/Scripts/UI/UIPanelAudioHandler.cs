using UnityEngine;

public class UIPanelAudioHandler : MonoBehaviour
{
    [Header("Audio Configuration")]
    [SerializeField] private UIAudioSet _audioSet;

    [SerializeField] private bool _checkVisibility = true;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayOpenEvent()
    {
        if (!CanPlay()) return;

        if (_audioSet != null)
        {
            AudioService.Instance?.PlaySFX(_audioSet.panelOpen);
        }
    }

    //public void PlayCloseEvent()
    //{
    //    if (!CanPlay()) return;

    //    if (_audioSet != null)
    //    {
    //        AudioService.Instance?.PlaySFX(_audioSet.panelClose);
    //    }
    //}


    private bool CanPlay()
    {
        if (_checkVisibility && _canvasGroup != null)
        {
            if (_canvasGroup.alpha <= 0.01f && gameObject.activeInHierarchy == false) return false;
        }
        return true;
    }

    public void SetAudioContext(UIAudioSet set)
    {
        _audioSet = set;
    }
}

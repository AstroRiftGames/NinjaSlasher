using UnityEngine;
using UnityEngine.UI;

public class CreditsModal : UIModalBase
{
    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    protected override void OnShown()
    {
        MusicEvents.OnEnterCredits?.Invoke();
    }
    
    protected override void OnHidden()
    {
        MusicEvents.OnEnterLevelSelection?.Invoke();
    }

    private void SetupButtons()
    {
        if (_closeButton == null)
        {
            Debug.LogWarning("[CreditsModal] Close button is not assigned.");
            return;
        }

        _closeButton.onClick.AddListener(OnCloseClicked);
    }

    private void OnCloseClicked()
    {
        UIEvents.RequestHideCreditsModal();
    }

    private void OnDestroy()
    {
        _closeButton?.onClick.RemoveListener(OnCloseClicked);
    }
}

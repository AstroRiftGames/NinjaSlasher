using UnityEngine;
using UnityEngine.UI;

public class ProfileModal : UIModalBase
{
    private const string PrivacyPolicyUrl = "https://sites.google.com/view/ninja-slasher-privacy-policy/inicio";
    private const string DiscordUrl = "https://discord.gg/KuG7vsZg";
    private const string LinkedInUrl = "https://www.linkedin.com/company/astro-rift-games";
    private const string InstagramUrl = "https://www.instagram.com/astroriftgames";
    private const string SupportUrl = "https://www.astroriftgames.com/";

    [SerializeField] private float _closeAnimationDuration = 0.4f;

    [Header("Background")]
    [SerializeField] private GameObject _backgroundObject;

    [Header("Buttons")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _creditsButton;
    [SerializeField] private Button _privacyPolicyButton;
    [SerializeField] private Button _discordButton;
    [SerializeField] private Button _linkedInButton;
    [SerializeField] private Button _instagramButton;
    [SerializeField] private Button _supportButton;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        if (_backgroundObject == null && _backgroundImage != null)
            _backgroundObject = _backgroundImage.gameObject;

        SetupButtons();
    }

    public override void Show()
    {
        if (_hasBackground && _backgroundObject != null)
            _backgroundObject.SetActive(true);

        base.Show();
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_hasBackground && _backgroundObject != null)
            _backgroundObject.SetActive(false);
    }

    private void SetupButtons()
    {
        BindButton(_closeButton, OnCloseClicked, "close");
        BindButton(_creditsButton, OnCreditsClicked, "credits");
        BindButton(_privacyPolicyButton, OnPrivacyPolicyClicked, "privacy policy");
        BindButton(_discordButton, OnDiscordClicked, "discord");
        BindButton(_linkedInButton, OnLinkedInClicked, "linkedin");
        BindButton(_instagramButton, OnInstagramClicked, "instagram");
        BindButton(_supportButton, OnSupportClicked, "support");
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction callback, string buttonName)
    {
        if (button == null)
        {
            Debug.LogWarning($"[ProfileModal] {buttonName} button is not assigned.");
            return;
        }

        button.onClick.AddListener(callback);
    }

    private void OnCloseClicked()
    {
        UIEvents.RequestHideProfileModal();
    }

    private void OnCreditsClicked()
    {
        UIEvents.RequestShowCreditsModal();
    }

    private void OnPrivacyPolicyClicked()
    {
        Application.OpenURL(PrivacyPolicyUrl);
    }

    private void OnDiscordClicked()
    {
        Application.OpenURL(DiscordUrl);
    }

    private void OnLinkedInClicked()
    {
        Application.OpenURL(LinkedInUrl);
    }

    private void OnInstagramClicked()
    {
        Application.OpenURL(InstagramUrl);
    }

    private void OnSupportClicked()
    {
        Application.OpenURL(SupportUrl);
    }

    private void OnDestroy()
    {
        _closeButton?.onClick.RemoveListener(OnCloseClicked);
        _creditsButton?.onClick.RemoveListener(OnCreditsClicked);
        _privacyPolicyButton?.onClick.RemoveListener(OnPrivacyPolicyClicked);
        _discordButton?.onClick.RemoveListener(OnDiscordClicked);
        _linkedInButton?.onClick.RemoveListener(OnLinkedInClicked);
        _instagramButton?.onClick.RemoveListener(OnInstagramClicked);
        _supportButton?.onClick.RemoveListener(OnSupportClicked);
    }
}

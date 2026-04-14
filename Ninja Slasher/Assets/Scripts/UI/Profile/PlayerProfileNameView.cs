using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProfileNameView : MonoBehaviour
{
    [SerializeField] private TMP_Text _targetText;
    [SerializeField] private string _guestFallbackName = "Guest";

    private void Awake()
    {
        if (_targetText == null)
        {
            _targetText = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        LoginManager.OnPlayerProfileChanged += HandlePlayerProfileChanged;
        Refresh();
    }

    private void OnDisable()
    {
        LoginManager.OnPlayerProfileChanged -= HandlePlayerProfileChanged;
    }

    private void HandlePlayerProfileChanged(PlayerProfileData profile)
    {
        Apply(profile);
    }

    private void Refresh()
    {
        Apply(LoginManager.Instance != null ? LoginManager.Instance.CurrentPlayerProfile : PlayerProfileData.Guest);
    }

    private void Apply(PlayerProfileData profile)
    {
        if (_targetText == null)
        {
            return;
        }

        string displayName = profile != null && !string.IsNullOrWhiteSpace(profile.DisplayName)
            ? profile.DisplayName
            : _guestFallbackName;

        if (_targetText.text != displayName)
        {
            _targetText.text = displayName;
        }
    }
}

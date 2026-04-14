using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlayerProfileIconView : MonoBehaviour
{
    [SerializeField] private Image _targetImage;
    [SerializeField] private float _pixelsPerUnit = 100f;
    [SerializeField] private bool _preserveAspect = true;

    private Sprite _placeholderSprite;
    private Sprite _runtimeSprite;
    private Texture2D _boundTexture;

    private void Awake()
    {
        if (_targetImage == null)
        {
            _targetImage = GetComponent<Image>();
        }

        if (_targetImage != null)
        {
            _placeholderSprite = _targetImage.sprite;
            _targetImage.preserveAspect = _preserveAspect;
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

    private void OnDestroy()
    {
        ReleaseRuntimeSprite();
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
        if (_targetImage == null)
        {
            return;
        }

        if (profile == null || !profile.HasAvatar)
        {
            SetPlaceholder();
            return;
        }

        Texture2D avatarTexture = profile.AvatarTexture;
        if (avatarTexture == _boundTexture && _runtimeSprite != null)
        {
            if (_targetImage.sprite != _runtimeSprite)
            {
                _targetImage.sprite = _runtimeSprite;
            }

            _targetImage.preserveAspect = _preserveAspect;
            return;
        }

        RebuildRuntimeSprite(avatarTexture);
    }

    private void RebuildRuntimeSprite(Texture2D avatarTexture)
    {
        if (_targetImage == null || avatarTexture == null)
        {
            SetPlaceholder();
            return;
        }

        ReleaseRuntimeSprite();

        _boundTexture = avatarTexture;
        _runtimeSprite = Sprite.Create(
            avatarTexture,
            new Rect(0f, 0f, avatarTexture.width, avatarTexture.height),
            new Vector2(0.5f, 0.5f),
            _pixelsPerUnit);
        _runtimeSprite.name = $"{avatarTexture.name}_ProfileAvatarSprite";
        _runtimeSprite.hideFlags = HideFlags.DontSave;

        _targetImage.sprite = _runtimeSprite;
        _targetImage.preserveAspect = _preserveAspect;
    }

    private void SetPlaceholder()
    {
        _boundTexture = null;

        if (_targetImage != null)
        {
            _targetImage.sprite = _placeholderSprite;
            _targetImage.preserveAspect = _preserveAspect;
        }
    }

    private void ReleaseRuntimeSprite()
    {
        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        _boundTexture = null;
    }
}

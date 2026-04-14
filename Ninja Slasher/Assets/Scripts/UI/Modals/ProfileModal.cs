using UnityEngine;

public class ProfileModal : UIModalBase
{
    [SerializeField] private float _closeAnimationDuration = 0.4f;

    [Header("Background")]
    [SerializeField] private GameObject _backgroundObject;

    protected override float HideAnimationDuration => _closeAnimationDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_modalAnimator == null)
            _modalAnimator = GetComponentInChildren<Animator>();

        if (_backgroundObject == null && _backgroundImage != null)
            _backgroundObject = _backgroundImage.gameObject;
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
}

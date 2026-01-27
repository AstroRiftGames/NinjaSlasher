using UnityEngine;

public class GameplayHUD : UIPanel
{
    protected override void Awake()
    {
        base.Awake();

        if (_panelTransform == null)
            _panelTransform = GetComponent<RectTransform>();

        _isVisible = gameObject.activeSelf;
    }

    public override void Show()
    {
        if (_isVisible) return;

        gameObject.SetActive(true);
        _isVisible = true;

        OnShown();
    }

    public override void Hide()
    {
        if (!_isVisible) return;

        _isVisible = false;

        OnHidden();

        gameObject.SetActive(false);
    }

    protected override void OnShown()
    {
        Debug.Log("[GameplayHUD] HUD de gameplay mostrado");
    }

    protected override void OnHidden()
    {
        Debug.Log("[GameplayHUD] HUD de gameplay ocultado");
    }
}
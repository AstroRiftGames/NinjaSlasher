using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UserNicknameEditPanel : UIPopupBase
{
    [Header("References")]
    [SerializeField] private TMP_InputField _nicknameInputField;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _closeButton;

    protected override void Awake()
    {
        base.Awake();
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);

        if (_closeButton != null)
            _closeButton.onClick.AddListener(Hide);
    }

    private void OnConfirmClicked()
    {
        if (_nicknameInputField != null && !string.IsNullOrEmpty(_nicknameInputField.text))
        {
            UIEvents.RaiseNicknameChanged(_nicknameInputField.text);
        }

        Hide();
    }

    public void SetNickname(string nickname)
    {
        if (_nicknameInputField != null)
            _nicknameInputField.text = nickname;
    }

    protected override void OnShown()
    {
        Debug.Log("[UserNicknameEditPanel] Panel mostrado");

        if (_nicknameInputField != null)
            _nicknameInputField.ActivateInputField();
    }

    protected override void OnHidden()
    {
        Debug.Log("[UserNicknameEditPanel] Panel ocultado");
    }

    private void OnDestroy()
    {
        if (_confirmButton != null)
            _confirmButton.onClick.RemoveAllListeners();

        if (_closeButton != null)
            _closeButton.onClick.RemoveAllListeners();
    }
}
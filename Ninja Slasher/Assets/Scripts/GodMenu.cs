using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GodMenu : MonoBehaviour
{
    [SerializeField] Animator _menuCanvasANIM;
    [SerializeField] Button _openCloseBTN;
    [SerializeField] Toggle _invincibleTGL;
    private bool _isOpen = false;
    private Controller _player;

    private void Awake()
    {
        _player = FindFirstObjectByType<Controller>().GetComponent<Controller>();
    }

    private void OnEnable()
    {
        _openCloseBTN.onClick.AddListener(OpenClose);
        _invincibleTGL.onValueChanged.AddListener(_player.SetInvincibility);
    }

    private void OnDisable()
    {
        _openCloseBTN.onClick.RemoveListener(OpenClose);
        _invincibleTGL.onValueChanged.RemoveListener(_player.SetInvincibility);
    }

    private void OpenClose()
    {
        _isOpen = !_isOpen;
        _menuCanvasANIM.SetTrigger(_isOpen ? "Open" : "Close");
    }
}

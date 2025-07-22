using UnityEngine;

public class ConfigPanelManager : MonoBehaviour
{
    [SerializeField] private Animator _configPanelAnim;
    [SerializeField] private bool _isOpen = false;

    public void OpenCloseConfigPanel()
    {
        if (_isOpen)
        {
            _configPanelAnim.SetTrigger("Close");
            _isOpen = false;
        }
        else
        {
            _configPanelAnim.SetTrigger("Open");
            _isOpen = true;
        }
    }
}
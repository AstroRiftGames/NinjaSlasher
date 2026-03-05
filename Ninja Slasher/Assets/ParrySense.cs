using System.Collections;
using UnityEngine;

public class ParrySense : MonoBehaviour
{
    [SerializeField] private GameObject _indicator;
    [SerializeField] private CircleCollider2D _col;
    [SerializeField] private NewController _playerController;

    private void OnEnable()
    {
        _playerController.OnParry += (SetIndicatorActive);
        _playerController.OnHit += (SetIndicatorActive);
    }

    private void OnDisable()
    {
        _playerController.OnParry -= (SetIndicatorActive);
        _playerController.OnHit -= (SetIndicatorActive);

    }

    private void Awake()
    {
        _col.radius = _playerController.Model.ParryRange / _playerController.transform.localScale.x;
        _indicator.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SetIndicatorActive(true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        SetIndicatorActive(false);
    }

    private void SetIndicatorActive(bool shouldActivate)
    {
        Debug.Log("Indicator: " +  shouldActivate);
        _indicator.SetActive(shouldActivate);
    }
}

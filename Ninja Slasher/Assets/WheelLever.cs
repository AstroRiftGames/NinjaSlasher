using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class WheelLever : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [Header("ROTATION SETTINGS")]
    [SerializeField] private RectTransform _leverArm;
    [SerializeField] private float _startAngle = 0f;
    [SerializeField] private float _targetAngle = -135f;
    [SerializeField] private float _activationThreshold = 0.8f;
    [SerializeField] private float _snapDuration = 0.5f;

    [Header("INTERACTION")]
    [SerializeField] private Image _handleImage;

    public event Action OnLeverActivated;

    private bool _isInteractable = true;
    private bool _isDragging = false;
    private float _angleOffset;
    private Canvas _parentCanvas;

    private void Awake()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (_leverArm != null)
        {
            _leverArm.localRotation = Quaternion.Euler(0, 0, _startAngle);
        }
    }

    public void SetInteractable(bool state)
    {
        _isInteractable = state;
        if (_handleImage != null)
        {
            _handleImage.color = state ? Color.white : Color.gray;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_isInteractable || _leverArm == null) return;

        _isDragging = true;
        _leverArm.DOKill();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _leverArm.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 direction = localPoint - _leverArm.anchoredPosition;
        float mouseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _angleOffset = _leverArm.localEulerAngles.z - mouseAngle;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isInteractable || !_isDragging || _leverArm == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _leverArm.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 direction = localPoint - _leverArm.anchoredPosition;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        float finalAngle = angle + _angleOffset;

        if (finalAngle > 180) finalAngle -= 360;

        float min = Mathf.Min(_startAngle, _targetAngle);
        float max = Mathf.Max(_startAngle, _targetAngle);

        finalAngle = Mathf.Clamp(finalAngle, min, max);

        _leverArm.localRotation = Quaternion.Euler(0, 0, finalAngle);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isInteractable || !_isDragging || _leverArm == null) return;

        _isDragging = false;
        CheckActivation();
    }

    private void CheckActivation()
    {
        float currentZ = _leverArm.localEulerAngles.z;
        if (currentZ > 180) currentZ -= 360;

        float progress = Mathf.InverseLerp(_startAngle, _targetAngle, currentZ);

        if (progress >= _activationThreshold)
        {
            OnLeverActivated?.Invoke();
            ResetLever();
        }
        else
        {
            ResetLever();
        }
    }

    private void ResetLever()
    {
        _leverArm.DOLocalRotate(new Vector3(0, 0, _startAngle), _snapDuration)
                 .SetEase(Ease.OutElastic);
    }
}
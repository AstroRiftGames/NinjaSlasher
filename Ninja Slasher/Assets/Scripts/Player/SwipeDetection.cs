using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeDetection : MonoBehaviour
{
    public delegate void Swipe(Vector2 direction);
    public event Swipe OnSwipe;

    public delegate void Tap(Vector2 position);
    public event Tap OnTap;

    public bool IsPressing => press != null && press.IsPressed();
    [HideInInspector] public Vector2 Direction = Vector2.zero;

    private InputActions _controls;
    private InputAction position;
    private InputAction press;

    private Vector2 currentPos => position.ReadValue<Vector2>();
    [SerializeField] private float swipeResistance = 100f;
    private Vector2 initialPos;

    private float currentTime => Time.time;
    [SerializeField] private float timeThreshold = .2f;
    private float pressTime;

    private void Awake()
    {
        _controls = new InputActions();
    }

    private void OnEnable()
    {
#if UNITY_EDITOR
        position = _controls.PC.Position;
        press = _controls.PC.Press;
        _controls.PC.Enable();
#else
        position = _controls.Mobile.Position;
        press = _controls.Mobile.Press;
        _controls.Mobile.Enable();
#endif

        press.performed += OnPressStarted;
        press.canceled += OnPressCanceled;
    }

    private void OnDisable()
    {
        press.performed -= OnPressStarted;
        press.canceled -= OnPressCanceled;

        _controls.Disable();
    }

    private void OnPressStarted(InputAction.CallbackContext _)
    {
        initialPos = currentPos;
        pressTime = Time.time;
    }

    private void OnPressCanceled(InputAction.CallbackContext _)
    {
        DetectInput();
    }

    private void Update()
    {
        if (IsPressing)
        {
            Direction = CalculateDirection();
        }
        else
        {
            Direction = Vector2.zero;
        }
    }

    private void DetectInput()
    {
        Vector2 direction = CalculateDirection();

        if (direction != Vector2.zero)
        {
            OnSwipe?.Invoke(direction);
        }
        else
        {
            float deltaTime = currentTime - pressTime;
            if (deltaTime <= timeThreshold)
            {
                OnTap?.Invoke(initialPos);
            }
        }
    }

    private Vector2 CalculateDirection()
    {
        Vector2 delta = initialPos - currentPos;
        Vector2 direction = Vector2.zero;
        if (delta.magnitude > swipeResistance)
        {
            return delta.normalized;
        }
        else
        {
            return Vector2.zero;
        }
    }
}
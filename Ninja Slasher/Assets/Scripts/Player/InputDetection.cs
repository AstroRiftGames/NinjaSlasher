using UnityEngine;
using UnityEngine.InputSystem;

public class InputDetection : MonoBehaviour
{
    private NewController _player;
    public delegate void ActionEvent();
    public event ActionEvent OnSwipeCanceled;
    public event ActionEvent OnSwipeResumed;

    public event Swipe OnInputStart;
    public event Swipe OnInputEnd;

    public delegate void Swipe(Vector2 direction);
    public event Swipe OnSwipe;

    public delegate void Tap(Vector2 position);
    public event Tap OnTap;

    private bool _isPressing = false;
    public bool IsPressing => _isPressing;
    [HideInInspector] public Vector2 Direction = Vector2.zero;

    private InputActions _controls;
    private InputAction position;
    private InputAction press;

    private Vector2 currentPos => position.ReadValue<Vector2>();
    public Vector2 CurrentPosition => currentPos;
    [SerializeField] private float swipeResistance = 100f;
    private Vector2 initialPos;

    private float currentTime => Time.time;
    [SerializeField] private float timeThreshold = .2f;
    [SerializeField] private float cancelRadius = 50f;
    private float pressTime;

    private bool isCanceled;

    private bool IsInputBlocked => Time.timeScale == 0f || (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive());

    private void Awake()
    {
        _controls = new InputActions();
        FindFirstObjectByType<InputFeedbackUI>().SetSwipeDetection(this);
        TryGetComponent(out NewController player);
        _player = player;
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
        if (IsInputBlocked || _player.IsDashing || GameManager.Instance.IsVictory || GameManager.Instance.PlayerHasDied) return;
        initialPos = currentPos;
        pressTime = Time.time;

        isCanceled = true;
        _isPressing = true;

        OnInputStart?.Invoke(currentPos);
    }

    private void OnPressCanceled(InputAction.CallbackContext _)
    {
        if (!_isPressing) return;
        
        _isPressing = false;

        if (IsInputBlocked || _player.IsDashing || GameManager.Instance.IsVictory || GameManager.Instance.PlayerHasDied) return;
        
        DetectInput();
    }

    private void Update()
    {
        if (IsInputBlocked) return;

        if (_isPressing)
        {
            Vector2 delta = initialPos - currentPos;
            float distance = delta.magnitude;

            bool nowCanceled = distance <= cancelRadius;

            if (nowCanceled && !isCanceled)
            {
                isCanceled = true;
                OnSwipeCanceled?.Invoke();
            }

            if (!nowCanceled && isCanceled)
            {
                isCanceled = false;
                OnSwipeResumed?.Invoke();
            }

            if (!isCanceled && distance > swipeResistance)
            {
                Direction = delta.normalized;
            }
            else
            {
                Direction = Vector2.zero;
            }
        }
        else
        {
            Direction = Vector2.zero;
        }
    }

    private void DetectInput()
    {
        Vector2 direction = CalculateDirection();

        if (!isCanceled && direction != Vector2.zero)
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
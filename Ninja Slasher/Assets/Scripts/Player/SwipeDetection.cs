using UnityEngine;
using UnityEngine.InputSystem;
    
public class SwipeDetection : MonoBehaviour
{
    public delegate void Swipe(Vector2 direction);
    public event Swipe OnSwipe;

    public delegate void Tap(Vector2 position);
    public event Tap OnTap;

    public bool IsPressing => press.IsInProgress();
    [HideInInspector] public Vector2 Direction = Vector2.zero;

    [SerializeField] private InputAction position, press;

    [SerializeField] private float swipeResistance = 100f;
    private Vector2 initialPos;
    private Vector2 currentPos => position.ReadValue<Vector2>();

    [SerializeField] private float timeThreshold = .2f;
    private float pressTime;
    private float currentTime => Time.time;

    private void Awake()
    {
        position.Enable();
        press.Enable();
        press.performed += _ => { initialPos = currentPos; pressTime = currentTime; };
        press.canceled += _ => DetectInput();
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
            OnSwipe(direction);
        }
        else
        {
            float deltaTime = currentTime - pressTime;
            if (deltaTime <= timeThreshold)
            {
                OnTap(initialPos);
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

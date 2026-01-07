using UnityEngine;
using UnityEngine.InputSystem;
    
public class SwipeDetection : MonoBehaviour
{
    public static SwipeDetection instance;
    public delegate void Swipe(Vector2 direction);
    public event Swipe OnSwipe;

    [SerializeField] private InputAction position, press;

    [SerializeField] private float swipeResistance = 1f;
    private Vector2 initialPos;
    private Vector2 currentPos => position.ReadValue<Vector2>();

    private void Awake()
    {
        position.Enable();
        press.Enable();
        press.performed += _ => { initialPos = currentPos; };
        press.canceled += _ => DetectSwipe();
        instance = this;
    }

    private void DetectSwipe()
    {
        Vector2 delta = initialPos - currentPos;
        Vector2 direction = Vector2.zero;
        if(delta.magnitude > swipeResistance)
        {
            direction = delta.normalized;
        }

        if (direction != Vector2.zero && OnSwipe != null)
        {
            OnSwipe(direction);
        }
    }
}

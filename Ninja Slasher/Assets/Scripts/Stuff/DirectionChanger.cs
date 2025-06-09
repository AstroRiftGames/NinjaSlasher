using UnityEngine;

public class DirectionChanger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            ConveyorPlatform conveyor = GetComponentInParent<ConveyorPlatform>();
            if (conveyor != null)
            {
                conveyor.ToggleDirection();
            }
        }
    }
}

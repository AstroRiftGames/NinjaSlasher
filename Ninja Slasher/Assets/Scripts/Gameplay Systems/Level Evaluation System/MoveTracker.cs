using UnityEngine;

public class MoveTracker : MonoBehaviour, ITracker
{
    public static int TotalMoves { get; private set; }
    public static event System.Action OnMoveRegistered;

    public static void RegisterMove()
    {
        TotalMoves++;
        OnMoveRegistered?.Invoke();
    }

    public void ResetTracker()
    {
        TotalMoves = 0;
    }
}

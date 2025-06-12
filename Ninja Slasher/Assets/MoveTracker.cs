using UnityEngine;

public class MoveTracker : MonoBehaviour
{
    public static int TotalMoves { get; private set; }

    public static void RegisterMove()
    {
        TotalMoves++;
    }

    public static void ResetMoves()
    {
        TotalMoves = 0;
    }
}
